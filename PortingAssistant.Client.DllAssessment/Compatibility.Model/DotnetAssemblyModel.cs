using System;
using System.Collections.Generic;

namespace AwsEncoreService.Compatibility.Model
{
    public class DotnetAssemblyModel
    {
        public List<MethodInfo> Methods;
        public string DllName;
        public string DllPath;
        public string DllVersion;

        public DotnetAssemblyModel()
        {
            Methods = new List<MethodInfo>();
        }

        public override string ToString()
        {
            return String.Format("DllName: {0}({1}), Version: {2}, Methods: {3}", DllPath, DllName, DllVersion, Methods.Count);
        }
    }

    public class MethodInfo
    {
        public string nameSpace;
        public string className;
        public string methodName;
        public List<string> parameters;
        public string returnValue;
        public string modifiers;
        public string signature;
    }
}
