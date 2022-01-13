using Amazon.Runtime;
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
using System.Threading.Tasks;
using Amazon;

namespace PortingAssistant.Client.Telemetry
{
    public class Uploader
    {
        public static bool Upload(TelemetryConfiguration teleConfig, string profile, string prefix)
        {
            try
            {
                var fileEntries = new List<string> {
                    teleConfig.LogFilePath,
                    teleConfig.MetricsFilePath
                }.Where(x => !string.IsNullOrEmpty(x) && File.Exists(x)).ToList();

                var lastReadTokenFile = Path.Combine(teleConfig.LogsPath, "lastToken.json");
                var fileLineNumberMap = new Dictionary<string, int>();
                if (File.Exists(lastReadTokenFile))
                {
                    fileLineNumberMap = JsonConvert.DeserializeObject<Dictionary<string, int>>(File.ReadAllText(lastReadTokenFile));
                }
                foreach (var file in fileEntries)
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

                    // Add new files to fileLineNumberMap
                    if (!fileLineNumberMap.ContainsKey(file))
                    {
                        fileLineNumberMap[file] = 0;
                    }
                    var initLineNumber = fileLineNumberMap[file];

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
                                    return true;
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
                                    success = PutLogData(logName, JsonConvert.SerializeObject(logs), profile, teleConfig).Result;
                                    if (success) { logs = new ArrayList(); }
                                    else
                                    {
                                        return false;
                                    }
                                }
                            }

                            if (logs.Count != 0)
                            {
                                success = PutLogData(logName, JsonConvert.SerializeObject(logs), profile, teleConfig).Result;
                                if (!success)
                                {
                                    return false;
                                }
                            }

                            if (success)
                            {
                                fileLineNumberMap[file] = currLineNumber;
                                string jsonString = JsonConvert.SerializeObject(fileLineNumberMap);
                                File.WriteAllText(lastReadTokenFile, jsonString);
                            }
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex.Message);
                return false;
            }
        }
        private static async Task<bool> PutLogData
           (
           string logName,
           string logData,
           string profile,
           TelemetryConfiguration telemetryConfiguration
           )
        {
            try
            {
                var chain = new CredentialProfileStoreChain();
                AWSCredentials awsCredentials;
                var profileName = profile;
                var region = telemetryConfiguration.Region;

                if (chain.TryGetAWSCredentials(profileName, out awsCredentials))
                {
                    dynamic requestMetadata = new JObject();
                    requestMetadata.version = "1.0";
                    requestMetadata.service = telemetryConfiguration.ServiceName;
                    requestMetadata.token = "12345678";
                    requestMetadata.description = telemetryConfiguration.Description;

                    dynamic log = new JObject();
                    log.timestamp = DateTime.Now.ToString();
                    log.logName = logName;
                    var logDataInBytes = System.Text.Encoding.UTF8.GetBytes(logData);
                    log.logData = System.Convert.ToBase64String(logDataInBytes);

                    dynamic body = new JObject();
                    body.requestMetadata = requestMetadata;
                    body.log = log;

                    var requestContent = new StringContent(body.ToString(Formatting.None), Encoding.UTF8, "application/json");
                    var config = new TelemetryConfig() 
                    { 
                        RegionEndpoint = RegionEndpoint.GetBySystemName(region), 
                        MaxErrorRetry = 2, 
                        ServiceURL = telemetryConfiguration.InvokeUrl,
                    };
                    var client = new TelemetryClient(awsCredentials, config);
                    var contentString = await requestContent.ReadAsStringAsync();
                    var telemetryRequest = new TelemetryRequest(telemetryConfiguration.ServiceName, contentString);
                    var telemetryResponse = await client.SendAsync(telemetryRequest);

                    if (telemetryResponse.HttpStatusCode != HttpStatusCode.OK)
                    {
                        Log.Logger.Error("Http response failed with status code: " + telemetryResponse.HttpStatusCode.ToString());
                    }

                    return telemetryResponse.HttpStatusCode == HttpStatusCode.OK;
                }
                Log.Logger.Error("Invalid Credentials.");
                return false;
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex.Message);
                return false;
            }
        }
    }
}
