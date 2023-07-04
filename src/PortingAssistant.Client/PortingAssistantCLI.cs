using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using CommandLine.Text;
using Newtonsoft.Json;
using NuGet.Protocol.Plugins;
using PortingAssistant.Client.Common.Utils;
using PortingAssistantExtensionTelemetry;
using Serilog.Events;

namespace PortingAssistant.Client.CLI
{
    [Verb("assess", HelpText = "Assess an .NET solution file.")]
    class AssessOptions
    {
        [Option('s', "solution-path", Required = true, HelpText = "Solution file path to be analyzed")]
        public string SolutionPath { get; set; }

        public string PortingProjectPath { get; set; }

        [Option('o', "output-path", Required = true, HelpText = "output folder.")]
        public string OutputPath { get; set; }

        [Option('t', "target", Required = false, Default = "net6.0", HelpText = "Target framework: net6.0, net5.0,  netcoreapp3.1 or netstandard2.1, by default is net6.0")]
        public string Target { get; set; }

        [Option('i', "ignore-projects", Separator = ',', Required = false, HelpText = "ignore projects in the solution")]
        public IEnumerable<string> IgnoreProjects { get; set; }

        [Option('p', "porting-projects", Separator = ',', Required = false, HelpText = "porting projects")]
        public IEnumerable<string> PortingProjects { get; set; }

        [Option('g', "tag", Required = false, Default = "client", HelpText = "metrics/logs will be tagged by provided tag")]
        public string Tag { get; set; }

        [Option('r', "profile", Required = false, HelpText = "Aws named profile, if provided, CLI will collect logs and metrics.")]
        public string Profile { get; set; }

        [Option('u', "use-generator", Required = false, Default = false, HelpText = "Set whether a generator is used to analyze the solution.")]
        public bool UseGenerator { get; set; }

        [Option('d', "enable-default-credentials", Required = false, Default = false, HelpText = "Set if default credentials is being used.")]
        public bool EnabledDefaultCredentials { get; set; }

        [Option('m', "disable-metrics", Required = false, Default = false, HelpText = "Prevents the metrics report from being generated.")]
        public bool DisabledMetrics { get; set; }

        [Option('l', "logging-level", Required = false, Default = "debug", HelpText = "Set the minimum logging level: debug (default), info, warn, error, fatal, or silent.")]
        public string MinimumLoggingLevel { get; set; }
        
        [Option('e', "egress-point", Required = false, Default = "", HelpText = "Set different egress point for logs and metrics upload.")]
        public string EgressPoint { get; set; }

        [Usage(ApplicationAlias = "Porting Assistant Client")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                return new List<Example>() {
                    new Example("analyze a solution", new AssessOptions { SolutionPath = "C://Path/To/example.sln", OutputPath = "C://output"}),
                    new Example("analyze a solution with ignored projects", new AssessOptions { SolutionPath = "C://Path/To/example.sln", OutputPath = "C://output", IgnoreProjects = new List<string>{"projectname1","projectname2"} }),
                    new Example("porting projects", new AssessOptions { SolutionPath = "C://Path/To/example.sln", OutputPath = "C://output", PortingProjects = new List<string>{"projectname1","projectname2"}, Target= "netcoreapp3.1" })
                };
            }
        }
    }

    [Verb("schema", HelpText = "Get the assessment schema information.")]
    class SchemaOptions
    {
        [Option('s', "schema-version", Required = false, HelpText = "Get the schema version.")]
        public bool SchemaVersion { get; set; }
    }

    public class PortingAssistantCLI
    {
        public string SolutionPath;
        public string OutputPath;
        public List<string> IgnoreProjects;
        public List<string> PortingProjects;
        public string Target;
        public string Tag;
        public string Profile;
        public bool UseGenerator;
        public bool EnabledDefaultCredentials;
        public LogEventLevel MinimumLoggingLevel;
        public string EgressPoint;

        public bool isAssess = false;
        public bool isSchema = false;
        public bool schemaVersion = false;

        public void HandleCommand(String[] args)
        {
            var TargetFrameworks = new HashSet<string> {"net7.0", "net6.0", "net5.0", "netcoreapp3.1", "netstandard2.1" };

            Parser.Default.ParseArguments<AssessOptions, SchemaOptions>(args)
                .WithNotParsed(HandleParseError)
                .WithParsed<AssessOptions>(o =>
                {
                    isAssess = true;
                    if (string.IsNullOrEmpty(o.SolutionPath) || !File.Exists(o.SolutionPath) || !o.SolutionPath.EndsWith(".sln"))
                    {
                        Console.WriteLine("Invalid command, please provide valid solution path");
                        Environment.Exit(-1);
                    }
                    SolutionPath = o.SolutionPath;

                    if (string.IsNullOrEmpty(o.OutputPath) || !Directory.Exists(o.OutputPath))
                    {
                        Console.WriteLine("Invalid output path " + OutputPath);
                        Environment.Exit(-1);
                    }
                    OutputPath = o.OutputPath;

                    if (!TargetFrameworks.Contains(o.Target.ToLower()))
                    {
                        Console.WriteLine("Invalid targetFramework " + OutputPath);
                        Environment.Exit(-1);
                    }
                    Target = o.Target;

                    Tag = o.Tag;

                    Profile = o.Profile;

                    UseGenerator = o.UseGenerator;

                    EnabledDefaultCredentials = o.EnabledDefaultCredentials;

                    EgressPoint = o.EgressPoint;

                    if (o.IgnoreProjects != null)
                    {
                        IgnoreProjects = o.IgnoreProjects.ToList();
                    }

                    if (o.PortingProjects != null)
                    {
                        PortingProjects = o.PortingProjects.ToList();
                    }

                    TelemetryCollector.ToggleMetrics(o.DisabledMetrics);
                    TraceEvent.ToggleMetrics(o.DisabledMetrics);
                    MemoryUtils.ToggleMetrics(o.DisabledMetrics);

                    switch (o.MinimumLoggingLevel.ToLower())
                    {
                        case "silent":
                            MinimumLoggingLevel = Serilog.Events.LogEventLevel.Fatal + 1;
                            break;
                        case "fatal":
                            MinimumLoggingLevel = Serilog.Events.LogEventLevel.Fatal;
                            break;
                        case "error":
                            MinimumLoggingLevel = Serilog.Events.LogEventLevel.Error;
                            break;
                        case "warn":
                            MinimumLoggingLevel = Serilog.Events.LogEventLevel.Warning;
                            break;
                        case "info":
                            MinimumLoggingLevel = Serilog.Events.LogEventLevel.Information;
                            break;
                        case "debug":
                            MinimumLoggingLevel = Serilog.Events.LogEventLevel.Debug;
                            break;
                        default:
                            MinimumLoggingLevel = Serilog.Events.LogEventLevel.Debug;
                            Console.WriteLine("Invalid logging level: \"" + o.MinimumLoggingLevel + "\". Minimum logging level has instead been set to \"debug\".");
                            break;
                    }
                })
                .WithParsed<SchemaOptions>(o =>
                {
                    isSchema = true;
                    if (o.SchemaVersion)
                    {
                        schemaVersion = o.SchemaVersion;
                    }

                }); ;
        }

        static void HandleParseError(IEnumerable<Error> errs)
        {
            Environment.Exit(-1);
        }
    }
}