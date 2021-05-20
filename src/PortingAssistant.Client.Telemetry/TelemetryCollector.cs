using Newtonsoft.Json;
using Serilog;
using System;
using System.IO;
using ILogger = Serilog.ILogger;

namespace PortingAssistantExtensionTelemetry
{
    public static class TelemetryCollector
    {
        private static FileStream _fs;
        private static ILogger _logger;

        public static void Builder(ILogger logger, string filePath)
        {
            if (_fs == null && _logger == null)
            {
                _logger = logger;
                _fs = new FileStream(filePath, FileMode.Append);
            }
        }

        private static void ConfigureDefault()
        {
            var AppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var metricsFilePath = Path.Combine(AppData, "logs", "metrics.metrics");
            _fs = new FileStream(metricsFilePath, FileMode.Append);
            var outputTemplate = "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}";
            var logConfiguration = new LoggerConfiguration().Enrich.FromLogContext()
            .MinimumLevel.Warning()
            .WriteTo.RollingFile(
                Path.Combine(AppData, "logs", "metrics.log"),
                outputTemplate: outputTemplate);
            _logger = logConfiguration.CreateLogger();
        }

        private static void WriteToFile(string content)
        {
            try
            {
                if (_fs == null)
                {
                    ConfigureDefault();
                }
                lock (_fs)
                {
                    using (var file = new StreamWriter(_fs))
                    {
                        file.WriteLine(content);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to write to the metrics file with error", ex);
            }
        }

        public static void Dispose()
        {
            if (_fs != null)
                _fs.Dispose();
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
