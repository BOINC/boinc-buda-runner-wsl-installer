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
using System.Text;

namespace boinc_buda_runner_wsl_installer
{
    /// <summary>
    /// Provides user-friendly troubleshooting guidance for common installation issues
    /// </summary>
    public static class TroubleshootingGuide
    {
        public enum ErrorCategory
        {
            WindowsVersion,
            WindowsFeatures,
            WslNotInstalled,
            WslVersionMismatch,
            WslInstallationFailed,
            WslCheckFailed,
            BoincRunning,
            BudaRunnerInstallFailed,
            BudaRunnerVersionCheckFailed,
            NetworkError,
            PermissionError,
            UnknownError
        }

        public class TroubleshootingAdvice
        {
            public string Title { get; set; }
            public string Description { get; set; }
            public List<string> Steps { get; set; } = new List<string>();
            public List<string> AdditionalResources { get; set; } = new List<string>();
            public bool ShouldOfferIssueReport { get; set; } = true;
        }

        public static TroubleshootingAdvice GetAdvice(ErrorCategory category, string errorDetails = null)
        {
            switch (category)
            {
                case ErrorCategory.WindowsVersion:
                    return GetWindowsVersionAdvice();

                case ErrorCategory.WindowsFeatures:
                    return GetWindowsFeaturesAdvice();

                case ErrorCategory.WslNotInstalled:
                    return GetWslNotInstalledAdvice();

                case ErrorCategory.WslVersionMismatch:
                    return GetWslVersionMismatchAdvice();

                case ErrorCategory.WslInstallationFailed:
                    return GetWslInstallationFailedAdvice(errorDetails);

                case ErrorCategory.WslCheckFailed:
                    return GetWslCheckFailedAdvice(errorDetails);

                case ErrorCategory.BoincRunning:
                    return GetBoincRunningAdvice();

                case ErrorCategory.BudaRunnerInstallFailed:
                    return GetBudaRunnerInstallFailedAdvice(errorDetails);

                case ErrorCategory.BudaRunnerVersionCheckFailed:
                    return GetBudaRunnerVersionCheckFailedAdvice();

                case ErrorCategory.NetworkError:
                    return GetNetworkErrorAdvice();

                case ErrorCategory.PermissionError:
                    return GetPermissionErrorAdvice();

                default:
                    return GetUnknownErrorAdvice(errorDetails);
            }
        }

        private static TroubleshootingAdvice GetWindowsVersionAdvice()
        {
            return new TroubleshootingAdvice
            {
                Title = "Windows Version Not Supported",
                Description = "Your Windows version does not meet the minimum requirements for BOINC WSL Distro installation. WSL 2 requires Windows 10 version 1903 (Build 18362) or later, or Windows 11.",
                Steps = new List<string>
                {
                    "Check your Windows version: Press Win+R, type 'winver', and press Enter",
                    "If you're running an older version of Windows 10, update to the latest version through Windows Update",
                    "Go to Settings > Update & Security > Windows Update",
                    "Click 'Check for updates' and install all available updates",
                    "After updating, restart your computer and run this installer again"
                },
                AdditionalResources = new List<string>
                {
                    "Windows 10 Update Assistant: https://www.microsoft.com/software-download/windows10",
                    "Windows 11 Upgrade: https://www.microsoft.com/software-download/windows11",
                    "WSL Requirements: https://docs.microsoft.com/windows/wsl/install"
                },
                ShouldOfferIssueReport = false
            };
        }

        private static TroubleshootingAdvice GetWindowsFeaturesAdvice()
        {
            return new TroubleshootingAdvice
            {
                Title = "Windows Features Need to be Enabled",
                Description = "Required Windows features for WSL are missing or not properly configured. The installer will attempt to enable them automatically, but a system restart may be required.",
                Steps = new List<string>
                {
                    "The installer will automatically enable the required Windows features",
                    "If a restart is required, you will be prompted",
                    "After restarting, run this installer again to continue",
                    "",
                    "If automatic enablement fails, you can enable features manually:",
                    "1. Open 'Turn Windows features on or off' (search in Start menu)",
                    "2. Enable 'Virtual Machine Platform'",
                    "3. Enable 'Windows Subsystem for Linux'",
                    "4. Click OK and restart your computer when prompted",
                    "5. Run this installer again after restart"
                },
                AdditionalResources = new List<string>
                {
                    "Manual WSL Installation Guide: https://docs.microsoft.com/windows/wsl/install-manual"
                },
                ShouldOfferIssueReport = true
            };
        }

        private static TroubleshootingAdvice GetWslNotInstalledAdvice()
        {
            return new TroubleshootingAdvice
            {
                Title = "WSL Not Installed",
                Description = "Windows Subsystem for Linux (WSL) is not installed on your system. The installer will attempt to install it automatically.",
                Steps = new List<string>
                {
                    "The installer will automatically download and install WSL",
                    "This may take several minutes depending on your internet connection",
                    "If automatic installation fails, you can install WSL manually:",
                    "",
                    "Manual Installation:",
                    "1. Open PowerShell or Command Prompt as Administrator",
                    "2. Run: wsl --install",
                    "3. Restart your computer when prompted",
                    "4. Run this installer again after restart",
                    "",
                    "Alternative Method:",
                    "1. Open Microsoft Store",
                    "2. Search for 'Windows Subsystem for Linux'",
                    "3. Install the WSL app",
                    "4. Restart your computer",
                    "5. Run this installer again"
                },
                AdditionalResources = new List<string>
                {
                    "WSL Installation Guide: https://docs.microsoft.com/windows/wsl/install",
                    "WSL Microsoft Store: ms-windows-store://pdp/?ProductId=9P9TQF7MRM4R"
                },
                ShouldOfferIssueReport = true
            };
        }

        private static TroubleshootingAdvice GetWslVersionMismatchAdvice()
        {
            return new TroubleshootingAdvice
            {
                Title = "WSL Version Needs Update",
                Description = "Your WSL installation needs to be updated or the default version needs to be changed to WSL 2. The installer will attempt to fix this automatically.",
                Steps = new List<string>
                {
                    "The installer will automatically update WSL and set the default version to 2",
                    "If automatic update fails, you can update manually:",
                    "",
                    "Manual Update:",
                    "1. Open PowerShell or Command Prompt as Administrator",
                    "2. Run: wsl --update",
                    "3. Wait for the update to complete",
                    "4. Run: wsl --set-default-version 2",
                    "5. Run this installer again"
                },
                AdditionalResources = new List<string>
                {
                    "WSL Update Guide: https://docs.microsoft.com/windows/wsl/install",
                    "WSL Releases: https://github.com/microsoft/WSL/releases"
                },
                ShouldOfferIssueReport = true
            };
        }

        private static TroubleshootingAdvice GetWslInstallationFailedAdvice(string errorDetails)
        {
            var advice = new TroubleshootingAdvice
            {
                Title = "WSL Installation Failed",
                Description = "The automatic WSL installation encountered an error. This could be due to network issues, permission problems, or system configuration issues.",
                Steps = new List<string>
                {
                    "Try the following solutions:",
                    "",
                    "Solution 1: Run as Administrator",
                    "1. Right-click the installer",
                    "2. Select 'Run as administrator'",
                    "3. Try the installation again",
                    "",
                    "Solution 2: Manual WSL Installation",
                    "1. Open PowerShell as Administrator",
                    "2. Run: wsl --install",
                    "3. If that fails, try: wsl --install --web-download",
                    "4. Restart your computer when prompted",
                    "5. Run this installer again",
                    "",
                    "Solution 3: Check Network Connection",
                    "1. Verify you have an active internet connection",
                    "2. Try disabling VPN or proxy temporarily",
                    "3. Check if your firewall is blocking the installation",
                    "4. Try the installation again"
                },
                AdditionalResources = new List<string>
                {
                    "WSL Troubleshooting: https://docs.microsoft.com/windows/wsl/troubleshooting",
                    "WSL Manual Installation: https://docs.microsoft.com/windows/wsl/install-manual"
                }
            };

            if (!string.IsNullOrEmpty(errorDetails))
            {
                if (errorDetails.Contains("0x80070057") || errorDetails.Contains("parameter is incorrect"))
                {
                    advice.Steps.Insert(1, "Error indicates invalid parameter. This may be due to corrupted download or system configuration.");
                    advice.Steps.Insert(2, "Try using: wsl --install --web-download --distribution Ubuntu");
                    advice.Steps.Insert(3, "");
                }
                else if (errorDetails.Contains("0x80070002") || errorDetails.Contains("cannot find"))
                {
                    advice.Steps.Insert(1, "Error indicates missing files. System files may be corrupted.");
                    advice.Steps.Insert(2, "Run System File Checker: Open Command Prompt as Admin and run: sfc /scannow");
                    advice.Steps.Insert(3, "After completion, restart and try installation again.");
                    advice.Steps.Insert(4, "");
                }
                else if (errorDetails.Contains("0x80070005") || errorDetails.Contains("denied"))
                {
                    advice.Steps.Insert(1, "Error indicates permission denied. Make sure you're running as Administrator.");
                    advice.Steps.Insert(2, "");
                }
                else if (errorDetails.Contains("0x800701bc") || errorDetails.Contains("virtualization"))
                {
                    advice.Steps.Insert(1, "Error indicates virtualization issue. Enable virtualization in BIOS:");
                    advice.Steps.Insert(2, "1. Restart computer and enter BIOS/UEFI settings");
                    advice.Steps.Insert(3, "2. Find and enable Intel VT-x or AMD-V");
                    advice.Steps.Insert(4, "3. Save and exit BIOS");
                    advice.Steps.Insert(5, "4. Run this installer again");
                    advice.Steps.Insert(6, "");
                }
            }

            return advice;
        }

        private static TroubleshootingAdvice GetWslCheckFailedAdvice(string errorDetails)
        {
            var advice = new TroubleshootingAdvice
            {
                Title = "WSL Status Check Failed",
                Description = "Unable to verify WSL installation status. This could indicate WSL is not properly configured or there's a system issue.",
                Steps = new List<string>
                {
                    "Try these troubleshooting steps:",
                    "",
                    "Step 1: Verify WSL Installation",
                    "1. Open PowerShell or Command Prompt",
                    "2. Run: wsl --version",
                    "3. If you see version info, WSL is installed",
                    "4. If you get an error, WSL needs to be installed or repaired",
                    "",
                    "Step 2: Repair WSL Installation",
                    "1. Open PowerShell as Administrator",
                    "2. Run: wsl --update",
                    "3. Run: wsl --shutdown",
                    "4. Run this installer again",
                    "",
                    "Step 3: Check Windows Features",
                    "1. Open 'Turn Windows features on or off'",
                    "2. Verify 'Virtual Machine Platform' is checked",
                    "3. Verify 'Windows Subsystem for Linux' is checked",
                    "4. If not, enable them and restart",
                    "5. Run this installer again"
                },
                AdditionalResources = new List<string>
                {
                    "WSL Troubleshooting: https://docs.microsoft.com/windows/wsl/troubleshooting",
                    "WSL GitHub Issues: https://github.com/microsoft/WSL/issues"
                }
            };

            if (!string.IsNullOrEmpty(errorDetails))
            {
                if (errorDetails.Contains("0x80370102"))
                {
                    advice.Steps.Insert(1, "Error 0x80370102: Virtualization is not enabled in BIOS.");
                    advice.Steps.Insert(2, "Enable VT-x/AMD-V in your computer's BIOS settings.");
                    advice.Steps.Insert(3, "");
                }
                else if (errorDetails.Contains("0x80370114"))
                {
                    advice.Steps.Insert(1, "Error 0x80370114: Enable nested virtualization if running in a VM.");
                    advice.Steps.Insert(2, "");
                }
            }

            return advice;
        }

        private static TroubleshootingAdvice GetBoincRunningAdvice()
        {
            return new TroubleshootingAdvice
            {
                Title = "BOINC Client is Running",
                Description = "The BOINC client is currently running and must be stopped before installation can proceed. This prevents potential conflicts during installation.",
                Steps = new List<string>
                {
                    "To stop the BOINC client:",
                    "",
                    "Method 1: BOINC Manager",
                    "1. Open BOINC Manager",
                    "2. Go to File > Exit BOINC",
                    "3. Wait a few seconds for BOINC to shut down completely",
                    "4. Run this installer again",
                    "",
                    "Method 2: Task Manager",
                    "1. Open Task Manager (Ctrl+Shift+Esc)",
                    "2. Find 'boinc.exe' or 'boincmgr.exe' in the Processes tab",
                    "3. Select and click 'End task'",
                    "4. Run this installer again",
                    "",
                    "Method 3: Services (if running as service)",
                    "1. Open Services (services.msc)",
                    "2. Find 'BOINC' service",
                    "3. Right-click and select 'Stop'",
                    "4. Run this installer again"
                },
                AdditionalResources = new List<string>
                {
                    "BOINC Documentation: https://boinc.berkeley.edu/wiki/User_manual"
                },
                ShouldOfferIssueReport = false
            };
        }

        private static TroubleshootingAdvice GetBudaRunnerInstallFailedAdvice(string errorDetails)
        {
            var advice = new TroubleshootingAdvice
            {
                Title = "BOINC WSL Distro Installation Failed",
                Description = "The installation of the BOINC WSL Distro encountered an error. This could be due to network issues, insufficient disk space, or WSL configuration problems.",
                Steps = new List<string>
                {
                    "Try these troubleshooting steps:",
                    "",
                    "Step 1: Verify Prerequisites",
                    "1. Make sure WSL is properly installed (run: wsl --version)",
                    "2. Verify you have at least 10 GB of free disk space",
                    "3. Check your internet connection is stable",
                    "",
                    "Step 2: Manual Installation Attempt",
                    "1. Download the latest release from:",
                    "   https://github.com/BOINC/boinc-buda-runner-wsl/releases/latest",
                    "2. Find the .wsl file matching your architecture (x64 or ARM64)",
                    "3. Open PowerShell as Administrator",
                    "4. Run: wsl --install --from-file \"path\\to\\downloaded.wsl\" --no-launch",
                    "5. If successful, run this installer again to verify",
                    "",
                    "Step 3: Clean Up and Retry",
                    "1. Open PowerShell as Administrator",
                    "2. Run: wsl --list",
                    "3. If you see 'boinc-buda-runner', unregister it: wsl --unregister boinc-buda-runner",
                    "4. Run this installer again",
                    "",
                    "Step 4: Check WSL Status",
                    "1. Run: wsl --status",
                    "2. Verify default version is 2",
                    "3. If not, run: wsl --set-default-version 2",
                    "4. Run this installer again"
                },
                AdditionalResources = new List<string>
                {
                    "BOINC WSL Distro Releases: https://github.com/BOINC/boinc-buda-runner-wsl/releases",
                    "WSL Import Documentation: https://docs.microsoft.com/windows/wsl/use-custom-distro",
                    "Create GitHub Issue: https://github.com/BOINC/boinc-buda-runner-wsl-installer/issues/new"
                }
            };

            if (!string.IsNullOrEmpty(errorDetails))
            {
                if (errorDetails.Contains("0x80070070") || errorDetails.Contains("disk space"))
                {
                    advice.Steps.Insert(1, "Error indicates insufficient disk space. You need at least 10 GB free.");
                    advice.Steps.Insert(2, "Free up disk space and try again.");
                    advice.Steps.Insert(3, "");
                }
                else if (errorDetails.Contains("timeout") || errorDetails.Contains("timed out"))
                {
                    advice.Steps.Insert(1, "Installation timed out. This may be due to slow internet or system performance.");
                    advice.Steps.Insert(2, "Try downloading the .wsl file manually and use Method 2 above.");
                    advice.Steps.Insert(3, "");
                }
                else if (errorDetails.Contains("0x80070032"))
                {
                    advice.Steps.Insert(1, "Error indicates the distribution is already in use.");
                    advice.Steps.Insert(2, "Close all WSL windows and try: wsl --shutdown");
                    advice.Steps.Insert(3, "Then run this installer again.");
                    advice.Steps.Insert(4, "");
                }
            }

            return advice;
        }

        private static TroubleshootingAdvice GetBudaRunnerVersionCheckFailedAdvice()
        {
            return new TroubleshootingAdvice
            {
                Title = "Unable to Verify BOINC WSL Distro Version",
                Description = "The installer cannot determine the version of the BOINC WSL Distro. This may indicate a corrupted installation or missing version file.",
                Steps = new List<string>
                {
                    "Try these solutions:",
                    "",
                    "Solution 1: Reinstall BOINC WSL Distro",
                    "1. Open PowerShell as Administrator",
                    "2. Run: wsl --unregister boinc-buda-runner",
                    "3. Run this installer again",
                    "",
                    "Solution 2: Manual Version Check",
                    "1. Open PowerShell",
                    "2. Run: wsl -d boinc-buda-runner cat /home/boinc/version.txt",
                    "3. If you see a version number, the installation is OK",
                    "4. If you get an error, reinstall using Solution 1",
                    "",
                    "Solution 3: Verify WSL Distribution",
                    "1. Run: wsl --list --verbose",
                    "2. Verify 'boinc-buda-runner' is listed",
                    "3. Verify it shows 'Running' or 'Stopped' (not 'Installing')",
                    "4. If status looks wrong, try Solution 1"
                },
                AdditionalResources = new List<string>
                {
                    "BOINC WSL Distro Documentation: https://github.com/BOINC/boinc-buda-runner-wsl"
                }
            };
        }

        private static TroubleshootingAdvice GetNetworkErrorAdvice()
        {
            return new TroubleshootingAdvice
            {
                Title = "Network Connection Error",
                Description = "The installer cannot connect to the internet to download required components. This could be due to network issues, firewall, or proxy settings.",
                Steps = new List<string>
                {
                    "Try these solutions:",
                    "",
                    "Solution 1: Check Internet Connection",
                    "1. Verify you can access https://github.com in your browser",
                    "2. Try downloading files from other websites",
                    "3. If internet is down, connect and try again",
                    "",
                    "Solution 2: Temporarily Disable VPN/Proxy",
                    "1. If using a VPN, disconnect temporarily",
                    "2. If behind a proxy, configure it or connect directly",
                    "3. Try the installation again",
                    "4. Re-enable VPN/proxy after installation",
                    "",
                    "Solution 3: Check Firewall Settings",
                    "1. Check if Windows Firewall is blocking the installer",
                    "2. Temporarily disable firewall (remember to re-enable)",
                    "3. Try the installation again",
                    "",
                    "Solution 4: Manual Download",
                    "1. Manually download required files from GitHub",
                    "2. WSL: https://github.com/microsoft/WSL/releases/latest",
                    "3. BOINC WSL Distro: https://github.com/BOINC/boinc-buda-runner-wsl/releases/latest",
                    "4. Follow manual installation instructions in the error message"
                },
                AdditionalResources = new List<string>
                {
                    "Windows Network Troubleshooting: https://support.microsoft.com/windows/network-connection-troubleshooter"
                }
            };
        }

        private static TroubleshootingAdvice GetPermissionErrorAdvice()
        {
            return new TroubleshootingAdvice
            {
                Title = "Insufficient Permissions",
                Description = "The installer requires administrator privileges to install system components like WSL and Windows features.",
                Steps = new List<string>
                {
                    "To run with administrator privileges:",
                    "",
                    "Method 1: Run as Administrator",
                    "1. Close this installer",
                    "2. Right-click the installer executable",
                    "3. Select 'Run as administrator'",
                    "4. Click 'Yes' on the User Account Control prompt",
                    "",
                    "Method 2: Enable Administrator Account (if needed)",
                    "1. Open Command Prompt as Administrator",
                    "2. Run: net user administrator /active:yes",
                    "3. Log out and log in as Administrator",
                    "4. Run the installer again",
                    "",
                    "Note: Some operations require administrator rights:",
                    "- Installing Windows features",
                    "- Installing WSL",
                    "- Configuring system settings"
                },
                AdditionalResources = new List<string>
                {
                    "Windows Administrator Guide: https://support.microsoft.com/windows/how-to-use-the-administrator-account"
                },
                ShouldOfferIssueReport = false
            };
        }

        private static TroubleshootingAdvice GetUnknownErrorAdvice(string errorDetails)
        {
            return new TroubleshootingAdvice
            {
                Title = "Unexpected Error Occurred",
                Description = "An unexpected error occurred during installation. This may be a new issue that needs to be reported.",
                Steps = new List<string>
                {
                    "General troubleshooting steps:",
                    "",
                    "Step 1: Restart and Retry",
                    "1. Restart your computer",
                    "2. Run the installer again as Administrator",
                    "",
                    "Step 2: Check System Requirements",
                    "1. Verify you're running Windows 10 (1903+) or Windows 11",
                    "2. Verify you have at least 10 GB free disk space",
                    "3. Verify you have a stable internet connection",
                    "",
                    "Step 3: Review Log File",
                    "1. Click 'Open Log' button in the installer",
                    "2. Look for specific error messages",
                    "3. Search for the error online or in GitHub issues",
                    "",
                    "Step 4: Report the Issue",
                    "1. If the error persists, please report it on GitHub",
                    "2. Include the log file contents in your report",
                    "3. Describe what you were doing when the error occurred",
                    "4. The development team will investigate and provide a solution"
                },
                AdditionalResources = new List<string>
                {
                    "Report Issue: https://github.com/BOINC/boinc-buda-runner-wsl-installer/issues/new",
                    "Existing Issues: https://github.com/BOINC/boinc-buda-runner-wsl-installer/issues",
                    "BOINC Forums: https://boinc.berkeley.edu/dev/"
                }
            };
        }

        public static string FormatAdviceAsMessage(TroubleshootingAdvice advice)
        {
            var sb = new StringBuilder();
            sb.AppendLine(advice.Title);
            sb.AppendLine(new string('=', advice.Title.Length));
            sb.AppendLine();
            sb.AppendLine(advice.Description);
            sb.AppendLine();

            if (advice.Steps.Count > 0)
            {
                foreach (var step in advice.Steps)
                {
                    if (string.IsNullOrEmpty(step))
                    {
                        sb.AppendLine();
                    }
                    else
                    {
                        sb.AppendLine(step);
                    }
                }
                sb.AppendLine();
            }

            if (advice.AdditionalResources.Count > 0)
            {
                sb.AppendLine("Additional Resources:");
                foreach (var resource in advice.AdditionalResources)
                {
                    sb.AppendLine($"  • {resource}");
                }
            }

            return sb.ToString();
        }

        public static ErrorCategory CategorizeError(string componentName, string errorMessage)
        {
            if (string.IsNullOrEmpty(errorMessage))
            {
                return ErrorCategory.UnknownError;
            }

            var lowerError = errorMessage.ToLowerInvariant();
            var lowerComponent = componentName?.ToLowerInvariant() ?? string.Empty;

            // Windows version issues
            if (lowerComponent.Contains("windowsversion") || lowerError.Contains("windows version"))
            {
                return ErrorCategory.WindowsVersion;
            }

            // Windows features issues
            if (lowerComponent.Contains("windowsfeatures") || lowerError.Contains("windows feature"))
            {
                return ErrorCategory.WindowsFeatures;
            }

            // WSL related issues
            if (lowerComponent.Contains("wsl"))
            {
                if (lowerError.Contains("not installed"))
                {
                    return ErrorCategory.WslNotInstalled;
                }
                if (lowerError.Contains("version") && (lowerError.Contains("mismatch") || lowerError.Contains("outdated") || lowerError.Contains("default")))
                {
                    return ErrorCategory.WslVersionMismatch;
                }
                if (lowerError.Contains("failed to install") || lowerError.Contains("installation failed"))
                {
                    return ErrorCategory.WslInstallationFailed;
                }
                return ErrorCategory.WslCheckFailed;
            }

            // BOINC process issues
            if (lowerComponent.Contains("boinc") && lowerError.Contains("running"))
            {
                return ErrorCategory.BoincRunning;
            }

            // BUDA Runner issues
            if (lowerComponent.Contains("buda"))
            {
                if (lowerError.Contains("version") && (lowerError.Contains("cannot") || lowerError.Contains("unable")))
                {
                    return ErrorCategory.BudaRunnerVersionCheckFailed;
                }
                if (lowerError.Contains("failed") || lowerError.Contains("error"))
                {
                    return ErrorCategory.BudaRunnerInstallFailed;
                }
            }

            // Network issues
            if (lowerError.Contains("network") || lowerError.Contains("connection") || 
                lowerError.Contains("timeout") || lowerError.Contains("download"))
            {
                return ErrorCategory.NetworkError;
            }

            // Permission issues
            if (lowerError.Contains("permission") || lowerError.Contains("access denied") || 
                lowerError.Contains("administrator") || lowerError.Contains("0x80070005"))
            {
                return ErrorCategory.PermissionError;
            }

            return ErrorCategory.UnknownError;
        }
    }
}
