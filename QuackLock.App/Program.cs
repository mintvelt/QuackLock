using System;
using System.Windows.Forms;
using System.Collections.Generic;
using QuackLock.Core.Monitoring;
using QuackLock.Core.Detection;
using System.Drawing;

namespace QuackLock.App
{
    internal static class Program
    {
        private static NotifyIcon? _trayIcon;
        private static readonly List<IMonitor> _monitors = new();
        const string RootKeyPath = @"SOFTWARE\Veldwerk\QuackStop"; //Voor de settings

        public static NotifyIcon TrayIcon => _trayIcon!;

        private static KeyboardMonitor? _keyboardMonitor;
        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            SetupTrayIcon();
            var handler = new ConsoleEventHandler();

            _keyboardMonitor = new KeyboardMonitor(handler);
            _monitors.Add(_keyboardMonitor);

            StartMonitors(handler);

            Application.ApplicationExit += App_Exit;
            Application.Run(_keyboardMonitor); // Houd de tray-app actief
        }

        private static void SetupTrayIcon()
        {
            _trayIcon = new NotifyIcon
            {
                Icon = new Icon("QuackStop.ico"),
                Text = "QuackLock is actief...",
                Visible = true,
                ContextMenuStrip = new ContextMenuStrip()
            };

            _trayIcon.ContextMenuStrip.Items.Add("Afsluiten", null, (s, e) => Application.Exit());
        }
        private static void StartMonitors(ConsoleEventHandler handler)
        {
            //_monitors.Add(new KeyboardMonitor(handler));

            foreach (var monitor in _monitors)
            {
                monitor.Start();
            }

            Console.WriteLine("[INFO] Alle monitors gestart.");
        }
        private static void App_Exit(object? sender, EventArgs e)
        {
            foreach (var monitor in _monitors)
            {
                monitor.Dispose();
            }

            _trayIcon?.Dispose();
        }

    }

}
