![Porting Assistant for .NET](./logo.png "Porting Assistant for .NET")

# Porting Assistant for .NET SDK
![Build Test](https://github.com/aws/porting-assistant-dotnet-client/workflows/Build%20Test/badge.svg)
 
Porting Assistant for .NET provides tools such as Porting Assistant for .NET standalone tool and Porting Assistant for .NET Visual Studio IDE Extension. Both tools provide functionality that scans .NET Framework applications and generates a .NET Core compatibility assessment, helping customers port their applications to Linux faster.
 
Porting Assistant for .NET tools quickly scans .NET Framework applications to identify incompatibilities with .NET Core, finds known replacements, and generates detailed compatibility assessment reports. This reduces the manual effort involved in modernizing applications to Linux.
 
**PortingAssistant.Client**  SDK package provides interfaces to analyze .NET applications, find the incompatibilities, and port applications to .NET Core. Please note that current support for porting is limited.
 
For more information about Porting Assistant and to try the tool, please refer to the documenation: https://aws.amazon.com/porting-assistant-dotnet/

## Getting Started

* Add the Porting Assistant NuGet package source into your Nuget configuration. 
   * [https://s3-us-west-2.amazonaws.com/aws.portingassistant.dotnet.download/nuget/index.json](https://s3-us-west-2.amazonaws.com/aws.portingassistant.dotnet.download/nuget/index.json)
   
* Add **PortingAssistant.Client** to your project as a Nuget Package.

* Follow the example below to see how the library can be integrated into your application for analyzing and porting an application.

```csharp
   var solutionPath = "/user/projects/TestProject/TestProject.sln";
   var outputPath = "/tmp/";
   
   /* Create configuration object */
   var configuration = new PortingAssistantConfiguration();

   /* Create PortingAssistatntClient object */
   var portingAssistantBuilder = PortingAssistantBuilder.Build(configuration, logConfig => logConfig.AddConsole());

   var portingAssistantClient = portingAssistantBuilder.GetPortingAssistant();

   /* For exporting the assessment results into a file */
   var reportExporter = portingAssistantBuilder.GetReportExporter();

   var analyzerSettings = new AnalyzerSettings
   {
       TargetFramework = "netcoreapp3.1",
    };

   /* Analyze the solution */
   var analyzeResults =  await portingAssistantClient.AnalyzeSolutionAsync(solutionPath, analyzerSettings);

   /* Generate JSON output */
   reportExporter.GenerateJsonReport(analyzeResults, outputPath);
   

   var filteredProjects = new List<string> {"projectname1", "projectname2"};

   /* Porting the application to .NET Core project */
   var projects = analyzeResults.SolutionDetails.Projects.Where(p => !filteredProjects.Contains(p.ProjectName)).ToList();
   var PortingProjectResults = analyzeResults.ProjectAnalysisResults
       .Where(project => !filteredProjects.Contains(project.ProjectName));

   var FilteredRecommendedActions = PortingProjectResults
       .SelectMany(project => project.PackageAnalysisResults.Values
       .Where(package =>
       {
            var comp = package.Result.CompatibilityResults.GetValueOrDefault(analyzerSettings.TargetFramework);
            return comp.Compatibility != Compatibility.COMPATIBLE && comp.CompatibleVersions.Count != 0;
        })
        .SelectMany(package => package.Result.Recommendations.RecommendedActions));

   var portingRequest = new PortingRequest
   {
       Projects = projects, //By default all projects are ported
       SolutionPath = solutionPath,
       TargetFramework = analyzerSettings.TargetFramework,
       RecommendedActions = FilteredRecommendedActions.ToList(),
       IncludeCodeFix = true
   };

   var portingResults =  portingAssistantClient.ApplyPortingChanges(portingRequest);

   /* Generate JSON output */
   reportExporter.GenerateJsonReport(portingResults, solutionPath, outputPath);          
```

## Getting Help

Please use these community resources for getting help. We use the GitHub issues
for tracking bugs and feature requests.

* If it turns out that you may have found a bug,
  please open an [issue](https://github.com/aws/porting-assistant-dotnet-client/issues/new)
  
* Send us an email to: aws-porting-assistant-support@amazon.com
  
## How to use this code?
* Clone the Git repository.
* Load the solution `PortingAssistant.Client.sln` using Visual Studio or Rider. 
* Create a "Run/Debug" Configuration for the "PortingAssistant.Client" project.
* Provide command line arguments for a solution path and output path, then run the application.

## Other Packages
[Codelyzer](https://github.com/aws/codelyzer): Porting Assistant uses Codelyzer to get package and API information used for finding compatibilities and replacements.

[Porting Assistant for .NET Datastore](https://github.com/aws/porting-assistant-dotnet-datastore): The repository containing the data set and recommendations used in compatibility assessment.

[Code translation assistant](https://github.com/aws/cta): The repository used to apply code translations


## Contributing
* [Adding Recommendations](https://github.com/aws/porting-assistant-dotnet-datastore/blob/master/RECOMMENDATIONS.md)

* We welcome community contributions and pull requests. See
[CONTRIBUTING](./CONTRIBUTING.md) for information on how to set up a development
environment and submit code.

# Additional Resources
 
[Porting Assistant for .NET](https://docs.aws.amazon.com/portingassistant/index.html)
 
[AWS Developer Center - Explore .NET on AWS](https://aws.amazon.com/developer/language/net/)
Find all the .NET code samples, step-by-step guides, videos, blog content, tools, and information about live events that you need in one place.
 
[AWS Developer Blog - .NET](https://aws.amazon.com/blogs/developer/category/programing-language/dot-net/)
Come see what .NET developers at AWS are up to!  Learn about new .NET software announcements, guides, and how-to's.

## Thank you
* [CsprojToVs2017](https://github.com/hvanbakel/CsprojToVs2017) - CsprojToVs2017 helps convert project files from from the legacy format to the Visual Studio 2017/2019 format.
* [Buildalyzer](https://github.com/daveaglick/Buildalyzer) - Buildalyzer lets you run MSBuild from your own code and returns information about the project.
* [Nuget.Client](https://github.com/NuGet/NuGet.Client) - Nuget.Client provides tools to interface with Nuget.org and parse Nuget configuration files.
* [Portability Analyzer](https://github.com/microsoft/dotnet-apiport) - Portability Analyzer analyzes assembly files to access API compatibility with various versions of .NET. Porting Assistant for .NET makes use of recommendations and data provided by Portability Analyzer.
* [The .NET Compiler Platform ("Roslyn")](https://github.com/dotnet/roslyn) - Roslyn provides open-source C# and Visual Basic compilers with rich code analysis APIs. 
* [.NET SDKs](https://dotnet.microsoft.com/) - .NET SDKs is a set of libraries and tools that allow developers to create .NET applications and libraries.
* [THIRD-PARTY](./THIRD-PARTY) - This project would not be possible without additional dependencies listed in [THIRD-PARTY](./THIRD-PARTY).

# License

Libraries in this repository are licensed under the Apache 2.0 License.

See [LICENSE](./LICENSE) and [NOTICE](./NOTICE) for more information.  

