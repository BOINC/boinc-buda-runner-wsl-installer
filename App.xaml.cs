// This file is part of BOINC.
// https://boinc.berkeley.edu
// Copyright (C) 2026 University of California
//
// BOINC is free software; you can redistribute it and/or modify it
// under the terms of the GNU Lesser General Public License
// as published by the Free Software Foundation,
// either version 3 of the License, or (at your option) any later version.
//
// BOINC is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License
// along with BOINC.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace boinc_buda_runner_wsl_installer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static bool IsQuiet { get; private set; }

        // Exit codes for quiet mode
        public const int ExitOk = 0;
        public const int ErrUnsupportedWindows = 10;
        public const int ErrWindowsFeaturesRestartRequired = 11;
        public const int ErrWindowsFeaturesEnableFailed = 12;
        public const int ErrWslCheckOrInstallFailed = 20;
        public const int ErrBoincRunning = 30;
        public const int ErrBudaRunnerInstallFailed = 40;
        public const int ErrIncompatibleBoincVersion = 50;
        public const int ErrUnexpected = 100;

        protected override async void OnStartup(StartupEventArgs e)
        {
            DebugLogger.LogInfo("Application starting up", "App");

            var args = e.Args ?? new string[0];
            var hasQuiet = args.Any(a => string.Equals(a, "--quiet", StringComparison.OrdinalIgnoreCase) || string.Equals(a, "-q", StringComparison.OrdinalIgnoreCase));
            var hasVeryQuiet = args.Any(a => string.Equals(a, "-qq", StringComparison.OrdinalIgnoreCase));
            var hasNoUninstall = args.Any(a => string.Equals(a, "-nu", StringComparison.OrdinalIgnoreCase));
            IsQuiet = hasQuiet || hasVeryQuiet;

            if (hasQuiet || hasVeryQuiet)
            {
                // Configure console logging channels
                // -q: info+debug to stdout, errors to stderr
                // -qq: info to stdout (optional) but NO debug; we silence debug by disabling console debug
                DebugLogger.ConfigureConsoleLogging(infoEnabled: true, debugEnabled: !hasVeryQuiet, errorEnabled: true);

                // Do not show any windows
                ShutdownMode = ShutdownMode.OnExplicitShutdown;

                int exitCode = await RunHeadlessInstallAsync();
                DebugLogger.LogInfo($"Headless install finished with exit code {exitCode}", "App");
                DebugLogger.Flush();
                Environment.Exit(exitCode);
                return;
            }

            // Normal (non-quiet) mode: show main window
            var mainWindow = new MainWindow(hasNoUninstall);
            mainWindow.Show();
        }

        private async Task<int> RunHeadlessInstallAsync()
        {
            try
            {
                var window = new MainWindow();

                // 1) Windows update check (we only inform; never stop for self-update)
                
                await window.CheckApplicationUpdateAsync();

                // 2) BOINC process must not run
                var boincOk = await window.CheckBoincProcess();
                if (!boincOk)
                {
                    return ErrBoincRunning;
                }

                // 3) BOINC version must be compatible
                var boincVersionOk = await window.CheckBoincVersion();
                if (!boincVersionOk)
                {
                    return ErrIncompatibleBoincVersion;
                }

                // 4) Windows version
                var isSupported = await window.CheckWindowsVersionCompatibility();
                if (!isSupported)
                {
                    DebugLogger.LogError("Unsupported Windows version", "Headless");
                    return ErrUnsupportedWindows;
                }

                // 5) Windows features
                var featuresOk = await window.CheckWindowsFeatures();
                if (!featuresOk)
                {
                    // Determine if this was because of restart required or general failure by reading last status
                    // We map any failure here to Windows features error; consumers can check logs for details.
                    return ErrWindowsFeaturesEnableFailed;
                }

                // 6) WSL check and install/fix
                var wslOk = await window.CheckWsl();
                if (!wslOk)
                {
                    return ErrWslCheckOrInstallFailed;
                }

                return ExitOk;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException(ex, "Unexpected error during headless installation", "Headless");
                return ErrUnexpected;
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            DebugLogger.LogInfo("Application shutting down", "App");
            DebugLogger.Flush();
            base.OnExit(e);
        }
    }
}
