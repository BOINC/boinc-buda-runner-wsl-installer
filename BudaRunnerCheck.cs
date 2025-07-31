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
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Linq;
using System.Text;

namespace boinc_buda_runner_wsl_installer
{
    internal class BudaRunnerCheck
    {
        private const string COMPONENT = "BudaRunnerCheck";

        public enum BudaRunnerStatus
        {
            NotInstalled,
            InstalledUpToDate,
            InstalledOutdated,
            InstalledNoVersion,
            InstallationRequired,
            Error
        }

        public class BudaRunnerVersionInfo
        {
            public string CurrentVersion { get; set; }
            public string LatestVersion { get; set; }
            public bool IsInstalled { get; set; }
            public bool HasVersionFile { get; set; }
        }

        public class BudaRunnerCheckResult
        {
            public BudaRunnerStatus Status { get; set; }
            public string Message { get; set; }
            public BudaRunnerVersionInfo VersionInfo { get; set; } = new BudaRunnerVersionInfo();
            public string DownloadUrl { get; set; }
            public string ErrorMessage { get; set; }
            public bool UpdateRequired { get; set; }
            public bool InitialSetupRequired { get; set; }
        }

        private const string BUDA_RUNNER_IMAGE_NAME = "boinc-buda-runner";
        private const string GITHUB_RELEASES_URL = "https://api.github.com/repos/BOINC/boinc-buda-runner-wsl/releases/latest";

        /// <summary>
        /// Performs comprehensive BUDA Runner check including installation status, version comparison, and update requirements
        /// </summary>
        public static async Task<BudaRunnerCheckResult> CheckBudaRunnerAsync()
        {
            DebugLogger.LogMethodStart("CheckBudaRunnerAsync", component: COMPONENT);
            var result = new BudaRunnerCheckResult();

            try
            {
                DebugLogger.LogConfiguration("BUDA_RUNNER_IMAGE_NAME", BUDA_RUNNER_IMAGE_NAME, COMPONENT);
                DebugLogger.LogConfiguration("GITHUB_RELEASES_URL", GITHUB_RELEASES_URL, COMPONENT);

                // Step 1: Check if WSL image is installed
                DebugLogger.LogInfo("Step 1: Checking if WSL image is installed", COMPONENT);
                result.VersionInfo.IsInstalled = await IsWslImageInstalledAsync(BUDA_RUNNER_IMAGE_NAME);
                DebugLogger.LogConfiguration("WSL Image Installed", result.VersionInfo.IsInstalled, COMPONENT);

                // Step 2: Get latest version from GitHub
                DebugLogger.LogInfo("Step 2: Getting latest version from GitHub", COMPONENT);
                result.VersionInfo.LatestVersion = await GetLatestVersionFromGitHubAsync();
                result.DownloadUrl = await GetLatestDownloadUrlFromGitHubAsync();
                DebugLogger.LogConfiguration("Latest Version", result.VersionInfo.LatestVersion, COMPONENT);
                DebugLogger.LogConfiguration("Download URL", result.DownloadUrl, COMPONENT);

                if (!result.VersionInfo.IsInstalled)
                {
                    DebugLogger.LogInfo("WSL image is not installed", COMPONENT);
                    result.Status = BudaRunnerStatus.NotInstalled;
                    result.Message = $"BOINC BUDA Runner WSL image '{BUDA_RUNNER_IMAGE_NAME}' is not installed";
                    result.InitialSetupRequired = true;
                    DebugLogger.LogMethodEnd("CheckBudaRunnerAsync", $"Status: {result.Status}", COMPONENT);
                    return result;
                }

                // Step 3: Check version file existence and content
                DebugLogger.LogInfo("Step 3: Checking version file existence", COMPONENT);
                result.VersionInfo.HasVersionFile = await CheckVersionFileExistsAsync(BUDA_RUNNER_IMAGE_NAME);
                DebugLogger.LogConfiguration("Has Version File", result.VersionInfo.HasVersionFile, COMPONENT);

                if (!result.VersionInfo.HasVersionFile)
                {
                    DebugLogger.LogWarning("Version file is missing", COMPONENT);
                    result.Status = BudaRunnerStatus.InstalledNoVersion;
                    result.Message = "BUDA Runner is installed but version file is missing - reinstallation required";
                    result.UpdateRequired = true;
                    DebugLogger.LogMethodEnd("CheckBudaRunnerAsync", $"Status: {result.Status}", COMPONENT);
                    return result;
                }

                // Step 4: Read current version from version file
                DebugLogger.LogInfo("Step 4: Reading current version from version file", COMPONENT);
                result.VersionInfo.CurrentVersion = await ReadVersionFromFileAsync(BUDA_RUNNER_IMAGE_NAME);
                DebugLogger.LogConfiguration("Current Version", result.VersionInfo.CurrentVersion ?? "null", COMPONENT);

                if (string.IsNullOrEmpty(result.VersionInfo.CurrentVersion))
                {
                    DebugLogger.LogWarning("Version could not be read from file", COMPONENT);
                    result.Status = BudaRunnerStatus.InstalledNoVersion;
                    result.Message = "BUDA Runner is installed but version could not be read - reinstallation required";
                    result.UpdateRequired = true;
                    DebugLogger.LogMethodEnd("CheckBudaRunnerAsync", $"Status: {result.Status}", COMPONENT);
                    return result;
                }

                // Step 5: Compare versions
                DebugLogger.LogInfo("Step 5: Comparing versions", COMPONENT);
                result.UpdateRequired = IsUpdateRequired(result.VersionInfo.CurrentVersion, result.VersionInfo.LatestVersion);
                DebugLogger.LogConfiguration("Update Required", result.UpdateRequired, COMPONENT);

                if (result.UpdateRequired)
                {
                    DebugLogger.LogInfo($"Update required: {result.VersionInfo.CurrentVersion} -> {result.VersionInfo.LatestVersion}", COMPONENT);
                    result.Status = BudaRunnerStatus.InstalledOutdated;
                    result.Message = $"BUDA Runner v{result.VersionInfo.CurrentVersion} is outdated. Latest version is v{result.VersionInfo.LatestVersion}";
                }
                else
                {
                    DebugLogger.LogInfo("BUDA Runner is up to date", COMPONENT);
                    result.Status = BudaRunnerStatus.InstalledUpToDate;
                    result.Message = $"BUDA Runner v{result.VersionInfo.CurrentVersion} is up to date";
                }

                DebugLogger.LogMethodEnd("CheckBudaRunnerAsync", $"Status: {result.Status}", COMPONENT);
                return result;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException(ex, "Error in CheckBudaRunnerAsync", COMPONENT);
                result.Status = BudaRunnerStatus.Error;
                result.ErrorMessage = ex.Message;
                result.Message = $"Error checking BUDA Runner: {ex.Message}";
                DebugLogger.LogMethodEnd("CheckBudaRunnerAsync", $"Status: {result.Status} (Exception)", COMPONENT);
                return result;
            }
        }

        /// <summary>
        /// Installs or updates BUDA Runner WSL image
        /// </summary>
        public static async Task<bool> InstallOrUpdateBudaRunnerAsync(BudaRunnerCheckResult checkResult, IProgress<string> progress = null)
        {
            DebugLogger.LogMethodStart("InstallOrUpdateBudaRunnerAsync", $"Status: {checkResult.Status}", COMPONENT);

            try
            {
                // If already installed and needs update, remove first
                if (checkResult.VersionInfo.IsInstalled && (checkResult.UpdateRequired || checkResult.Status == BudaRunnerStatus.InstalledNoVersion))
                {
                    DebugLogger.LogInfo("Removing existing BUDA Runner installation", COMPONENT);
                    progress?.Report("Removing existing BUDA Runner installation...");
                    bool removed = await RemoveWslImageAsync(BUDA_RUNNER_IMAGE_NAME);
                    DebugLogger.LogConfiguration("Removal Result", removed, COMPONENT);
                    if (!removed)
                    {
                        DebugLogger.LogWarning("Could not remove existing installation", COMPONENT);
                        progress?.Report("Warning: Could not remove existing installation");
                    }
                }

                // Download and install the latest version
                DebugLogger.LogInfo($"Downloading latest BUDA Runner from: {checkResult.DownloadUrl}", COMPONENT);
                progress?.Report("Downloading latest BUDA Runner from GitHub...");
                string downloadPath = await DownloadLatestReleaseAsync(checkResult.DownloadUrl, progress);
                DebugLogger.LogConfiguration("Download Path", downloadPath ?? "null", COMPONENT);

                if (string.IsNullOrEmpty(downloadPath))
                {
                    DebugLogger.LogError("Failed to download BUDA Runner", COMPONENT);
                    progress?.Report("Failed to download BUDA Runner");
                    DebugLogger.LogMethodEnd("InstallOrUpdateBudaRunnerAsync", "false (download failed)", COMPONENT);
                    return false;
                }

                DebugLogger.LogInfo($"Installing BUDA Runner WSL image from: {downloadPath}", COMPONENT);
                progress?.Report("Installing BUDA Runner WSL image...");
                bool installed = await InstallWslImageAsync(downloadPath);
                DebugLogger.LogConfiguration("Installation Result", installed, COMPONENT);

                if (!installed)
                {
                    DebugLogger.LogError("Failed to install BUDA Runner WSL image", COMPONENT);
                    progress?.Report("Failed to install BUDA Runner WSL image");
                    DebugLogger.LogMethodEnd("InstallOrUpdateBudaRunnerAsync", "false (install failed)", COMPONENT);
                    return false;
                }

                // Run initial setup
                DebugLogger.LogInfo("Running initial BUDA Runner setup", COMPONENT);
                progress?.Report("Running initial BUDA Runner setup...");
                bool setupSuccess = await RunInitialSetupAsync(BUDA_RUNNER_IMAGE_NAME);
                DebugLogger.LogConfiguration("Setup Result", setupSuccess, COMPONENT);

                if (setupSuccess)
                {
                    DebugLogger.LogInfo("BUDA Runner installed and configured successfully", COMPONENT);
                    progress?.Report("BUDA Runner installed and configured successfully");
                }
                else
                {
                    DebugLogger.LogWarning("BUDA Runner installed but initial setup encountered issues", COMPONENT);
                    progress?.Report("BUDA Runner installed but initial setup encountered issues");
                }

                // Clean up downloaded file
                try
                {
                    DebugLogger.LogInfo($"Cleaning up downloaded file: {downloadPath}", COMPONENT);
                    File.Delete(downloadPath);
                    DebugLogger.LogInfo("Downloaded file cleaned up successfully", COMPONENT);
                }
                catch (Exception ex)
                {
                    DebugLogger.LogException(ex, "Error cleaning up downloaded file", COMPONENT);
                }

                DebugLogger.LogMethodEnd("InstallOrUpdateBudaRunnerAsync", "true", COMPONENT);
                return true;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException(ex, "Error in InstallOrUpdateBudaRunnerAsync", COMPONENT);
                progress?.Report($"Installation failed: {ex.Message}");
                DebugLogger.LogMethodEnd("InstallOrUpdateBudaRunnerAsync", "false (exception)", COMPONENT);
                return false;
            }
        }

        /// <summary>
        /// Checks if WSL image is installed
        /// </summary>
        private static async Task<bool> IsWslImageInstalledAsync(string imageName)
        {
            DebugLogger.LogMethodStart("IsWslImageInstalledAsync", $"imageName: {imageName}", COMPONENT);

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "wsl.exe",
                    Arguments = "--list --quiet",
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
                    await Task.Run(() => process.WaitForExit(10000));

                    DebugLogger.LogProcessExecution("wsl.exe", "--list --quiet", process.ExitCode, output, error, COMPONENT);

                    if (process.ExitCode == 0)
                    {
                        bool contains = output.Contains(imageName);
                        DebugLogger.LogConfiguration("Image Found in Output", contains, COMPONENT);
                        DebugLogger.LogMethodEnd("IsWslImageInstalledAsync", contains.ToString(), COMPONENT);
                        return contains;
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException(ex, "Error checking WSL image installation", COMPONENT);
            }

            DebugLogger.LogMethodEnd("IsWslImageInstalledAsync", "false", COMPONENT);
            return false;
        }

        /// <summary>
        /// Checks if version file exists in the WSL image
        /// </summary>
        private static async Task<bool> CheckVersionFileExistsAsync(string imageName)
        {
            DebugLogger.LogMethodStart("CheckVersionFileExistsAsync", $"imageName: {imageName}", COMPONENT);

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "wsl.exe",
                    Arguments = $"-d {imageName} test -f /home/boinc/version.txt && echo \"exists\" || echo \"missing\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                using (var process = new Process { StartInfo = startInfo })
                {
                    process.Start();
                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error = await process.StandardError.ReadToEndAsync();
                    await Task.Run(() => process.WaitForExit(15000));

                    DebugLogger.LogProcessExecution("wsl.exe", startInfo.Arguments, process.ExitCode, output, error, COMPONENT);

                    if (process.ExitCode == 0)
                    {
                        bool exists = output.Trim().Contains("exists");
                        DebugLogger.LogConfiguration("Version File Exists", exists, COMPONENT);
                        DebugLogger.LogMethodEnd("CheckVersionFileExistsAsync", exists.ToString(), COMPONENT);
                        return exists;
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException(ex, "Error checking version file existence", COMPONENT);
            }

            DebugLogger.LogMethodEnd("CheckVersionFileExistsAsync", "false", COMPONENT);
            return false;
        }

        /// <summary>
        /// Reads version from the version file in WSL image
        /// </summary>
        private static async Task<string> ReadVersionFromFileAsync(string imageName)
        {
            DebugLogger.LogMethodStart("ReadVersionFromFileAsync", $"imageName: {imageName}", COMPONENT);

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "wsl.exe",
                    Arguments = $"-d {imageName} cat /home/boinc/version.txt",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                using (var process = new Process { StartInfo = startInfo })
                {
                    process.Start();
                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error = await process.StandardError.ReadToEndAsync();
                    await Task.Run(() => process.WaitForExit(15000));

                    DebugLogger.LogProcessExecution("wsl.exe", startInfo.Arguments, process.ExitCode, output, error, COMPONENT);

                    if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
                    {
                        var rawContent = output.Trim();
                        DebugLogger.LogConfiguration("Raw Version Content", rawContent, COMPONENT);

                        // Parse version content - expected format: "version: 2" or similar
                        var parsedVersion = ParseVersionContent(rawContent);
                        DebugLogger.LogConfiguration("Parsed Version", parsedVersion ?? "null", COMPONENT);
                        DebugLogger.LogMethodEnd("ReadVersionFromFileAsync", parsedVersion ?? "null", COMPONENT);
                        return parsedVersion;
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException(ex, "Error reading version from file", COMPONENT);
            }

            DebugLogger.LogMethodEnd("ReadVersionFromFileAsync", "null", COMPONENT);
            return null;
        }

        /// <summary>
        /// Parses version content from the version file, handling formats like "version: 2"
        /// </summary>
        private static string ParseVersionContent(string content)
        {
            DebugLogger.LogMethodStart("ParseVersionContent", $"content: {content}", COMPONENT);

            if (string.IsNullOrWhiteSpace(content))
            {
                DebugLogger.LogMethodEnd("ParseVersionContent", "null (empty content)", COMPONENT);
                return null;
            }

            // Handle different possible formats:
            // "version: 2" -> "2"
            // "version:2" -> "2"
            // "2" -> "2"
            // "v2.1.0" -> "2.1.0"

            // First, try to extract version after "version:" prefix
            if (content.Contains(":"))
            {
                DebugLogger.LogDebug("Content contains colon, attempting to parse with colon separator", COMPONENT);
                var parts = content.Split(':');
                if (parts.Length >= 2)
                {
                    var versionPart = parts[1].Trim();
                    if (!string.IsNullOrEmpty(versionPart))
                    {
                        DebugLogger.LogMethodEnd("ParseVersionContent", versionPart, COMPONENT);
                        return versionPart;
                    }
                }
            }

            // If no colon found or parsing failed, try to extract version number directly
            // Remove common prefixes like "version", "v", etc.
            var cleanContent = content.Trim();
            DebugLogger.LogDebug($"Attempting direct parsing of: {cleanContent}", COMPONENT);

            // Remove "version" prefix (case insensitive)
            if (cleanContent.StartsWith("version", StringComparison.OrdinalIgnoreCase))
            {
                cleanContent = cleanContent.Substring(7).Trim();
                DebugLogger.LogDebug($"After removing 'version' prefix: {cleanContent}", COMPONENT);
            }

            // Remove "v" prefix if present
            if (cleanContent.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            {
                cleanContent = cleanContent.Substring(1).Trim();
                DebugLogger.LogDebug($"After removing 'v' prefix: {cleanContent}", COMPONENT);
            }

            // Remove any leading/trailing whitespace and non-alphanumeric characters except dots and dashes
            cleanContent = Regex.Replace(cleanContent, @"^[^\d\w]+|[^\d\w\.]+$", "").Trim();
            DebugLogger.LogDebug($"After regex cleanup: {cleanContent}", COMPONENT);

            var result = !string.IsNullOrEmpty(cleanContent) ? cleanContent : null;
            DebugLogger.LogMethodEnd("ParseVersionContent", result ?? "null", COMPONENT);
            return result;
        }

        /// <summary>
        /// Gets the latest version from GitHub releases
        /// </summary>
        private static async Task<string> GetLatestVersionFromGitHubAsync()
        {
            DebugLogger.LogMethodStart("GetLatestVersionFromGitHubAsync", component: COMPONENT);

            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "BOINC-BUDA-WSL-Installer");

                    DebugLogger.LogInfo($"Fetching latest version from GitHub: {GITHUB_RELEASES_URL}", COMPONENT);
                    var response = await httpClient.GetStringAsync(GITHUB_RELEASES_URL);

                    // Parse tag_name from JSON response
                    var tagNameMatch = Regex.Match(response, @"""tag_name"":\s*""([^""]+)""");
                    if (tagNameMatch.Success)
                    {
                        var version = tagNameMatch.Groups[1].Value.TrimStart('v'); // Remove 'v' prefix if present
                        DebugLogger.LogConfiguration("GitHub Latest Version", version, COMPONENT);
                        DebugLogger.LogMethodEnd("GetLatestVersionFromGitHubAsync", version, COMPONENT);
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
                DebugLogger.LogException(ex, "Error fetching latest version from GitHub", COMPONENT);
            }

            DebugLogger.LogMethodEnd("GetLatestVersionFromGitHubAsync", "Unknown", COMPONENT);
            return "Unknown";
        }

        /// <summary>
        /// Gets the download URL for the latest release
        /// </summary>
        private static async Task<string> GetLatestDownloadUrlFromGitHubAsync()
        {
            DebugLogger.LogMethodStart("GetLatestDownloadUrlFromGitHubAsync", component: COMPONENT);

            const string fallbackUrl = "https://github.com/BOINC/boinc-buda-runner-wsl/releases/latest";
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "BOINC-BUDA-WSL-Installer");

                    DebugLogger.LogInfo($"Fetching download URL from GitHub: {GITHUB_RELEASES_URL}", COMPONENT);
                    var response = await httpClient.GetStringAsync(GITHUB_RELEASES_URL);

                    // Find .wsl download URL
                    var downloadMatches = Regex.Matches(response, @"""browser_download_url"":\s*""([^""]+\.wsl)""");
                    DebugLogger.LogConfiguration("Download Matches Found", downloadMatches.Count, COMPONENT);

                    if (downloadMatches.Count > 0)
                    {
                        var downloadUrl = downloadMatches[0].Groups[1].Value;
                        DebugLogger.LogConfiguration("GitHub Download URL", downloadUrl, COMPONENT);
                        DebugLogger.LogMethodEnd("GetLatestDownloadUrlFromGitHubAsync", downloadUrl, COMPONENT);
                        return downloadUrl;
                    }
                    else
                    {
                        DebugLogger.LogWarning("No .wsl download URLs found in GitHub response", COMPONENT);
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException(ex, "Error fetching download URL from GitHub", COMPONENT);
            }

            DebugLogger.LogConfiguration("Using Fallback URL", fallbackUrl, COMPONENT);
            DebugLogger.LogMethodEnd("GetLatestDownloadUrlFromGitHubAsync", fallbackUrl, COMPONENT);
            return fallbackUrl;
        }

        /// <summary>
        /// Downloads the latest release from GitHub
        /// </summary>
        private static async Task<string> DownloadLatestReleaseAsync(string downloadUrl, IProgress<string> progress = null)
        {
            DebugLogger.LogMethodStart("DownloadLatestReleaseAsync", $"downloadUrl: {downloadUrl}", COMPONENT);

            try
            {
                var tempPath = Path.GetTempPath();
                var fileName = Path.GetFileName(new Uri(downloadUrl).LocalPath);
                var filePath = Path.Combine(tempPath, fileName);

                DebugLogger.LogConfiguration("Temp Path", tempPath, COMPONENT);
                DebugLogger.LogConfiguration("File Name", fileName, COMPONENT);
                DebugLogger.LogConfiguration("Full File Path", filePath, COMPONENT);

                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromMinutes(15); // Extended timeout for large files
                    DebugLogger.LogConfiguration("HTTP Client Timeout", "15 minutes", COMPONENT);

                    progress?.Report("Downloading BUDA Runner archive...");
                    DebugLogger.LogInfo("Starting download from GitHub", COMPONENT);

                    var response = await httpClient.GetAsync(downloadUrl);
                    response.EnsureSuccessStatusCode();
                    DebugLogger.LogConfiguration("HTTP Response Status", response.StatusCode.ToString(), COMPONENT);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await response.Content.CopyToAsync(fileStream);
                    }

                    DebugLogger.LogInfo($"Download completed successfully: {filePath}", COMPONENT);
                }

                // Verify file exists and get size
                if (File.Exists(filePath))
                {
                    var fileInfo = new FileInfo(filePath);
                    DebugLogger.LogConfiguration("Downloaded File Size", $"{fileInfo.Length} bytes", COMPONENT);
                }

                DebugLogger.LogMethodEnd("DownloadLatestReleaseAsync", filePath, COMPONENT);
                return filePath;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException(ex, "Error downloading latest release", COMPONENT);
                progress?.Report($"Download failed: {ex.Message}");
                DebugLogger.LogMethodEnd("DownloadLatestReleaseAsync", "null (error)", COMPONENT);
                return null;
            }
        }

        /// <summary>
        /// Installs WSL image from downloaded archive
        /// </summary>
        private static async Task<bool> InstallWslImageAsync(string archivePath)
        {
            DebugLogger.LogMethodStart("InstallWslImageAsync", $"archivePath: {archivePath}", COMPONENT);

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "wsl.exe",
                    Arguments = $"--install --from-file \"{archivePath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                DebugLogger.LogInfo($"Starting WSL installation process: wsl.exe {startInfo.Arguments}", COMPONENT);

                using (var process = new Process { StartInfo = startInfo })
                {
                    process.Start();

                    // Read output line by line asynchronously and wait for "Podman setup complete"
                    bool setupComplete = false;
                    var outputTask = Task.Run(async () =>
                    {
                        try
                        {
                            using (var reader = process.StandardOutput)
                            {
                                string line;
                                while ((line = await reader.ReadLineAsync()) != null)
                                {
                                    DebugLogger.LogDebug($"WSL Install Output: {line}", COMPONENT);
                                    // Check if the line contains "Podman setup complete"
                                    if (line.Contains("Podman setup complete"))
                                    {
                                        DebugLogger.LogInfo("Detected 'Podman setup complete' message", COMPONENT);
                                        setupComplete = true;
                                        break;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            DebugLogger.LogException(ex, "Error reading WSL install output", COMPONENT);
                        }
                    });

                    // Read error output in parallel
                    var errorTask = Task.Run(async () =>
                    {
                        try
                        {
                            using (var reader = process.StandardError)
                            {
                                string line;
                                while ((line = await reader.ReadLineAsync()) != null)
                                {
                                    DebugLogger.LogWarning($"WSL Install Error: {line}", COMPONENT);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            DebugLogger.LogException(ex, "Error reading WSL install error output", COMPONENT);
                        }
                    });

                    // Wait for either setup completion or timeout (5 minutes)
                    var timeoutTask = Task.Delay(300000); // 5 minute timeout
                    DebugLogger.LogInfo("Waiting for installation completion or 5-minute timeout", COMPONENT);
                    var completedTask = await Task.WhenAny(outputTask, timeoutTask);

                    if (completedTask == timeoutTask)
                    {
                        DebugLogger.LogError("WSL installation timed out after 5 minutes", COMPONENT);
                        // Timeout occurred - kill the process
                        try
                        {
                            if (!process.HasExited)
                            {
                                DebugLogger.LogInfo("Killing timed-out process", COMPONENT);
                                process.Kill();
                            }
                        }
                        catch (Exception ex)
                        {
                            DebugLogger.LogException(ex, "Error killing timed-out process", COMPONENT);
                        }
                        DebugLogger.LogMethodEnd("InstallWslImageAsync", "false (timeout)", COMPONENT);
                        return false;
                    }

                    // If we reach here, setup should be complete
                    if (setupComplete)
                    {
                        DebugLogger.LogInfo("Setup completed successfully, terminating process", COMPONENT);
                        // Kill the process since it won't end by itself after Podman setup complete
                        try
                        {
                            if (!process.HasExited)
                            {
                                process.Kill();
                                // Give the process a moment to terminate
                                await Task.Delay(1000);
                            }
                        }
                        catch (Exception ex)
                        {
                            DebugLogger.LogException(ex, "Error killing completed process", COMPONENT);
                        }
                        DebugLogger.LogMethodEnd("InstallWslImageAsync", "true", COMPONENT);
                        return true;
                    }

                    // Wait a bit more for the process to exit naturally, then kill if needed
                    DebugLogger.LogInfo("Waiting additional 10 seconds for natural process termination", COMPONENT);
                    await Task.Run(() => process.WaitForExit(10000)); // 10 second grace period

                    if (!process.HasExited)
                    {
                        try
                        {
                            DebugLogger.LogInfo("Process did not exit naturally, killing it", COMPONENT);
                            process.Kill();
                        }
                        catch (Exception ex)
                        {
                            DebugLogger.LogException(ex, "Error killing process during grace period", COMPONENT);
                        }
                    }

                    // Installation is considered successful if we detected the completion message
                    DebugLogger.LogConfiguration("Final Setup Complete Status", setupComplete, COMPONENT);
                    DebugLogger.LogMethodEnd("InstallWslImageAsync", setupComplete.ToString(), COMPONENT);
                    return setupComplete;
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException(ex, "Error in InstallWslImageAsync", COMPONENT);
                DebugLogger.LogMethodEnd("InstallWslImageAsync", "false (exception)", COMPONENT);
                return false;
            }
        }

        /// <summary>
        /// Removes WSL image
        /// </summary>
        private static async Task<bool> RemoveWslImageAsync(string imageName)
        {
            DebugLogger.LogMethodStart("RemoveWslImageAsync", $"imageName: {imageName}", COMPONENT);

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "wsl.exe",
                    Arguments = $"--unregister {imageName}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                using (var process = new Process { StartInfo = startInfo })
                {
                    process.Start();
                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error = await process.StandardError.ReadToEndAsync();
                    await Task.Run(() => process.WaitForExit(60000)); // 1 minute timeout

                    DebugLogger.LogProcessExecution("wsl.exe", startInfo.Arguments, process.ExitCode, output, error, COMPONENT);

                    var success = process.ExitCode == 0;
                    DebugLogger.LogMethodEnd("RemoveWslImageAsync", success.ToString(), COMPONENT);
                    return success;
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException(ex, "Error removing WSL image", COMPONENT);
                DebugLogger.LogMethodEnd("RemoveWslImageAsync", "false (exception)", COMPONENT);
                return false;
            }
        }

        /// <summary>
        /// Runs initial setup for BUDA Runner
        /// </summary>
        private static async Task<bool> RunInitialSetupAsync(string imageName)
        {
            DebugLogger.LogMethodStart("RunInitialSetupAsync", $"imageName: {imageName}", COMPONENT);

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "wsl.exe",
                    Arguments = $"-d {imageName} /bin/bash -c \"echo 'BUDA Runner initial setup completed' && exit 0\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                using (var process = new Process { StartInfo = startInfo })
                {
                    process.Start();
                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error = await process.StandardError.ReadToEndAsync();
                    await Task.Run(() => process.WaitForExit(120000)); // 2 minute timeout

                    DebugLogger.LogProcessExecution("wsl.exe", startInfo.Arguments, process.ExitCode, output, error, COMPONENT);

                    var success = process.ExitCode == 0;
                    DebugLogger.LogMethodEnd("RunInitialSetupAsync", success.ToString(), COMPONENT);
                    return success;
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException(ex, "Error running initial setup", COMPONENT);
                DebugLogger.LogMethodEnd("RunInitialSetupAsync", "false (exception)", COMPONENT);
                return false;
            }
        }

        /// <summary>
        /// Compares version strings to determine if update is required
        /// </summary>
        private static bool IsUpdateRequired(string currentVersion, string latestVersion)
        {
            DebugLogger.LogMethodStart("IsUpdateRequired", $"current: {currentVersion}, latest: {latestVersion}", COMPONENT);

            if (string.IsNullOrEmpty(currentVersion) || string.IsNullOrEmpty(latestVersion) || latestVersion == "Unknown")
            {
                DebugLogger.LogMethodEnd("IsUpdateRequired", "false (invalid versions)", COMPONENT);
                return false;
            }

            try
            {
                // Normalize version strings for comparison
                var current = NormalizeVersionString(currentVersion);
                var latest = NormalizeVersionString(latestVersion);
                DebugLogger.LogConfiguration("Normalized Current Version", current, COMPONENT);
                DebugLogger.LogConfiguration("Normalized Latest Version", latest, COMPONENT);

                var currentVer = new Version(current);
                var latestVer = new Version(latest);
                var updateRequired = currentVer < latestVer;

                DebugLogger.LogConfiguration("Update Required", updateRequired, COMPONENT);
                DebugLogger.LogMethodEnd("IsUpdateRequired", updateRequired.ToString(), COMPONENT);
                return updateRequired;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException(ex, "Error comparing versions, assuming update needed", COMPONENT);
                DebugLogger.LogMethodEnd("IsUpdateRequired", "true (exception)", COMPONENT);
                return true;
            }
        }

        /// <summary>
        /// Normalizes version string for comparison
        /// </summary>
        private static string NormalizeVersionString(string version)
        {
            DebugLogger.LogMethodStart("NormalizeVersionString", $"version: {version}", COMPONENT);

            // Remove any non-numeric characters except dots
            var normalized = Regex.Replace(version, @"[^0-9.]", "");
            DebugLogger.LogDebug($"After regex cleanup: {normalized}", COMPONENT);

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
        public static string GetStatusDisplayMessage(BudaRunnerCheckResult result)
        {
            DebugLogger.LogMethodStart("GetStatusDisplayMessage", $"status: {result.Status}", COMPONENT);

            string message;
            switch (result.Status)
            {
                case BudaRunnerStatus.NotInstalled:
                    message = "BOINC BUDA Runner is not installed";
                    break;
                case BudaRunnerStatus.InstalledUpToDate:
                    message = $"BUDA Runner v{result.VersionInfo.CurrentVersion} is up to date";
                    break;
                case BudaRunnerStatus.InstalledOutdated:
                    message = $"BUDA Runner v{result.VersionInfo.CurrentVersion} is outdated (latest: v{result.VersionInfo.LatestVersion})";
                    break;
                case BudaRunnerStatus.InstalledNoVersion:
                    message = "BUDA Runner is installed but version cannot be determined";
                    break;
                case BudaRunnerStatus.Error:
                    message = $"Error checking BUDA Runner: {result.ErrorMessage}";
                    break;
                default:
                    message = "Unknown BUDA Runner status";
                    break;
            }

            DebugLogger.LogMethodEnd("GetStatusDisplayMessage", message, COMPONENT);
            return message;
        }
    }
}
