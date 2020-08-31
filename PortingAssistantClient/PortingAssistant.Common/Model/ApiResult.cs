using System.Collections.Generic;

namespace PortingAssistant.Model
{
    public class ApiResult
    {
        string name { get; set; }
        string replacement { get; set; }
        Compatibility isCompatible { get; set; }
        Dictionary<string, bool> deprecated { get; set; }
    }
}
