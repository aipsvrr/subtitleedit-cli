﻿using System.Text;
using System.Text.RegularExpressions;

namespace seconv.libse.Forms
{
    public class CheckForUpdatesHelper
    {
        private static readonly Regex VersionNumberRegex = new Regex(@"\d\.\d", RegexOptions.Compiled); // 3.4.0 (xth June 2014)
        readonly List<string> _months = new List<string> { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };

        private const string ChangeLogUrl = "https://raw.githubusercontent.com/SubtitleEdit/subtitleedit/master/Changelog.txt";

        private string _changeLog;
        private int _successCount;

        public string Error { get; set; }

        public bool Done => _successCount == 1;

        public string LatestVersionNumber { get; set; }
        public string LatestChangeLog { get; set; }

        public CheckForUpdatesHelper()
        {
            Error = null;
            _successCount = 0;
        }

        private string GetLatestVersionNumber(string latestChangeLog)
        {
            foreach (var line in latestChangeLog.Replace(Environment.NewLine, "\n").Split('\n'))
            {
                var s = line.Trim();
                if (!s.Contains("BETA", StringComparison.OrdinalIgnoreCase) &&
                    !s.Contains('x') && 
                    !s.Contains('*') && 
                    s.Contains('(') && 
                    s.Contains(')') && 
                    _months.Any(month=>s.Contains(month)) &&
                    VersionNumberRegex.IsMatch(s))
                {
                    var indexOfSpace = s.IndexOf(' ');
                    if (indexOfSpace > 0)
                    {
                        return s.Substring(0, indexOfSpace).Trim();
                    }
                }
            }

            return null;
        }

        private string GetLatestChangeLog(string changeLog)
        {
            var releaseOn = false;
            var sb = new StringBuilder();
            foreach (var line in changeLog.Replace(Environment.NewLine, "\n").Split('\n'))
            {
                var s = line.Trim();
                if (s.Length == 0 && releaseOn)
                {
                    return sb.ToString();
                }

                if (!releaseOn)
                {
                    if (!s.Contains('x') && 
                        !s.Contains('*') && 
                        s.Contains('(') && 
                        s.Contains(')') &&
                        _months.Any(month => s.Contains(month)) &&
                        VersionNumberRegex.IsMatch(s))
                    {
                        releaseOn = true;
                    }
                }

                if (releaseOn)
                {
                    sb.AppendLine(line);
                }
            }

            return sb.ToString();
        }

        public void CheckForUpdates()
        {
            try
            {
                using (var httpClient = HttpClientHelper.MakeHttpClient())
                {
                    _changeLog = httpClient.GetStringAsync(ChangeLogUrl).Result;
                }

                LatestChangeLog = GetLatestChangeLog(_changeLog);
                LatestVersionNumber = GetLatestVersionNumber(LatestChangeLog);
                _successCount = 1;
            }
            catch (Exception exception)
            {
                if (Error == null)
                {
                    Error = exception.Message;
                }
            }
        }

        public bool IsUpdateAvailable()
        {
            if (!string.IsNullOrEmpty(Error))
            {
                return false;
            }

            try
            {
                //string[] currentVersionInfo = "3.3.14".Split('.'); // for testing...
                return Version.Parse(LatestVersionNumber) > Version.Parse(Utilities.AssemblyVersion);
            }
            catch
            {
                return false;
            }
        }
    }
}
