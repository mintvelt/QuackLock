using QuackLock.Core.Detection;
using QuackLock.Core.Monitoring;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

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
            var assembly = Assembly.GetExecutingAssembly();
            var stream = assembly.GetManifestResourceStream("QuackLock.App.QuackStop.ico");
            var icon = SystemIcons.Application; // Fallback naar standaard icoon
            if (stream != null)
            {
                icon = new Icon(stream);

            }
            _trayIcon = new NotifyIcon
            {
                Icon = icon,
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
