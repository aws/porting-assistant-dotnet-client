using System;
namespace PortingAssistant.Client.Model
{
    public class PortingAssistantException : Exception
    {
        public PortingAssistantException(string message, Exception innerException) :
            base(message, innerException)
        {
        }
    }
}
