using Newtonsoft.Json;
using Serilog;
using System;
using System.IO;
using System.Threading;
using ILogger = Serilog.ILogger;

namespace PortingAssistantExtensionTelemetry
{
    public static class TelemetryCollector
    {
        private static string _filePath;
        private static ILogger _logger;
        private static ReaderWriterLockSlim _readWriteLock = new ReaderWriterLockSlim();

        public static void Builder(ILogger logger, string filePath)
        {
            if (_logger == null && _filePath == null)
            {
                _logger = logger;
                _filePath = filePath;
            }
        }

        private static void ConfigureDefault()
        {
            var AppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var metricsFilePath = Path.Combine(AppData, "logs", "metrics.metrics");
            _filePath = metricsFilePath;
            var outputTemplate = "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}";
            var logConfiguration = new LoggerConfiguration().Enrich.FromLogContext()
            .MinimumLevel.Warning()
            .WriteTo.File(
                Path.Combine(AppData, "logs", "metrics.log"),
                outputTemplate: outputTemplate);
            _logger = logConfiguration.CreateLogger();
        }

        private static void WriteToFile(string content)
        {
            _readWriteLock.EnterWriteLock();
            try
            {
                if (_filePath == null)
                {
                    ConfigureDefault();
                }
                using (StreamWriter sw = File.AppendText(_filePath))
                {
                    sw.WriteLine(content);
                    sw.Close();
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to write to the metrics file with error", ex);
            }
            finally
            {
                // Release lock
                _readWriteLock.ExitWriteLock();
            }
        }

        public static void Collect<T>(T t)
        {
            WriteToFile(JsonConvert.SerializeObject(t));
        }
        public static void Collect<T1, TResult>(Func<T1, TResult> collector, T1 t1)
        {
            TResult output = collector.Invoke(t1);
            WriteToFile(JsonConvert.SerializeObject(output));
        }

        public static void Collect<T1, T2, TResult>(Func<T1, T2, TResult> collector, T1 t1, T2 t2)
        {
            TResult output = collector.Invoke(t1, t2);
            WriteToFile(JsonConvert.SerializeObject(output));
        }

        public static void Collect<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> collector, T1 t1, T2 t2, T3 t3)
        {
            TResult output = collector.Invoke(t1, t2, t3);
            WriteToFile(JsonConvert.SerializeObject(output));
        }

        public static void Collect<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, TResult> collector, T1 t1, T2 t2, T3 t3, T4 t4)
        {
            TResult output = collector.Invoke(t1, t2, t3, t4);
            WriteToFile(JsonConvert.SerializeObject(output));
        }

        public static void Collect<T1, T2, T3, T4, T5, TResult>(Func<T1, T2, T3, T4, T5, TResult> collector, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5)
        {
            TResult output = collector.Invoke(t1, t2, t3, t4, t5);
            WriteToFile(JsonConvert.SerializeObject(output));
        }
    }
}
