using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using CommandLine.Text;

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

        [Option('t', "target", Required = false, Default = "netcoreapp3.1", HelpText = "Target framework: net5.0,  netcoreapp3.1 or netstandard2.1, by default is netcoreapp3.1")]
        public string Target { get; set; }

        [Option('i', "ignore-projects", Separator = ',', Required = false, HelpText = "ignore projects in the solution")]
        public IEnumerable<string> IgnoreProjects { get; set; }

        [Option('p', "porting-projects", Separator = ',', Required = false, HelpText = "porting projects")]
        public IEnumerable<string> PortingProjects { get; set; }

        [Option('g', "tag", Required = false, Default = "client", HelpText = "tag the metrics source with: MH, drift or client, by default is client")]
        public string Tag { get; set; }


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

        public bool isAssess = false;
        public bool isSchema = false;
        public bool schemaVersion = false;

        public void HandleCommand(String[] args)
        {
            var TargetFrameworks = new HashSet<string> { "net5.0", "netcoreapp3.1", "netstandard2.1" };
            var Tags = new HashSet<string> { "mh", "drift", "client" };

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

                    if (!Tags.Contains(o.Tag.ToLower()))
                    {
                        Console.WriteLine("Invalid tag " + OutputPath);
                        Environment.Exit(-1);
                    }
                    Tag = o.Tag;

                    if (o.IgnoreProjects != null)
                    {
                        IgnoreProjects = o.IgnoreProjects.ToList();
                    }

                    if (o.PortingProjects != null)
                    {
                        PortingProjects = o.PortingProjects.ToList();
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