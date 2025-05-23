using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using QuackLock.Core.Monitoring;
using QuackLock.Core.Events;

namespace QuackLock.Core.Detection
{
    public class KeyRateMonitor : IMonitor
    {
        private readonly IDetectionEventHandler _handler;

        private bool[] _lastKeyStates = new bool[256];

        // statistics
        private int _activeSamples = 0;
        private int _sumKeyRates = 0;
        private int _maxKeyRate = 0;
        private int _strokeCount = 0;

        private Timer? _pollingTimer;
        private Timer? _reportingTimer;
        private Timer? _statsTimer;
        // Settings
        private const int PollIntervalMs = 20;
        private const int ReportIntervalMs = 1000;
        private const int SuspiciousThreshold = 15;     // Toetsaanslagen per seconde
        private const int SuspiciousDurationSeconds = 2; // Aantal seconden die overschrijding moet aanhouden
        private const int StatsIntervalMs = 60 * 1000; //5 * 60 * 1000; // bijv. elke 5 minuten

        private int _consecutiveHighRateCount = 0;

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        public KeyRateMonitor(IDetectionEventHandler detectionHandler)
        {
            _handler = detectionHandler;
        }

        public void Start()
        {
            _pollingTimer = new Timer(PollKeys, null, 0, PollIntervalMs);
            _reportingTimer = new Timer(ReportAndReset, null, ReportIntervalMs, ReportIntervalMs);
            _statsTimer = new Timer(SendStats, null, StatsIntervalMs, StatsIntervalMs);
        }
        public void Dispose()
        {
            // Voor toekomstig opruimen van timers of andere resources
        }

        private void PollKeys(object? state)
        {
            for (int i = 0; i < 256; i++)
            {
                bool isDown = (GetAsyncKeyState(i) & 0x8000) != 0;

                if (isDown && !_lastKeyStates[i])
                {
                    Interlocked.Increment(ref _strokeCount);
                }

                _lastKeyStates[i] = isDown;
            }
        }

        private void ReportAndReset(object? state)
        {
            int count = Interlocked.Exchange(ref _strokeCount, 0);
            Console.WriteLine($"[INFO] Aanslagen in laatste seconde: {count}");
            if (count > 0)
            {
                _activeSamples++;
                _sumKeyRates += count;
                if (count > _maxKeyRate) _maxKeyRate = count;

            }
            if (count >= SuspiciousThreshold)
            {
                _consecutiveHighRateCount++;
                Console.WriteLine($"[INFO] Hoge keyrate gedetecteerd ({_consecutiveHighRateCount}s op rij)");

                if (_consecutiveHighRateCount >= SuspiciousDurationSeconds)
                {
                    Console.WriteLine($"[WARNING] Aanhoudende verdachte keyrate! Trigger actie.");

                    _handler?.OnDetectionEvent(new DetectionEvent
                    {
                        Source = "KeyRateMonitor",
                        EventType = "HighKeyRate",
                        Message = $"Aanhoudende keyrate van {count} keys/sec gedetecteerd",
                        Severity = EventSeverity.Critical,
                        Data = new Dictionary<string, object>
                        {
                            { "KeyRate", count },
                            { "Duration", _consecutiveHighRateCount }
                        }
                    });

                    // Na melding resetten om dubbele acties te voorkomen
                    _consecutiveHighRateCount = 0;
                }
            }
            else
            {
                _consecutiveHighRateCount = 0;
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
                Message = $"Gemiddeld: {avg:F1}/sec, Max: {max}/sec",
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
