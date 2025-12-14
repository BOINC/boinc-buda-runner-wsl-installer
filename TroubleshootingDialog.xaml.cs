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
using System.Text;
using System.Windows;

namespace boinc_buda_runner_wsl_installer
{
    public partial class TroubleshootingDialog : Window
    {
        private TroubleshootingGuide.TroubleshootingAdvice _advice;
        private string _errorContext;

        public TroubleshootingDialog(TroubleshootingGuide.TroubleshootingAdvice advice, string errorContext = null)
        {
            InitializeComponent();
            _advice = advice;
            _errorContext = errorContext;
            
            LoadAdvice();
        }

        private void LoadAdvice()
        {
            TitleTextBlock.Text = _advice.Title;
            DescriptionTextBlock.Text = _advice.Description;

            var stepsBuilder = new StringBuilder();
            foreach (var step in _advice.Steps)
            {
                stepsBuilder.AppendLine(step);
            }
            StepsTextBlock.Text = stepsBuilder.ToString();

            if (_advice.AdditionalResources.Count > 0)
            {
                var resourcesBuilder = new StringBuilder();
                foreach (var resource in _advice.AdditionalResources)
                {
                    resourcesBuilder.AppendLine($"• {resource}");
                }
                ResourcesTextBlock.Text = resourcesBuilder.ToString();
                ResourcesExpander.Visibility = Visibility.Visible;
            }
            else
            {
                ResourcesExpander.Visibility = Visibility.Collapsed;
            }

            ReportIssueButton.Visibility = _advice.ShouldOfferIssueReport ? Visibility.Visible : Visibility.Collapsed;
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var fullText = TroubleshootingGuide.FormatAdviceAsMessage(_advice);
                
                if (!string.IsNullOrEmpty(_errorContext))
                {
                    fullText += "\n\nError Context:\n" + _errorContext;
                }

                Clipboard.SetText(fullText);
                MessageBox.Show(
                    "Troubleshooting information has been copied to clipboard.",
                    "Copied",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to copy to clipboard: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ReportIssueButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var issueTitle = Uri.EscapeDataString(_advice.Title);
                
                var issueBody = new StringBuilder();
                issueBody.AppendLine("## Problem Description");
                issueBody.AppendLine(_advice.Description);
                issueBody.AppendLine();
                issueBody.AppendLine("## Steps Attempted");
                issueBody.AppendLine("(Please describe what troubleshooting steps you've tried)");
                issueBody.AppendLine();
                
                if (!string.IsNullOrEmpty(_errorContext))
                {
                    issueBody.AppendLine("## Error Context");
                    issueBody.AppendLine("```");
                    issueBody.AppendLine(_errorContext);
                    issueBody.AppendLine("```");
                    issueBody.AppendLine();
                }

                issueBody.AppendLine("## System Information");
                issueBody.AppendLine($"- OS: {Environment.OSVersion}");
                issueBody.AppendLine($"- 64-bit OS: {Environment.Is64BitOperatingSystem}");
                issueBody.AppendLine($"- 64-bit Process: {Environment.Is64BitProcess}");
                
                try
                {
                    var version = FileVersionInfo.GetVersionInfo(Process.GetCurrentProcess().MainModule.FileName).FileVersion;
                    issueBody.AppendLine($"- Installer Version: {version}");
                }
                catch { }

                if (!string.IsNullOrEmpty(DebugLogger.LogFilePath))
                {
                    issueBody.AppendLine();
                    issueBody.AppendLine("## Log File");
                    issueBody.AppendLine($"Log file path: `{DebugLogger.LogFilePath}`");
                    issueBody.AppendLine("(Please attach the log file to this issue)");
                }

                var url = $"https://github.com/BOINC/boinc-buda-runner-wsl-installer/issues/new?title={issueTitle}&body={Uri.EscapeDataString(issueBody.ToString())}";
                
                Process.Start(url);
                
                MessageBox.Show(
                    "Your web browser will open to report this issue on GitHub.\n\nPlease provide as much detail as possible and attach your log file if available.",
                    "Report Issue",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to open browser: {ex.Message}\n\nPlease manually visit:\nhttps://github.com/BOINC/boinc-buda-runner-wsl-installer/issues/new",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
