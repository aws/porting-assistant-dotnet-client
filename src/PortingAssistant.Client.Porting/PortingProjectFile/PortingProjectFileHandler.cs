using CTA.Rules.Models;
using CTA.Rules.PortCore;
using Microsoft.Extensions.Logging;
using PortingAssistant.Client.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PortingAssistant.Client.PortingProjectFile
{
    /// <summary>
    /// Creates a handler to port projects
    /// </summary>
    public class PortingProjectFileHandler : IPortingProjectFileHandler
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Creates an instance of a PortingProjectFileHandler
        /// </summary>
        /// <param name="logger">An ILogger object</param>
        public PortingProjectFileHandler(ILogger<PortingProjectFileHandler> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Ports a list of projects
        /// </summary>
        /// <param name="projectPaths">List of projects paths</param>
        /// <param name="solutionPath">Path to solution file</param>
        /// <param name="targetFramework">Target framework to be used when porting</param>
        /// <param name="upgradeVersions">List of key/value pairs where key is package and value is version number</param>
        /// <returns>A PortingProjectFileResult object, representing the result of the porting operation</returns>
        /// 
        public List<PortingResult> ApplyProjectChanges(
            List<ProjectDetails> projects, string solutionPath, string targetFramework,
            Dictionary<string, Tuple<string, string>> upgradeVersions)
        {
            return ApplyProjectChanges(projects, solutionPath, targetFramework, true, upgradeVersions);
        }

        /// <summary>
        /// Ports a list of projects
        /// </summary>
        /// <param name="projectPaths">List of projects paths</param>
        /// <param name="solutionPath">Path to solution file</param>
        /// <param name="targetFramework">Target framework to be used when porting</param>
        /// <param name="upgradeVersions">List of key/value pairs where key is package and value is version number</param>
        /// <returns>A PortingProjectFileResult object, representing the result of the porting operation</returns>
        /// 
        public List<PortingResult> ApplyProjectChanges(
            List<ProjectDetails> projects, string solutionPath, string targetFramework,
            bool includeCodeFix,
            Dictionary<string, Tuple<string, string>> upgradeVersions)
        {
            var results = new List<PortingResult>();

            var projectFilesNotFound = projects.Where((p) => !File.Exists(p.ProjectFilePath)).ToList();
            projectFilesNotFound.ForEach((p) => results.Add(new PortingResult
            {
                Message = "File not found.",
                ProjectFile = p.ProjectFilePath,
                ProjectName = Path.GetFileNameWithoutExtension(p.ProjectFilePath),
                Success = false
            }));
            projects = projects.Where((p) => File.Exists(p.ProjectFilePath)).ToList();

            var (projectsWithAccess, noAccessPortingResults) = VerifyFileAccess(projects);
            results.AddRange(noAccessPortingResults);
            projects = projectsWithAccess;

            _logger.LogInformation("Applying porting changes to {0}", projects.Select(p => p.ProjectFilePath).ToList());

            List<PortCoreConfiguration> configs = new List<PortCoreConfiguration>();
            projects.Where(p => !p.IsBuildFailed).ToList().ForEach((proj) =>
            {
                var upgradePackages = upgradeVersions
                    .Where(p => proj.PackageReferences
                    .Exists(package => package.PackageId == p.Key))
                    .ToDictionary(t => t.Key, t => t.Value);

                configs.Add(new PortCoreConfiguration()
                {
                    ProjectPath = proj.ProjectFilePath,
                    UseDefaultRules = true,
                    PackageReferences = upgradePackages,
                    TargetVersions = new List<string> { targetFramework },
                    PortCode = includeCodeFix
                });
            });

            try
            {
                SolutionPort solutionPort = new SolutionPort(solutionPath, configs, _logger);
                solutionPort.Run();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to port projects {projects.Select(p => p.ProjectFilePath).ToList()} with error: {ex}");
                configs.ForEach(config =>
                {
                    if (!projectFilesNotFound.Exists(p => p.ProjectFilePath == config.ProjectPath))
                    {
                        results.Add(new PortingResult
                        {
                            Message = $"porting project with error {ex.Message}",
                            Success = false,
                            ProjectFile = config.ProjectPath,
                            ProjectName = Path.GetFileNameWithoutExtension(config.ProjectPath)
                        });
                    }
                });
                return results;
            }

            //TODO Return result from solution run
            projects.Where(p => !projectFilesNotFound.Exists(proj => proj.ProjectFilePath == p.ProjectFilePath) && !p.IsBuildFailed)
                .ToList().ForEach((p) => results.Add(new PortingResult
                {
                    ProjectFile = p.ProjectFilePath,
                    ProjectName = Path.GetFileNameWithoutExtension(p.ProjectFilePath),
                    Success = true
                }));

            _logger.LogInformation("Completed porting changes to {0}", projects.Select(p => p.ProjectFilePath).ToList());

            return results;
        }

        /// <summary>
        /// Checks projects to make sure we have access to the project file and at least one csharp file
        /// </summary>
        /// <param name="projects">List of projects to check for write access</param>
        /// <returns>
        /// Valid projects with access, Porting result for projects without access
        /// </returns>
        private (List<ProjectDetails>, List<PortingResult>) VerifyFileAccess(List<ProjectDetails> projects)
        {
            var noAccessPortingResults = new List<PortingResult>();
            var projectsWithAccess = new List<ProjectDetails>();

            foreach (ProjectDetails project in projects)
            {
                if (Common.Utils.FileSystemAccess.CheckWriteAccessForProject(project.ProjectFilePath))
                {
                    projectsWithAccess.Add(project);
                }
                else
                {
                    noAccessPortingResults.Add(new PortingResult
                    {
                        Success = false,
                        ProjectFile = project.ProjectFilePath,
                        ProjectName = project.ProjectName,
                        Message = $"Application does not have write access to project: {project.ProjectName}",
                        Exception = new UnauthorizedAccessException()
                    });
                }
            }
            return (projectsWithAccess, noAccessPortingResults);
        }
    }
}
