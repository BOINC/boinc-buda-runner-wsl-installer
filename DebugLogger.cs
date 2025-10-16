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
using System.IO;
using System.Text;

namespace boinc_buda_runner_wsl_installer
{
    /// <summary>
    /// Debug logging utility that writes verbose information to a log file and optionally to console
    /// </summary>
    internal static class DebugLogger
    {
        private static string _logFilePath;
        private static bool _isEnabled = true; // Always enabled
        private static readonly object _lockObject = new object();
        private static bool _initialized = false;

        // Console logging configuration
        private static bool _consoleInfoEnabled = false;
        private static bool _consoleDebugEnabled = false;
        private static bool _consoleErrorEnabled = false;

        /// <summary>
        /// Configure console logging behavior (stdout/stderr). INFO and WARN go to stdout if infoEnabled;
        /// DEBUG goes to stdout if debugEnabled; ERROR goes to stderr if errorEnabled.
        /// </summary>
        public static void ConfigureConsoleLogging(bool infoEnabled, bool debugEnabled, bool errorEnabled)
        {
            _consoleInfoEnabled = infoEnabled;
            _consoleDebugEnabled = debugEnabled;
            _consoleErrorEnabled = errorEnabled;
        }

        /// <summary>
        /// Gets or sets whether debug logging is enabled
        /// </summary>
        public static bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                _isEnabled = value;
                if (_isEnabled && string.IsNullOrEmpty(_logFilePath))
                {
                    InitializeLogFile();
                }
            }
        }

        /// <summary>
        /// Gets the current log file path
        /// </summary>
        public static string LogFilePath => _logFilePath;

        /// <summary>
        /// Initializes the log file with a timestamped filename in the temp directory
        /// </summary>
        private static void InitializeLogFile()
        {
            if (_initialized) return;
            _initialized = true;

            try
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var fileName = $"boinc_buda_installer_debug_{timestamp}.log";
                var tempPath = Path.GetTempPath();
                _logFilePath = Path.Combine(tempPath, fileName);

                // Create the initial log entry
                LogInfo($"Debug logging started at {DateTime.Now:yyyy-MM-dd HH:mm:ss}", "DebugLogger");
                LogInfo($"Log file: {_logFilePath}", "DebugLogger");
                LogInfo($"Application: BOINC WSL Distro Installer", "DebugLogger");
                LogInfo($"Application Version: 2.1.0", "DebugLogger");
                LogInfo($"Windows Version: {Environment.OSVersion}", "DebugLogger");
                LogInfo($"Is 64-bit OS: {Environment.Is64BitOperatingSystem}", "DebugLogger");
                LogInfo($"Is 64-bit Process: {Environment.Is64BitProcess}", "DebugLogger");
                LogInfo($"CLR Version: {Environment.Version}", "DebugLogger");
                LogInfo($"Working Directory: {Environment.CurrentDirectory}", "DebugLogger");
                LogInfo($"User Domain: {Environment.UserDomainName}", "DebugLogger");
                LogInfo($"User Name: {Environment.UserName}", "DebugLogger");
                LogInfo("=" + new string('=', 70), "DebugLogger");
            }
            catch
            {
                // If we can't create the log file, disable logging silently
                _isEnabled = false;
            }
        }

        /// <summary>
        /// Logs an informational message
        /// </summary>
        public static void LogInfo(string message, string component = null)
        {
            Log("INFO", message, component);
        }

        /// <summary>
        /// Logs a warning message
        /// </summary>
        public static void LogWarning(string message, string component = null)
        {
            Log("WARN", message, component);
        }

        /// <summary>
        /// Logs an error message
        /// </summary>
        public static void LogError(string message, string component = null)
        {
            Log("ERROR", message, component);
        }

        /// <summary>
        /// Logs an exception with full details
        /// </summary>
        public static void LogException(Exception ex, string context = null, string component = null)
        {
            var message = context != null ? $"{context}: {ex}" : ex.ToString();
            Log("ERROR", message, component);
        }

        /// <summary>
        /// Logs a debug message (most verbose level)
        /// </summary>
        public static void LogDebug(string message, string component = null)
        {
            Log("DEBUG", message, component);
        }

        /// <summary>
        /// Logs the start of a method or operation
        /// </summary>
        public static void LogMethodStart(string methodName, string parameters = null, string component = null)
        {
            var message = parameters != null ? $"Starting {methodName}({parameters})" : $"Starting {methodName}()";
            Log("DEBUG", message, component);
        }

        /// <summary>
        /// Logs the end of a method or operation with result
        /// </summary>
        public static void LogMethodEnd(string methodName, string result = null, string component = null)
        {
            var message = result != null ? $"Completed {methodName} -> {result}" : $"Completed {methodName}";
            Log("DEBUG", message, component);
        }

        /// <summary>
        /// Logs process execution details
        /// </summary>
        public static void LogProcessExecution(string fileName, string arguments, int exitCode, string output = null, string error = null, string component = null)
        {
            LogDebug($"Process: {fileName} {arguments}", component);
            LogDebug($"Exit Code: {exitCode}", component);

            if (!string.IsNullOrEmpty(output))
            {
                LogDebug($"Output: {output.Trim()}", component);
            }

            if (!string.IsNullOrEmpty(error))
            {
                LogWarning($"Error Output: {error.Trim()}", component);
            }
        }

        /// <summary>
        /// Logs UI status changes
        /// </summary>
        public static void LogUIStatusChange(string elementId, string newIcon, string newStatus, string component = "UI")
        {
            LogDebug($"UI Update: {elementId} -> Icon: {newIcon}, Status: {newStatus}", component);
        }

        /// <summary>
        /// Logs configuration or state information
        /// </summary>
        public static void LogConfiguration(string name, object value, string component = null)
        {
            LogInfo($"Config: {name} = {value}", component);
        }

        /// <summary>
        /// Core logging method that writes to the file
        /// </summary>
        private static void Log(string level, string message, string component)
        {
            // Write to file
            if (_isEnabled)
            {
                if (string.IsNullOrEmpty(_logFilePath))
                {
                    InitializeLogFile();
                }
                try
                {
                    if (!string.IsNullOrEmpty(_logFilePath))
                    {
                        lock (_lockObject)
                        {
                            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                            var threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
                            var componentPart = !string.IsNullOrEmpty(component) ? $"[{component}] " : "";
                            var logEntry = $"{timestamp} [{level}] [T{threadId:D2}] {componentPart}{message}";
                            File.AppendAllText(_logFilePath, logEntry + Environment.NewLine, Encoding.UTF8);
                        }
                    }
                }
                catch { }
            }

            // Write to console according to configuration
            try
            {
                if (level == "ERROR")
                {
                    if (_consoleErrorEnabled)
                    {
                        Console.Error.WriteLine(message);
                    }
                }
                else if (level == "DEBUG")
                {
                    if (_consoleDebugEnabled)
                    {
                        Console.Out.WriteLine(message);
                    }
                }
                else // INFO or WARN
                {
                    if (_consoleInfoEnabled)
                    {
                        Console.Out.WriteLine(message);
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Logs a section separator for better readability
        /// </summary>
        public static void LogSeparator(string title = null)
        {
            if (!_isEnabled) return;

            var separator = new string('-', 50);
            if (!string.IsNullOrEmpty(title))
            {
                LogInfo($"{separator} {title} {separator}");
            }
            else
            {
                LogInfo(separator);
            }
        }

        /// <summary>
        /// Flushes any pending log entries (useful before app shutdown)
        /// </summary>
        public static void Flush()
        {
            if (_isEnabled && !string.IsNullOrEmpty(_logFilePath))
            {
                LogInfo("Debug logging session ended.", "DebugLogger");
            }
        }
    }
}
