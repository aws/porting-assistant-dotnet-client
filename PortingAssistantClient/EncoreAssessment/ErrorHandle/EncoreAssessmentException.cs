using System;
namespace EncoreAssessment.ErrorHandle
{
    public class EncoreAssessmentException : Exception
    {
        public EncoreAssessmentException() 
        { }

        public EncoreAssessmentException(string message) : base(message)
        { }

        public EncoreAssessmentException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}
