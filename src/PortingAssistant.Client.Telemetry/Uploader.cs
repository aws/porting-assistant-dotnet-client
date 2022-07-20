﻿using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
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
        private Dictionary<string, int> _fileLineNumberMap = new Dictionary<string, int>();
        private Dictionary<string, int> _updatedFileLineNumberMap = new Dictionary<string, int>();
        private string _lastReadTokenFile;

        public Uploader(TelemetryConfiguration telemetryConfig, ITelemetryClient telemetryClient)
        {
            _configuration = telemetryConfig;
            _client = telemetryClient;
            ReadFileLineMap();
            GetLogName = GetLogNameDefault;
        }

        public Func<string, string> GetLogName { get; set; }

        private static string GetLogNameDefault(string file)
        {
            var logName = Path.GetFileNameWithoutExtension(file);
            var fileExtension = Path.GetExtension(file);
            if (fileExtension == ".log")
            {
                logName = $"{logName}-logs";
            }
            else if (fileExtension == ".metrics")
            {
                logName = $"{logName}-metrics";
            }
            return logName;
        }

        public bool Upload(IEnumerable<string> fileEntries, bool shareMetrics = false)
        {
            try
            {
                foreach (var file in fileEntries)
                {
                    var logName = GetLogName(file);
                    bool uploaded = true;
                    if (shareMetrics)
                    {
                        uploaded = UploadFile(file, logName);
                    }
                    if (uploaded)
                    {
                        // either uploaded is true because we don't share metrics
                        // or upload is successful, either way remove old files.
                        RemoveFileIfOld(file);
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
                Log.Logger.Error(ex.Message);
                return false;
            }
        }

        private void RemoveFileIfOld(string file)
        {
            DateTime lastModified = File.GetLastWriteTime(file);
            if (lastModified < DateTime.Now.AddDays(_configuration.KeepLogsForDays * -1))
            {
                File.Delete(file);
                if (_fileLineNumberMap.ContainsKey(file))
                {
                    _fileLineNumberMap.Remove(file);
                }
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
            using FileStream fs = WaitForFile(_lastReadTokenFile,
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.Read);
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
            using (StreamWriter writer = new StreamWriter(fs))
            {
                writer.Write(JsonConvert.SerializeObject(_fileLineNumberMap));
            }
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

                if (telemetryResponse.HttpStatusCode != HttpStatusCode.OK)
                {
                    Log.Logger.Error("Http response failed with status code: " + telemetryResponse.HttpStatusCode.ToString());
                }

                return telemetryResponse.HttpStatusCode == HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex.Message);
                return false;
            }
        }
    }
}
