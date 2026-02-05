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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace boinc_buda_runner_wsl_installer
{
    internal class WslCheck
    {
        private const string COMPONENT = "WslCheck";

        public enum WslStatus
        {
            NotInstalled,
            OutdatedVersion,
            CorrectVersion,
            WrongDefaultVersion,
            AllGood,
            Error
        }

        public class WslVersionInfo
        {
            public string WslVersion { get; set; }
        }

        public class WslStatusInfo
        {
            public int DefaultVersion { get; set; }
        }

        public class WslCheckResult
        {
            public WslStatus Status { get; set; }
            public string Message { get; set; }
            public WslVersionInfo VersionInfo { get; set; }
            public WslStatusInfo StatusInfo { get; set; }
            public string LatestVersion { get; set; }
            public string DownloadUrl { get; set; }
            public string DownloadSha256 { get; set; }
            public bool UpdateRequired { get; set; }
            public bool VersionChangeRequired { get; set; }
        }

        public class WslDownloadInfo
        {
            public string DownloadUrl { get; set; }
            public string Sha256 { get; set; }
            public string FileName { get; set; }
        }

        /// <summary>
        /// Performs a comprehensive WSL check including version, status, and update requirements
        /// </summary>
        public static async Task<WslCheckResult> CheckWslAsync()
        {
            DebugLogger.LogMethodStart("CheckWslAsync", component: COMPONENT);
            var result = new WslCheckResult();

            try
            {
                // Step 1: Check if WSL is installed and get version
                DebugLogger.LogInfo("Step 1: Checking if WSL is installed and getting version", COMPONENT);
                result.VersionInfo = await GetWslVersionAsync();
                if (result.VersionInfo == null)
                {
                    DebugLogger.LogWarning("WSL is not installed on this system", COMPONENT);
                    result.Status = WslStatus.NotInstalled;
                    result.Message = "WSL is not installed on this system";
                    DebugLogger.LogMethodEnd("CheckWslAsync", $"Status: {result.Status}", COMPONENT);
                    return result;
                }

                DebugLogger.LogConfiguration("WSL Version", result.VersionInfo.WslVersion ?? "Unknown", COMPONENT);

                // Step 2: Check WSL status (default distribution and version)
                DebugLogger.LogInfo("Step 2: Checking WSL status (default distribution and version)", COMPONENT);
                result.StatusInfo = await GetWslStatusAsync();
                if (result.StatusInfo != null)
                {
                    DebugLogger.LogConfiguration("Default Version", result.StatusInfo.DefaultVersion, COMPONENT);
                }
                else
                {
                    DebugLogger.LogWarning("Could not get WSL status information", COMPONENT);
                }

                // Step 3: Get latest version from GitHub
                DebugLogger.LogInfo("Step 3: Getting latest version from GitHub", COMPONENT);
                result.LatestVersion = await GetLatestWslVersionFromGitHubAsync();
                var downloadInfo = await GetLatestWslDownloadInfoAsync();
                result.DownloadUrl = downloadInfo?.DownloadUrl;
                result.DownloadSha256 = downloadInfo?.Sha256;
                DebugLogger.LogConfiguration("Latest Version", result.LatestVersion, COMPONENT);
                DebugLogger.LogConfiguration("Download URL", result.DownloadUrl, COMPONENT);
                DebugLogger.LogConfiguration("Download SHA256", result.DownloadSha256 ?? "null", COMPONENT);

                // Step 4: Compare versions and determine status
                DebugLogger.LogInfo("Step 4: Comparing versions and determining requirements", COMPONENT);
                result.UpdateRequired = IsUpdateRequired(result.VersionInfo.WslVersion, result.LatestVersion);
                result.VersionChangeRequired = result.StatusInfo?.DefaultVersion != 2;
                DebugLogger.LogConfiguration("Update Required", result.UpdateRequired, COMPONENT);
                DebugLogger.LogConfiguration("Version Change Required", result.VersionChangeRequired, COMPONENT);

                // Step 5: Determine overall status
                DebugLogger.LogInfo("Step 5: Determining overall status", COMPONENT);
                if (result.UpdateRequired)
                {
                    result.Status = WslStatus.OutdatedVersion;
                    result.Message = $"WSL version {result.VersionInfo.WslVersion} is outdated. Latest version is {result.LatestVersion}";
                    DebugLogger.LogInfo($"WSL is outdated: {result.VersionInfo.WslVersion} -> {result.LatestVersion}", COMPONENT);
                }
                else if (result.VersionChangeRequired)
                {
                    result.Status = WslStatus.WrongDefaultVersion;
                    result.Message = $"WSL default version is {result.StatusInfo.DefaultVersion}, but should be 2";
                    DebugLogger.LogInfo($"WSL default version needs change: {result.StatusInfo.DefaultVersion} -> 2", COMPONENT);
                }
                else
                {
                    result.Status = WslStatus.AllGood;
                    result.Message = $"WSL version {result.VersionInfo.WslVersion} is up to date and configured correctly";
                    DebugLogger.LogInfo("WSL is up to date and properly configured", COMPONENT);
                }

                DebugLogger.LogMethodEnd("CheckWslAsync", $"Status: {result.Status}", COMPONENT);
                return result;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException(ex, "Error in CheckWslAsync", COMPONENT);
                result.Status = WslStatus.Error;
                result.Message = $"Error checking WSL: {ex.Message}";
                DebugLogger.LogMethodEnd("CheckWslAsync", $"Status: {result.Status} (Exception)", COMPONENT);
                return result;
            }
        }

        /// <summary>
        /// Gets WSL version information by executing 'wsl --version'
        /// </summary>
        public static async Task<WslVersionInfo> GetWslVersionAsync()
        {
            DebugLogger.LogMethodStart("GetWslVersionAsync", component: COMPONENT);

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "wsl.exe",
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    StandardOutputEncoding = Encoding.Unicode,
                    StandardErrorEncoding = Encoding.Unicode
                };

                using (var process = new Process { StartInfo = startInfo })
                {
                    process.Start();

                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error = await process.StandardError.ReadToEndAsync();

                    await Task.Run(() => process.WaitForExit(10000)); // 10 second timeout

                    DebugLogger.LogProcessExecution("wsl.exe", "--version", process.ExitCode, output, error, COMPONENT);

                    if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
                    {
                        var versionInfo = ParseWslVersionOutput(output);
                        DebugLogger.LogMethodEnd("GetWslVersionAsync", $"Success: {versionInfo.WslVersion}", COMPONENT);
                        return versionInfo;
                    }
                    else
                    {
                        // WSL might not be installed or version command not supported
                        DebugLogger.LogWarning("WSL version command failed or returned empty output", COMPONENT);
                        DebugLogger.LogMethodEnd("GetWslVersionAsync", "null (failed)", COMPONENT);
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException(ex, "Error getting WSL version", COMPONENT);
                DebugLogger.LogMethodEnd("GetWslVersionAsync", "null (exception)", COMPONENT);
                return null;
            }
        }

        /// <summary>
        /// Gets WSL status information by executing 'wsl --status'
        /// </summary>
        public static async Task<WslStatusInfo> GetWslStatusAsync()
        {
            DebugLogger.LogMethodStart("GetWslStatusAsync", component: COMPONENT);

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "wsl.exe",
                    Arguments = "--status",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    StandardOutputEncoding = Encoding.Unicode,
                    StandardErrorEncoding = Encoding.Unicode
                };

                using (var process = new Process { StartInfo = startInfo })
                {
                    process.Start();

                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error = await process.StandardError.ReadToEndAsync();

                    await Task.Run(() => process.WaitForExit(10000)); // 10 second timeout

                    DebugLogger.LogProcessExecution("wsl.exe", "--status", process.ExitCode, output, error, COMPONENT);

                    if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
                    {
                        var statusInfo = ParseWslStatusOutput(output);
                        DebugLogger.LogMethodEnd("GetWslStatusAsync", $"Success: DefaultVersion={statusInfo.DefaultVersion}", COMPONENT);
                        return statusInfo;
                    }
                    else
                    {
                        DebugLogger.LogWarning("WSL status command failed or returned empty output", COMPONENT);
                        DebugLogger.LogMethodEnd("GetWslStatusAsync", "null (failed)", COMPONENT);
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException(ex, "Error getting WSL status", COMPONENT);
                DebugLogger.LogMethodEnd("GetWslStatusAsync", "null (exception)", COMPONENT);
                return null;
            }
        }

        /// <summary>
        /// Gets the latest WSL version from GitHub releases API using simple string parsing
        /// </summary>
        public static async Task<string> GetLatestWslVersionFromGitHubAsync()
        {
            DebugLogger.LogMethodStart("GetLatestWslVersionFromGitHubAsync", component: COMPONENT);

            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "BOINC-BUDA-WSL-Installer");

                    DebugLogger.LogInfo("Fetching latest WSL version from GitHub", COMPONENT);
                    var response = await httpClient.GetStringAsync("https://api.github.com/repos/microsoft/WSL/releases/latest");

                    // Simple regex parsing for tag_name since System.Text.Json is not available in .NET Framework 4.8
                    var tagNameMatch = Regex.Match(response, @"""tag_name"":\s*""([^""]+)""");
                    if (tagNameMatch.Success)
                    {
                        var version = tagNameMatch.Groups[1].Value.TrimStart('v'); // Remove 'v' prefix if present
                        DebugLogger.LogConfiguration("GitHub WSL Latest Version", version, COMPONENT);
                        DebugLogger.LogMethodEnd("GetLatestWslVersionFromGitHubAsync", version, COMPONENT);
                        return version;
                    }
                    else
                    {
                        DebugLogger.LogWarning("Could not parse tag_name from GitHub response", COMPONENT);
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException(ex, "Error fetching latest WSL version from GitHub", COMPONENT);
            }

            DebugLogger.LogMethodEnd("GetLatestWslVersionFromGitHubAsync", "Unknown", COMPONENT);
            return "Unknown";
        }

        /// <summary>
        /// Gets the download URL for the latest WSL installer based on OS architecture
        /// </summary>
        public static async Task<string> GetLatestWslDownloadUrlAsync()
        {
            var info = await GetLatestWslDownloadInfoAsync();
            return info?.DownloadUrl;
        }

        /// <summary>
        /// Gets the download URL and expected SHA256 hash for the latest WSL installer
        /// </summary>
        public static async Task<WslDownloadInfo> GetLatestWslDownloadInfoAsync()
        {
            DebugLogger.LogMethodStart("GetLatestWslDownloadInfoAsync", component: COMPONENT);

            try
            {
                string architecture = GetSystemArchitecture();
                DebugLogger.LogConfiguration("Detected Architecture", architecture, COMPONENT);

                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "BOINC-BUDA-WSL-Installer");

                    DebugLogger.LogInfo("Fetching WSL download URLs from GitHub", COMPONENT);
                    var response = await httpClient.GetStringAsync("https://api.github.com/repos/microsoft/WSL/releases/latest");

                    var downloadMatches = Regex.Matches(response, @"""browser_download_url"":\s*""([^""]+\.(?:msi|msix))""");
                    DebugLogger.LogConfiguration("Download Matches Found", downloadMatches.Count, COMPONENT);

                    if (downloadMatches.Count > 0)
                    {
                        string preferredUrl = FindArchitectureSpecificInstaller(downloadMatches, architecture);
                        if (!string.IsNullOrEmpty(preferredUrl))
                        {
                            var info = BuildWslDownloadInfo(response, preferredUrl);
                            DebugLogger.LogConfiguration("Architecture-Specific URL", preferredUrl, COMPONENT);
                            DebugLogger.LogMethodEnd("GetLatestWslDownloadInfoAsync", preferredUrl, COMPONENT);
                            return info;
                        }

                        var fallbackUrl = downloadMatches[0].Groups[1].Value;
                        var fallbackInfo = BuildWslDownloadInfo(response, fallbackUrl);
                        DebugLogger.LogConfiguration("Fallback URL", fallbackUrl, COMPONENT);
                        DebugLogger.LogMethodEnd("GetLatestWslDownloadInfoAsync", fallbackUrl, COMPONENT);
                        return fallbackInfo;
                    }

                    DebugLogger.LogWarning("No WSL installer download URLs found in GitHub response", COMPONENT);
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException(ex, "Error fetching WSL download URL from GitHub", COMPONENT);
            }

            var storeUrl = "ms-windows-store://pdp/?ProductId=9P9TQF7MRM4R";
            DebugLogger.LogConfiguration("Using Microsoft Store Fallback", storeUrl, COMPONENT);
            DebugLogger.LogMethodEnd("GetLatestWslDownloadInfoAsync", storeUrl, COMPONENT);
            return new WslDownloadInfo { DownloadUrl = storeUrl, Sha256 = null, FileName = null };
        }

        /// <summary>
        /// Gets the system architecture using the same method as WindowsVersionCheck
        /// </summary>
        private static string GetSystemArchitecture()
        {
            DebugLogger.LogMethodStart("GetSystemArchitecture", component: COMPONENT);

            try
            {
                DebugLogger.LogConfiguration("Environment.Is64BitOperatingSystem", Environment.Is64BitOperatingSystem, COMPONENT);

                if (Environment.Is64BitOperatingSystem)
                {
                    // Check if it's ARM64 or x64
                    var processorArch = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");
                    DebugLogger.LogConfiguration("PROCESSOR_ARCHITECTURE", processorArch ?? "null", COMPONENT);

                    if (processorArch != null && processorArch.Equals("ARM64", StringComparison.OrdinalIgnoreCase))
                    {
                        DebugLogger.LogMethodEnd("GetSystemArchitecture", "ARM64", COMPONENT);
                        return "ARM64";
                    }
                    DebugLogger.LogMethodEnd("GetSystemArchitecture", "x64", COMPONENT);
                    return "x64";
                }
                else
                {
                    DebugLogger.LogMethodEnd("GetSystemArchitecture", "x86", COMPONENT);
                    return "x86";
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException(ex, "Error detecting system architecture, defaulting to x64", COMPONENT);
                DebugLogger.LogMethodEnd("GetSystemArchitecture", "x64 (default)", COMPONENT);
                return "x64";
            }
        }

        /// <summary>
        /// Finds the most appropriate installer based on system architecture
        /// </summary>
        private static string FindArchitectureSpecificInstaller(MatchCollection downloadMatches, string architecture)
        {
            DebugLogger.LogMethodStart("FindArchitectureSpecificInstaller", $"architecture: {architecture}, matches: {downloadMatches.Count}", COMPONENT);

            string fallbackUrl = null;
            string preferredUrl = null;

            foreach (Match match in downloadMatches)
            {
                string url = match.Groups[1].Value;
                string filename = Path.GetFileName(url).ToLowerInvariant();
                DebugLogger.LogDebug($"Evaluating installer: {filename}", COMPONENT);

                // Store the first URL as fallback
                if (fallbackUrl == null)
                {
                    fallbackUrl = url;
                    DebugLogger.LogConfiguration("Fallback URL Set", fallbackUrl, COMPONENT);
                }

                // Architecture-specific matching
                switch (architecture.ToUpperInvariant())
                {
                    case "ARM64":
                        // Look for ARM64 specific installers
                        if (filename.Contains("arm64") || filename.Contains("arm"))
                        {
                            DebugLogger.LogInfo($"Found ARM64-specific installer: {filename}", COMPONENT);
                            DebugLogger.LogMethodEnd("FindArchitectureSpecificInstaller", url, COMPONENT);
                            return url; // Return immediately if exact match found
                        }
                        // If no ARM64 specific, prefer x64 over x86
                        if (preferredUrl == null && (filename.Contains("x64") || filename.Contains("amd64")))
                        {
                            preferredUrl = url;
                            DebugLogger.LogInfo($"Set x64 as preferred for ARM64: {filename}", COMPONENT);
                        }
                        break;

                    case "X64":
                        // Look for x64 specific installers
                        if (filename.Contains("x64") || filename.Contains("amd64"))
                        {
                            DebugLogger.LogInfo($"Found x64-specific installer: {filename}", COMPONENT);
                            DebugLogger.LogMethodEnd("FindArchitectureSpecificInstaller", url, COMPONENT);
                            return url; // Return immediately if exact match found
                        }
                        // Avoid ARM64 installers for x64 systems
                        if (preferredUrl == null && !filename.Contains("arm"))
                        {
                            preferredUrl = url;
                            DebugLogger.LogInfo($"Set as preferred for x64 (non-ARM): {filename}", COMPONENT);
                        }
                        break;

                    default:
                        DebugLogger.LogDebug($"No specific matching for architecture: {architecture}", COMPONENT);
                        break;
                }
            }

            // Return preferred URL if found, otherwise fallback
            var result = preferredUrl ?? fallbackUrl;
            DebugLogger.LogConfiguration("Final Selected URL", result ?? "null", COMPONENT);
            DebugLogger.LogMethodEnd("FindArchitectureSpecificInstaller", result ?? "null", COMPONENT);
            return result;
        }

        /// <summary>
        /// Downloads and installs the latest WSL version using msiexec.exe
        /// </summary>
        public static async Task<bool> DownloadAndInstallLatestWslAsync(string downloadUrl, string expectedSha256, IProgress<string> progress = null)
        {
            DebugLogger.LogMethodStart("DownloadAndInstallLatestWslAsync", $"downloadUrl: {downloadUrl}", COMPONENT);

            try
            {
                progress?.Report("Downloading latest WSL installer...");

                var tempPath = Path.GetTempPath();
                var fileName = Path.GetFileName(new Uri(downloadUrl).LocalPath);
                var installerPath = Path.Combine(tempPath, fileName);

                DebugLogger.LogConfiguration("Temp Path", tempPath, COMPONENT);
                DebugLogger.LogConfiguration("File Name", fileName, COMPONENT);
                DebugLogger.LogConfiguration("Installer Path", installerPath, COMPONENT);

                // Download the installer
                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromMinutes(10); // Set reasonable timeout
                    DebugLogger.LogConfiguration("HTTP Timeout", "10 minutes", COMPONENT);

                    DebugLogger.LogInfo("Starting WSL installer download", COMPONENT);
                    var response = await httpClient.GetAsync(downloadUrl);
                    response.EnsureSuccessStatusCode();
                    DebugLogger.LogConfiguration("Download Response Status", response.StatusCode.ToString(), COMPONENT);

                    using (var fileStream = new FileStream(installerPath, FileMode.Create))
                    {
                        await response.Content.CopyToAsync(fileStream);
                    }

                    // Verify download
                    if (File.Exists(installerPath))
                    {
                        var fileInfo = new FileInfo(installerPath);
                        DebugLogger.LogConfiguration("Downloaded File Size", $"{fileInfo.Length} bytes", COMPONENT);
                    }
                }

                if (string.IsNullOrWhiteSpace(expectedSha256))
                {
                    DebugLogger.LogError("WSL installer SHA256 hash was not available from GitHub release data", COMPONENT);
                    progress?.Report("WSL installer verification data unavailable; aborting for safety.");
                    DebugLogger.LogMethodEnd("DownloadAndInstallLatestWslAsync", "false (missing hash)", COMPONENT);
                    return false;
                }

                progress?.Report("Verifying WSL installer integrity...");
                var actualSha256 = DownloadVerification.ComputeSha256(installerPath);
                DebugLogger.LogConfiguration("Expected SHA256", expectedSha256, COMPONENT);
                DebugLogger.LogConfiguration("Actual SHA256", actualSha256 ?? "null", COMPONENT);

                if (!string.Equals(DownloadVerification.NormalizeHash(actualSha256), DownloadVerification.NormalizeHash(expectedSha256), StringComparison.OrdinalIgnoreCase))
                {
                    DebugLogger.LogError("WSL installer hash verification failed", COMPONENT);
                    progress?.Report("WSL installer verification failed. Downloaded file hash does not match.");
                    try
                    {
                        File.Delete(installerPath);
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.LogException(ex, "Error deleting installer after hash mismatch", COMPONENT);
                    }
                    DebugLogger.LogMethodEnd("DownloadAndInstallLatestWslAsync", "false (hash mismatch)", COMPONENT);
                    return false;
                }

                var normalizedHash = DownloadVerification.NormalizeHash(actualSha256);
                var systemRoot = Environment.GetEnvironmentVariable("SystemRoot") ?? Environment.GetEnvironmentVariable("windir");
                if (string.IsNullOrWhiteSpace(systemRoot))
                {
                    throw new InvalidOperationException("SystemRoot environment variable not found.");
                }

                var downloadRoot = Path.Combine(systemRoot, "Downloaded Installations");
                var hashFolder = Path.Combine(downloadRoot, normalizedHash);
                Directory.CreateDirectory(hashFolder);

                var finalInstallerPath = Path.Combine(hashFolder, fileName);
                if (File.Exists(finalInstallerPath))
                {
                    File.Delete(finalInstallerPath);
                }

                File.Move(installerPath, finalInstallerPath);
                installerPath = finalInstallerPath;

                DebugLogger.LogConfiguration("Installer Hash Folder", hashFolder, COMPONENT);
                DebugLogger.LogConfiguration("Installer Final Path", installerPath, COMPONENT);

                progress?.Report("Installing WSL update...");

                // Determine the installer type and appropriate arguments
                var fileExtension = Path.GetExtension(fileName).ToLowerInvariant();
                DebugLogger.LogConfiguration("File Extension", fileExtension, COMPONENT);
                ProcessStartInfo startInfo;

                if (fileExtension == ".msi")
                {
                    DebugLogger.LogInfo("Using msiexec.exe for MSI installation", COMPONENT);
                    // Use msiexec.exe for MSI files
                    startInfo = new ProcessStartInfo
                    {
                        FileName = @"C:\Windows\System32\msiexec.exe",
                        Arguments = $"/i \"{installerPath}\" /quiet /norestart",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        Verb = "runas" // Run as administrator
                    };
                }
                else if (fileExtension == ".msix")
                {
                    DebugLogger.LogInfo("Using PowerShell for MSIX installation", COMPONENT);
                    // Use PowerShell for MSIX files (Windows 10/11 App Installer)
                    startInfo = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-Command \"Add-AppxPackage -Path '{installerPath}'\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        Verb = "runas" // Run as administrator
                    };
                }
                else
                {
                    DebugLogger.LogWarning($"Unknown installer type ({fileExtension}), attempting direct execution", COMPONENT);
                    // Fallback to direct execution for other file types
                    progress?.Report($"Warning: Unknown installer type ({fileExtension}), attempting direct execution...");
                    startInfo = new ProcessStartInfo
                    {
                        FileName = installerPath,
                        Arguments = "/quiet",
                        UseShellExecute = true,
                        Verb = "runas", // Run as administrator
                        WindowStyle = ProcessWindowStyle.Hidden
                    };
                }

                DebugLogger.LogInfo($"Executing installer: {startInfo.FileName} {startInfo.Arguments}", COMPONENT);

                // Execute the installer
                using (var process = new Process { StartInfo = startInfo })
                {
                    process.Start();

                    // For msiexec and PowerShell, we can capture output
                    if (fileExtension == ".msi" || fileExtension == ".msix")
                    {
                        var output = await process.StandardOutput.ReadToEndAsync();
                        var error = await process.StandardError.ReadToEndAsync();

                        await Task.Run(() => process.WaitForExit(300000)); // 5 minute timeout

                        DebugLogger.LogProcessExecution(startInfo.FileName, startInfo.Arguments, process.ExitCode, output, error, COMPONENT);

                        // Log any errors for debugging (optional)
                        if (!string.IsNullOrEmpty(error))
                        {
                            progress?.Report($"Installation process reported: {error.Trim()}");
                        }
                    }
                    else
                    {
                        await Task.Run(() => process.WaitForExit(300000)); // 5 minute timeout
                        DebugLogger.LogConfiguration("Process Exit Code", process.ExitCode, COMPONENT);
                    }

                    // Check exit code for success
                    if (process.ExitCode == 0)
                    {
                        DebugLogger.LogInfo("WSL installation completed successfully", COMPONENT);
                        progress?.Report("WSL installation completed successfully");
                        DebugLogger.LogMethodEnd("DownloadAndInstallLatestWslAsync", "true", COMPONENT);
                        return true;
                    }
                    else
                    {
                        DebugLogger.LogError($"WSL installation failed with exit code: {process.ExitCode}", COMPONENT);
                        progress?.Report($"Installation failed with exit code: {process.ExitCode}");
                        DebugLogger.LogMethodEnd("DownloadAndInstallLatestWslAsync", "false", COMPONENT);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException(ex, "Error in DownloadAndInstallLatestWslAsync", COMPONENT);
                progress?.Report($"Installation error: {ex.Message}");
                DebugLogger.LogMethodEnd("DownloadAndInstallLatestWslAsync", "false (exception)", COMPONENT);
                return false;
            }
        }

        /// <summary>
        /// Sets the default WSL version to 2
        /// </summary>
        public static async Task<bool> SetDefaultWslVersionTo2Async()
        {
            DebugLogger.LogMethodStart("SetDefaultWslVersionTo2Async", component: COMPONENT);

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "wsl.exe",
                    Arguments = "--set-default-version 2",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    StandardOutputEncoding = Encoding.Unicode,
                    StandardErrorEncoding = Encoding.Unicode
                };

                using (var process = new Process { StartInfo = startInfo })
                {
                    process.Start();

                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error = await process.StandardError.ReadToEndAsync();

                    await Task.Run(() => process.WaitForExit(30000)); // 30 second timeout

                    DebugLogger.LogProcessExecution("wsl.exe", "--set-default-version 2", process.ExitCode, output, error, COMPONENT);

                    var success = process.ExitCode == 0;
                    DebugLogger.LogMethodEnd("SetDefaultWslVersionTo2Async", success.ToString(), COMPONENT);
                    return success;
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException(ex, "Error setting WSL default version to 2", COMPONENT);
                DebugLogger.LogMethodEnd("SetDefaultWslVersionTo2Async", "false (exception)", COMPONENT);
                return false;
            }
        }

        /// <summary>
        /// Performs all necessary WSL fixes (update and set version 2)
        /// </summary>
        public static async Task<bool> FixWslIssuesAsync(WslCheckResult checkResult, IProgress<string> progress = null)
        {
            DebugLogger.LogMethodStart("FixWslIssuesAsync", $"UpdateRequired: {checkResult.UpdateRequired}, VersionChangeRequired: {checkResult.VersionChangeRequired}", COMPONENT);

            try
            {
                bool success = true;

                // Update WSL if needed
                if (checkResult.UpdateRequired && !string.IsNullOrEmpty(checkResult.DownloadUrl))
                {
                    DebugLogger.LogInfo("Updating WSL to latest version", COMPONENT);
                    progress?.Report("Updating WSL to latest version...");
                    success = await DownloadAndInstallLatestWslAsync(checkResult.DownloadUrl, checkResult.DownloadSha256, progress);

                    if (!success)
                    {
                        DebugLogger.LogError("Failed to update WSL", COMPONENT);
                        progress?.Report("Failed to update WSL");
                        DebugLogger.LogMethodEnd("FixWslIssuesAsync", "false (update failed)", COMPONENT);
                        return false;
                    }
                    DebugLogger.LogInfo("WSL update completed successfully", COMPONENT);
                }

                // Set default version to 2 if needed
                if (checkResult.VersionChangeRequired)
                {
                    DebugLogger.LogInfo("Setting WSL default version to 2", COMPONENT);
                    progress?.Report("Setting WSL default version to 2...");
                    success = await SetDefaultWslVersionTo2Async();

                    if (!success)
                    {
                        DebugLogger.LogError("Failed to set WSL default version to 2", COMPONENT);
                        progress?.Report("Failed to set WSL default version to 2");
                        DebugLogger.LogMethodEnd("FixWslIssuesAsync", "false (version change failed)", COMPONENT);
                        return false;
                    }
                    DebugLogger.LogInfo("WSL default version set to 2 successfully", COMPONENT);
                }

                DebugLogger.LogInfo("WSL configuration completed successfully", COMPONENT);
                progress?.Report("WSL configuration completed successfully");
                DebugLogger.LogMethodEnd("FixWslIssuesAsync", "true", COMPONENT);
                return true;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException(ex, "Error in FixWslIssuesAsync", COMPONENT);
                progress?.Report("Error occurred during WSL configuration");
                DebugLogger.LogMethodEnd("FixWslIssuesAsync", "false (exception)", COMPONENT);
                return false;
            }
        }

        /// <summary>
        /// Parses the output of 'wsl --version' command
        /// </summary>
        private static WslVersionInfo ParseWslVersionOutput(string output)
        {
            DebugLogger.LogMethodStart("ParseWslVersionOutput", $"output length: {output?.Length ?? 0}", COMPONENT);
            var versionInfo = new WslVersionInfo();

            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            DebugLogger.LogConfiguration("Output Lines Count", lines.Length, COMPONENT);

            foreach (var line in lines)
            {
                DebugLogger.LogDebug($"Parsing line: {line}", COMPONENT);

                if (line.Contains("WSL") && !line.Contains("WSLg"))
                {
                    versionInfo.WslVersion = ExtractVersionValue(line);
                    break;
                }
            }

            DebugLogger.LogConfiguration("Parsed WSL Version", versionInfo.WslVersion ?? "null", COMPONENT);
            DebugLogger.LogMethodEnd("ParseWslVersionOutput", $"WSL Version: {versionInfo.WslVersion}", COMPONENT);
            return versionInfo;
        }

        /// <summary>
        /// Parses the output of 'wsl --status' command
        /// </summary>
        private static WslStatusInfo ParseWslStatusOutput(string output)
        {
            DebugLogger.LogMethodStart("ParseWslStatusOutput", $"output length: {output?.Length ?? 0}", COMPONENT);
            var statusInfo = new WslStatusInfo();

            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            DebugLogger.LogConfiguration("Status Output Lines Count", lines.Length, COMPONENT);

            foreach (var line in lines)
            {
                DebugLogger.LogDebug($"Parsing status line: {line}", COMPONENT);

                var versionStr = ExtractVersionValue(line);
                if (int.TryParse(versionStr, out var version))
                    if (version == 1 || version == 2)
                        statusInfo.DefaultVersion = version;
            }

            DebugLogger.LogConfiguration("Parsed Default Version", statusInfo.DefaultVersion, COMPONENT);
            DebugLogger.LogMethodEnd("ParseWslStatusOutput", $"DefaultVersion: {statusInfo.DefaultVersion}", COMPONENT);
            return statusInfo;
        }

        /// <summary>
        /// Extracts version value from a line like "Version: 1.2.3"
        /// </summary>
        private static string ExtractVersionValue(string line)
        {
            var colonIndex = line.IndexOf(':');
            if (colonIndex >= 0 && colonIndex < line.Length - 1)
            {
                return line.Substring(colonIndex + 1).Trim();
            }
            return string.Empty;
        }

        /// <summary>
        /// Determines if an update is required by comparing version strings
        /// </summary>
        private static bool IsUpdateRequired(string currentVersion, string latestVersion)
        {
            DebugLogger.LogMethodStart("IsUpdateRequired", $"current: {currentVersion}, latest: {latestVersion}", COMPONENT);

            if (string.IsNullOrEmpty(currentVersion) || string.IsNullOrEmpty(latestVersion) || latestVersion == "Unknown")
            {
                DebugLogger.LogMethodEnd("IsUpdateRequired", "true (invalid versions)", COMPONENT);
                return true;
            }

            try
            {
                // Simple version comparison - can be enhanced for more complex scenarios
                var current = new Version(NormalizeVersionString(currentVersion));
                var latest = new Version(NormalizeVersionString(latestVersion));

                DebugLogger.LogConfiguration("Normalized Current", current.ToString(), COMPONENT);
                DebugLogger.LogConfiguration("Normalized Latest", latest.ToString(), COMPONENT);

                var updateRequired = current < latest;
                DebugLogger.LogMethodEnd("IsUpdateRequired", updateRequired.ToString(), COMPONENT);
                return updateRequired;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException(ex, "Error comparing versions, forcing the update", COMPONENT);
                DebugLogger.LogMethodEnd("IsUpdateRequired", "true (exception)", COMPONENT);
                return true;
            }
        }

        /// <summary>
        /// Normalizes version string to ensure it's compatible with System.Version
        /// </summary>
        private static string NormalizeVersionString(string version)
        {
            DebugLogger.LogMethodStart("NormalizeVersionString", $"version: {version}", COMPONENT);

            // Remove any non-numeric characters except dots
            var normalized = Regex.Replace(version, @"[^0-9.]", "");
            DebugLogger.LogDebug($"After regex: {normalized}", COMPONENT);

            // Ensure we have at least major.minor format
            var parts = normalized.Split('.');
            string result;

            if (parts.Length == 1)
                result = $"{parts[0]}.0";
            else if (parts.Length == 2)
                result = $"{parts[0]}.{parts[1]}";
            else
                // Take only first 4 parts (major.minor.build.revision)
                result = string.Join(".", parts.Take(4));

            DebugLogger.LogMethodEnd("NormalizeVersionString", result, COMPONENT);
            return result;
        }

        /// <summary>
        /// Gets user-friendly status message for display purposes
        /// </summary>
        public static string GetStatusDisplayMessage(WslCheckResult result)
        {
            DebugLogger.LogMethodStart("GetStatusDisplayMessage", $"status: {result.Status}", COMPONENT);

            string message;
            switch (result.Status)
            {
                case WslStatus.NotInstalled:
                    message = "WSL is not installed";
                    break;
                case WslStatus.OutdatedVersion:
                    message = $"WSL needs update (current: {result.VersionInfo?.WslVersion}, latest: {result.LatestVersion})";
                    break;
                case WslStatus.WrongDefaultVersion:
                    message = $"WSL default version is {result.StatusInfo?.DefaultVersion}, should be 2";
                    break;
                case WslStatus.AllGood:
                    message = $"WSL {result.VersionInfo?.WslVersion} is up to date and properly configured";
                    break;
                case WslStatus.Error:
                    message = "Error checking WSL status";
                    break;
                default:
                    message = "Unknown WSL status";
                    break;
            }

            DebugLogger.LogMethodEnd("GetStatusDisplayMessage", message, COMPONENT);
            return message;
        }

        private static WslDownloadInfo BuildWslDownloadInfo(string releaseJson, string url)
        {
            var fileName = Path.GetFileName(new Uri(url).LocalPath);
            var sha256 = DownloadVerification.TryGetSha256FromReleaseJson(releaseJson, url);

            if (string.IsNullOrEmpty(sha256))
            {
                DebugLogger.LogWarning($"Could not find SHA256 hash for {fileName} in GitHub release data", COMPONENT);
            }

            return new WslDownloadInfo
            {
                DownloadUrl = url,
                Sha256 = sha256,
                FileName = fileName
            };
        }

        
    }
}
