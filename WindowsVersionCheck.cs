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

using Microsoft.Win32;
using System;
using System.Runtime.InteropServices;

namespace boinc_buda_runner_wsl_installer
{
    internal static class WindowsVersionCheck
    {
        private const string COMPONENT = "WindowsVersionCheck";

        // P/Invoke declarations for getting Windows version
        [DllImport("kernel32.dll")]
        private static extern bool GetVersionEx(ref OSVERSIONINFOEX osVersionInfo);

        [DllImport("ntdll.dll")]
        private static extern int RtlGetVersion(ref OSVERSIONINFOEX versionInfo);

        [StructLayout(LayoutKind.Sequential)]
        private struct OSVERSIONINFOEX
        {
            public int dwOSVersionInfoSize;
            public int dwMajorVersion;
            public int dwMinorVersion;
            public int dwBuildNumber;
            public int dwPlatformId;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szCSDVersion;
            public short wServicePackMajor;
            public short wServicePackMinor;
            public short wSuiteMask;
            public byte wProductType;
            public byte wReserved;
        }

        public enum WindowsVersionStatus
        {
            Supported,
            UnsupportedOldVersion,
            UnsupportedBuildNumber,
            UnsupportedArchitecture,
            DetectionError
        }

        public class WindowsVersionInfo
        {
            public int MajorVersion { get; set; }
            public int MinorVersion { get; set; }
            public int BuildNumber { get; set; }
            public string ProductName { get; set; }
            public string Architecture { get; set; }
            public WindowsVersionStatus Status { get; set; }
            public string StatusMessage { get; set; }
        }

        /// <summary>
        /// Gets Windows version information and validates compatibility requirements
        /// </summary>
        /// <returns>WindowsVersionInfo with version details and compatibility status</returns>
        public static WindowsVersionInfo GetWindowsVersionInfo()
        {
            DebugLogger.LogMethodStart("GetWindowsVersionInfo", component: COMPONENT);
            var info = new WindowsVersionInfo();

            try
            {
                DebugLogger.LogInfo("Attempting to get Windows version using RtlGetVersion", COMPONENT);

                // Get OS version using RtlGetVersion (more reliable than GetVersionEx)
                var osVersionInfo = new OSVERSIONINFOEX();
                osVersionInfo.dwOSVersionInfoSize = Marshal.SizeOf(osVersionInfo);

                var result = RtlGetVersion(ref osVersionInfo);
                DebugLogger.LogConfiguration("RtlGetVersion Result", result, COMPONENT);

                if (result != 0)
                {
                    DebugLogger.LogWarning("RtlGetVersion failed, falling back to registry method", COMPONENT);
                    // Fallback to registry method
                    var registryInfo = GetWindowsVersionFromRegistry();
                    DebugLogger.LogMethodEnd("GetWindowsVersionInfo", $"Registry fallback: {registryInfo.StatusMessage}", COMPONENT);
                    return registryInfo;
                }

                info.MajorVersion = osVersionInfo.dwMajorVersion;
                info.MinorVersion = osVersionInfo.dwMinorVersion;
                info.BuildNumber = osVersionInfo.dwBuildNumber;

                DebugLogger.LogConfiguration("OS Major Version", info.MajorVersion, COMPONENT);
                DebugLogger.LogConfiguration("OS Minor Version", info.MinorVersion, COMPONENT);
                DebugLogger.LogConfiguration("OS Build Number", info.BuildNumber, COMPONENT);
                DebugLogger.LogConfiguration("OS Service Pack", osVersionInfo.szCSDVersion ?? "None", COMPONENT);

                // Get architecture
                DebugLogger.LogInfo("Determining system architecture", COMPONENT);
                DebugLogger.LogConfiguration("Environment.Is64BitOperatingSystem", Environment.Is64BitOperatingSystem, COMPONENT);
                DebugLogger.LogConfiguration("PROCESSOR_ARCHITECTURE", Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE") ?? "Unknown", COMPONENT);

                info.Architecture = Environment.Is64BitOperatingSystem ? "x64" :
                                   Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE") == "ARM64" ? "ARM64" : "x86";
                DebugLogger.LogConfiguration("Detected Architecture", info.Architecture, COMPONENT);

                // Get product name from registry
                DebugLogger.LogInfo("Getting product name from registry", COMPONENT);
                info.ProductName = GetProductNameFromRegistry();
                DebugLogger.LogConfiguration("Product Name", info.ProductName, COMPONENT);

                // Validate compatibility
                DebugLogger.LogInfo("Validating Windows compatibility", COMPONENT);
                info.Status = ValidateWindowsCompatibility(info);
                info.StatusMessage = GetStatusMessage(info);

                DebugLogger.LogConfiguration("Compatibility Status", info.Status, COMPONENT);
                DebugLogger.LogConfiguration("Status Message", info.StatusMessage, COMPONENT);
                DebugLogger.LogMethodEnd("GetWindowsVersionInfo", $"Status: {info.Status}", COMPONENT);

                return info;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException(ex, "Error in GetWindowsVersionInfo", COMPONENT);
                info.Status = WindowsVersionStatus.DetectionError;
                info.StatusMessage = $"Error detecting Windows version: {ex.Message}";
                DebugLogger.LogMethodEnd("GetWindowsVersionInfo", $"Error: {ex.Message}", COMPONENT);
                return info;
            }
        }

        /// <summary>
        /// Fallback method to get Windows version from registry
        /// </summary>
        private static WindowsVersionInfo GetWindowsVersionFromRegistry()
        {
            DebugLogger.LogMethodStart("GetWindowsVersionFromRegistry", component: COMPONENT);
            var info = new WindowsVersionInfo();

            try
            {
                DebugLogger.LogInfo("Opening Windows NT CurrentVersion registry key", COMPONENT);
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    if (key != null)
                    {
                        DebugLogger.LogInfo("Registry key opened successfully", COMPONENT);

                        // Get major version
                        var majorVersion = key.GetValue("CurrentMajorVersionNumber");
                        if (majorVersion != null)
                        {
                            info.MajorVersion = Convert.ToInt32(majorVersion);
                            DebugLogger.LogConfiguration("Registry Major Version", info.MajorVersion, COMPONENT);
                        }

                        // Get minor version
                        var minorVersion = key.GetValue("CurrentMinorVersionNumber");
                        if (minorVersion != null)
                        {
                            info.MinorVersion = Convert.ToInt32(minorVersion);
                            DebugLogger.LogConfiguration("Registry Minor Version", info.MinorVersion, COMPONENT);
                        }

                        // Get build number
                        var buildNumber = key.GetValue("CurrentBuildNumber");
                        if (buildNumber != null)
                        {
                            info.BuildNumber = Convert.ToInt32(buildNumber.ToString());
                            DebugLogger.LogConfiguration("Registry Build Number", info.BuildNumber, COMPONENT);
                        }

                        // Get product name
                        info.ProductName = key.GetValue("ProductName")?.ToString() ?? "Unknown";
                        DebugLogger.LogConfiguration("Registry Product Name", info.ProductName, COMPONENT);

                        // Log additional registry values for debugging
                        try
                        {
                            var displayVersion = key.GetValue("DisplayVersion")?.ToString();
                            DebugLogger.LogConfiguration("Registry Display Version", displayVersion ?? "N/A", COMPONENT);

                            var releaseId = key.GetValue("ReleaseId")?.ToString();
                            DebugLogger.LogConfiguration("Registry Release ID", releaseId ?? "N/A", COMPONENT);

                            var currentBuild = key.GetValue("CurrentBuild")?.ToString();
                            DebugLogger.LogConfiguration("Registry Current Build", currentBuild ?? "N/A", COMPONENT);
                        }
                        catch (Exception ex)
                        {
                            DebugLogger.LogException(ex, "Error reading additional registry values", COMPONENT);
                        }
                    }
                    else
                    {
                        DebugLogger.LogError("Could not open Windows NT CurrentVersion registry key", COMPONENT);
                    }
                }

                info.Architecture = Environment.Is64BitOperatingSystem ? "x64" :
                                   Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE") == "ARM64" ? "ARM64" : "x86";
                DebugLogger.LogConfiguration("Registry Detected Architecture", info.Architecture, COMPONENT);

                info.Status = ValidateWindowsCompatibility(info);
                info.StatusMessage = GetStatusMessage(info);

                DebugLogger.LogConfiguration("Registry Compatibility Status", info.Status, COMPONENT);
                DebugLogger.LogMethodEnd("GetWindowsVersionFromRegistry", $"Status: {info.Status}", COMPONENT);

                return info;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException(ex, "Error in GetWindowsVersionFromRegistry", COMPONENT);
                info.Status = WindowsVersionStatus.DetectionError;
                info.StatusMessage = $"Registry detection failed: {ex.Message}";
                DebugLogger.LogMethodEnd("GetWindowsVersionFromRegistry", $"Error: {ex.Message}", COMPONENT);
                return info;
            }
        }

        /// <summary>
        /// Gets the product name from registry
        /// </summary>
        private static string GetProductNameFromRegistry()
        {
            DebugLogger.LogMethodStart("GetProductNameFromRegistry", component: COMPONENT);

            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    var productName = key?.GetValue("ProductName")?.ToString() ?? "Unknown Windows Version";
                    DebugLogger.LogMethodEnd("GetProductNameFromRegistry", productName, COMPONENT);
                    return productName;
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException(ex, "Error getting product name from registry", COMPONENT);
                DebugLogger.LogMethodEnd("GetProductNameFromRegistry", "Unknown Windows Version", COMPONENT);
                return "Unknown Windows Version";
            }
        }

        /// <summary>
        /// Validates Windows compatibility based on the requirements
        /// </summary>
        private static WindowsVersionStatus ValidateWindowsCompatibility(WindowsVersionInfo info)
        {
            DebugLogger.LogMethodStart("ValidateWindowsCompatibility", $"Ver: {info.MajorVersion}.{info.MinorVersion}.{info.BuildNumber}, Arch: {info.Architecture}", COMPONENT);

            WindowsVersionStatus status;

            // Windows 11 (any build) - Supported
            if (info.MajorVersion >= 10 && info.BuildNumber >= 22000)
            {
                DebugLogger.LogInfo("Detected Windows 11 - Supported", COMPONENT);
                status = WindowsVersionStatus.Supported;
            }
            // Windows 10 checks
            else if (info.MajorVersion == 10 && info.MinorVersion == 0)
            {
                DebugLogger.LogInfo("Detected Windows 10, checking architecture and build requirements", COMPONENT);

                if (info.Architecture.Equals("x64", StringComparison.OrdinalIgnoreCase))
                {
                    // Windows 10 x64: 1903 or higher (Build 18362 or higher)
                    if (info.BuildNumber >= 18362)
                    {
                        DebugLogger.LogInfo($"Windows 10 x64 build {info.BuildNumber} meets requirement (>= 18362)", COMPONENT);
                        status = WindowsVersionStatus.Supported;
                    }
                    else
                    {
                        DebugLogger.LogWarning($"Windows 10 x64 build {info.BuildNumber} does not meet requirement (>= 18362)", COMPONENT);
                        status = WindowsVersionStatus.UnsupportedBuildNumber;
                    }
                }
                else if (info.Architecture.Equals("ARM64", StringComparison.OrdinalIgnoreCase))
                {
                    // Windows 10 ARM64: 2004 or higher (Build 19041 or higher)
                    if (info.BuildNumber >= 19041)
                    {
                        DebugLogger.LogInfo($"Windows 10 ARM64 build {info.BuildNumber} meets requirement (>= 19041)", COMPONENT);
                        status = WindowsVersionStatus.Supported;
                    }
                    else
                    {
                        DebugLogger.LogWarning($"Windows 10 ARM64 build {info.BuildNumber} does not meet requirement (>= 19041)", COMPONENT);
                        status = WindowsVersionStatus.UnsupportedBuildNumber;
                    }
                }
                else
                {
                    // x86 architecture not supported
                    DebugLogger.LogError($"x86 architecture ({info.Architecture}) is not supported", COMPONENT);
                    status = WindowsVersionStatus.UnsupportedArchitecture;
                }
            }
            else
            {
                // Older than Windows 10
                DebugLogger.LogError($"Windows version {info.MajorVersion}.{info.MinorVersion} is older than Windows 10", COMPONENT);
                status = WindowsVersionStatus.UnsupportedOldVersion;
            }

            DebugLogger.LogMethodEnd("ValidateWindowsCompatibility", status.ToString(), COMPONENT);
            return status;
        }

        /// <summary>
        /// Gets a human-readable status message
        /// </summary>
        private static string GetStatusMessage(WindowsVersionInfo info)
        {
            DebugLogger.LogMethodStart("GetStatusMessage", $"Status: {info.Status}", COMPONENT);

            var versionString = $"{info.ProductName} (Build {info.BuildNumber}, {info.Architecture})";
            DebugLogger.LogConfiguration("Version String", versionString, COMPONENT);

            string message;
            switch (info.Status)
            {
                case WindowsVersionStatus.Supported:
                    message = $"Compatible: {versionString}";
                    break;

                case WindowsVersionStatus.UnsupportedOldVersion:
                    message = $"Unsupported: {versionString}. Requires Windows 10 or 11.";
                    break;

                case WindowsVersionStatus.UnsupportedBuildNumber:
                    if (info.Architecture.Equals("x64", StringComparison.OrdinalIgnoreCase))
                    {
                        message = $"Unsupported: {versionString}. Requires Windows 10 1903+ (Build 18362+) for x64.";
                    }
                    else if (info.Architecture.Equals("ARM64", StringComparison.OrdinalIgnoreCase))
                    {
                        message = $"Unsupported: {versionString}. Requires Windows 10 2004+ (Build 19041+) for ARM64.";
                    }
                    else
                    {
                        message = $"Unsupported build: {versionString}";
                    }
                    break;

                case WindowsVersionStatus.UnsupportedArchitecture:
                    message = $"Unsupported: {versionString}. x86 architecture not supported.";
                    break;

                case WindowsVersionStatus.DetectionError:
                    message = $"Detection Error: {info.StatusMessage}";
                    break;

                default:
                    message = $"Unknown status: {versionString}";
                    break;
            }

            DebugLogger.LogMethodEnd("GetStatusMessage", message, COMPONENT);
            return message;
        }
    }
}
