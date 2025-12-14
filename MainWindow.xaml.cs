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

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel; // for CancelEventArgs

namespace boinc_buda_runner_wsl_installer
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<TableRow> TableItems { get; set; }
        private bool _isInstalling = false; // track if installation is in progress
        internal bool LastWindowsFeaturesRestartRequired { get; private set; } // quiet-mode detail

        public MainWindow()
        {
            InitializeComponent();

            // Enable debug logging by default
            DebugLogger.IsEnabled = true;

            TableItems = new ObservableCollection<TableRow>
            {
                new TableRow { Id = ID.ApplicationUpdate, Icon = "", Status = "Check installer update", IsVisible=false },
                new TableRow { Id = ID.WindowsVersion, Icon = "", Status = "Check Windows version", IsVisible=false },
                new TableRow { Id = ID.WindowsFeatures, Icon = "", Status = "Check Windows features", IsVisible=false },
                new TableRow { Id = ID.WslCheck, Icon = "", Status = "Check WSL installation", IsVisible=false },
                new TableRow { Id = ID.BoincProcessCheck, Icon = "", Status = "Check BOINC process", IsVisible=false },
                new TableRow { Id = ID.BudaRunnerCheck, Icon = "", Status = "Check BOINC WSL Distro installation", IsVisible=false }
            };
            DataContext = this;

            DebugLogger.LogInfo("MainWindow initialized", "MainWindow");
            DebugLogger.LogConfiguration("Initial table items count", TableItems.Count, "MainWindow");
        }

        private void ResetInstallationSteps()
        {
            DebugLogger.LogInfo("Resetting installation steps (hiding rows and restoring initial text)", "MainWindow");
            foreach (var row in TableItems)
            {
                row.IsVisible = false;
                row.Icon = string.Empty;
                switch (row.Id)
                {
                    case ID.ApplicationUpdate:
                        row.Status = "Check installer update"; break;
                    case ID.WindowsVersion:
                        row.Status = "Check Windows version"; break;
                    case ID.WindowsFeatures:
                        row.Status = "Check Windows features"; break;
                    case ID.WslCheck:
                        row.Status = "Check WSL installation"; break;
                    case ID.BoincProcessCheck:
                        row.Status = "Check BOINC process"; break;
                    case ID.BudaRunnerCheck:
                        row.Status = "Check BOINC WSL Distro installation"; break;
                }
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            DebugLogger.LogMethodStart("OnClosing", component: "MainWindow");

            if (_isInstalling)
            {
                var result = MessageBox.Show(
                    "Installation is currently in progress. Are you sure you want to exit?",
                    "Confirm Exit",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                DebugLogger.LogInfo($"Installation in progress exit confirmation dialog result: {result}", "MainWindow");
                if (result != MessageBoxResult.Yes)
                {
                    e.Cancel = true;
                    DebugLogger.LogMethodEnd("OnClosing", "cancelled by user during installation", "MainWindow");
                    return;
                }
            }

            DebugLogger.LogInfo("Application closing", "MainWindow");
            DebugLogger.Flush();
            DebugLogger.LogMethodEnd("OnClosing", component: "MainWindow");
            base.OnClosing(e);
        }

        internal bool CheckWindowsVersionCompatibility()
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
            if (!isSupported && !App.IsQuiet)
            {
                MessageBox.Show(
                    "Your Windows version is not supported for this installation.",
                    "Unsupported Version",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

            DebugLogger.LogMethodEnd("CheckWindowsVersionCompatibility", isSupported.ToString(), "MainWindow");
            return isSupported;
        }

        internal async Task<bool> CheckWindowsFeatures()
        {
            DebugLogger.LogMethodStart("CheckWindowsFeatures", component: "MainWindow");

            LastWindowsFeaturesRestartRequired = false;

            // Update UI to show we're checking features
            ChangeRowIconAndStatus(ID.WindowsFeatures, "BlueInfoIcon", "Checking Windows features...");

            // Allow UI to update
            await Task.Delay(100);

            try
            {
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
                        LastWindowsFeaturesRestartRequired = true;
                        DebugLogger.LogWarning("Restart required after enabling Windows features", "MainWindow");
                        ChangeRowIconAndStatus(ID.WindowsFeatures, "YellowExclamationIcon", "Features enabled - restart required");
                        if (!App.IsQuiet)
                        {
                            MessageBox.Show(
                                "Windows features have been enabled but require a restart.\n\nPlease restart your computer and run this installation again.",
                                "Computer restart is required",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                        }
                        DebugLogger.LogMethodEnd("CheckWindowsFeatures", "false (restart required)", "MainWindow");
                        return false;
                    }

                    // Features were enabled successfully
                    if (enableResult.AllFeaturesEnabled)
                    {
                        ChangeRowIconAndStatus(ID.WindowsFeatures, "GreenCheckboxIcon", "Windows features enabled successfully");
                        DebugLogger.LogMethodEnd("CheckWindowsFeatures", "true (features enabled)", "MainWindow");
                        return true;
                    }
                    else
                    {
                        // Some features failed to enable
                        ChangeRowIconAndStatus(ID.WindowsFeatures, "RedCancelIcon", "Failed to enable some Windows features");
                        DebugLogger.LogError("Failed to enable some required Windows features", "MainWindow");
                        
                        var failedFeatures = string.Join(", ", enableResult.FeatureResults
                            .Where(f => f.Status != WindowsFeaturesCheck.WindowsFeatureStatus.Enabled)
                            .Select(f => f.FeatureName));
                        
                        PromptOpenIssue("Windows features enable failed", 
                            $"Failed to enable required Windows features: {failedFeatures}. " +
                            $"Error details: {string.Join("; ", enableResult.FeatureResults.Select(f => f.Message))}");
                        
                        DebugLogger.LogMethodEnd("CheckWindowsFeatures", "false (enable failed)", "MainWindow");
                        return false;
                    }
                }

                // All features were already enabled
                ChangeRowIconAndStatus(ID.WindowsFeatures, "GreenCheckboxIcon", "All Windows features are already enabled");
                DebugLogger.LogMethodEnd("CheckWindowsFeatures", "true (all features already enabled)", "MainWindow");
                return true;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException(ex, "Error occurred while checking Windows features", "MainWindow");
                ChangeRowIconAndStatus(ID.WindowsFeatures, "RedCancelIcon", "Error checking Windows features");
                PromptOpenIssue("Windows features check exception", ex.ToString());
                DebugLogger.LogMethodEnd("CheckWindowsFeatures", "false (exception)", "MainWindow");
                return false;
            }
        }

        internal async Task<bool> CheckWsl()
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
                        PromptOpenIssue("WSL check error", 
                            $"WSL Status: {wslResult.Status}\n" +
                            $"Message: {wslResult.Message}\n" +
                            $"Version Info: {wslResult.VersionInfo?.WslVersion ?? "N/A"}\n" +
                            $"Default Version: {wslResult.StatusInfo?.DefaultVersion.ToString() ?? "N/A"}");
                        return false;
                }
            }
            catch (System.Exception ex)
            {
                DebugLogger.LogException(ex, "Error occurred while checking WSL", "MainWindow");
                ChangeRowIconAndStatus(ID.WslCheck, "RedCancelIcon", "Error occurred while checking WSL");
                DebugLogger.LogMethodEnd("CheckWsl", "false (exception)", "MainWindow");
                PromptOpenIssue("WSL check exception", 
                    $"Exception Type: {ex.GetType().Name}\n" +
                    $"Message: {ex.Message}\n" +
                    $"Stack Trace:\n{ex.StackTrace}");
                return false;
            }
        }

        internal async Task<bool> CheckBoincProcess()
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

                        if (!App.IsQuiet)
                        {
                            // Show instruction to stop BOINC (do NOT offer to create an issue here)
                            MessageBox.Show(
                                $"BOINC client is currently running and may interfere with the installation.\n\nPlease stop the running BOINC client and retry the installation.",
                                $"{boincResult.Message}",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                        }
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
                        // Continue but offer to report if user wants
                        PromptOpenIssue("BOINC process check error", boincResult.ErrorMessage);
                        return true; // Continue despite error
                }
            }
            catch (System.Exception ex)
            {
                DebugLogger.LogException(ex, "Error occurred while checking BOINC process", "MainWindow");
                ChangeRowIconAndStatus(ID.BoincProcessCheck, "YellowExclamationIcon", "Error occurred while checking BOINC process");
                DebugLogger.LogMethodEnd("CheckBoincProcess", "true (exception, continue anyway)", "MainWindow");
                // Offer to report issue
                PromptOpenIssue("BOINC process check exception", ex.ToString());
                return true; // Continue despite error
            }
        }

        internal async Task<bool> CheckBudaRunner()
        {
            DebugLogger.LogMethodStart("CheckBudaRunner", component: "MainWindow");

            // Update UI to show we're checking BUDA Runner
            ChangeRowIconAndStatus(ID.BudaRunnerCheck, "BlueInfoIcon", "Checking BOINC WSL Distro installation...");

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
                        ChangeRowIconAndStatus(ID.BudaRunnerCheck, "YellowExclamationIcon", "BOINC WSL Distro is not installed");
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
                        // Offer to report issue
                        PromptOpenIssue("BUDA Runner check error", 
                            $"Status: {budaResult.Status}\n" +
                            $"Error Message: {budaResult.ErrorMessage ?? "N/A"}\n" +
                            $"Is Installed: {budaResult.VersionInfo.IsInstalled}\n" +
                            $"Current Version: {budaResult.VersionInfo.CurrentVersion ?? "N/A"}\n" +
                            $"Latest Version: {budaResult.VersionInfo.LatestVersion ?? "N/A"}\n" +
                            $"Has Version File: {budaResult.VersionInfo.HasVersionFile}");
                        return true; // Continue despite error
                }
            }
            catch (System.Exception ex)
            {
                DebugLogger.LogException(ex, "Error occurred while checking BUDA Runner", "MainWindow");
                ChangeRowIconAndStatus(ID.BudaRunnerCheck, "YellowExclamationIcon", "Error occurred while checking BOINC WSL Distro");
                DebugLogger.LogMethodEnd("CheckBudaRunner", "true (exception, continue anyway)", "MainWindow");
                // Offer to report issue
                PromptOpenIssue("BUDA Runner check exception", 
                    $"Exception Type: {ex.GetType().Name}\n" +
                    $"Message: {ex.Message}\n" +
                    $"Stack Trace:\n{ex.StackTrace}");
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
                    ChangeRowIconAndStatus(ID.BudaRunnerCheck, "GreenCheckboxIcon", "BOINC WSL Distro installed and configured successfully");
                    DebugLogger.LogMethodEnd("InstallBudaRunner", "true", "MainWindow");
                    return true;
                }
                else
                {
                    DebugLogger.LogError("BUDA Runner installation failed", "MainWindow");
                    ChangeRowIconAndStatus(ID.BudaRunnerCheck, "RedCancelIcon", "Failed to install BOINC WSL Distro");
                    DebugLogger.LogMethodEnd("InstallBudaRunner", "false", "MainWindow");
                    // Offer to report issue with detailed context
                    PromptOpenIssue("BOINC WSL Distro installation failed", 
                        $"Installation Status: Failed\n" +
                        $"Was Previously Installed: {budaResult.VersionInfo.IsInstalled}\n" +
                        $"Current Version: {budaResult.VersionInfo.CurrentVersion ?? "N/A"}\n" +
                        $"Target Version: {budaResult.VersionInfo.LatestVersion ?? "N/A"}\n" +
                        $"Download URL: {budaResult.DownloadUrl ?? "N/A"}\n" +
                        $"Update Required: {budaResult.UpdateRequired}\n" +
                        $"Check the log file for detailed error information.");
                    return false;
                }
            }
            catch (System.Exception ex)
            {
                DebugLogger.LogException(ex, "BUDA Runner installation failed", "MainWindow");
                ChangeRowIconAndStatus(ID.BudaRunnerCheck, "RedCancelIcon", $"BOINC WSL Distro installation failed: {ex.Message}");
                DebugLogger.LogMethodEnd("InstallBudaRunner", "false (exception)", "MainWindow");
                // Offer to report issue
                PromptOpenIssue("BOINC WSL Distro installation exception", 
                    $"Exception Type: {ex.GetType().Name}\n" +
                    $"Message: {ex.Message}\n" +
                    $"Was Previously Installed: {budaResult.VersionInfo.IsInstalled}\n" +
                    $"Target Version: {budaResult.VersionInfo.LatestVersion ?? "N/A"}\n" +
                    $"Stack Trace:\n{ex.StackTrace}");
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
                    // Offer to report issue
                    PromptOpenIssue("WSL installation failed", 
                        $"Download URL: {downloadUrl}\n" +
                        $"Installation failed with no specific error. Check the log file for more details.");
                    return false;
                }
            }
            catch (System.Exception ex)
            {
                DebugLogger.LogException(ex, "WSL installation failed", "MainWindow");
                ChangeRowIconAndStatus(ID.WslCheck, "RedCancelIcon", "WSL installation failed");
                DebugLogger.LogMethodEnd("InstallWsl", "false (exception)", "MainWindow");
                // Offer to report issue
                PromptOpenIssue("WSL installation exception", 
                    $"Exception Type: {ex.GetType().Name}\n" +
                    $"Message: {ex.Message}\n" +
                    $"Stack Trace:\n{ex.StackTrace}");
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
                    // Offer to report issue
                    PromptOpenIssue("WSL fix issues could not be resolved", 
                        $"WSL Status: {wslResult.Status}\n" +
                        $"Current Version: {wslResult.VersionInfo?.WslVersion ?? "N/A"}\n" +
                        $"Latest Version: {wslResult.LatestVersion ?? "N/A"}\n" +
                        $"Default Version: {wslResult.StatusInfo?.DefaultVersion.ToString() ?? "N/A"}\n" +
                        $"Update Required: {wslResult.UpdateRequired}\n" +
                        $"Version Change Required: {wslResult.VersionChangeRequired}\n" +
                        $"Message: {wslResult.Message}");
                    return false;
                }
            }
            catch (System.Exception ex)
            {
                DebugLogger.LogException(ex, "Failed to fix WSL configuration", "MainWindow");
                ChangeRowIconAndStatus(ID.WslCheck, "RedCancelIcon", "Failed to fix WSL configuration");
                DebugLogger.LogMethodEnd("FixWslIssues", "false (exception)", "MainWindow");
                // Offer to report issue
                PromptOpenIssue("WSL fix exception", 
                    $"Exception Type: {ex.GetType().Name}\n" +
                    $"Message: {ex.Message}\n" +
                    $"WSL Status: {wslResult.Status}\n" +
                    $"Stack Trace:\n{ex.StackTrace}");
                return false;
            }
        }

        // Button Event Handlers
        private async void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            DebugLogger.LogSeparator("Installation Process Started");
            DebugLogger.LogMethodStart("InstallButton_Click", component: "MainWindow");

            if (IntroTextBlock != null && IntroTextBlock.Visibility == Visibility.Visible)
            {
                IntroTextBlock.Visibility = Visibility.Collapsed; // hide intro once installation begins
            }

            var installButton = sender as Button;
            if (installButton != null)
            {
                // If this is a retry (button content may be 'Retry') we hide existing steps
                if (installButton.Content != null && installButton.Content.ToString().Equals("Retry", StringComparison.OrdinalIgnoreCase))
                {
                    ResetInstallationSteps();
                    if (IntroTextBlock != null) IntroTextBlock.Visibility = Visibility.Collapsed; // ensure hidden on retry
                }
                installButton.IsEnabled = false;
                DebugLogger.LogInfo("Install button disabled during execution", "MainWindow");
            }

            bool success = false; // track overall result
            _isInstalling = true; // mark installation started

            try
            {
                try
                {
                    ChangeRowIconAndStatus(ID.ApplicationUpdate, "BlueInfoIcon", "Checking for installer updates...");
                    await Task.Delay(50);
                    await CheckApplicationUpdateAsync();
                }
                catch (Exception ex)
                {
                    ChangeRowIconAndStatus(ID.ApplicationUpdate, "YellowExclamationIcon", "Could not check for installer updates");
                    DebugLogger.LogException(ex, "Self-update check failed (continuing without update)", "MainWindow");
                }

                ChangeRowIconAndStatus(ID.WindowsVersion, "BlueInfoIcon", "Checking Windows version...");
                await Task.Delay(100);
                var res = CheckWindowsVersionCompatibility();
                if (!res)
                {
                    DebugLogger.LogError("Windows version is not supported", "MainWindow");
                    if (!App.IsQuiet)
                    {
                        MessageBox.Show(
                            "Your Windows version is not supported for this installation.",
                            "Unsupported Version",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                    return; // failure
                }

                res = await CheckWindowsFeatures();
                if (!res)
                {
                    DebugLogger.LogError("Windows features check/installation failed", "MainWindow");
                    return; // failure
                }

                res = await CheckWsl();
                if (!res)
                {
                    DebugLogger.LogError("WSL check/installation failed", "MainWindow");
                    return; // failure
                }

                res = await CheckBoincProcess();
                if (!res)
                {
                    DebugLogger.LogError("BOINC process check failed", "MainWindow");
                    return; // failure
                }

                res = await CheckBudaRunner();
                if (!res)
                {
                    DebugLogger.LogError("BOINC WSL Distro check/installation failed", "MainWindow");
                    return; // failure
                }

                await Task.Delay(100);
                DebugLogger.LogInfo("Installation completed successfully", "MainWindow");
                if (!App.IsQuiet)
                {
                    MessageBox.Show(
                        "BOINC WSL Distro installation completed successfully!\n\nAll system requirements are met and BOINC WSL Distro is installed and ready to use.",
                        "Installation Complete",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                success = true; // mark success
            }
            catch (System.Exception ex)
            {
                DebugLogger.LogException(ex, "Unexpected error during installation", "MainWindow");
            }
            finally
            {
                _isInstalling = false; // installation ended
                if (installButton != null)
                {
                    if (!success)
                    {
                        installButton.IsEnabled = true;
                        installButton.Content = "Retry"; // allow retry on failure
                        DebugLogger.LogInfo("Install button re-enabled for retry", "MainWindow");
                    }
                    else
                    {
                        installButton.IsEnabled = false; // keep disabled on success
                        DebugLogger.LogInfo("Install succeeded; button remains disabled", "MainWindow");
                    }
                }
                DebugLogger.LogSeparator("Installation Process Completed");
            }

            DebugLogger.LogMethodEnd("InstallButton_Click", component: "MainWindow");
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            DebugLogger.LogMethodStart("ExitButton_Click", component: "MainWindow");

            // Delegate to window close to reuse confirmation logic
            this.Close();

            DebugLogger.LogMethodEnd("ExitButton_Click", component: "MainWindow");
        }

        internal void ChangeRowIconAndStatus(ID id, string newIcon, string status)
        {
            var row = TableItems.FirstOrDefault(item => item.Id == id);
            if (row != null)
            {
                if (!row.IsVisible) row.IsVisible = true; // reveal step when first updated
                row.Icon = newIcon;
                row.Status = status;
                DebugLogger.LogUIStatusChange(id.ToString(), newIcon, status);
            }
            else
            {
                DebugLogger.LogWarning($"Could not find table row with ID: {id}", "MainWindow");
            }
        }

        private void PromptOpenIssue(string title, string details)
        {
            try
            {
                // Categorize the error to provide appropriate troubleshooting guidance
                var errorCategory = TroubleshootingGuide.CategorizeError(title, details);
                var advice = TroubleshootingGuide.GetAdvice(errorCategory, details);

                if (!App.IsQuiet)
                {
                    // Show troubleshooting dialog instead of simple message box
                    var dialog = new TroubleshootingDialog(advice, details)
                    {
                        Owner = this
                    };
                    dialog.ShowDialog();
                }
                else
                {
                    // In quiet mode, log the troubleshooting information
                    var formattedAdvice = TroubleshootingGuide.FormatAdviceAsMessage(advice);
                    DebugLogger.LogInfo($"Troubleshooting guidance for '{title}':\n{formattedAdvice}", "MainWindow");
                    
                    // Build a GitHub new issue URL with prefilled title/body for logging purposes
                    var repoNewIssueUrl = "https://github.com/BOINC/boinc-buda-runner-wsl-installer/issues/new";

                    var sb = new StringBuilder();
                    sb.AppendLine("Describe the problem and steps to reproduce here.\n");
                    sb.AppendLine("Error context:");
                    sb.AppendLine(details ?? "(no details)");
                    sb.AppendLine();
                    sb.AppendLine($"App version: {FileVersionInfo.GetVersionInfo(Process.GetCurrentProcess().MainModule.FileName).FileVersion}");
                    sb.AppendLine($"OS: {Environment.OSVersion}");
                    sb.AppendLine($"64-bit OS: {Environment.Is64BitOperatingSystem}, 64-bit Process: {Environment.Is64BitProcess}");
                    sb.AppendLine();
                    if (!string.IsNullOrEmpty(DebugLogger.LogFilePath))
                    {
                        sb.AppendLine($"Log file path (attach this file in the issue): {DebugLogger.LogFilePath}");
                    }

                    var url = repoNewIssueUrl + "?title=" + Uri.EscapeDataString(title ?? "Installer error")
                              + "&body=" + Uri.EscapeDataString(sb.ToString());
                    
                    DebugLogger.LogInfo($"Issue report URL (quiet mode): {url}", "MainWindow");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException(ex, "Failed to show troubleshooting guidance", "MainWindow");
                
                // Fallback to old behavior if troubleshooting dialog fails
                if (!App.IsQuiet)
                {
                    var ask = MessageBox.Show(
                        "An error occurred. Would you like to report it on GitHub? Your debug log file path will be included in the report so you can attach it.",
                        "Report Error",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (ask == MessageBoxResult.Yes)
                    {
                        try
                        {
                            var repoNewIssueUrl = "https://github.com/BOINC/boinc-buda-runner-wsl-installer/issues/new";
                            var sb = new StringBuilder();
                            sb.AppendLine("Describe the problem and steps to reproduce here.\n");
                            sb.AppendLine("Error context:");
                            sb.AppendLine(details ?? "(no details)");
                            sb.AppendLine();
                            sb.AppendLine($"App version: {FileVersionInfo.GetVersionInfo(Process.GetCurrentProcess().MainModule.FileName).FileVersion}");
                            sb.AppendLine($"OS: {Environment.OSVersion}");
                            sb.AppendLine($"64-bit OS: {Environment.Is64BitOperatingSystem}, 64-bit Process: {Environment.Is64BitProcess}");
                            if (!string.IsNullOrEmpty(DebugLogger.LogFilePath))
                            {
                                sb.AppendLine($"Log file path (attach this file in the issue): {DebugLogger.LogFilePath}");
                            }

                            var url = repoNewIssueUrl + "?title=" + Uri.EscapeDataString(title ?? "Installer error")
                                      + "&body=" + Uri.EscapeDataString(sb.ToString());
                            Process.Start(url);
                        }
                        catch (Exception innerEx)
                        {
                            DebugLogger.LogException(innerEx, "Failed to open GitHub issue link", "MainWindow");
                        }
                    }
                }
            }
        }

        // --- Self-update logic (lightweight JSON parsing) ---
        private static string ExtractJsonString(string json, string propertyName)
        {
            if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(propertyName)) return null;
            var pattern = $"\"{Regex.Escape(propertyName)}\"\\s*:\\s*\"([^\\\"]*)\"";
            var m = Regex.Match(json, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            return m.Success ? m.Groups[1].Value : null;
        }

        private static Version TryParseVersionFromTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return null;
            var s = tag.Trim();
            if (s.StartsWith("v", StringComparison.OrdinalIgnoreCase)) s = s.Substring(1);

            // Remove any suffix like -beta, -rc1, etc.
            int dash = s.IndexOf('-');
            if (dash > 0) s = s.Substring(0, dash);

            try
            {
                // Ensure at least Major.Minor
                var parts = s.Split('.');
                int major = 0, minor = 0, build = 0, revision = 0;
                if (parts.Length > 0) int.TryParse(new string(parts[0].TakeWhile(char.IsDigit).ToArray()), out major);
                if (parts.Length > 1) int.TryParse(new string(parts[1].TakeWhile(char.IsDigit).ToArray()), out minor);
                if (parts.Length > 2) int.TryParse(new string(parts[2].TakeWhile(char.IsDigit).ToArray()), out build);
                if (parts.Length > 3) int.TryParse(new string(parts[3].TakeWhile(char.IsDigit).ToArray()), out revision);

                if (parts.Length >= 4) return new Version(major, minor, build, revision);
                if (parts.Length == 3) return new Version(major, minor, build);
                return new Version(major, minor);
            }
            catch
            {
                return null;
            }
        }

        internal static Version TryGetCurrentFileVersion(string exePath)
        {
            try
            {
                var fvi = FileVersionInfo.GetVersionInfo(exePath);
                var verStr = fvi.FileVersion ?? fvi.ProductVersion;
                if (string.IsNullOrWhiteSpace(verStr)) return null;
                // Normalize similar to tag parsing
                var dash = verStr.IndexOf('-');
                if (dash > 0) verStr = verStr.Substring(0, dash);
                Version v;
                if (Version.TryParse(verStr, out v)) return v;
            }
            catch { }
            return null;
        }

        internal async Task<bool> CheckApplicationUpdateAsync()
        {
            DebugLogger.LogMethodStart("CheckApplicationUpdateAsync", component: "MainWindow");

            try
            {
                // Ensure TLS 1.2 for GitHub API
                try { ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12; } catch { }

                using (var http = new HttpClient())
                {
                    http.DefaultRequestHeaders.UserAgent.ParseAdd("boinc-buda-runner-wsl-installer/1.0 (+https://github.com/BOINC/boinc-buda-runner-wsl-installer)");
                    var apiUrl = "https://api.github.com/repos/BOINC/boinc-buda-runner-wsl-installer/releases/latest";
                    DebugLogger.LogConfiguration("GitHub Releases API URL", apiUrl, "MainWindow");

                    using (var resp = await http.GetAsync(apiUrl))
                    {
                        if (!resp.IsSuccessStatusCode)
                        {
                            ChangeRowIconAndStatus(ID.ApplicationUpdate, "YellowExclamationIcon", "Could not check for installer updates");
                            DebugLogger.LogWarning($"GitHub release check failed: {(int)resp.StatusCode} {resp.ReasonPhrase}", "MainWindow");
                            DebugLogger.LogMethodEnd("CheckApplicationUpdateAsync", "false (api error)", "MainWindow");
                            return false; // Continue without update
                        }

                        var json = await resp.Content.ReadAsStringAsync();
                        var latestTag = ExtractJsonString(json, "tag_name");
                        var latestVersion = TryParseVersionFromTag(latestTag);

                        var exePath = Process.GetCurrentProcess().MainModule.FileName;
                        var currentVersion = TryGetCurrentFileVersion(exePath);

                        DebugLogger.LogConfiguration("Current EXE Path", exePath, "MainWindow");
                        DebugLogger.LogConfiguration("Current Version", currentVersion != null ? currentVersion.ToString() : "unknown", "MainWindow");
                        DebugLogger.LogConfiguration("Latest Tag", latestTag ?? "null", "MainWindow");
                        DebugLogger.LogConfiguration("Latest Version Parsed", latestVersion != null ? latestVersion.ToString() : "unknown", "MainWindow");

                        bool updateAvailable = false;
                        if (latestVersion != null && currentVersion != null)
                        {
                            updateAvailable = latestVersion > currentVersion;
                        }
                        else if (currentVersion == null && latestVersion != null)
                        {
                            updateAvailable = true;
                        }

                        if (!updateAvailable)
                        {
                            ChangeRowIconAndStatus(ID.ApplicationUpdate, "GreenCheckboxIcon", "Installer is up to date");
                            DebugLogger.LogInfo("Application is up to date; no update available", "MainWindow");
                            DebugLogger.LogMethodEnd("CheckApplicationUpdateAsync", "false (up to date)", "MainWindow");
                            return false;
                        }

                        // Update available: inform user but do not perform self-update
                        var newVerText = latestVersion != null ? latestVersion.ToString() : latestTag ?? "latest";
                        var statusText = $"New installer version available: {newVerText}. Please download the latest installer from the Releases page: https://github.com/BOINC/boinc-buda-runner-wsl-installer/releases";
                        ChangeRowIconAndStatus(ID.ApplicationUpdate, "YellowExclamationIcon", statusText);
                        DebugLogger.LogInfo($"Update available ({newVerText}), self-update is disabled; informing user only.", "MainWindow");
                        DebugLogger.LogMethodEnd("CheckApplicationUpdateAsync", "false (update available, no self-update)", "MainWindow");
                        return false; // Never trigger stop; just inform
                    }
                }
            }
            catch (Exception ex)
            {
                ChangeRowIconAndStatus(ID.ApplicationUpdate, "YellowExclamationIcon", "Could not check for installer updates");
                DebugLogger.LogException(ex, "Self-update check failed (continuing without update)", "MainWindow");
            }

            DebugLogger.LogMethodEnd("CheckApplicationUpdateAsync", "false (no update)", "MainWindow");
            return false;
        }

        private void OpenLogButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var logPath = DebugLogger.LogFilePath;
                if (string.IsNullOrEmpty(logPath))
                {
                    if (!App.IsQuiet)
                        MessageBox.Show("Log file is not available yet.", "Open Log", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (!System.IO.File.Exists(logPath))
                {
                    if (!App.IsQuiet)
                        MessageBox.Show("Log file does not exist.", "Open Log", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var psi = new ProcessStartInfo
                {
                    FileName = logPath,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                DebugLogger.LogException(ex, "Failed to open log file", "MainWindow");
                if (!App.IsQuiet)
                    MessageBox.Show("Failed to open the log file.", "Open Log", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
