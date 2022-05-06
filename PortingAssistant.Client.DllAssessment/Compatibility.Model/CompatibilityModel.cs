using System;
using System.Collections.Generic;
using System.Text;
using Amazon.DynamoDBv2.DataModel;
using Nest;
using Newtonsoft.Json;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;

namespace AwsEncoreService.Compatibility.Model
{
    public class CompatibilityInput
    {
        public CompatibilityInput(string packageName, string packageVersion)
        {
            this.PackageName = packageName;
            this.PackageVersion = packageVersion;
        }
        
        [JsonProperty("packageId")]
        public string PackageName { get; set; }
        
        [JsonProperty("version")]
        public string PackageVersion { get; set; }

        public override bool Equals(object obj)
        {
            return obj is CompatibilityInput id &&
                   PackageName == id.PackageName &&
                   PackageVersion == id.PackageVersion;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(PackageName, PackageVersion);
        }

        public override string ToString()
        {
            return String.Format($"PackageName: { PackageName }, Version: {PackageVersion}");
        }
    }

    public class SdkCompatibilityInput : CompatibilityInput
    {
        public string InstallLibPath;
        public SdkCompatibilityInput(string packageName, string packageVersion) 
            : base(packageName, packageVersion)
        {
        }
    }
    
    public class PackageCompatResult
    {
        [DynamoDBProperty("packageId")]
        [Text(Name = "packageId")]
        public String PackageId { get; set; }
        
        [DynamoDBHashKey("packageName")]
        [Text(Name = "packageName")]
        public String PackageName { get; set; }
        
        [DynamoDBRangeKey("packageVersion")]
        [Text(Name = "packageVersion")]
        public String PackageVersion { get; set; }
        
        [DynamoDBProperty("packagePath")]
        [Text(Name = "packagePath")]
        public String PackagePath { get; set; }
        
        [DynamoDBProperty("nuGetPackageFolder")]
        [Text(Name = "nuGetPackageFolder")]
        public String NuGetPackageFolder { get; set; }
        
        [DynamoDBProperty("result")]
        [Text(Name = "result")]
        public bool Result { get; set; }

        [DynamoDBProperty("nugetSize")]
        [Text(Name = "nugetSize")]
        public long Size { get; set; }

        [DynamoDBProperty("targetDotnetFrameworks")]
        [Text(Name = "targetDotnetFrameworks")]
        public HashSet<String> TargetDotnetFrameworks { get; set; }
        
        [DynamoDBProperty("dependencies")]
        [Text(Name = "dependencies")]
        public HashSet<String> Dependencies { get; set; }
        
        [DynamoDBProperty("assemblies")]
        [Text(Name = "assemblies")]
        public HashSet<String> Assemblies { get; set; }
        
        [DynamoDBProperty("packageDependencies")]
        [Text(Name = "packageDependencies")]
        public HashSet<String> PackageDependencies { get; set; }
        
        [DynamoDBProperty("packageLicenseInfo")]
        [Text(Name = "packageLicenseInfo")]
        public PackageLicenseInfo PackageLicenseInfo { get; internal set; }
        
        [DynamoDBProperty("allDlls")]
        [Text(Name = "allDlls")]
        public HashSet<String> AllDlls { get; set; }

        [DynamoDBProperty("DebugInfo")]
        [Text(Name = "DebugInfo")]
        public String Debug { get; set; }

        [DynamoDBProperty("nearestTarget")]
        [Text(Name = "nearestTarget")]
        public String NearestTarget { get; set; }

        [DynamoDBProperty("supportedFrameworks")]
        [Text(Name = "supportedFrameworks")]
        public HashSet<String> SupportedFrameworks { get; set; }

        [DynamoDBProperty("otherInfo")]
        [Text(Name = "otherInfo")]
        public String OtherInfo { get; set; }

        //intermediate result
        public Status CompatStatus { get; set; }

        public enum Status
        {
            Supported,
            Unsupported,
            Unknown
        }

        public PackageCompatResult()
        {
            PackageId = "";
            Result = false;
            Dependencies = new HashSet<string>();
            Assemblies = new HashSet<string>();
            PackageDependencies = new HashSet<string>();
            AllDlls = new HashSet<string>();
            CompatStatus = Status.Unknown;
            SupportedFrameworks = new HashSet<string>();
            TargetDotnetFrameworks = new HashSet<string>();
            Debug = string.Empty;
        }

        public bool ResultHasDependencies()
        {
            return ( Dependencies.Count > 0
                     ||  AllDlls.Count > 0 );
        }

        public HashSet<String> GetDependenciesOnResult()
        {
            if (Dependencies.Count > 0) return Dependencies;

            return AllDlls;
                     
        }

        /*
         *  Don't use AppendFormat with value directly like "{Dll}"
         *  Dll = "lib/{tfm}/ActivationHooks.dll" (It gives formating exception)
         */
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("PackageId: {0} {1} , Compatibility: {2} \n",
                PackageName, PackageVersion, Result.ToString());

            sb.AppendFormat("TargetDotnetFrameworks: {0}\n", String.Join(", ", TargetDotnetFrameworks));

            sb.AppendFormat("NuGetFolder: {0}; Size: {1} Mb", NuGetPackageFolder, Size);

            sb.AppendFormat("\nSupported Frameworks: {0}\n", String.Join("\n\t", SupportedFrameworks));
            if (Dependencies.Count > 0)
            {
                sb.AppendFormat("\nDependencies: {0}\n", String.Join("\n\t", Dependencies));
            }

            sb.AppendFormat("\nAll Dlls: {0}\n", String.Join("\n\t", AllDlls));

            if (PackageDependencies.Count > 0)
            {
                sb.AppendFormat("\nPackageDependencies: {0}\n", String.Join("\n\t", PackageDependencies));
            }

            if (Assemblies.Count > 0)
            {
                sb.AppendFormat("\nAssemblies: {0}\n", String.Join("\n\t", Assemblies));
            } 
            sb.AppendFormat($"\nLicense Info: { PackageLicenseInfo }\n");
            if (Debug != string.Empty)
            {
                sb.AppendFormat("\nDebug info:\n{0} \n", Debug);
            }

            return sb.ToString();
        }

        public override bool Equals(object obj)
        {
            return obj is PackageCompatResult result &&
                   PackageId == result.PackageId &&
                   PackageName == result.PackageName &&
                   PackageVersion == result.PackageVersion &&
                   EqualityComparer<HashSet<string>>.Default.Equals(TargetDotnetFrameworks, result.TargetDotnetFrameworks) &&
                   NuGetPackageFolder == result.NuGetPackageFolder &&
                   Result == result.Result &&
                   EqualityComparer<HashSet<string>>.Default.Equals(Dependencies, result.Dependencies) &&
                   EqualityComparer<HashSet<string>>.Default.Equals(Assemblies, result.Assemblies) &&
                   EqualityComparer<HashSet<string>>.Default.Equals(PackageDependencies, result.PackageDependencies) &&
                   EqualityComparer<PackageLicenseInfo>.Default.Equals(PackageLicenseInfo, result.PackageLicenseInfo) &&
                   NearestTarget == result.NearestTarget &&
                   OtherInfo == result.OtherInfo;
        }

        public override int GetHashCode()
        {
            HashCode hash = new HashCode();
            hash.Add(PackageId);
            hash.Add(PackageName);
            hash.Add(PackageVersion);
            hash.Add(TargetDotnetFrameworks);
            hash.Add(NuGetPackageFolder);
            hash.Add(Result);
            hash.Add(Dependencies);
            hash.Add(Assemblies);
            hash.Add(PackageDependencies);
            hash.Add(PackageLicenseInfo);
            hash.Add(NearestTarget);
            hash.Add(OtherInfo);
            return hash.ToHashCode();
        }
    }

    public class PackageLicenseInfo
    {
        public string LicenseUrl { get; internal set; }
        public string ProjectUrl { get; internal set; }
        public bool LicenseAcceptance { get; internal set; }
        public string License { get; internal set; }
        public string Description { get; internal set; }
        public string Title { get; internal set; }
        public string Summary { get; internal set; }

        public PackageLicenseInfo()
        {
            LicenseUrl = "";
            ProjectUrl = "";
            LicenseAcceptance = false;
            License = "";
            Description = "";
            Title = "";
            Summary = "";
        }

        public override bool Equals(object obj)
        {
            return obj is PackageLicenseInfo info &&
                   LicenseUrl == info.LicenseUrl &&
                   ProjectUrl == info.ProjectUrl &&
                   LicenseAcceptance == info.LicenseAcceptance &&
                   License == info.License &&
                   Description == info.Description;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(LicenseUrl, ProjectUrl, LicenseAcceptance, License, Description);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat($"\tTitle: {Title}, License: {License}");
            sb.AppendFormat($"\n\tLicenseUrl: {LicenseUrl}");
            sb.AppendFormat($"\n\tProjectUrl: {ProjectUrl}");
            sb.AppendFormat($"\n\tSummary: {Summary}");
            //sb.AppendFormat($"\n\tDetails: {Description}");
            return sb.ToString();
        }
    }

    public class PackageContext
    {
        public PackageIdentity PackageId;
        public SourcePackageDependencyInfo PackageDependencyInfo;
        public int FileSizeMb;
        public PackageReaderBase PackageReaderBase;
        public DownloadResourceResult DownloadResourceResult;

        public PackageContext(SourcePackageDependencyInfo packageSourceInfo)
        {
            this.PackageDependencyInfo = packageSourceInfo;
        }

        public override string ToString()
        {
            return String.Format($"Package: {PackageDependencyInfo.Id},  Size: {FileSizeMb} Mb");
        }
    }
}
