using System;
namespace PortingAssistant.Model
{
    public class PortingAssistantException : Exception
    {
        public PortingAssistantException(string Message, Exception Innerexception):
            base(Message, Innerexception)
        {
        }
    }
}
