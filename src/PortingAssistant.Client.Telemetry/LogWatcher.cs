using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Aws4RequestSigner;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PortingAssistantExtensionTelemetry.Model;

namespace PortingAssistantExtensionTelemetry
{
    public class LogWatcher
    {
        private readonly TelemetryConfiguration telemetryConfiguration;
        private readonly string profile;
        private readonly string lastReadTokenFile;
        private readonly HttpClient client;
        private readonly string prefix;

        public LogWatcher(
            TelemetryConfiguration telemetryConfiguration,
            string profile,
            string prefix)
        {
            this.telemetryConfiguration = telemetryConfiguration;
            this.profile = profile;
            lastReadTokenFile = Path.Combine(telemetryConfiguration.LogsPath, "lastToken.json");
            client = new HttpClient();
            this.prefix = prefix;
        }

        public void Start()
        {
            try
            {
                var fileSystemWatcher = new FileSystemWatcher();

                fileSystemWatcher.Changed += (s, e)
                    => FileSystemWatcher_Changed(s, e, telemetryConfiguration, profile);
                fileSystemWatcher.Created += (s, e)
                    => FileSystemWatcher_Changed(s, e, telemetryConfiguration, profile);
                fileSystemWatcher.Deleted += (s, e)
                    => FileSystemWatcher_Deleted(s, e, telemetryConfiguration);

                fileSystemWatcher.Path = telemetryConfiguration.LogsPath;

                fileSystemWatcher.EnableRaisingEvents = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("metric upload failed with error", ex);
            }

        }

        private void FileSystemWatcher_Changed
            (
            object sender,
            FileSystemEventArgs e,
            TelemetryConfiguration telemetryConfiguration,
            string profile
            )
        {
            try
            {
                var fileExtension = Path.GetExtension(e.FullPath);
                if (!telemetryConfiguration.Suffix.Exists(s => fileExtension.Equals(s))) return;
                var logName = prefix + fileExtension.Trim().Substring(1);

                FileInfo fileInfo = new FileInfo(e.FullPath);
                var fileName = e.Name;

                // Json File to record last read log token (line number).
                var initLineNumber = 0;
                Dictionary<string, int> fileLineNumberMap;

                var logs = new ArrayList();

                if (!IsFileLocked(fileInfo))
                {
                    if (File.Exists(lastReadTokenFile))
                    {
                        fileLineNumberMap = JsonConvert.DeserializeObject<Dictionary<string, int>>(File.ReadAllText(lastReadTokenFile));
                        if (fileLineNumberMap != null)
                        {
                            initLineNumber = fileLineNumberMap.ContainsKey(fileName) ? fileLineNumberMap[fileName] : 0;
                        }
                        else
                        {
                            fileLineNumberMap = new Dictionary<string, int>();
                            initLineNumber = 0;
                        }
                    }
                    else
                    {
                        fileLineNumberMap = new Dictionary<string, int>();
                        initLineNumber = 0;
                    }

                    using (FileStream fs = fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        using (StreamReader reader = new StreamReader(fs))
                        {
                            string line = null;
                            int currLineNumber = 0;
                            for (; currLineNumber < initLineNumber; currLineNumber++)
                            {
                                line = reader.ReadLine();
                                if (line == null)
                                {
                                    return;
                                }
                            }

                            line = reader.ReadLine();

                            while (line != null)
                            {
                                currLineNumber++;
                                logs.Add(line);
                                line = reader.ReadLine();

                                // send 1000 lines of logs each time when there are large files
                                if (logs.Count >= 1000)
                                {
                                    logs.TrimToSize();
                                    PutLogData(client, logName, JsonConvert.SerializeObject(logs), profile, telemetryConfiguration);
                                    logs = new ArrayList();
                                }
                            }

                            fileLineNumberMap[fileName] = currLineNumber;
                            string jsonString = JsonConvert.SerializeObject(fileLineNumberMap);
                            File.WriteAllText(lastReadTokenFile, jsonString);

                            if (logs.Count != 0)
                            {
                                PutLogData(client, logName, JsonConvert.SerializeObject(logs), profile, telemetryConfiguration);
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine("File Locked");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void FileSystemWatcher_Deleted
            (
            Object sender,
            FileSystemEventArgs e,
            TelemetryConfiguration telemetryConfiguration
            )
        {
            try
            {


                if (!telemetryConfiguration.Suffix.Exists(s => Path.GetExtension(e.FullPath).Equals(s))) return;

                FileInfo fileInfo = new FileInfo(e.FullPath);
                var fileName = e.Name;

                Dictionary<string, int> fileLineNumberMap;

                // If log file is deleted set Last Read Line Number to 0.
                if (File.Exists(lastReadTokenFile))
                {
                    fileLineNumberMap = JsonConvert.DeserializeObject<Dictionary<string, int>>(File.ReadAllText(lastReadTokenFile));
                    if (fileLineNumberMap.ContainsKey(fileName)) fileLineNumberMap[fileName] = 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static async void PutLogData
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

                    await response.Content.ReadAsStringAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static Boolean IsFileLocked(FileInfo file)
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
