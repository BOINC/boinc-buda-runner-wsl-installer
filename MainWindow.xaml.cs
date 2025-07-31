// This file is part of BOINC.
// https://boinc.berkeley.edu
// Copyright (C) 2025 University of California
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

using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace boinc_buda_runner_wsl_installer
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<TableRow> TableItems { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            TableItems = new ObservableCollection<TableRow>
            {
                new TableRow { Id = ID.WindowsVersion, Icon = "", Status = "Check Windows version" },
                new TableRow { Id = ID.WindowsFeatures, Icon = "", Status = "Check Windows features" },
                new TableRow { Id = ID.WslCheck, Icon = "", Status = "Check WSL installation" },
                new TableRow { Id = ID.BoincProcessCheck, Icon = "", Status = "Check BOINC process" },
                new TableRow { Id = ID.BudaRunnerCheck, Icon = "", Status = "Check BUDA Runner installation" }
            };
            DataContext = this;

            DebugLogger.LogInfo("MainWindow initialized", "MainWindow");
            DebugLogger.LogConfiguration("Initial table items count", TableItems.Count, "MainWindow");
        }

        // Debug checkbox event handlers
        private void DebugLogCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            DebugLogger.IsEnabled = true;
            DebugLogger.LogInfo("Debug logging enabled by user", "MainWindow");
        }

        private void DebugLogCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            DebugLogger.LogInfo("Debug logging being disabled by user", "MainWindow");
            DebugLogger.IsEnabled = false;
        }

        private bool CheckWindowsVersionCompatibility()
        {
            DebugLogger.LogMethodStart("CheckWindowsVersionCompatibility", component: "MainWindow");

            // Check Windows version compatibility first
            var windowsInfo = WindowsVersionCheck.GetWindowsVersionInfo();

            DebugLogger.LogInfo($"Windows version check result: Status={windowsInfo.Status}, Message={windowsInfo.StatusMessage}", "MainWindow");
            DebugLogger.LogConfiguration("Windows Major Version", windowsInfo.MajorVersion, "MainWindow");
            DebugLogger.LogConfiguration("Windows Minor Version", windowsInfo.MinorVersion, "MainWindow");
            DebugLogger.LogConfiguration("Windows Build Number", windowsInfo.BuildNumber, "MainWindow");
            DebugLogger.LogConfiguration("Windows Architecture", windowsInfo.Architecture, "MainWindow");

            // Add version check result to the table
            var versionIcon = windowsInfo.Status == WindowsVersionCheck.WindowsVersionStatus.Supported ? "GreenCheckboxIcon" : "RedCancelIcon";
            var windowsVersion = windowsInfo.StatusMessage;
            ChangeRowIconAndStatus(ID.WindowsVersion, versionIcon, "Windows version: " + windowsVersion);

            var isSupported = windowsInfo.Status == WindowsVersionCheck.WindowsVersionStatus.Supported;
            DebugLogger.LogMethodEnd("CheckWindowsVersionCompatibility", isSupported.ToString(), "MainWindow");

            return isSupported;
        }

        private async Task<bool> CheckWindowsFeatures()
        {
            DebugLogger.LogMethodStart("CheckWindowsFeatures", component: "MainWindow");

            // Update UI to show we're checking features
            ChangeRowIconAndStatus(ID.WindowsFeatures, "BlueInfoIcon", "Checking Windows features...");

            // Allow UI to update
            await Task.Delay(100);

            // Check all required features
            var checkResult = await WindowsFeaturesCheck.CheckRequiredFeaturesAsync();
            DebugLogger.LogInfo($"Windows features check completed. All enabled: {checkResult.AllFeaturesEnabled}", "MainWindow");

            foreach (var feature in checkResult.FeatureResults)
            {
                DebugLogger.LogConfiguration($"Feature {feature.FeatureName}", $"Status: {feature.Status}, Message: {feature.Message}", "MainWindow");
            }

            if (!checkResult.AllFeaturesEnabled)
            {
                DebugLogger.LogInfo("Some Windows features are disabled, attempting to enable them", "MainWindow");

                // Update UI to show we're enabling features
                ChangeRowIconAndStatus(ID.WindowsFeatures, "YellowExclamationIcon", "Enabling missing Windows features...");

                // Allow UI to update
                await Task.Delay(100);

                // Enable missing features
                var enableResult = await WindowsFeaturesCheck.EnableRequiredFeaturesAsync();
                DebugLogger.LogInfo($"Windows features enable completed. Success: {enableResult.AllFeaturesEnabled}", "MainWindow");

                // Check if restart is required
                if (WindowsFeaturesCheck.IsRestartRequired(enableResult.FeatureResults))
                {
                    DebugLogger.LogWarning("Restart required after enabling Windows features", "MainWindow");
                    ChangeRowIconAndStatus(ID.WindowsFeatures, "YellowExclamationIcon", "Features enabled - restart required");
                    MessageBox.Show(
                        "Windows features have been enabled but require a restart.\n\nPlease restart your computer and run this installation again.",
                        "Computer restart is required",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    DebugLogger.LogMethodEnd("CheckWindowsFeatures", "false (restart required)", "MainWindow");
                    return false;
                }

                // Features were enabled successfully
                ChangeRowIconAndStatus(ID.WindowsFeatures, "GreenCheckboxIcon", "Windows features enabled successfully");
                DebugLogger.LogMethodEnd("CheckWindowsFeatures", "true (features enabled)", "MainWindow");
                return true;
            }

            // All features were already enabled
            ChangeRowIconAndStatus(ID.WindowsFeatures, "GreenCheckboxIcon", "All Windows features are already enabled");
            DebugLogger.LogMethodEnd("CheckWindowsFeatures", "true (all features already enabled)", "MainWindow");
            return true;
        }

        private async Task<bool> CheckWsl()
        {
            DebugLogger.LogMethodStart("CheckWsl", component: "MainWindow");

            // Update UI to show we're checking WSL
            ChangeRowIconAndStatus(ID.WslCheck, "BlueInfoIcon", "Checking WSL installation and configuration...");

            // Allow UI to update
            await Task.Delay(100);

            try
            {
                // Perform comprehensive WSL check
                var wslResult = await WslCheck.CheckWslAsync();
                DebugLogger.LogInfo($"WSL check completed. Status: {wslResult.Status}, Message: {wslResult.Message}", "MainWindow");

                switch (wslResult.Status)
                {
                    case WslCheck.WslStatus.AllGood:
                        ChangeRowIconAndStatus(ID.WslCheck, "GreenCheckboxIcon", WslCheck.GetStatusDisplayMessage(wslResult));
                        DebugLogger.LogMethodEnd("CheckWsl", "true (WSL all good)", "MainWindow");
                        return true;

                    case WslCheck.WslStatus.NotInstalled:
                        DebugLogger.LogWarning("WSL is not installed, attempting installation", "MainWindow");
                        ChangeRowIconAndStatus(ID.WslCheck, "YellowExclamationIcon", "WSL is not installed on this system");
                        var installResult = await InstallWsl();
                        DebugLogger.LogMethodEnd("CheckWsl", $"{installResult} (WSL install attempted)", "MainWindow");
                        return installResult;

                    case WslCheck.WslStatus.OutdatedVersion:
                    case WslCheck.WslStatus.WrongDefaultVersion:
                        DebugLogger.LogWarning($"WSL needs fixing: {wslResult.Status}", "MainWindow");
                        ChangeRowIconAndStatus(ID.WslCheck, "YellowExclamationIcon", WslCheck.GetStatusDisplayMessage(wslResult));
                        var fixResult = await FixWslIssues(wslResult);
                        DebugLogger.LogMethodEnd("CheckWsl", $"{fixResult} (WSL fix attempted)", "MainWindow");
                        return fixResult;

                    case WslCheck.WslStatus.Error:
                    default:
                        DebugLogger.LogError($"WSL check failed with status: {wslResult.Status}", "MainWindow");
                        ChangeRowIconAndStatus(ID.WslCheck, "RedCancelIcon", WslCheck.GetStatusDisplayMessage(wslResult));
                        DebugLogger.LogMethodEnd("CheckWsl", "false (WSL error)", "MainWindow");
                        return false;
                }
            }
            catch (System.Exception ex)
            {
                DebugLogger.LogException(ex, "Error occurred while checking WSL", "MainWindow");
                ChangeRowIconAndStatus(ID.WslCheck, "RedCancelIcon", "Error occurred while checking WSL");
                DebugLogger.LogMethodEnd("CheckWsl", "false (exception)", "MainWindow");
                return false;
            }
        }

        private async Task<bool> CheckBoincProcess()
        {
            DebugLogger.LogMethodStart("CheckBoincProcess", component: "MainWindow");

            // Update UI to show we're checking BOINC process
            ChangeRowIconAndStatus(ID.BoincProcessCheck, "BlueInfoIcon", "Checking BOINC process...");

            // Allow UI to update
            await Task.Delay(100);

            try
            {
                // Perform BOINC process check
                var boincResult = await BoincProcessCheck.CheckBoincProcessAsync();
                DebugLogger.LogInfo($"BOINC process check completed. Status: {boincResult.Status}, Process Count: {boincResult.ProcessCount}", "MainWindow");

                switch (boincResult.Status)
                {
                    case BoincProcessCheck.BoincProcessStatus.Running:
                        DebugLogger.LogWarning($"BOINC process is running: {boincResult.Message}", "MainWindow");
                        ChangeRowIconAndStatus(ID.BoincProcessCheck, "RedCancelIcon", boincResult.Message);

                        // Ask user if they want to stop BOINC
                        var result = MessageBox.Show(
                            $"BOINC client is currently running and may interfere with the installation.\n\nPlease stop the running BOINC client and retry the installation.",
                            $"{boincResult.Message}",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        DebugLogger.LogMethodEnd("CheckBoincProcess", "false (BOINC running)", "MainWindow");
                        return false;

                    case BoincProcessCheck.BoincProcessStatus.NotRunning:
                        DebugLogger.LogInfo("BOINC process is not running, ready for installation", "MainWindow");
                        ChangeRowIconAndStatus(ID.BoincProcessCheck, "GreenCheckboxIcon", "BOINC is not running - ready for installation");
                        DebugLogger.LogMethodEnd("CheckBoincProcess", "true (BOINC not running)", "MainWindow");
                        return true;

                    case BoincProcessCheck.BoincProcessStatus.Error:
                    default:
                        DebugLogger.LogWarning($"Unable to check BOINC status: {boincResult.ErrorMessage}", "MainWindow");
                        ChangeRowIconAndStatus(ID.BoincProcessCheck, "YellowExclamationIcon", $"Unable to check BOINC status: {boincResult.ErrorMessage}");
                        DebugLogger.LogMethodEnd("CheckBoincProcess", "true (error, continue anyway)", "MainWindow");
                        return true; // Continue despite error
                }
            }
            catch (System.Exception ex)
            {
                DebugLogger.LogException(ex, "Error occurred while checking BOINC process", "MainWindow");
                ChangeRowIconAndStatus(ID.BoincProcessCheck, "YellowExclamationIcon", "Error occurred while checking BOINC process");
                DebugLogger.LogMethodEnd("CheckBoincProcess", "true (exception, continue anyway)", "MainWindow");
                return true; // Continue despite error
            }
        }

        private async Task<bool> CheckBudaRunner()
        {
            DebugLogger.LogMethodStart("CheckBudaRunner", component: "MainWindow");

            // Update UI to show we're checking BUDA Runner
            ChangeRowIconAndStatus(ID.BudaRunnerCheck, "BlueInfoIcon", "Checking BUDA Runner installation...");

            // Allow UI to update
            await Task.Delay(100);

            try
            {
                // Perform comprehensive BUDA Runner check
                var budaResult = await BudaRunnerCheck.CheckBudaRunnerAsync();
                DebugLogger.LogInfo($"BUDA Runner check completed. Status: {budaResult.Status}", "MainWindow");
                DebugLogger.LogConfiguration("BUDA Runner Installed", budaResult.VersionInfo.IsInstalled, "MainWindow");
                DebugLogger.LogConfiguration("BUDA Runner Current Version", budaResult.VersionInfo.CurrentVersion ?? "N/A", "MainWindow");
                DebugLogger.LogConfiguration("BUDA Runner Latest Version", budaResult.VersionInfo.LatestVersion ?? "N/A", "MainWindow");
                DebugLogger.LogConfiguration("BUDA Runner Update Required", budaResult.UpdateRequired, "MainWindow");

                switch (budaResult.Status)
                {
                    case BudaRunnerCheck.BudaRunnerStatus.InstalledUpToDate:
                        DebugLogger.LogInfo("BUDA Runner is installed and up to date", "MainWindow");
                        ChangeRowIconAndStatus(ID.BudaRunnerCheck, "GreenCheckboxIcon", BudaRunnerCheck.GetStatusDisplayMessage(budaResult));
                        DebugLogger.LogMethodEnd("CheckBudaRunner", "true (up to date)", "MainWindow");
                        return true;

                    case BudaRunnerCheck.BudaRunnerStatus.NotInstalled:
                        DebugLogger.LogInfo("BUDA Runner is not installed, attempting installation", "MainWindow");
                        ChangeRowIconAndStatus(ID.BudaRunnerCheck, "YellowExclamationIcon", "BUDA Runner is not installed");
                        var installResult = await InstallBudaRunner(budaResult);
                        DebugLogger.LogMethodEnd("CheckBudaRunner", $"{installResult} (install attempted)", "MainWindow");
                        return installResult;

                    case BudaRunnerCheck.BudaRunnerStatus.InstalledOutdated:
                    case BudaRunnerCheck.BudaRunnerStatus.InstalledNoVersion:
                        DebugLogger.LogInfo($"BUDA Runner needs update/reinstall: {budaResult.Status}", "MainWindow");
                        ChangeRowIconAndStatus(ID.BudaRunnerCheck, "YellowExclamationIcon", BudaRunnerCheck.GetStatusDisplayMessage(budaResult));
                        var updateResult = await InstallBudaRunner(budaResult);
                        DebugLogger.LogMethodEnd("CheckBudaRunner", $"{updateResult} (update attempted)", "MainWindow");
                        return updateResult;

                    case BudaRunnerCheck.BudaRunnerStatus.Error:
                    default:
                        DebugLogger.LogError($"BUDA Runner check failed: {budaResult.ErrorMessage}", "MainWindow");
                        ChangeRowIconAndStatus(ID.BudaRunnerCheck, "YellowExclamationIcon", BudaRunnerCheck.GetStatusDisplayMessage(budaResult));
                        DebugLogger.LogMethodEnd("CheckBudaRunner", "true (error, continue anyway)", "MainWindow");
                        return true; // Continue despite error
                }
            }
            catch (System.Exception ex)
            {
                DebugLogger.LogException(ex, "Error occurred while checking BUDA Runner", "MainWindow");
                ChangeRowIconAndStatus(ID.BudaRunnerCheck, "YellowExclamationIcon", "Error occurred while checking BUDA Runner");
                DebugLogger.LogMethodEnd("CheckBudaRunner", "true (exception, continue anyway)", "MainWindow");
                return true; // Continue despite error
            }
        }

        private async Task<bool> InstallBudaRunner(BudaRunnerCheck.BudaRunnerCheckResult budaResult)
        {
            DebugLogger.LogMethodStart("InstallBudaRunner", $"Status: {budaResult.Status}", "MainWindow");

            try
            {
                // Create a progress reporter for UI updates
                var progress = new System.Progress<string>(message =>
                {
                    DebugLogger.LogInfo($"BUDA Runner install progress: {message}", "MainWindow");
                    ChangeRowIconAndStatus(ID.BudaRunnerCheck, "BlueInfoIcon", message);
                });

                // Install or update BUDA Runner
                bool success = await BudaRunnerCheck.InstallOrUpdateBudaRunnerAsync(budaResult, progress);

                if (success)
                {
                    DebugLogger.LogInfo("BUDA Runner installation completed successfully", "MainWindow");
                    ChangeRowIconAndStatus(ID.BudaRunnerCheck, "GreenCheckboxIcon", "BUDA Runner installed and configured successfully");
                    DebugLogger.LogMethodEnd("InstallBudaRunner", "true", "MainWindow");
                    return true;
                }
                else
                {
                    DebugLogger.LogError("BUDA Runner installation failed", "MainWindow");
                    ChangeRowIconAndStatus(ID.BudaRunnerCheck, "RedCancelIcon", "Failed to install BUDA Runner");
                    DebugLogger.LogMethodEnd("InstallBudaRunner", "false", "MainWindow");
                    return false;
                }
            }
            catch (System.Exception ex)
            {
                DebugLogger.LogException(ex, "BUDA Runner installation failed", "MainWindow");
                ChangeRowIconAndStatus(ID.BudaRunnerCheck, "RedCancelIcon", $"BUDA Runner installation failed: {ex.Message}");
                DebugLogger.LogMethodEnd("InstallBudaRunner", "false (exception)", "MainWindow");
                return false;
            }
        }

        private async Task<bool> InstallWsl()
        {
            DebugLogger.LogMethodStart("InstallWsl", component: "MainWindow");

            ChangeRowIconAndStatus(ID.WslCheck, "BlueInfoIcon", "Installing WSL...");

            try
            {
                // Create a progress reporter for UI updates
                var progress = new System.Progress<string>(message =>
                {
                    DebugLogger.LogInfo($"WSL install progress: {message}", "MainWindow");
                    ChangeRowIconAndStatus(ID.WslCheck, "BlueInfoIcon", message);
                });

                // Get the latest WSL download URL
                var downloadUrl = await WslCheck.GetLatestWslDownloadUrlAsync();
                DebugLogger.LogConfiguration("WSL Download URL", downloadUrl, "MainWindow");

                // Download and install WSL
                bool success = await WslCheck.DownloadAndInstallLatestWslAsync(downloadUrl, progress);

                if (success)
                {
                    DebugLogger.LogInfo("WSL installation completed successfully", "MainWindow");
                    ChangeRowIconAndStatus(ID.WslCheck, "GreenCheckboxIcon", "WSL installed successfully");

                    // Set default version to 2
                    DebugLogger.LogInfo("Setting WSL default version to 2", "MainWindow");
                    await Task.Delay(2000); // Give WSL time to initialize
                    await WslCheck.SetDefaultWslVersionTo2Async();

                    DebugLogger.LogMethodEnd("InstallWsl", "true", "MainWindow");
                    return true;
                }
                else
                {
                    DebugLogger.LogError("WSL installation failed", "MainWindow");
                    ChangeRowIconAndStatus(ID.WslCheck, "RedCancelIcon", "Failed to install WSL");
                    DebugLogger.LogMethodEnd("InstallWsl", "false", "MainWindow");
                    return false;
                }
            }
            catch (System.Exception ex)
            {
                DebugLogger.LogException(ex, "WSL installation failed", "MainWindow");
                ChangeRowIconAndStatus(ID.WslCheck, "RedCancelIcon", "WSL installation failed");
                DebugLogger.LogMethodEnd("InstallWsl", "false (exception)", "MainWindow");
                return false;
            }
        }

        private async Task<bool> FixWslIssues(WslCheck.WslCheckResult wslResult)
        {
            DebugLogger.LogMethodStart("FixWslIssues", $"Status: {wslResult.Status}", "MainWindow");

            try
            {
                // Create a progress reporter for UI updates
                var progress = new System.Progress<string>(message =>
                {
                    DebugLogger.LogInfo($"WSL fix progress: {message}", "MainWindow");
                    ChangeRowIconAndStatus(ID.WslCheck, "BlueInfoIcon", message);
                });

                // Fix WSL issues automatically
                bool success = await WslCheck.FixWslIssuesAsync(wslResult, progress);

                if (success)
                {
                    DebugLogger.LogInfo("WSL configuration updated successfully", "MainWindow");
                    ChangeRowIconAndStatus(ID.WslCheck, "GreenCheckboxIcon", "WSL configuration updated successfully");
                    DebugLogger.LogMethodEnd("FixWslIssues", "true", "MainWindow");
                    return true;
                }
                else
                {
                    DebugLogger.LogWarning("Some WSL issues could not be fixed automatically", "MainWindow");
                    ChangeRowIconAndStatus(ID.WslCheck, "YellowExclamationIcon", "Some WSL issues could not be fixed automatically");
                    DebugLogger.LogMethodEnd("FixWslIssues", "false", "MainWindow");
                    return false;
                }
            }
            catch (System.Exception ex)
            {
                DebugLogger.LogException(ex, "Failed to fix WSL configuration", "MainWindow");
                ChangeRowIconAndStatus(ID.WslCheck, "RedCancelIcon", "Failed to fix WSL configuration");
                DebugLogger.LogMethodEnd("FixWslIssues", "false (exception)", "MainWindow");
                return false;
            }
        }

        // Button Event Handlers
        private async void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            DebugLogger.LogSeparator("Installation Process Started");
            DebugLogger.LogMethodStart("InstallButton_Click", component: "MainWindow");

            // Show confirmation dialog
            var result = MessageBox.Show(
                "This will install BOINC BUDA Runner with WSL support on your system.\n\nDo you want to continue?",
                "Confirm Installation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            DebugLogger.LogInfo($"User confirmation dialog result: {result}", "MainWindow");

            if (result == MessageBoxResult.Yes)
            {
                // Disable the install button during execution to prevent multiple clicks
                var installButton = sender as Button;
                if (installButton != null)
                {
                    installButton.IsEnabled = false;
                    DebugLogger.LogInfo("Install button disabled during execution", "MainWindow");
                }

                try
                {
                    // Update UI to show we're checking Windows version
                    ChangeRowIconAndStatus(ID.WindowsVersion, "BlueInfoIcon", "Checking Windows version...");

                    // Allow UI to update
                    await Task.Delay(100);

                    var res = CheckWindowsVersionCompatibility();
                    if (!res)
                    {
                        DebugLogger.LogError("Windows version is not supported", "MainWindow");
                        MessageBox.Show(
                            "Your Windows version is not supported for this installation.",
                            "Unsupported Version",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        return;
                    }

                    // Check Windows features asynchronously
                    res = await CheckWindowsFeatures();
                    if (!res)
                    {
                        DebugLogger.LogError("Windows features check/installation failed", "MainWindow");
                        return;
                    }

                    // Check WSL asynchronously
                    res = await CheckWsl();
                    if (!res)
                    {
                        DebugLogger.LogError("WSL check/installation failed", "MainWindow");
                        return;
                    }

                    // Check BOINC process asynchronously
                    res = await CheckBoincProcess();
                    if (!res)
                    {
                        DebugLogger.LogError("BOINC process check failed", "MainWindow");
                        return;
                    }

                    // Check BUDA Runner asynchronously (final step)
                    res = await CheckBudaRunner();
                    if (!res)
                    {
                        DebugLogger.LogError("BUDA Runner check/installation failed", "MainWindow");
                        return;
                    }

                    // All checks passed - installation complete
                    await Task.Delay(100); // Allow final UI update

                    DebugLogger.LogInfo("Installation completed successfully", "MainWindow");
                    MessageBox.Show(
                        "BOINC BUDA Runner installation completed successfully!\n\nAll system requirements are met and BOINC BUDA Runner is installed and ready to use.",
                        "Installation Complete",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                catch (System.Exception ex)
                {
                    DebugLogger.LogException(ex, "Unexpected error during installation", "MainWindow");
                }
                finally
                {
                    // Re-enable the install button
                    if (installButton != null)
                    {
                        installButton.IsEnabled = true;
                        DebugLogger.LogInfo("Install button re-enabled", "MainWindow");
                    }
                    DebugLogger.LogSeparator("Installation Process Completed");
                }
            }
            else
            {
                DebugLogger.LogInfo("User cancelled installation", "MainWindow");
            }

            DebugLogger.LogMethodEnd("InstallButton_Click", component: "MainWindow");
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            DebugLogger.LogMethodStart("ExitButton_Click", component: "MainWindow");

            // Show confirmation dialog before exiting
            var result = MessageBox.Show(
                "Are you sure you want to exit the installer?",
                "Confirm Exit",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            DebugLogger.LogInfo($"Exit confirmation dialog result: {result}", "MainWindow");

            if (result == MessageBoxResult.Yes)
            {
                DebugLogger.LogInfo("User confirmed exit, shutting down application", "MainWindow");
                DebugLogger.Flush();
                Application.Current.Shutdown();
            }
            else
            {
                DebugLogger.LogInfo("User cancelled exit", "MainWindow");
            }

            DebugLogger.LogMethodEnd("ExitButton_Click", component: "MainWindow");
        }

        private void ChangeRowIconAndStatus(ID id, string newIcon, string status)
        {
            var rowIndex = TableItems.IndexOf(TableItems.FirstOrDefault(item => item.Id == id));
            if (rowIndex >= 0 && rowIndex < TableItems.Count)
            {
                TableItems[rowIndex].Icon = newIcon;
                TableItems[rowIndex].Status = status;
                DebugLogger.LogUIStatusChange(id.ToString(), newIcon, status);
            }
            else
            {
                DebugLogger.LogWarning($"Could not find table row with ID: {id}", "MainWindow");
            }
        }
    }
}
