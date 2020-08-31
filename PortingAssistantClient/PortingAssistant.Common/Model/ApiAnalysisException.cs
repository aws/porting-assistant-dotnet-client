using System;
using System.Collections.Generic;

namespace PortingAssistant.Model
{
    public class ApiAnalysisException : Exception
    {
        public string SolutionPath { get; }
        public string ProjectPath { get; }
        public List<string> Errors { get; }
        public Exception Cause { get; }

        public ApiAnalysisException(string solutionPath, string projectPath, List<string> errors, Exception innerException) :
            base("Solution: " + solutionPath + ", project: " + projectPath + " failed analysis", innerException)
        {
            SolutionPath = solutionPath;
            ProjectPath = projectPath;
            Errors = errors;
            Cause = innerException;
        }
    }
}
