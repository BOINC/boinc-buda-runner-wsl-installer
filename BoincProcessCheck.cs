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
using System.Threading.Tasks;

namespace boinc_buda_runner_wsl_installer
{
    internal class BoincProcessCheck
    {
        private const string COMPONENT = "BoincProcessCheck";

        public enum BoincProcessStatus
        {
            Running,
            NotRunning,
            Error
        }

        public class BoincProcessCheckResult
        {
            public BoincProcessStatus Status { get; set; }
            public string Message { get; set; }
            public int ProcessCount { get; set; }
            public string ErrorMessage { get; set; }
        }

        /// <summary>
        /// Checks if BOINC process is running on the system
        /// </summary>
        /// <returns>BoincProcessCheckResult with process status and details</returns>
        public static async Task<BoincProcessCheckResult> CheckBoincProcessAsync()
        {
            DebugLogger.LogMethodStart("CheckBoincProcessAsync", component: COMPONENT);
            var result = new BoincProcessCheckResult();

            try
            {
                await Task.Run(() =>
                {
                    DebugLogger.LogInfo("Searching for BOINC processes", COMPONENT);

                    // Get all processes named "boinc"
                    var boincProcesses = Process.GetProcessesByName("boinc");
                    result.ProcessCount = boincProcesses.Length;

                    DebugLogger.LogConfiguration("BOINC Process Count", result.ProcessCount, COMPONENT);

                    if (boincProcesses.Length == 0)
                    {
                        DebugLogger.LogInfo("No BOINC processes found", COMPONENT);
                        result.Status = BoincProcessStatus.NotRunning;
                        result.Message = "BOINC process is not running";
                    }
                    else
                    {
                        DebugLogger.LogWarning($"Found {boincProcesses.Length} BOINC process(es) running", COMPONENT);

                        // Log details of all running BOINC processes
                        for (int i = 0; i < boincProcesses.Length; i++)
                        {
                            try
                            {
                                var process = boincProcesses[i];
                                DebugLogger.LogConfiguration($"BOINC Process #{i + 1} PID", process.Id, COMPONENT);
                                DebugLogger.LogConfiguration($"BOINC Process #{i + 1} Process Name", process.ProcessName, COMPONENT);
                                DebugLogger.LogConfiguration($"BOINC Process #{i + 1} Start Time", process.StartTime.ToString("yyyy-MM-dd HH:mm:ss"), COMPONENT);

                                // Try to get additional information safely
                                try
                                {
                                    DebugLogger.LogConfiguration($"BOINC Process #{i + 1} Main Window Title", process.MainWindowTitle ?? "N/A", COMPONENT);
                                    DebugLogger.LogConfiguration($"BOINC Process #{i + 1} Working Set", $"{process.WorkingSet64 / 1024 / 1024} MB", COMPONENT);
                                }
                                catch (Exception ex)
                                {
                                    DebugLogger.LogException(ex, $"Error getting extended info for BOINC process #{i + 1}", COMPONENT);
                                }
                            }
                            catch (Exception ex)
                            {
                                DebugLogger.LogException(ex, $"Error getting info for BOINC process #{i + 1}", COMPONENT);
                            }
                        }

                        result.Status = BoincProcessStatus.Running;
                        result.Message = $"BOINC process is running (PID: {boincProcesses[0].Id})";
                    }

                    // Dispose of process objects
                    DebugLogger.LogInfo("Disposing of process objects", COMPONENT);
                    foreach (var process in boincProcesses)
                    {
                        try
                        {
                            process?.Dispose();
                        }
                        catch (Exception ex)
                        {
                            DebugLogger.LogException(ex, "Error disposing process object", COMPONENT);
                        }
                    }
                });

                DebugLogger.LogConfiguration("Final Status", result.Status, COMPONENT);
                DebugLogger.LogConfiguration("Final Message", result.Message, COMPONENT);
                DebugLogger.LogMethodEnd("CheckBoincProcessAsync", $"Status: {result.Status}", COMPONENT);
                return result;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException(ex, "Error in CheckBoincProcessAsync", COMPONENT);
                result.Status = BoincProcessStatus.Error;
                result.ErrorMessage = $"Error checking BOINC process: {ex.Message}";
                result.Message = "Failed to check BOINC process status";
                DebugLogger.LogMethodEnd("CheckBoincProcessAsync", $"Status: {result.Status} (Exception)", COMPONENT);
                return result;
            }
        }
    }
}
