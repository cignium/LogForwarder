using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace WatcherLog
{
    class Program
    {
        private static LogMonitor _logMonitor;
        private static bool _switch = true;

        static void Main()
        {
            _logMonitor = new LogMonitor();
            while (_switch)
            {
                try
                {
                    _logMonitor.Exec();
                }
                catch (Exception ex)
                {
                    Console.Out.WriteLine(ex.Message);
                    Console.Out.WriteLine("Error. Stopping Monitor...");
                    _switch = false;
                }
            }
        }
    }

    class LogMonitor
    {
        
        private bool _initizated;
        private readonly string _logFile;
        private readonly string _logHistoryFile;
        private static int _lineNumber;
        private static DateTime _dateCreatedLog;
        private SplunkHelper _splunkHelper;
        
        public LogMonitor()
        {
            _logFile = GetLogFile();
            _logHistoryFile = GetHistoryFile();
            _lineNumber = 0;
        }

        public void Exec()
        {
            if (_splunkHelper == null)
                _splunkHelper = new SplunkHelper(GetSplunkHostName(), GetSplunkUserName(), GetSplunkPassword(), GetIndexName());

            if (!_initizated)
                InitLogFile();

            // You can use ReadAndSend() method if wanna send one line each time
            ReadLinesAndSend();

            var delay = Convert.ToInt32(GetDelay()) * 1000;
            Thread.Sleep(delay);
        }

        private void InitLogFile()
        {
            Console.Out.WriteLine("Initializing Log reader");

            if (!LogHistoryFileExists())
                WriteHistory();
            else
            {
                SetDateCreatedLogFromHistory();
                if (_dateCreatedLog != ReadDateCreatedLogOfFile())
                    ResetHistory();
            }

            SetLineNumberFromHistory();
            _initizated = true;

            Console.Out.WriteLine("Log reader initialized");
        }

        private void ReadAndSend()
        {
            var line = ReadLogLine();
            if (line == null) return;
            
            if (!_splunkHelper.SendMessage(line))
                return;

            _lineNumber++;
            WriteHistory();
        }

        private void ReadLinesAndSend()
        {
            var lines = ReadLogLines();
            if (!lines.Any()) return;

            if (!_splunkHelper.SendMessages(lines))
                return;

            _lineNumber += lines.Count;
            WriteHistory();
        }

        private void ResetHistory(int lineNumber = 0)
        {
            _lineNumber = lineNumber;
            _dateCreatedLog = ReadDateCreatedLogOfFile();
            WriteHistory();

            Console.Out.WriteLine($"Log reader reseted with lineNumber = {lineNumber}");
        }

        #region IOMethods

        private void WriteHistory()
        {
            using (var sw = new StreamWriter(_logHistoryFile, false))
            {
                sw.WriteLine(_lineNumber);
                sw.WriteLine(_dateCreatedLog);
            }
        }

        private void SetLineNumberFromHistory()
        {
            var line = ReadLines(_logHistoryFile).ElementAt(0);
            int.TryParse(line, out _lineNumber);
        }

        private void SetDateCreatedLogFromHistory()
        {
            try
            {
                var line = ReadLines(_logHistoryFile).ElementAt(1);
                DateTime.TryParse(line, out _dateCreatedLog);
            }
            catch (ArgumentOutOfRangeException) // Catch exception for sites doesn't have reset feature
            {
                SetLineNumberFromHistory();
                ResetHistory(_lineNumber);
            }
        }

        private bool LogHistoryFileExists()
        {
            return File.Exists(_logHistoryFile);
        }

        private DateTime ReadDateCreatedLogOfFile()
        {
            return File.GetCreationTime(_logHistoryFile);
        }

        private string ReadLogLine()
        {
            var line = ReadLines(_logFile).Skip(_lineNumber).FirstOrDefault();
            return line;
        }

        private List<string> ReadLogLines()
        {
            var line = ReadLines(_logFile).Skip(_lineNumber).ToList();
            return line;
        }

        private static IEnumerable<string> ReadLines(string path)
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 0x1000, FileOptions.SequentialScan))
            using (var sr = new StreamReader(fs, Encoding.UTF8))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    yield return line;
                }
            }
        }

        #endregion

        #region Getting Environment Variables
        private static string GetHistoryFile()
        {
            var rootExtensionPath = GetEnvironmentVariable("HOME");
            return Path.Combine(rootExtensionPath, @"SiteExtensions\LogForwarder", "history.txt");
        }

        private static string GetLogFile()
        {
            var rootPath = GetEnvironmentVariable("HOME");
            var logFilePath = GetEnvironmentVariable("LOG_FILE_PATH");
            return GetStartOnHome() == "1"
                ? Path.Combine(rootPath, logFilePath)
                : Path.Combine(rootPath, @"site\wwwroot", logFilePath);
        }

        private static string GetIndexName()
        {
            return GetEnvironmentVariable("INDEX_NAME");
        }

        private static string GetSplunkHostName()
        {
            return GetEnvironmentVariable("SPLUNK_HOSTNAME");
        }

        private static string GetSplunkUserName()
        {
            return GetEnvironmentVariable("SPLUNK_USERNAME");
        }

        private static string GetSplunkPassword()
        {
            return GetEnvironmentVariable("SPLUNK_PASSWORD");
        }

        private static string GetDelay()
        {
            return GetEnvironmentVariable("DELAY");
        }

        private static string GetStartOnHome()
        {
            return GetEnvironmentVariable("START_ON_HOME");
        }

        private static string GetEnvironmentVariable(string keyName)
        {
            var rootPath = Environment.GetEnvironmentVariable(keyName);

            if (rootPath == null)
                throw new NullReferenceException($"{keyName} variable environment doesn't exist");

            return rootPath;
        }
        #endregion
    }
}
