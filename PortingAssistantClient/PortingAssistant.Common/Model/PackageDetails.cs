using System;
using System.Collections.Generic;

namespace PortingAssistant.Model
{
    public class PackageDetails
    {
        public string Name { get; set; }
        public SortedSet<string> Versions { get; set; }
        public Dictionary<string, SortedSet<string>> Targets { get; set; }
        public LicenseDetails License { get; set; }
        public ApiDetails[] Api { get; set; }
        public bool Deprecated { get; set; }
        // only for name space
        public string[] Namespaces { get; set; }
        public string[] Assemblies { get; set; }

        public override bool Equals(object obj)
        {
            return obj is PackageDetails details &&
                   Name == details.Name &&
                   EqualityComparer<SortedSet<string>>.Default.Equals(Versions, details.Versions) &&
                   EqualityComparer<Dictionary<string, SortedSet<string>>>.Default.Equals(Targets, details.Targets) &&
                   EqualityComparer<LicenseDetails>.Default.Equals(License, details.License) &&
                   EqualityComparer<ApiDetails[]>.Default.Equals(Api, details.Api);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Versions, Targets, License, Api);
        }
    }

    public class LicenseDetails
    {
        public Dictionary<string, SortedSet<string>> License { get; set; }

        public override bool Equals(object obj)
        {
            return obj is LicenseDetails details &&
                   EqualityComparer<Dictionary<string, SortedSet<string>>>.Default.Equals(License, details.License);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(License); ;
        }
    }

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
