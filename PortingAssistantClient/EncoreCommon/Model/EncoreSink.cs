using System;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Configuration;
using System.Collections.Generic;
using Serilog.Formatting;
using System.IO;
using System.Text;

namespace EncoreCommon.Model
{
    public delegate void OnDataUpdate(string response);

    public class EncoreSink : ILogEventSink
    {
        private readonly ITextFormatter _formatProvider;
        private readonly List<OnDataUpdate> _onDataUpdateDelegates = new List<OnDataUpdate>();

        public EncoreSink(ITextFormatter formatProvider)
        {
            _formatProvider = formatProvider;

        }

        public void Emit(LogEvent logEvent)
        {
            var buffer = new StringWriter(new StringBuilder());
            _formatProvider.Format(logEvent, buffer);
            string message = buffer.ToString();
            _onDataUpdateDelegates.ForEach(listener => listener.Invoke(message));
        }

        public void registerOnData(OnDataUpdate listener)
        {
            _onDataUpdateDelegates.Add(listener);
        }
    }
}
