using System;
using System.Collections.Generic;

namespace PortingAssistant.Model
{
    public class ApiDetails
    {
        public string MethodSignature { get; set; }
        public string MethodNameSpace { get; set; }
        public string MethodName { get; set; }
        public string[] MethodParameters { get; set; }
        public string MethodReturnValue { get; set; }
        public Dictionary<string, SortedSet<string>> Targets { get; set; }

        public override bool Equals(object obj)
        {
            return obj is ApiDetails details &&
                    MethodSignature == details.MethodSignature &&
                    MethodNameSpace == details.MethodNameSpace &&
                    MethodName == details.MethodName &&
                    EqualityComparer<string[]>.Default.Equals(MethodParameters, details.MethodParameters) &&
                    MethodReturnValue == details.MethodReturnValue &&
                    EqualityComparer<Dictionary<string, SortedSet<string>>>.Default.Equals(Targets, details.Targets);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(MethodSignature, MethodNameSpace, MethodName, MethodParameters, MethodReturnValue, Targets);
        }
    }
}
