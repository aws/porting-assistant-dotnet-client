using System;

namespace EncoreCommon.Model
{
    public class PortingProjectChange
    {
        public string SourceFile { get; set; }
        public uint? LineNumber { get; set; }
        public string Code { get; set; }
        public string Message { get; set; }
        public Exception Execption { get; set; }
    }
}
