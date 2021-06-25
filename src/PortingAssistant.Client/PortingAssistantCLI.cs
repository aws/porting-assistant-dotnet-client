using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using CommandLine.Text;

namespace PortingAssistant.Client.CLI
{

    class Options
    {
        [Option('b', "protobuf-path", Required = false, HelpText = "Proto file path to be used")]
        public string ProtoFilePath { get; set; }

        [Option('s', "solution-path", Required = false, HelpText = "Solution file path to be analyzed")]
        public string SolutionPath { get; set; }

        public string PortingProjectPath { get; set; }

        [Option('o', "output-path", Required = false, HelpText = "output folder.")]
        public string OutputPath { get; set; }

        [Option('t', "target", Required = false, Default = "netcoreapp3.1", HelpText = "Target framework: net5.0,  netcoreapp3.1 or netstandard2.1, by default is netcoreapp3.1")]
        public string Target { get; set; }

        [Option('i', "ignore-projects", Separator = ',', Required = false, HelpText = "ignore projects in the solution")]
        public IEnumerable<string> IgnoreProjects { get; set; }

        [Option('p', "porting-projects", Separator = ',', Required = false, HelpText = "porting projects")]
        public IEnumerable<string> PortingProjects { get; set; }


        [Usage(ApplicationAlias = "Porting Assistant Client")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                return new List<Example>() {
        new Example("analyze a solution", new Options { SolutionPath = "C://Path/To/example.sln", OutputPath = "C://output"}),
        new Example("analyze a solution with ignored projects", new Options { SolutionPath = "C://Path/To/example.sln", OutputPath = "C://output", IgnoreProjects = new List<string>{"projectname1","projectname2"} }),
        new Example("porting projects", new Options { SolutionPath = "C://Path/To/example.sln", OutputPath = "C://output", PortingProjects = new List<string>{"projectname1","projectname2"}, Target= "netcoreapp3.1" })
      };
            }
        }
    }

    public class PortingAssistantCLI
    {
        public string ProtoFilePath;
        public string SolutionPath;
        public string OutputPath;
        public List<string> IgnoreProjects;
        public List<string> PortingProjects;
        public string Target;

        public void HandleCommand(String[] args)
        {
            var TargetFrameworks = new HashSet<string> { "net5.0", "netcoreapp3.1", "netstandard2.1" };

            Parser.Default.ParseArguments<Options>(args)
                .WithNotParsed(HandleParseError)
                .WithParsed(o =>
                {
                    if (o.ProtoFilePath != null)
                    {
                        ProtoFilePath = o.ProtoFilePath;
                    }

                    if ((string.IsNullOrEmpty(o.ProtoFilePath) || !File.Exists(o.ProtoFilePath)) && (string.IsNullOrEmpty(o.SolutionPath) || !File.Exists(o.SolutionPath) || !o.SolutionPath.EndsWith(".sln")))
                    {
                        Console.WriteLine("Invalid command, please provide valid solution path or proto file path");
                        Environment.Exit(-1);
                    }

                    if (o.SolutionPath != null)
                    {
                        SolutionPath = o.SolutionPath;
                    }


                    if ((string.IsNullOrEmpty(o.ProtoFilePath) || !File.Exists(o.ProtoFilePath)) && (string.IsNullOrEmpty(o.OutputPath) || !Directory.Exists(o.OutputPath)))
                    {
                        Console.WriteLine("Invalid output path " + OutputPath);
                        Environment.Exit(-1);
                    }

                    if (o.OutputPath != null)
                    {
                        OutputPath = o.OutputPath;
                    }

                    if (!TargetFrameworks.Contains(o.Target.ToLower()))
                    {
                        Console.WriteLine("Invalid targetFramework " + OutputPath);
                        Environment.Exit(-1);
                    }
                    Target = o.Target;

                    if (o.IgnoreProjects != null)
                    {
                        IgnoreProjects = o.IgnoreProjects.ToList();
                    }

                    if (o.PortingProjects != null)
                    {
                        PortingProjects = o.PortingProjects.ToList();
                    }
                });
        }

        static void HandleParseError(IEnumerable<Error> errs)
        {
            Environment.Exit(-1);
        }
    }
}
