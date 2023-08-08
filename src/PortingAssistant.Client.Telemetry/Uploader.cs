using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PortingAssistantExtensionTelemetry.Model;
using Serilog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PortingAssistant.Client.Telemetry
{
    public class Uploader
    {
        private readonly TelemetryConfiguration _configuration;
        private readonly ITelemetryClient _client;
        private Dictionary<string, int> _fileLineNumberMap = new();
        private readonly Dictionary<string, int> _updatedFileLineNumberMap = new();
        private readonly ILogger _logger;
        private string _lastReadTokenFile;
        private readonly Dictionary<string, int> _errors = new();
        private const long LogDirectorySizeLimit = 250000000;
        private readonly bool _shareMetrics;

        public Uploader(TelemetryConfiguration telemetryConfig,
            ITelemetryClient telemetryClient,
            ILogger logger,
            bool shareMetrics)
        {
            _configuration = telemetryConfig;
            _client = telemetryClient;
            _logger = logger;
            _shareMetrics = shareMetrics;
            ReadFileLineMap();
            GetLogName = GetLogNameDefault;
        }

        /// <summary>
        /// Takes file name and returns log name for upload. Returning blank will skip the file
        /// </summary>
        public Func<string, string> GetLogName { get; set; }

        public bool Run()
        {
            try
            {
                if (_shareMetrics)
                {
                    Upload();
                }
                CleanupLogFolder();
                return true;
            }
            catch (Exception ex)
            {
                AddError(ex);
                return false;
            }
        }

        public void WriteLogUploadErrors()
        {
            foreach (var error in _errors)
            {
                _logger.Error($"Log Upload Error({error.Value}): {error.Key}");
            }
        }

        private bool Upload()
        {
            try
            {
                string[] fileEntries = Directory.GetFiles(_configuration.LogsPath)
                    .Where(f => _configuration.Suffix.ToArray()
                                    .Any(f.EndsWith) &&
                                (string.IsNullOrEmpty(_configuration.LogPrefix) ||
                                 Path.GetFileName(f).StartsWith(_configuration.LogPrefix)) &&
                                File.GetLastWriteTime(f) > DateTime.Now.Subtract(TimeSpan.FromDays(21)))
                    .ToArray();

                foreach (var file in fileEntries)
                {
                    var logName = GetLogName(file);
                    if (string.IsNullOrEmpty(logName))
                    {
                        continue;
                    }
                    if (_client != null)
                    {
                        UploadFile(file, logName);
                    }
                }
                if (_updatedFileLineNumberMap.Any())
                {
                    UpdateFileLineMapJson();
                }
                return true;
            }
            catch (Exception ex)
            {
                AddError(ex);
                return false;
            }
        }

        private void CleanupLogFolder()
        {
            try
            {
                long folderSizeLimit = _configuration.LogsFolderSizeLimit != 0
                    ? _configuration.LogsFolderSizeLimit
                    : LogDirectorySizeLimit;
                long currentSize = 0;
                ReadFileLineMap();
                var logsDirectory = new DirectoryInfo(_configuration.LogsPath);
                foreach (var file in logsDirectory
                             .GetFiles()
                             .Where(file => _configuration.Suffix.ToArray().Any(file.Name.EndsWith))
                             .OrderByDescending(f => f.LastWriteTime))
                {
                    if (currentSize <= folderSizeLimit)
                    {
                        currentSize += file.Length;
                        continue;
                    }

                    try
                    {
                        File.Delete(file.FullName);
                    }
                    catch (Exception e)
                    {
                        AddError(e);
                    }
                }
            }
            catch (Exception e)
            {
                AddError(e);
            }
        }

        private static string GetLogNameDefault(string file)
        {
            var logName = Path.GetFileNameWithoutExtension(file);
            var fileExtension = Path.GetExtension(file);
            string logNameWithoutDate = int.TryParse(logName.Split('-').LastOrDefault() ?? "", out _) ? string.Join('-', logName.Split('-').SkipLast(1)) : logName;
            if (fileExtension == ".log")
            {
                logName = $"{logNameWithoutDate}-logs";
            }
            else if (fileExtension == ".metrics")
            {
                logName = $"{logNameWithoutDate}-metrics";
            }
            return logName;
        }

        private void AddError(Exception e)
        {
            if (_errors.ContainsKey(e.Message))
            {
                _errors[e.Message] += 1;
            }
            _errors[e.Message] = 1;
        }

        private void ReadFileLineMap()
        {
            _lastReadTokenFile = Path.Combine(_configuration.LogsPath, "lastToken.json");
            if (File.Exists(_lastReadTokenFile))
            {
                using FileStream fs = WaitForFile(_lastReadTokenFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                using StreamReader reader = new StreamReader(fs);
                var content = reader.ReadToEnd();
                try
                {
                    _fileLineNumberMap = JsonConvert.DeserializeObject<Dictionary<string, int>>(content);
                    if (_fileLineNumberMap == null)
                    {
                        _fileLineNumberMap = new Dictionary<string, int>();
                    }
                }
                catch
                {
                    _fileLineNumberMap = new Dictionary<string, int>();
                }
            }
            else
            {
                _fileLineNumberMap = new Dictionary<string, int>();
            }
        }

        private void UpdateFileLineMapJson()
        {
            ReadFileLineMap();
            foreach (var pair in _updatedFileLineNumberMap)
            {
                if (_fileLineNumberMap.ContainsKey(pair.Key))
                {
                    _fileLineNumberMap[pair.Key] = pair.Value;
                }
                else
                {
                    _fileLineNumberMap.Add(pair.Key, pair.Value);
                }
            }
            using FileStream fs = WaitForFile(_lastReadTokenFile,
                FileMode.Create,
                FileAccess.ReadWrite,
                FileShare.None);
            using StreamWriter writer = new StreamWriter(fs);
            writer.Write(JsonConvert.SerializeObject(_fileLineNumberMap));
        }

        private FileStream WaitForFile(string fullPath,
            FileMode mode,
            FileAccess access,
            FileShare share)
        {
            for (int numTries = 0; numTries < 5; numTries++)
            {
                FileStream fs = null;
                try
                {
                    fs = new FileStream(fullPath, mode, access, share);
                    return fs;
                }
                catch (IOException)
                {
                    if (fs != null)
                    {
                        fs.Dispose();
                    }
                    Thread.Sleep(5000);
                }
            }
            return null;
        }

        private bool UploadFile(string file, string logName)
        {
            var initLineNumber = _fileLineNumberMap.ContainsKey(file) ? _fileLineNumberMap[file] : 0;

            FileInfo fileInfo = new FileInfo(file);
            var success = false;
            if (!IsFileLocked(fileInfo))
            {
                using FileStream fs = fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using StreamReader reader = new StreamReader(fs);
                // If put-log api works keep sending logs else wait and do it next time
                var logs = new ArrayList();

                int currLineNumber = 0;
                for (; currLineNumber < initLineNumber; currLineNumber++)
                {
                    string line = reader.ReadLine();
                    if (line == null)
                    {
                        {
                            return true;
                        }
                    }
                }
                while (!reader.EndOfStream)
                {
                    currLineNumber++;
                    logs.Add(reader.ReadLine());

                    // send 1000 lines of logs each time when there are large files
                    if (logs.Count >= 1000)
                    {
                        // logs.TrimToSize();
                        success = PutLogData(logName, JsonConvert.SerializeObject(logs)).Result;
                        if (success)
                        {
                            logs = new ArrayList();
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
                if (logs.Count != 0)
                {
                    success = PutLogData(logName, JsonConvert.SerializeObject(logs)).Result;
                    if (!success)
                    {
                        return false;
                    }
                }
                if (success)
                {
                    if (_updatedFileLineNumberMap.ContainsKey(file))
                    {
                        _updatedFileLineNumberMap[file] = currLineNumber;
                    }
                    else
                    {
                        _updatedFileLineNumberMap.Add(file, currLineNumber);
                    }
                    return true;
                }
            }
            return false;
        }

        private async Task<bool> PutLogData(
            string logName,
            string logData)
        {
            try
            {
                dynamic requestMetadata = new JObject();
                requestMetadata.version = "1.0";
                requestMetadata.service = _configuration.ServiceName;
                requestMetadata.token = "12345678";
                requestMetadata.description = _configuration.Description;

                dynamic log = new JObject();
                if (_configuration.InvokeUrl.Contains("application-transformation"))
                {
                    log.timeStamp = DateTimeOffset.Now.ToUnixTimeSeconds().ToString();
                }
                else
                {
                    log.timestamp = DateTime.Now.ToString();
                }
                log.logName = logName;
                var logDataInBytes = System.Text.Encoding.UTF8.GetBytes(logData);
                log.logData = System.Convert.ToBase64String(logDataInBytes);

                dynamic body = new JObject();
                body.requestMetadata = requestMetadata;
                body.log = log;

                var requestContent = new StringContent(body.ToString(Formatting.None), Encoding.UTF8, "application/json");
                
                var contentString = await requestContent.ReadAsStringAsync();
                var telemetryRequest = new TelemetryRequest(_configuration.ServiceName, contentString, _configuration.InvokeUrl);
                var telemetryResponse = await _client.SendAsync(telemetryRequest);
                return telemetryResponse.HttpStatusCode == HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                AddError(ex);
                return false;
            }
        }

        private static bool IsFileLocked(FileInfo file)
        {
            FileStream stream = null;
            try
            {
                stream = file.Open
                (
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite
                );
            }
            catch (IOException)
            {
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }
            return false;
        }
    }
}
