using System;
namespace PortingAssistant.Model
{
    public class PortingResult
    {
        public bool Success { get; set; }
        public string ProjectFile { get; set; }
        public string ProjectName { get; set; }
        public string Message { get; set; }
        public Exception Exception { get; set; }
    }
}
