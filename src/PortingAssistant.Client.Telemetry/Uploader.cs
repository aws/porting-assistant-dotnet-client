using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Aws4RequestSigner;
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
using System.Text;
using System.Threading.Tasks;

namespace PortingAssistant.Client.Telemetry
{
    public class Uploader
    {
        public static bool Upload(TelemetryConfiguration teleConfig, HttpClient client, string profile, string prefix)
        {
            try
            {
                var fileEntries = new List<string> {
                    teleConfig.LogFilePath,
                    teleConfig.MetricsFilePath
                }.Where(x => !string.IsNullOrEmpty(x)).ToList();
                // Get or Create fileLineNumberMap
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

                    FileInfo fileInfo = new FileInfo(file);
                    var success = false;
                    using (FileStream fs = fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        using (StreamReader reader = new StreamReader(fs))
                        {
                            // If put-log api works keep sending logs else wait and do it next time
                            var logs = new ArrayList();
                            while (!reader.EndOfStream)
                            {
                                logs.Add(reader.ReadLine());

                                // send 1000 lines of logs each time when there are large files
                                if (logs.Count >= 1000)
                                {
                                    // logs.TrimToSize();
                                    success = PutLogData(client, logName, JsonConvert.SerializeObject(logs), profile, teleConfig).Result;
                                    if (success) { logs = new ArrayList(); }
                                    else return false;
                                }
                            }

                            if (logs.Count != 0)
                            {
                                success = PutLogData(client, logName, JsonConvert.SerializeObject(logs), profile, teleConfig).Result;
                                if (!success) return false;
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
           HttpClient client,
           string logName,
           string logData,
           string profile,
           TelemetryConfiguration telemetryConfiguration
           )
        {
            const string PathTemplate = "/put-log-data";
            try
            {
                var chain = new CredentialProfileStoreChain();
                AWSCredentials awsCredentials;
                var profileName = profile;
                var region = telemetryConfiguration.Region;

                if (chain.TryGetAWSCredentials(profileName, out awsCredentials))
                {
                    var signer = new AWS4RequestSigner
                        (
                        awsCredentials.GetCredentials().AccessKey,
                        awsCredentials.GetCredentials().SecretKey
                        );

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

                    var requestUri = new Uri(String.Join("", telemetryConfiguration.InvokeUrl, PathTemplate));

                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Post,
                        RequestUri = requestUri,
                        Content = requestContent
                    };

                    request = await signer.Sign(request, "execute-api", region);

                    var response = await client.SendAsync(request);

                    if (!response.IsSuccessStatusCode) Log.Logger.Error("Http response faild with status code: " + response.StatusCode.ToString());

                    return response.IsSuccessStatusCode;
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
