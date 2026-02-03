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

using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace boinc_buda_runner_wsl_installer
{
    internal static class DownloadVerification
    {
        public static string ExtractJsonStringValue(string json, string propertyName)
        {
            if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(propertyName)) return null;
            var pattern = $"\"{Regex.Escape(propertyName)}\"\\s*:\\s*\"((?:\\\\.|[^\"])*)\"";
            var match = Regex.Match(json, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (!match.Success) return null;

            var value = match.Groups[1].Value;
            return UnescapeJsonString(value);
        }

        public static string TryExtractSha256FromBody(string releaseBody, string fileName)
        {
            if (string.IsNullOrEmpty(releaseBody) || string.IsNullOrEmpty(fileName)) return null;

            var patterns = new[]
            {
                $@"(?im)^(?<hash>[a-f0-9]{{64}})\s+\*?{Regex.Escape(fileName)}\s*$",
                $@"(?im)^{Regex.Escape(fileName)}\s*[:=]\s*(?<hash>[a-f0-9]{{64}})\s*$",
                $@"(?im)^SHA256\s*\(\s*{Regex.Escape(fileName)}\s*\)\s*=\s*(?<hash>[a-f0-9]{{64}})\s*$"
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(releaseBody, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                if (match.Success)
                {
                    return match.Groups["hash"].Value;
                }
            }

            return null;
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

        private static string UnescapeJsonString(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value
                .Replace("\\r", "\r")
                .Replace("\\n", "\n")
                .Replace("\\\"", "\"")
                .Replace("\\/", "/")
                .Replace("\\\\", "\\");
        }
    }
}
