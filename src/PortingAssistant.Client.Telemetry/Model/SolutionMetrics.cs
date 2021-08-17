using System;
using System.Collections.Generic;
using System.Text;

namespace PortingAssistant.Client.Telemetry.Model
{
    public class SolutionMetrics : MetricsBase
    {
        public string solutionName { get; set; }
        public string solutionPath { get; set; }
        public double analysisTime { get; set; }
    }
}
