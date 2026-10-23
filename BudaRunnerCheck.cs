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
using System.Text;
using System.Threading.Tasks;

namespace boinc_buda_runner_wsl_installer
{
    internal class BudaRunnerCheck
    {
        private const string COMPONENT = "BudaRunnerCheck";

        public const string BUDA_RUNNER_IMAGE_NAME = "boinc-buda-runner";

        /// <summary>
        /// Checks if WSL image is installed
        /// </summary>
        public static async Task<bool> IsWslImageInstalledAsync(string imageName)
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
        /// Removes WSL image
        /// </summary>
        public static async Task<bool> RemoveWslImageAsync(string imageName)
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
    }
}
