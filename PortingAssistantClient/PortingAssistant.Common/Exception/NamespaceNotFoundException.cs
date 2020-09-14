using System;

namespace PortingAssistant.Model
{
    public class NamespaceNotFoundException : Exception
    {
        public NamespaceNotFoundException(string message):
            base(message)
        {
        }
    }
}
