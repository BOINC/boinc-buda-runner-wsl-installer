# BOINC BUDA Runner WSL Installer - Troubleshooting Guide

This document provides comprehensive troubleshooting guidance for common issues encountered during the BOINC BUDA Runner WSL installation process.

## Overview

The installer has been enhanced with intelligent error detection and user-friendly troubleshooting guidance. When an error occurs, the installer will:

1. Automatically categorize the error
2. Provide step-by-step troubleshooting instructions
3. Offer relevant resources and documentation links
4. Allow you to report the issue on GitHub if needed

## Common Installation Issues

### 1. Windows Version Not Supported

**Symptom**: Installer reports your Windows version is not supported.

**Solution**:
- WSL 2 requires Windows 10 version 1903 (Build 18362) or later, or Windows 11
- Check your version: Press Win+R, type `winver`, and press Enter
- Update Windows through Settings > Update & Security > Windows Update
- Restart and run the installer again

**Resources**:
- [Windows 10 Update Assistant](https://www.microsoft.com/software-download/windows10)
- [Windows 11 Upgrade](https://www.microsoft.com/software-download/windows11)

### 2. Windows Features Not Enabled

**Symptom**: Required Windows features are missing or disabled.

**Solution**:
- The installer will automatically enable required features
- If automatic enablement fails, enable manually:
  1. Search for "Turn Windows features on or off" in Start menu
  2. Enable "Virtual Machine Platform"
  3. Enable "Windows Subsystem for Linux"
  4. Restart your computer
  5. Run the installer again

**Resources**:
- [Manual WSL Installation Guide](https://docs.microsoft.com/windows/wsl/install-manual)

### 3. WSL Not Installed

**Symptom**: WSL is not installed on your system.

**Solution**:
- The installer will automatically download and install WSL
- For manual installation:
  1. Open PowerShell as Administrator
  2. Run: `wsl --install`
  3. Restart when prompted
  4. Run the installer again

**Alternative**: Install WSL from Microsoft Store
- Open Microsoft Store
- Search for "Windows Subsystem for Linux"
- Install and restart

**Resources**:
- [WSL Installation Guide](https://docs.microsoft.com/windows/wsl/install)
- [WSL Microsoft Store](ms-windows-store://pdp/?ProductId=9P9TQF7MRM4R)

### 4. WSL Version Issues

**Symptom**: WSL version is outdated or default version is not set to 2.

**Solution**:
- The installer will automatically update WSL
- For manual update:
  1. Open PowerShell as Administrator
  2. Run: `wsl --update`
  3. Run: `wsl --set-default-version 2`
  4. Run the installer again

**Resources**:
- [WSL Update Guide](https://docs.microsoft.com/windows/wsl/install)
- [WSL Releases](https://github.com/microsoft/WSL/releases)

### 5. WSL Installation Failed

**Symptom**: Automatic WSL installation encounters an error.

**Common Causes**:
- **Error 0x80070057**: Invalid parameter - may be due to corrupted download
- **Error 0x80070002**: Missing files - system files may be corrupted
- **Error 0x80070005**: Permission denied - run as Administrator
- **Error 0x800701bc**: Virtualization not enabled - enable in BIOS

**Solutions**:

#### Solution 1: Run as Administrator
1. Right-click the installer
2. Select "Run as administrator"
3. Try again

#### Solution 2: Manual WSL Installation
1. Open PowerShell as Administrator
2. Run: `wsl --install`
3. If that fails, try: `wsl --install --web-download`
4. Restart when prompted

#### Solution 3: Fix System Files
1. Open Command Prompt as Admin
2. Run: `sfc /scannow`
3. Wait for completion
4. Restart and try again

#### Solution 4: Enable Virtualization
1. Restart and enter BIOS/UEFI settings
2. Find and enable Intel VT-x or AMD-V
3. Save and exit
4. Run the installer again

**Resources**:
- [WSL Troubleshooting](https://docs.microsoft.com/windows/wsl/troubleshooting)
- [WSL Manual Installation](https://docs.microsoft.com/windows/wsl/install-manual)

### 6. BOINC Client is Running

**Symptom**: BOINC client must be stopped before installation.

**Solution**:

#### Method 1: BOINC Manager
1. Open BOINC Manager
2. Go to File > Exit BOINC
3. Wait a few seconds
4. Run the installer again

#### Method 2: Task Manager
1. Open Task Manager (Ctrl+Shift+Esc)
2. Find `boinc.exe` or `boincmgr.exe`
3. Select and click "End task"
4. Run the installer again

#### Method 3: Services
1. Open Services (services.msc)
2. Find "BOINC" service
3. Right-click and select "Stop"
4. Run the installer again

### 7. BOINC WSL Distro Installation Failed

**Symptom**: Failed to install or update BOINC WSL Distro.

**Common Causes**:
- **Error 0x80070070**: Insufficient disk space (need at least 10 GB free)
- **Error 0x80070032**: Distribution already in use
- **Timeout**: Slow internet connection or system performance

**Solutions**:

#### Solution 1: Verify Prerequisites
1. Check WSL is installed: `wsl --version`
2. Verify at least 10 GB free disk space
3. Check internet connection is stable

#### Solution 2: Manual Installation
1. Download latest release from [BOINC WSL Distro Releases](https://github.com/BOINC/boinc-buda-runner-wsl/releases/latest)
2. Find the .wsl file for your architecture (x64 or ARM64)
3. Open PowerShell as Administrator
4. Run: `wsl --install --from-file "path\to\downloaded.wsl" --no-launch`
5. Run the installer again to verify

#### Solution 3: Clean Up and Retry
1. Open PowerShell as Administrator
2. Run: `wsl --list`
3. If "boinc-buda-runner" exists: `wsl --unregister boinc-buda-runner`
4. Run the installer again

#### Solution 4: Check WSL Status
1. Run: `wsl --status`
2. Verify default version is 2
3. If not: `wsl --set-default-version 2`
4. Run the installer again

**Resources**:
- [BOINC WSL Distro Releases](https://github.com/BOINC/boinc-buda-runner-wsl/releases)
- [WSL Import Documentation](https://docs.microsoft.com/windows/wsl/use-custom-distro)

### 8. Network Connection Errors

**Symptom**: Cannot download required components.

**Solutions**:

#### Solution 1: Check Internet Connection
1. Verify you can access https://github.com in browser
2. Try downloading files from other websites
3. If internet is down, connect and try again

#### Solution 2: Disable VPN/Proxy
1. Temporarily disconnect VPN
2. Configure proxy or connect directly
3. Try installation again
4. Re-enable VPN/proxy after installation

#### Solution 3: Check Firewall
1. Check if Windows Firewall is blocking installer
2. Temporarily disable firewall (remember to re-enable)
3. Try installation again

#### Solution 4: Manual Download
1. Manually download required files from GitHub
2. WSL: https://github.com/microsoft/WSL/releases/latest
3. BOINC WSL Distro: https://github.com/BOINC/boinc-buda-runner-wsl/releases/latest
4. Follow manual installation instructions

### 9. Permission Errors

**Symptom**: Access denied or insufficient permissions.

**Solution**:
1. Close the installer
2. Right-click the installer executable
3. Select "Run as administrator"
4. Click "Yes" on User Account Control prompt

**Note**: Administrator rights are required for:
- Installing Windows features
- Installing WSL
- Configuring system settings

### 10. Version Check Failed

**Symptom**: Cannot determine BOINC WSL Distro version.

**Solutions**:

#### Solution 1: Reinstall
1. Open PowerShell as Administrator
2. Run: `wsl --unregister boinc-buda-runner`
3. Run the installer again

#### Solution 2: Manual Version Check
1. Open PowerShell
2. Run: `wsl -d boinc-buda-runner cat /home/boinc/version.txt`
3. If you see a version number, installation is OK
4. If error, use Solution 1

#### Solution 3: Verify Distribution
1. Run: `wsl --list --verbose`
2. Verify "boinc-buda-runner" is listed
3. Verify status shows "Running" or "Stopped" (not "Installing")
4. If status looks wrong, use Solution 1

## Using the Installer

### Features

1. **Automatic Error Detection**: The installer automatically detects and categorizes errors
2. **Troubleshooting Dialog**: User-friendly dialog shows step-by-step solutions
3. **Copy to Clipboard**: Copy troubleshooting information for reference
4. **Report on GitHub**: Directly report issues with pre-filled information
5. **Log Files**: Detailed logs for debugging (access via "Open Log" button)

### Installation Process

The installer checks and configures the following in order:

1. **Installer Update**: Checks for newer installer version
2. **Windows Version**: Verifies Windows 10 (1903+) or Windows 11
3. **Windows Features**: Enables Virtual Machine Platform and WSL
4. **WSL Installation**: Installs and configures WSL 2
5. **BOINC Process**: Verifies BOINC is not running
6. **BOINC WSL Distro**: Installs and configures the BOINC WSL distribution

### Running the Installer

**Recommended**:
1. Right-click the installer
2. Select "Run as administrator"
3. Follow on-screen instructions

**If Installation Fails**:
1. Note the error message
2. Click the troubleshooting dialog that appears
3. Follow the step-by-step solutions
4. If problem persists, click "Report Issue on GitHub"
5. Attach the log file from "Open Log" button

## Advanced Troubleshooting

### Enable Detailed Logging

Detailed logging is enabled by default. Log files are saved to:
- `%TEMP%\BOINC_WSL_Installer_YYYYMMDD_HHMMSS.log`

To view logs:
1. Click "Open Log" button in installer
2. Or navigate to `%TEMP%` folder
3. Look for files starting with `BOINC_WSL_Installer_`

### Check Windows Features Status

To manually check if Windows features are enabled:

```powershell
# Open PowerShell as Administrator
dism.exe /online /get-featureinfo /featurename:VirtualMachinePlatform
dism.exe /online /get-featureinfo /featurename:Microsoft-Windows-Subsystem-Linux
```

### Check WSL Status

```powershell
# Check WSL version
wsl --version

# Check installed distributions
wsl --list --verbose

# Check WSL status
wsl --status
```

### Check BOINC WSL Distro

```powershell
# Check if distribution is installed
wsl --list | findstr boinc-buda-runner

# Check version file
wsl -d boinc-buda-runner cat /home/boinc/version.txt

# Start distribution
wsl -d boinc-buda-runner
```

### System Requirements

**Minimum Requirements**:
- Windows 10 version 1903 (Build 18362) or later, or Windows 11
- 64-bit processor with Second Level Address Translation (SLAT)
- 4GB system memory
- BIOS-level hardware virtualization support must be enabled
- At least 10 GB free disk space

**Check Your System**:
1. Windows version: Press Win+R, type `winver`, press Enter
2. Processor: Task Manager > Performance > CPU
3. Memory: Task Manager > Performance > Memory
4. Disk space: File Explorer > This PC

### Virtualization Support

**Check if virtualization is enabled**:
1. Open Task Manager (Ctrl+Shift+Esc)
2. Go to Performance tab
3. Click CPU
4. Look for "Virtualization: Enabled"

**If Virtualization is Disabled**:
1. Restart computer
2. Enter BIOS/UEFI settings (usually Del, F2, F10, or Esc during boot)
3. Find virtualization settings (Intel VT-x or AMD-V)
4. Enable virtualization
5. Save and exit BIOS
6. Run the installer again

## Reporting Issues

If you encounter an issue not covered in this guide:

1. **Use the Troubleshooting Dialog**:
   - The installer will show a troubleshooting dialog when errors occur
   - Review the suggested solutions
   - Try the recommended steps

2. **Gather Information**:
   - Click "Open Log" button to view the log file
   - Note your Windows version and system configuration
   - Note the exact error message
   - Note what you were doing when the error occurred

3. **Report on GitHub**:
   - Click "Report Issue on GitHub" in the troubleshooting dialog
   - Or visit: https://github.com/BOINC/boinc-buda-runner-wsl-installer/issues/new
   - Fill in the pre-populated template
   - Attach your log file
   - Provide detailed description

4. **Check Existing Issues**:
   - Search existing issues: https://github.com/BOINC/boinc-buda-runner-wsl-installer/issues
   - Your issue may already be reported with a solution

## Additional Resources

### Official Documentation
- [BOINC Project](https://boinc.berkeley.edu/)
- [BOINC WSL Distro Repository](https://github.com/BOINC/boinc-buda-runner-wsl)
- [WSL Documentation](https://docs.microsoft.com/windows/wsl/)
- [WSL Troubleshooting Guide](https://docs.microsoft.com/windows/wsl/troubleshooting)

### Community Support
- [BOINC Forums](https://boinc.berkeley.edu/dev/)
- [BOINC Reddit](https://www.reddit.com/r/BOINC/)
- [GitHub Issues](https://github.com/BOINC/boinc-buda-runner-wsl-installer/issues)

### Microsoft Resources
- [Windows Update](https://support.microsoft.com/windows/update-windows)
- [Windows Subsystem for Linux](https://docs.microsoft.com/windows/wsl/)
- [Virtual Machine Platform](https://docs.microsoft.com/virtualization/hyper-v-on-windows/quick-start/enable-hyper-v)
