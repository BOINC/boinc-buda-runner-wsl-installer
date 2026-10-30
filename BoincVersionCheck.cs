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

using Microsoft.Win32;
using System;
using System.Threading.Tasks;

namespace boinc_buda_runner_wsl_installer
{
    internal class BoincVersionCheck
    {
        private const string COMPONENT = "BoincVersionCheck";

        public enum BoincVersionStatus
        {
            Compatible,
            Incompatible,
            Unknown,
            Error
        }

        public class BoincVersionCheckResult
        {
            public BoincVersionStatus Status;
            public string Message { get; set; }
            public string ErrorMessage { get; set; }
            public string DetectedVersion { get; set; }
            public string RequiredVersion { get; internal set; }
        }

        /// <summary>
        /// Check if BOINC version is compatible (8.2.16 or later)
        /// </summary>
        /// <returns>BoincVersionCheckResult with result</returns>
        public static async Task<BoincVersionCheckResult> CheckBoincVersionAsync()
        {
            DebugLogger.LogMethodStart("CheckBoincVersionAsync", component: COMPONENT);
            var result = new BoincVersionCheckResult
            {
                RequiredVersion = "8.2.16"
            };

            try
            {
                await Task.Run(() =>
                {
                    DebugLogger.LogInfo("Opening BOINC installation registry key", COMPONENT);
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Space Sciences Laboratory, U.C. Berkeley\BOINC Setup"))
                    {
                        if (key != null)
                        {
                            DebugLogger.LogInfo("BOINC installation registry key opened successfully", COMPONENT);
                            var version = key.GetValue("UpgradingTo");
                            DebugLogger.LogConfiguration("Registry UpgradingTo Value", version?.ToString());
                            if (version != null)
                            {
                                if (Version.TryParse(version.ToString(), out var detected) && detected != null)
                                {
                                    result.DetectedVersion = version.ToString();
                                    if (Version.TryParse(result.RequiredVersion, out var required) && required != null)
                                    {
                                        if (IsVersionCompatible(detected, required))
                                        {
                                            result.Status = BoincVersionStatus.Compatible;
                                            result.Message = $"BOINC version is compatible: {version}";
                                            DebugLogger.LogInfo($"BOINC version is compatible: {version}", COMPONENT);
                                            DebugLogger.LogMethodEnd("CheckBoincVersionAsync", $"Status: {result.Status}", COMPONENT);
                                            return result;
                                        }
                                        else
                                        {
                                            result.Status = BoincVersionStatus.Incompatible;
                                            result.Message = $"BOINC version is incompatible: detected {version}, required {result.RequiredVersion}";
                                            DebugLogger.LogWarning($"BOINC version is incompatible: detected {version}, required {result.RequiredVersion}", COMPONENT);
                                            DebugLogger.LogMethodEnd("CheckBoincVersionAsync", $"Status: {result.Status}", COMPONENT);
                                            return result;
                                        }
                                    }
                                    else
                                    {
                                        // This must never happen
                                        result.Status = BoincVersionStatus.Unknown;
                                        result.Message = $"BOINC version is unknown, proceed with caution";
                                        DebugLogger.LogWarning($"Failed to parse required version: {result.RequiredVersion}", COMPONENT);
                                        DebugLogger.LogMethodEnd("CheckBoincVersionAsync", $"Status: {result.Status}", COMPONENT);
                                        return result;
                                    }
                                }
                                else
                                {
                                    result.Status = BoincVersionStatus.Unknown;
                                    result.Message = $"BOINC version is unknown, proceed with caution";
                                    DebugLogger.LogWarning($"Failed to parse BOINC version from the registry: {version.ToString()}", COMPONENT);
                                    DebugLogger.LogMethodEnd("CheckBoincVersionAsync", $"Status: {result.Status}", COMPONENT);
                                    return result;
                                }
                            }
                            else
                            {
                                result.Status = BoincVersionStatus.Unknown;
                                result.Message = $"BOINC version is unknown, proceed with caution";
                                DebugLogger.LogWarning("Failed to get BOINC version from the registry", COMPONENT);
                                DebugLogger.LogMethodEnd("CheckBoincVersionAsync", $"Status: {result.Status}", COMPONENT);
                                return result;
                            }
                        }
                        else
                        {
                            result.Status = BoincVersionStatus.Unknown;
                            result.Message = $"BOINC version is unknown, proceed with caution";
                            DebugLogger.LogWarning("Failed to open BOINC installation registry key", COMPONENT);
                            DebugLogger.LogMethodEnd("CheckBoincVersionAsync", $"Status: {result.Status}", COMPONENT);
                            return result;
                        }
                    }
                });
                DebugLogger.LogConfiguration("Final Status", result.Status, COMPONENT);
                DebugLogger.LogConfiguration("Final Message", result.Message, COMPONENT);
                DebugLogger.LogMethodEnd("CheckBoincVersionAsync", $"Status: {result.Status}", COMPONENT);
                return result;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException(ex, "Error in CheckBoincVersionAsync", COMPONENT);
                result.Status = BoincVersionStatus.Error;
                result.ErrorMessage = $"Error checking BOINC version: {ex.Message}";
                result.Message = "Failed to check BOINC version";
                DebugLogger.LogMethodEnd("CheckBoincVersionAsync", $"Status: {result.Status} (Exception)", COMPONENT);
                return result;
            }
        }

        /// <summary>
        /// Checks if detected version is compatible
        /// </summary>
        /// <param name="detected">Detected version</param>
        /// <param name="required">Required version</param>
        /// <returns>true if version is compatible, false otherwise</returns>
        private static bool IsVersionCompatible(Version detected, Version required)
        {
            if (detected == null) { return false; }
            if (required == null) { return false; }
            if (detected.Major > required.Major) { return true; }
            if (detected.Major < required.Major) { return false; }
            if (detected.Minor > required.Minor) { return true; }
            if (detected.Minor < required.Minor) { return false; }
            if (detected.Build > required.Build) { return true; }
            if (detected.Build < required.Build) { return false; }
            return true;
        }
    }
}
