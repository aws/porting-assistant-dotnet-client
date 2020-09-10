using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using PortingAssistant.Model;
using Project2015To2017;
using Project2015To2017.Analysis;
using Project2015To2017.Caching;
using Project2015To2017.Definition;
using Project2015To2017.Migrate2019.Library;
using Project2015To2017.Writing;

namespace PortingAssistant.PortingProjectFile
{
    public class PortingProjectFileHandler : IPortingProjectFileHandler
    {
        private readonly ILogger _logger;
        private readonly MigrationFacility _facility;

        private static readonly ProjectWriteOptions writeOptions = new ProjectWriteOptions
        {
            MakeBackups = false
        };


        public PortingProjectFileHandler(ILogger<PortingProjectFileHandler> logger)
        {
            _logger = logger;
            _facility = new MigrationFacility(_logger);
        }

        public List<PortingResult> ApplyProjectChanges(
            List<string> projectPaths, string solutionPath, string targetFramework,
            Dictionary<string, string> upgradeVersions)
        {
            _logger.LogInformation("Applying porting changes to {0}", projectPaths);

            var results = new List<PortingResult>();
            var projectFilesNotFound = projectPaths.Where((path) => !File.Exists(path)).ToList();
            projectFilesNotFound.ForEach((path) => results.Add(new PortingResult
            {
                Message = "File not found.",
                ProjectFile = path,
                ProjectName = Path.GetFileNameWithoutExtension(path),
                Success = false
            }));

            var conversionOptions = new ConversionOptions
            {
                ForceOnUnsupportedProjects = true,
                ProjectCache = new DefaultProjectCache(),
                TargetFrameworks = new List<string> { targetFramework },
            };

            var (projects, _) = _facility.ParseProjects(new[] { solutionPath }, Vs16TransformationSet.Instance, conversionOptions);

            var selectedProjects = projects.Where(project => projectPaths.Contains(project.FilePath.FullName)).ToList();

            var writer = new ProjectWriter(_logger, writeOptions);

            foreach (var project in selectedProjects)
            {
                try
                {
                    project.PackageReferences = project.PackageReferences.Select(p =>
                        new PackageReference
                        {
                            Id = p.Id,
                            Version = upgradeVersions.ContainsKey(p.Id) ? upgradeVersions[p.Id] : p.Version,
                            IsDevelopmentDependency = p.IsDevelopmentDependency,
                            DefinitionElement = p.DefinitionElement
                        }).ToList();

                    if (writer.TryWrite(project))
                    {
                        results.Add(new PortingResult
                        {
                            Success = true,
                            ProjectFile = project.FilePath.FullName,
                            ProjectName = project.ProjectName
                        });
                    } else
                    {
                        results.Add(new PortingResult
                        {
                            Success = false,
                            ProjectFile = project.FilePath.FullName,
                            ProjectName = project.ProjectName
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Project {Item} analysis has thrown an exception",
                        project.ProjectName);

                    results.Add(new PortingResult
                    {
                        Success = false,
                        ProjectFile = project.FilePath.FullName,
                        ProjectName = project.ProjectName,
                        Execption = ex
                    });
                }
            }

            conversionOptions.ProjectCache?.Purge();

            _logger.LogInformation("Completed porting changes to {0}", projectPaths);

            return results;
        }
    }
}
