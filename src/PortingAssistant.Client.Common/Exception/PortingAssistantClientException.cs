using System;

namespace PortingAssistant.Client.Model
{
    public class PortingAssistantClientException : Exception
    {
        public PortingAssistantClientException(string message, Exception innerException) :
            base(message, innerException)
        {
        }
    }
}
