using System;
namespace PortingAssistantHandler.ErrorHandle
{
    public class PortingAssistantAssessmentException : Exception
    {
        public PortingAssistantAssessmentException()
        { }

        public PortingAssistantAssessmentException(string message) : base(message)
        { }

        public PortingAssistantAssessmentException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}
