using System;
namespace PortingAssistant.Model
{
    public class InvocationWithCompatibility
    {
        public Invocation invocation { get; set; }
        public string replacement { get; set; }
        public bool isCompatible { get; set; }
        public bool deprecated { get; set; }
    }
}
