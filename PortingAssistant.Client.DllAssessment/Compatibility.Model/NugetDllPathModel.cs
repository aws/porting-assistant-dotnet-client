//using System.IO;
//using AwsEncoreServiceCache.Compatibility.TargetFrameworks;

//namespace AwsEncoreService.Compatibility.Model
//{
//    public class NugetPackageDllModel
//    {
//        public readonly string DllPath;
//        public readonly string DllName;
//        public readonly string TargetFramework;
//        public readonly TargetFrameworkInfo TargetFrameworkInfo;

//        /// <summary>
//        /// Extracts information from a Nuget Package's dll path
//        /// </summary>
//        public NugetPackageDllModel (string dllPath)
//        {
//            DllPath = dllPath;
//            DllName = Path.GetFileName(dllPath);
//            TargetFramework = new DirectoryInfo(dllPath).Parent.Name;

//            TargetFrameworkInfo = TargetFrameworks.Lookup.TryGetValue(TargetFramework, out var targetFrameworkInfo) 
//                ? targetFrameworkInfo
//                : null;
//        }
//    }
//}