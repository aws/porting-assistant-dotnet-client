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
        private readonly ILogger _logger;
        private Dictionary<string, int> _fileLineNumberMap = new();
        private readonly Dictionary<string, int> _updatedFileLineNumberMap = new();
        private string _lastReadTokenFile;

        public Uploader(TelemetryConfiguration telemetryConfig, ITelemetryClient telemetryClient, ILogger logger)
        {
            _configuration = telemetryConfig;
            _client = telemetryClient;
            ReadFileLineMap();
            GetLogName = GetLogNameDefault;
            _logger = logger;
        }

        public Func<string, string> GetLogName { get; set; }

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

        public bool Upload(IEnumerable<string> fileEntries)
        {
            try
            {
                foreach (var file in fileEntries)
                {
                    var logName = GetLogName(file);
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
                _logger.Error(ex.Message);
                return false;
            }
        }

        public void CleanupLogFolder()
        {
            long folderSizeLimit = _configuration.LogsFolderSizeLimit != 0
                ? _configuration.LogsFolderSizeLimit
                : 250000000;
            long currentSize = 0;
            ReadFileLineMap();
            var logsDirectory = new DirectoryInfo(_configuration.LogsPath);
            bool updateLastTokenJson = false;
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

                File.Delete(file.FullName);
                if (_fileLineNumberMap.ContainsKey(file.FullName))
                {
                    updateLastTokenJson = true;
                    _fileLineNumberMap.Remove(file.FullName);
                }
            }
            if (updateLastTokenJson)
            {
                UpdateFileLineMapJson();
            }
        }

        private void ReadFileLineMap()
        {
            _lastReadTokenFile = Path.Combine(_configuration.LogsPath, "lastToken.json");
            if (File.Exists(_lastReadTokenFile))
            {
                _fileLineNumberMap = JsonConvert.DeserializeObject<Dictionary<string, int>>(File.ReadAllText(_lastReadTokenFile));
                if (_fileLineNumberMap == null)
                {
                    _fileLineNumberMap = new Dictionary<string, int>();
                }
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
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.Read);
            using StreamWriter writer = new StreamWriter(fs);
            writer.Write(JsonConvert.SerializeObject(_fileLineNumberMap));
        }

        private FileStream WaitForFile(string fullPath,
            FileMode mode,
            FileAccess access,
            FileShare share)
        {
            for (int numTries = 0; numTries < 10; numTries++)
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
                    Thread.Sleep(50);
                }
            }
            return null;
        }

        private bool UploadFile(string file, string logName)
        {
            var initLineNumber = _fileLineNumberMap.ContainsKey(file) ? _fileLineNumberMap[file] : 0;

            FileInfo fileInfo = new FileInfo(file);
            var success = false;
            using (FileStream fs = fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (StreamReader reader = new StreamReader(fs))
                {
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
                                {
                                    return false;
                                }
                            }
                        }
                    }
                    if (logs.Count != 0)
                    {
                        success = PutLogData(logName, JsonConvert.SerializeObject(logs)).Result;
                        if (!success)
                        {
                            {
                                return false;
                            }
                        }
                    }
                    if (success)
                    {
                        _updatedFileLineNumberMap.Add(file, currLineNumber);
                        return true;
                    }
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
                log.timestamp = DateTime.Now.ToString();
                log.logName = logName;
                var logDataInBytes = System.Text.Encoding.UTF8.GetBytes(logData);
                log.logData = System.Convert.ToBase64String(logDataInBytes);

                dynamic body = new JObject();
                body.requestMetadata = requestMetadata;
                body.log = log;

                var requestContent = new StringContent(body.ToString(Formatting.None), Encoding.UTF8, "application/json");
                
                var contentString = await requestContent.ReadAsStringAsync();
                var telemetryRequest = new TelemetryRequest(_configuration.ServiceName, contentString);
                var telemetryResponse = await _client.SendAsync(telemetryRequest);
                return telemetryResponse.HttpStatusCode == HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                return false;
            }
        }
    }
}
