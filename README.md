# QuackLock

QuackLock is a lightweight Windows utility designed to help protect your machine from automated keystroke injection attacks (such as those performed by "rubber ducky" devices). It runs in the background, monitoring your keyboard input speed. If it detects typing that is suspiciously fast (beyond human capability), it will immediately lock your Windows session to prevent potential harm.

## Features

- Monitors keyboard input speed in real-time
- Detects suspiciously high key rates (configurable thresholds in code)
- Locks the workstation automatically on detection
- Runs silently in the system tray
- Logs detection events to the Windows Event Log or a local file

## How It Works

QuackLock uses low-level keyboard monitoring to measure the rate of keystrokes. If the key rate exceeds a defined threshold for a sustained period, it triggers a lock of the Windows session. This helps prevent malicious scripts or devices from injecting commands at inhuman speeds.

## Requirements

- Windows 10 or later
- .NET 8.0 SDK (for building)
- Admin rights may be required for some logging features

## Build Instructions

1. Clone the repository:

	```sh
	git clone https://github.com/mintvelt/QuackLock.git
	```

2. Open the solution `QuackLock.sln` in Visual Studio 2022 or later.
3. Build the solution (both `QuackLock.App` and `QuackLock.Core` projects will be built).

## Run Instructions

1. Run the `QuackLock.App` project (either from Visual Studio or by launching the built executable from `QuackLock.App/bin/Debug/net8.0-windows/QuackLock.App.exe`).
2. The app will appear as a tray icon and start monitoring keyboard input.
3. To exit, right-click the tray icon and select "Afsluiten" (Exit).

## Usage

- The application runs in the background and requires no configuration for basic use.
- If a suspicious key rate is detected, your session will be locked and a warning will be logged.
- You can review logs in the Windows Event Viewer or in the `%TEMP%` folder (look for `QuackStop.YYYYMMDD.log`).

## Customization

- Key rate thresholds and durations can be adjusted in the source code (`KeyboardMonitor.cs` and `KeyRateMonitor.cs`).
- The tray icon and messages can be customized in `Program.cs` and `ConsoleEventHandler.cs`.

## Disclaimer

This tool is experimental and provided as-is. Use at your own risk. It may interfere with legitimate high-speed typing or macro tools.
