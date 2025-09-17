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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace boinc_buda_runner_wsl_installer
{
    internal class WindowsFeaturesCheck
    {
        private const string COMPONENT = "WindowsFeaturesCheck";

        public enum WindowsFeatureStatus
        {
            Enabled,
            Disabled,
            NotFound,
            Error
        }

        public class WindowsFeatureResult
        {
            public string FeatureName { get; set; }
            public WindowsFeatureStatus Status { get; set; }
            public string Message { get; set; }
            public bool RestartRequired { get; set; }
        }

        public class WindowsFeatureCheckResult
        {
            public bool AllFeaturesEnabled { get; set; }
            public List<WindowsFeatureResult> FeatureResults { get; set; } = new List<WindowsFeatureResult>();
            public string ErrorMessage { get; set; }
        }

        // Required Windows Features for WSL
        private static readonly string[] RequiredFeatures = {
            "VirtualMachinePlatform",
            "Microsoft-Windows-Subsystem-Linux"
        };

        /// <summary>
        /// Checks if all required Windows features are enabled
        /// </summary>
        public static async Task<WindowsFeatureCheckResult> CheckRequiredFeaturesAsync()
        {
            DebugLogger.LogMethodStart("CheckRequiredFeaturesAsync", component: COMPONENT);
            var result = new WindowsFeatureCheckResult();

            try
            {
                DebugLogger.LogConfiguration("Required Features Count", RequiredFeatures.Length, COMPONENT);
                DebugLogger.LogConfiguration("Required Features", string.Join(", ", RequiredFeatures), COMPONENT);

                foreach (var feature in RequiredFeatures)
                {
                    DebugLogger.LogInfo($"Checking feature: {feature}", COMPONENT);
                    var featureResult = await CheckWindowsFeatureAsync(feature);
                    result.FeatureResults.Add(featureResult);
                    DebugLogger.LogConfiguration($"Feature {feature} Status", featureResult.Status, COMPONENT);
                    DebugLogger.LogConfiguration($"Feature {feature} Message", featureResult.Message, COMPONENT);
                }

                result.AllFeaturesEnabled = result.FeatureResults.All(f => f.Status == WindowsFeatureStatus.Enabled);
                DebugLogger.LogConfiguration("All Features Enabled", result.AllFeaturesEnabled, COMPONENT);

                var enabledCount = result.FeatureResults.Count(f => f.Status == WindowsFeatureStatus.Enabled);
                var disabledCount = result.FeatureResults.Count(f => f.Status == WindowsFeatureStatus.Disabled);
                var errorCount = result.FeatureResults.Count(f => f.Status == WindowsFeatureStatus.Error);

                DebugLogger.LogConfiguration("Features Enabled", enabledCount, COMPONENT);
                DebugLogger.LogConfiguration("Features Disabled", disabledCount, COMPONENT);
                DebugLogger.LogConfiguration("Features Error", errorCount, COMPONENT);

                DebugLogger.LogMethodEnd("CheckRequiredFeaturesAsync", $"AllEnabled: {result.AllFeaturesEnabled}", COMPONENT);
            }
            catch (Exception ex)
            {
                DebugLogger.LogException(ex, "Error in CheckRequiredFeaturesAsync", COMPONENT);
                result.ErrorMessage = $"Error checking Windows features: {ex.Message}";
                DebugLogger.LogMethodEnd("CheckRequiredFeaturesAsync", $"Error: {ex.Message}", COMPONENT);
            }

            return result;
        }

        /// <summary>
        /// Checks if a specific Windows feature is enabled using DISM
        /// </summary>
        public static async Task<WindowsFeatureResult> CheckWindowsFeatureAsync(string featureName)
        {
            DebugLogger.LogMethodStart("CheckWindowsFeatureAsync", $"featureName: {featureName}", COMPONENT);
            var result = new WindowsFeatureResult { FeatureName = featureName };

            try
            {
                // First try checking via Registry (faster)
                DebugLogger.LogInfo($"Attempting registry check for feature: {featureName}", COMPONENT);
                var registryResult = CheckFeatureViaRegistry(featureName);
                DebugLogger.LogConfiguration("Registry Check Status", registryResult.Status, COMPONENT);

                if (registryResult.Status != WindowsFeatureStatus.Error)
                {
                    DebugLogger.LogInfo("Registry check successful, returning result", COMPONENT);
                    DebugLogger.LogMethodEnd("CheckWindowsFeatureAsync", $"Registry: {registryResult.Status}", COMPONENT);
                    return registryResult;
                }

                // Fallback to DISM command line
                DebugLogger.LogInfo("Registry check failed, falling back to DISM", COMPONENT);
                var dismResult = await CheckFeatureWithDismCommandAsync(featureName);
                DebugLogger.LogMethodEnd("CheckWindowsFeatureAsync", $"DISM: {dismResult.Status}", COMPONENT);
                return dismResult;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException(ex, $"Error in CheckWindowsFeatureAsync for {featureName}", COMPONENT);
                result.Status = WindowsFeatureStatus.Error;
                result.Message = $"Error checking feature '{featureName}': {ex.Message}";
                DebugLogger.LogMethodEnd("CheckWindowsFeatureAsync", $"Exception: {ex.Message}", COMPONENT);
                return result;
            }
        }

        /// <summary>
        /// Checks Windows feature status via Registry (faster method)
        /// </summary>
        private static WindowsFeatureResult CheckFeatureViaRegistry(string featureName)
        {
            DebugLogger.LogMethodStart("CheckFeatureViaRegistry", $"featureName: {featureName}", COMPONENT);
            var result = new WindowsFeatureResult { FeatureName = featureName };

            try
            {
                DebugLogger.LogInfo("Opening OptionalFeatures registry key", COMPONENT);
                // Check in Windows Features registry location
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\OptionalFeatures"))
                {
                    if (key != null)
                    {
                        DebugLogger.LogInfo("OptionalFeatures key opened successfully", COMPONENT);
                        using (var featureKey = key.OpenSubKey(featureName))
                        {
                            if (featureKey != null)
                            {
                                DebugLogger.LogInfo($"Feature key found for {featureName}", COMPONENT);
                                var enabled = featureKey.GetValue("Enabled");
                                DebugLogger.LogConfiguration("Registry Enabled Value", enabled?.ToString() ?? "null", COMPONENT);

                                if (enabled != null && enabled.ToString() == "1")
                                {
                                    result.Status = WindowsFeatureStatus.Enabled;
                                    result.Message = $"Feature '{featureName}' is enabled";
                                    DebugLogger.LogInfo($"Registry indicates {featureName} is enabled", COMPONENT);
                                }
                                else
                                {
                                    result.Status = WindowsFeatureStatus.Disabled;
                                    result.Message = $"Feature '{featureName}' is disabled";
                                    DebugLogger.LogInfo($"Registry indicates {featureName} is disabled", COMPONENT);
                                }

                                DebugLogger.LogMethodEnd("CheckFeatureViaRegistry", result.Status.ToString(), COMPONENT);
                                return result;
                            }
                            else
                            {
                                DebugLogger.LogWarning($"Feature key not found for {featureName}", COMPONENT);
                            }
                        }
                    }
                    else
                    {
                        DebugLogger.LogWarning("Could not open OptionalFeatures registry key", COMPONENT);
                    }
                }

                // Feature not found in registry, use DISM
                DebugLogger.LogInfo("Feature not found in registry, will use DISM", COMPONENT);
                result.Status = WindowsFeatureStatus.Error;
                result.Message = "Feature not found in registry, will use DISM";
                DebugLogger.LogMethodEnd("CheckFeatureViaRegistry", "Error (not found)", COMPONENT);
                return result;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException(ex, $"Registry check error for feature {featureName}", COMPONENT);
                result.Status = WindowsFeatureStatus.Error;
                result.Message = $"Registry check error for feature '{featureName}': {ex.Message}";
                DebugLogger.LogMethodEnd("CheckFeatureViaRegistry", $"Exception: {ex.Message}", COMPONENT);
                return result;
            }
        }

        /// <summary>
        /// Checks Windows feature using DISM command line
        /// </summary>
        private static async Task<WindowsFeatureResult> CheckFeatureWithDismCommandAsync(string featureName)
        {
            DebugLogger.LogMethodStart("CheckFeatureWithDismCommandAsync", $"featureName: {featureName}", COMPONENT);
            var result = new WindowsFeatureResult { FeatureName = featureName };

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "dism.exe",
                    Arguments = $"/English /online /get-featureinfo /featurename:{featureName}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                DebugLogger.LogInfo($"Executing DISM command: {startInfo.FileName} {startInfo.Arguments}", COMPONENT);

                using (var process = new Process { StartInfo = startInfo })
                {
                    process.Start();

                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error = await process.StandardError.ReadToEndAsync();

                    await Task.Run(() => process.WaitForExit(30000)); // 30 second timeout

                    DebugLogger.LogProcessExecution("dism.exe", startInfo.Arguments, process.ExitCode, output, error, COMPONENT);

                    if (process.ExitCode == 0)
                    {
                        if (output.Contains("State : Enabled"))
                        {
                            result.Status = WindowsFeatureStatus.Enabled;
                            result.Message = $"Feature '{featureName}' is enabled";
                            DebugLogger.LogInfo($"DISM indicates {featureName} is enabled", COMPONENT);
                        }
                        else if (output.Contains("State : Disabled"))
                        {
                            result.Status = WindowsFeatureStatus.Disabled;
                            result.Message = $"Feature '{featureName}' is disabled";
                            DebugLogger.LogInfo($"DISM indicates {featureName} is disabled", COMPONENT);
                        }
                        else
                        {
                            result.Status = WindowsFeatureStatus.NotFound;
                            result.Message = $"Feature '{featureName}' state unknown";
                            DebugLogger.LogWarning($"DISM output for {featureName} does not contain recognizable state", COMPONENT);
                        }
                    }
                    else
                    {
                        if (error.Contains("feature name") && error.Contains("was not found"))
                        {
                            result.Status = WindowsFeatureStatus.NotFound;
                            result.Message = $"Feature '{featureName}' not found";
                            DebugLogger.LogWarning($"DISM indicates {featureName} was not found", COMPONENT);
                        }
                        else
                        {
                            result.Status = WindowsFeatureStatus.Error;
                            result.Message = $"DISM error: {error}";
                            DebugLogger.LogError($"DISM error for {featureName}: {error}", COMPONENT);
                        }
                    }
                }

                DebugLogger.LogMethodEnd("CheckFeatureWithDismCommandAsync", result.Status.ToString(), COMPONENT);
            }
            catch (Exception ex)
            {
                DebugLogger.LogException(ex, $"Command execution error for feature {featureName}", COMPONENT);
                result.Status = WindowsFeatureStatus.Error;
                result.Message = $"Command execution error for feature '{featureName}': {ex.Message}";
                DebugLogger.LogMethodEnd("CheckFeatureWithDismCommandAsync", $"Exception: {ex.Message}", COMPONENT);
            }

            return result;
        }

        /// <summary>
        /// Enables all required Windows features that are currently disabled
        /// </summary>
        public static async Task<WindowsFeatureCheckResult> EnableRequiredFeaturesAsync()
        {
            DebugLogger.LogMethodStart("EnableRequiredFeaturesAsync", component: COMPONENT);
            var result = new WindowsFeatureCheckResult();

            try
            {
                // First check current status
                DebugLogger.LogInfo("Checking current status of all features before enabling", COMPONENT);
                var checkResult = await CheckRequiredFeaturesAsync();

                foreach (var featureResult in checkResult.FeatureResults)
                {
                    if (featureResult.Status == WindowsFeatureStatus.Disabled)
                    {
                        DebugLogger.LogInfo($"Feature {featureResult.FeatureName} is disabled, attempting to enable", COMPONENT);
                        var enableResult = await EnableWindowsFeatureAsync(featureResult.FeatureName);
                        result.FeatureResults.Add(enableResult);
                    }
                    else
                    {
                        DebugLogger.LogInfo($"Feature {featureResult.FeatureName} is already {featureResult.Status}, no action needed", COMPONENT);
                        result.FeatureResults.Add(featureResult);
                    }
                }

                result.AllFeaturesEnabled = result.FeatureResults.All(f => f.Status == WindowsFeatureStatus.Enabled);
                DebugLogger.LogConfiguration("Final All Features Enabled", result.AllFeaturesEnabled, COMPONENT);

                var restartRequired = IsRestartRequired(result.FeatureResults);
                DebugLogger.LogConfiguration("Restart Required", restartRequired, COMPONENT);

                DebugLogger.LogMethodEnd("EnableRequiredFeaturesAsync", $"AllEnabled: {result.AllFeaturesEnabled}, RestartRequired: {restartRequired}", COMPONENT);
            }
            catch (Exception ex)
            {
                DebugLogger.LogException(ex, "Error in EnableRequiredFeaturesAsync", COMPONENT);
                result.ErrorMessage = $"Error enabling Windows features: {ex.Message}";
                DebugLogger.LogMethodEnd("EnableRequiredFeaturesAsync", $"Error: {ex.Message}", COMPONENT);
            }

            return result;
        }

        /// <summary>
        /// Enables a specific Windows feature using DISM command line
        /// </summary>
        public static async Task<WindowsFeatureResult> EnableWindowsFeatureAsync(string featureName)
        {
            DebugLogger.LogMethodStart("EnableWindowsFeatureAsync", $"featureName: {featureName}", COMPONENT);
            var result = new WindowsFeatureResult { FeatureName = featureName };

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "dism.exe",
                    Arguments = $"/English /online /enable-feature /featurename:{featureName} /all /norestart",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                DebugLogger.LogInfo($"Executing DISM enable command: {startInfo.FileName} {startInfo.Arguments}", COMPONENT);

                using (var process = new Process { StartInfo = startInfo })
                {
                    process.Start();

                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error = await process.StandardError.ReadToEndAsync();

                    await Task.Run(() => process.WaitForExit(120000)); // 2 minute timeout

                    DebugLogger.LogProcessExecution("dism.exe", startInfo.Arguments, process.ExitCode, output, error, COMPONENT);

                    if (process.ExitCode == 0)
                    {
                        result.Status = WindowsFeatureStatus.Enabled;
                        result.Message = $"Feature '{featureName}' enabled successfully";
                        result.RestartRequired = output.Contains("restart") || output.Contains("reboot");
                        DebugLogger.LogInfo($"Feature {featureName} enabled successfully", COMPONENT);
                        DebugLogger.LogConfiguration($"Restart Required for {featureName}", result.RestartRequired, COMPONENT);
                    }
                    else if (process.ExitCode == unchecked((int)0x800f0874)) // Feature already enabled
                    {
                        result.Status = WindowsFeatureStatus.Enabled;
                        result.Message = $"Feature '{featureName}' was already enabled";
                        result.RestartRequired = false;
                        DebugLogger.LogInfo($"Feature {featureName} was already enabled", COMPONENT);
                    }
                    else
                    {
                        result.Status = WindowsFeatureStatus.Error;
                        result.Message = $"Failed to enable feature '{featureName}': {error}";
                        DebugLogger.LogError($"Failed to enable feature {featureName}. Exit code: {process.ExitCode}, Error: {error}", COMPONENT);
                    }
                }

                DebugLogger.LogMethodEnd("EnableWindowsFeatureAsync", $"Status: {result.Status}, RestartRequired: {result.RestartRequired}", COMPONENT);
            }
            catch (Exception ex)
            {
                DebugLogger.LogException(ex, $"Error enabling feature {featureName}", COMPONENT);
                result.Status = WindowsFeatureStatus.Error;
                result.Message = $"Error enabling feature '{featureName}': {ex.Message}";
                DebugLogger.LogMethodEnd("EnableWindowsFeatureAsync", $"Exception: {ex.Message}", COMPONENT);
            }

            return result;
        }

        /// <summary>
        /// Checks if a restart is required after enabling features
        /// </summary>
        public static bool IsRestartRequired(List<WindowsFeatureResult> results)
        {
            DebugLogger.LogMethodStart("IsRestartRequired", $"results count: {results.Count}", COMPONENT);

            var restartRequired = results.Any(r => r.RestartRequired);

            if (restartRequired)
            {
                var featuresRequiringRestart = results.Where(r => r.RestartRequired).Select(r => r.FeatureName);
                DebugLogger.LogConfiguration("Features Requiring Restart", string.Join(", ", featuresRequiringRestart), COMPONENT);
            }

            DebugLogger.LogMethodEnd("IsRestartRequired", restartRequired.ToString(), COMPONENT);
            return restartRequired;
        }
    }
}
