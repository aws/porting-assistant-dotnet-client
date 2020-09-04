using System;

namespace PortingAssistant.Model
{
    public class PortingAssistantClientException : Exception
    {

        public PortingAssistantClientException(string Message, Exception Innerexception):
            base(Message, Innerexception)
        {
        }
    }
}
