using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortingAssistant.Compatibility.Common.Model
{
    public class PackageDetails
    {

        public string Name { get; set; }
        public SortedSet<string> Versions { get; set; }
        public Dictionary<string, SortedSet<string>> Targets { get; set; }
        public LicenseDetails License { get; set; }
        public ApiDetails[]? Api { get; set; }
        public bool IsDeprecated { get; set; }
        // only for namespace
        public string[] Namespaces { get; set; }
        public string[] Assemblies { get; set; }

        public override bool Equals(object obj)
        {
            return obj is PackageDetails details &&
                   Name == details.Name &&
                   EqualityComparer<SortedSet<string>>.Default.Equals(Versions, details.Versions) &&
                   EqualityComparer<Dictionary<string, SortedSet<string>>>.Default.Equals(Targets, details.Targets) &&
                   EqualityComparer<LicenseDetails>.Default.Equals(License, details.License) &&
                   EqualityComparer<ApiDetails[]?>.Default.Equals(Api, details.Api);
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
            return HashCode.Combine(License);
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

    public class ApiDetailsV2
    {
        public string methodSignature { get; set; }
        public string methodNameSpace { get; set; }
        public string methodClassName { get; set; }
        public string methodName { get; set; }
        public string[] methodParameters { get; set; }
        public string methodReturnValue { get; set; }
        public bool IsCompatible { get; set; }

        public override bool Equals(object obj)
        {
            return obj is ApiDetailsV2 details &&
                    methodSignature == details.methodSignature &&
                    methodNameSpace == details.methodNameSpace &&
                    methodClassName == details.methodClassName &&
                    methodName == details.methodName &&
                    EqualityComparer<string[]>.Default.Equals(methodParameters, details.methodParameters) &&
                    methodReturnValue == details.methodReturnValue &&
                    IsCompatible == details.IsCompatible;
                    
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(methodSignature, methodNameSpace, methodClassName, methodName, methodParameters, methodReturnValue, IsCompatible);
        }
    }
}
