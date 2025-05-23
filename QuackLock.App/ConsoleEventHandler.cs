using QuackLock.Core;
using QuackLock.Core.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace QuackLock.App
{
    public class ConsoleEventHandler : IDetectionEventHandler
    {
        private readonly HashSet<string> _trustedKeyboards = new();
        private const string EventLogSource = "QuackLock";

        public ConsoleEventHandler()
        {
        }

        public void OnDetectionEvent(DetectionEvent evt)
        {
            Console.WriteLine($"[{evt.Timestamp:HH:mm:ss}] [{evt.Severity}] {evt.EventType}: {evt.Message}");

            if (evt.Severity == EventSeverity.Critical)
            {
                LogEvent(evt.Message, EventLogEntryType.Warning);
                NativeMethods.LockWorkStation();
            }
            if (evt.EventType == "KeyRateStats" && evt.Severity == EventSeverity.Info)
            {
                Program.TrayIcon.Text = $"QuackLock — {evt.Message}";
            }

        }
        public static void LogEvent(string message, EventLogEntryType type = EventLogEntryType.Information)
        {
            string source = "Application"; // Altijd bestaande logbron
            try
            {
                EventLog.WriteEntry(source, message, type);
            }
            catch
            {
                // Fallback: schrijf naar %TEMP%
                try
                {
                    string tempPath = System.IO.Path.GetTempPath();
                    string fileName = $"QuackStop.{DateTime.Now:yyyyMMdd}.log";
                    string fullPath = System.IO.Path.Combine(tempPath, fileName);
                    string logLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{type}] {message}{Environment.NewLine}";

                    System.IO.File.AppendAllText(fullPath, logLine);
                }
                catch
                {
                    // Desnoods naar console loggen als echt alles faalt
                    Console.WriteLine($"[FATAL] Logging failed: {message}");
                }
            }
        }

    }

    internal static class NativeMethods
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool LockWorkStation();
    }

}
