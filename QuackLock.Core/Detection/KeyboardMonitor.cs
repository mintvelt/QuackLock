using System;
using System.Windows.Forms;
using System.Threading;
using System.Runtime.InteropServices;
using QuackLock.Core.Monitoring;
using QuackLock.Core.Events;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace QuackLock.Core.Detection
{
    public class KeyboardMonitor : Form, IMonitor
    {
        private readonly IDetectionEventHandler _handler;
        private ushort? _lastKeyDown;

        // statistics
        private int _activeSamples = 0;
        private int _sumKeyRates = 0;
        private int _maxKeyRate = 0;
        private int _strokeCount = 0;
        private int _consecutiveMediumRateCount = 0;
        private int _consecutiveHighRateCount = 0;

        private System.Threading.Timer? _reportingTimer;
        private System.Threading.Timer? _statsTimer;
        // Settings
        private const int ReportIntervalMs = 1000;
        private const int HighSpeedThreshold = 15;     // Toetsaanslagen per seconde
        private const int HighSpeedDuration = 2; // Aantal seconden die overschrijding moet aanhouden
        private const int MediumSpeedThreshold = 9;     // Toetsaanslagen per seconde
        private const int MediumSpeedDuration = 8; // Aantal seconden die overschrijding moet aanhouden
        private const int StatsIntervalMs = 60 * 1000; //5 * 60 * 1000; // bijv. elke 5 minuten

        public event Action<DetectionEvent>? OnDetectionEvent;

        public KeyboardMonitor(IDetectionEventHandler detectionHandler)
        {
            _handler = detectionHandler;

            // Zet venster onzichtbaar
            this.ShowInTaskbar = false;
            this.WindowState = FormWindowState.Minimized;
            this.Visible = false;

        }
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            // Registratie voor RAW Input
            var rid = new RawInputInterop.RAWINPUTDEVICE[1];
            rid[0].usUsagePage = 0x01; // Generic desktop controls
            rid[0].usUsage = 0x06;     // Keyboard
            rid[0].dwFlags = RawInputInterop.RIDEV_INPUTSINK;
            rid[0].hwndTarget = this.Handle;

            RawInputInterop.RegisterRawInputDevices(rid, (uint)rid.Length, (uint)Marshal.SizeOf(typeof(RawInputInterop.RAWINPUTDEVICE)));
        }

        public void Start()
        {
            _reportingTimer = new System.Threading.Timer(Report, null, ReportIntervalMs, ReportIntervalMs);
            _statsTimer = new System.Threading.Timer(SendStats, null, StatsIntervalMs, StatsIntervalMs);
        }
        public void Dispose()
        {
            _reportingTimer?.Dispose();
            _statsTimer?.Dispose();
            this.Close();        // Form sluiten
            base.Dispose();      // Form resources opruimen
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == RawInputInterop.WM_INPUT)
            {
                HandleRawInput(m.LParam);
            }
            base.WndProc(ref m);
        }

        private void HandleRawInput(IntPtr lParam)
        {
            uint dwSize = 0;
            RawInputInterop.GetRawInputData(lParam, RawInputInterop.RID_INPUT, IntPtr.Zero, ref dwSize, (uint)Marshal.SizeOf(typeof(RawInputInterop.RAWINPUTHEADER)));
            IntPtr buffer = Marshal.AllocHGlobal((int)dwSize);

            try
            {
                if (RawInputInterop.GetRawInputData(lParam, RawInputInterop.RID_INPUT, buffer, ref dwSize, (uint)Marshal.SizeOf(typeof(RawInputInterop.RAWINPUTHEADER))) == dwSize)
                {
                    var raw = (RawInputInterop.RAWINPUT)Marshal.PtrToStructure(buffer, typeof(RawInputInterop.RAWINPUT))!;
                    if (raw.header.dwType == RawInputInterop.RIM_TYPEKEYBOARD)
                    {
                        // Alleen echte key down events
                        if ((raw.data.keyboard.Message == 0x0100 /* WM_KEYDOWN */ ||
                             raw.data.keyboard.Message == 0x0104 /* WM_SYSKEYDOWN */))
                        {
                            // Check op repeat (zelfde toets als vorige)
                            if (_lastKeyDown != raw.data.keyboard.VKey)
                            {
                                Interlocked.Increment(ref _strokeCount);
                                _lastKeyDown = raw.data.keyboard.VKey;
                            }
                            // else: ignore repeated key
                        }
                        // Reset bij key up (optioneel: maakt repeat na loslaten weer mogelijk)
                        else if (raw.data.keyboard.Message == 0x0101 /* WM_KEYUP */ ||
                                 raw.data.keyboard.Message == 0x0105 /* WM_SYSKEYUP */)
                        {
                            if (_lastKeyDown == raw.data.keyboard.VKey)
                                _lastKeyDown = null;
                        }
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        private void Report(object? state)
        {
            int count = Interlocked.Exchange(ref _strokeCount, 0);
            if (count > 0)
            {
                _activeSamples++;
                _sumKeyRates += count;
                if (count > _maxKeyRate) _maxKeyRate = count;

            }
            if (count >= HighSpeedThreshold) _consecutiveHighRateCount++; else _consecutiveHighRateCount = 0;
            if (count >= MediumSpeedThreshold) _consecutiveMediumRateCount++; else _consecutiveMediumRateCount = 0;
            string reason = _consecutiveHighRateCount >= HighSpeedDuration
               ? $"≥{HighSpeedThreshold}/s for {HighSpeedDuration} sec"
               : $"≥{MediumSpeedThreshold}/s for {MediumSpeedDuration} sec";
            int duration = _consecutiveHighRateCount >= HighSpeedDuration ? HighSpeedDuration : MediumSpeedDuration;

            if (_consecutiveHighRateCount >= HighSpeedDuration || _consecutiveMediumRateCount >= MediumSpeedDuration)
            {

                _handler?.OnDetectionEvent(new DetectionEvent
                {
                    Source = "KeyRateMonitor",
                    EventType = "HighKeyRate",
                    Message = $"Suspicious keyrate: {count}/sec ({reason})",
                    Severity = EventSeverity.Critical,
                    Data = new Dictionary<string, object>
                        {
                            { "KeyRate", count },
                            { "Duration", duration }
                        }
                });

                // Na melding resetten om dubbele acties te voorkomen
                _consecutiveHighRateCount = 0;
                _consecutiveMediumRateCount = 0;
            }

        }
        public double GetAverageKeyRate() => _activeSamples > 0 ? (double)_sumKeyRates / _activeSamples : 0;

        private void SendStats(object? state)
        {
            if (_activeSamples == 0) return; // Alleen versturen als er echt iets gemeten is

            var avg = GetAverageKeyRate();
            var max = _maxKeyRate;

            _handler?.OnDetectionEvent(new DetectionEvent
            {
                Source = "KeyRateMonitor",
                EventType = "KeyRateStats",
                Severity = EventSeverity.Info,
                Message = $"Keyrate Avg: {avg:F1}/sec, Max: {max}/sec",
                Timestamp = DateTime.UtcNow,
                Data = new Dictionary<string, object>
                    {
                        { "AverageKeyRate", avg },
                        { "MaxKeyRate", max },
                        { "ActiveSamples", _activeSamples }
                    }
            });
        }

    }
}