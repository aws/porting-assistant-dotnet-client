using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using CommandLine.Text;

namespace PortingAssistant.Client.CLI
{
    public enum TargetFramework
    {
        netcoreapp31,
        netstandard21
    }

    class Options
    {
        [Option('s', "solution-path", Required = true, HelpText = "Solution file path to be analyzed")]
        public string SolutionPath { get; set; }

        public string PortingProjectPath { get; set; }

        [Option('o', "ouput-path", Required = true, HelpText = "output folder.")]
        public string OutputPath { get; set; }

        [Option('i', "ignore-projects", Separator = ',', Required = false, HelpText = "ignore projects in the solution")]
        public IEnumerable<string> IgnoreProjects { get; set; }

        [Option('p', "porting-projects", Separator = ',', Required = false, HelpText = "porting projects")]
        public IEnumerable<string> PortingProjects { get; set; }

        [Option('t', "target", Required = false, Default = TargetFramework.netcoreapp31, HelpText = "porting target framework netcoreapp31 or netstandard21, by default is netcoreapp31")]
        public TargetFramework Target { get; set; }

        [Usage(ApplicationAlias = "Porting Assistant Client")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                return new List<Example>() {
        new Example("analyze a solution", new Options { SolutionPath = "C://Path/To/example.sln", OutputPath = "C://output"}),
        new Example("analyze a solution with ignored projects", new Options { SolutionPath = "C://Path/To/example.sln", OutputPath = "C://output", IgnoreProjects = new List<string>{"projectname1","projectname2"} }),
        new Example("porting projects", new Options { SolutionPath = "C://Path/To/example.sln", OutputPath = "C://output", PortingProjects = new List<string>{"projectname1","projectname2"}, Target= TargetFramework.netcoreapp31 })
      };
            }
        }
    }

    public class PortingAssistantCLI
    {
        public string SolutionPath;
        public string OutputPath;
        public List<string> IgnoreProjects;
        public List<string> PortingProjects;
        public TargetFramework Target;

        public void HandleCommand(String[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithNotParsed(HandleParseError)
                .WithParsed(o =>
                {
                    if (string.IsNullOrEmpty(o.SolutionPath))
                    {
                        Console.WriteLine("Invalid command, please provide solution path");
                        Environment.Exit(-1);
                    }
                    SolutionPath = o.SolutionPath;

                    if (o.OutputPath == null || o.OutputPath.Length == 0)
                    {
                        Console.WriteLine("Invalid output path " + OutputPath);
                        Environment.Exit(-1);
                    }
                    OutputPath = o.OutputPath;

                    if (o.IgnoreProjects != null)
                    {
                        IgnoreProjects = o.IgnoreProjects.ToList();
                    }

                    if (o.PortingProjects != null)
                    {
                        PortingProjects = o.PortingProjects.ToList();
                        Target = o.Target;
                    }
                });
        }

        static void HandleParseError(IEnumerable<Error> errs)
        {
            Environment.Exit(-1);
        }
    }
}
