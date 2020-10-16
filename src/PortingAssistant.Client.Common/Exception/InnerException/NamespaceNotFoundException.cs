using System;

namespace PortingAssistant.Client.Model
{
    public class NamespaceNotFoundException : Exception
    {
        public NamespaceNotFoundException(string message) :
            base(message)
        {
        }
    }
}
