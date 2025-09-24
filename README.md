# BOINC WSL Distro Installer

A Windows desktop installer that prepares your system to run the BOINC WSL Distro (BOINC environment under Windows Subsystem for Linux 2) and installs or updates it for you.

The app is a WPF application targeting .NET Framework 4.8.

## What it does

The installer performs a sequence of checks and guided actions:

- Check installer update
  - Queries the latest GitHub release of this installer and informs you if a newer version exists.
  - Note: The installer does not self-update. If an update is available, please download it from the [Releases page](https://github.com/BOINC/boinc-buda-runner-wsl-installer/releases).
- Check Windows version
  - Verifies that your Windows version supports WSL2.
- Check Windows features
  - Ensures required Windows features (e.g., Virtual Machine Platform, WSL) are enabled.
  - Attempts to enable missing features. A restart may be required.
- Check WSL installation and configuration
  - Installs WSL if missing, or fixes common configuration issues.
  - Sets the default WSL version to 2.
- Check BOINC process
  - Detects if a BOINC client is currently running. If running, you’ll be asked to stop it before continuing.
- Check BOINC WSL Distro
  - Installs or updates the BOINC WSL Distro to the latest version, or confirms that it’s up to date.

## Requirements

- Supported OS:
  - Windows 11 (recommended)
  - Windows 10 version 2004 (build 19041) or newer (WSL2 support required)
- Administrator privileges
  - Enabling Windows features and installing WSL require elevation. Run the installer as Administrator.
- Internet access
  - Required to query GitHub releases, download WSL (when needed), and download the BOINC WSL Distro.

## Quick start

1. Download the latest release of the installer:
   - https://github.com/BOINC/boinc-buda-runner-wsl-installer/releases
2. Right-click the downloaded installer executable and choose "Run as administrator".
3. Click "Install".
4. Follow on-screen prompts. If a restart is required after enabling Windows features, restart and re-run the installer.
5. When all checks pass, the BOINC WSL Distro will be installed and ready to use.

## UI overview

- `Install`
  - Starts the full check-and-install sequence described above.
- `Exit`
  - Closes the application. A confirmation dialog is shown. The same confirmation appears when closing the window via the system close button (X).
- `Open Log`
  - Opens the current debug log file in your system’s default editor.

The main view shows a list of steps with a status icon and text updated as the installer runs.

## Logging and support

- Debug logging is enabled by default.
- Click `Open Log` to view the current log file.
- If an error occurs, the app can help you open a pre-filled GitHub issue. Attach the log file to help with troubleshooting.
- Report issues here: https://github.com/BOINC/boinc-buda-runner-wsl-installer/issues

## Network endpoints used

- GitHub Releases API to check for updates to this installer.
- Official sources to download WSL and BOINC WSL Distro artifacts when needed.

## Build from source

Prerequisites:
- Windows 10/11
- Visual Studio 2022 with the ".NET desktop development" workload
- .NET Framework 4.8 Developer Pack

Steps:
- Clone this repository
- Open the solution/project in Visual Studio 2022
- Build and run the `boinc-buda-runner-wsl-installer` project

Project notes:
- Language version: C# 7.3
- UI: WPF

## Limitations

- The installer does not self-update. When a newer installer is available, you’ll be informed and can download it from the Releases page.
- If BOINC is running, installation is paused until the user stops BOINC.

## Contributing

Contributions are welcome!
- Open issues for bugs and feature requests: https://github.com/BOINC/boinc-buda-runner-wsl-installer/issues
- Submit pull requests with clear descriptions and testing notes.

## License

This project is licensed under the GNU Lesser General Public License v3.0 or later (LGPL-3.0-or-later).

- SPDX: LGPL-3.0-or-later
- Full text: https://www.gnu.org/licenses/lgpl-3.0.html

## Acknowledgements

- BOINC community © https://boinc.berkeley.edu
- Microsoft WSL © https://learn.microsoft.com/windows/wsl/
