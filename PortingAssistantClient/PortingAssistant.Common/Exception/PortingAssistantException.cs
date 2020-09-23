using System;
namespace PortingAssistant.Model
{
    public class PortingAssistantException : Exception
    {
        public PortingAssistantException(string message, Exception innerException) :
            base(message, innerException)
        {
        }
    }
}
