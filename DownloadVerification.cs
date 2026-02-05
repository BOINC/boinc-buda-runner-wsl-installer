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
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace boinc_buda_runner_wsl_installer
{
    internal static class DownloadVerification
    {
        public static string TryGetSha256FromReleaseJson(string releaseJson, string downloadUrl)
        {
            if (string.IsNullOrEmpty(releaseJson) || string.IsNullOrEmpty(downloadUrl)) return null;

            var urlPattern = Regex.Escape(downloadUrl);
            var digestPatternAfter = $"\"browser_download_url\"\\s*:\\s*\"{urlPattern}\"(?:(?!\"browser_download_url\").)*?\"digest\"\\s*:\\s*\"([^\"]+)\"";
            var digestPatternBefore = $"\"digest\"\\s*:\\s*\"([^\"]+)\"(?:(?!\"browser_download_url\").)*?\"browser_download_url\"\\s*:\\s*\"{urlPattern}\"";

            var digestMatch = Regex.Match(releaseJson, digestPatternAfter, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (!digestMatch.Success)
            {
                digestMatch = Regex.Match(releaseJson, digestPatternBefore, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            }

            if (!digestMatch.Success) return null;

            var digest = digestMatch.Groups[1].Value;
            if (digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
            {
                digest = digest.Substring("sha256:".Length);
            }

            return digest;
        }

        public static string ComputeSha256(string filePath)
        {
            using (var stream = File.OpenRead(filePath))
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(stream);
                var sb = new StringBuilder(hash.Length * 2);
                foreach (var b in hash)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }

        public static string NormalizeHash(string hash)
        {
            return string.IsNullOrWhiteSpace(hash) ? null : Regex.Replace(hash.Trim(), @"\s+", string.Empty).ToLowerInvariant();
        }

    }
}
