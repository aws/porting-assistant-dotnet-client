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
        public List<PortingResult> ApplyProjectChanges(
            List<string> projectPaths, string solutionPath, string targetFramework,
            Dictionary<string, string> upgradeVersions)
        {
            _logger.LogInformation("Applying porting changes to {0}", projectPaths);

            var results = new List<PortingResult>();
            List<PortCoreConfiguration> configs = new List<PortCoreConfiguration>();

            projectPaths.ForEach((proj) =>
            {
                configs.Add(new PortCoreConfiguration()
                {
                    ProjectPath = proj,
                    UseDefaultRules = true,
                    PackageReferences = upgradeVersions,
                    TargetVersions = new List<string> { targetFramework }
                });
            });

            var projectFilesNotFound = projectPaths.Where((path) => !File.Exists(path)).ToList();
            projectFilesNotFound.ForEach((path) => results.Add(new PortingResult
            {
                Message = "File not found.",
                ProjectFile = path,
                ProjectName = Path.GetFileNameWithoutExtension(path),
                Success = false
            }));

            try
            {
                SolutionPort solutionPort = new SolutionPort(solutionPath, configs, _logger);
                solutionPort.Run();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to port projects{projectPaths}");
                configs.ForEach(config =>
                {
                    if (!projectFilesNotFound.Contains(config.ProjectPath))
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
            projectPaths.Where(p => !projectFilesNotFound.Contains(p)).ToList().ForEach((path) => results.Add(new PortingResult
            {
                ProjectFile = path,
                ProjectName = Path.GetFileNameWithoutExtension(path),
                Success = true
            }));

            _logger.LogInformation("Completed porting changes to {0}", projectPaths);

            return results;
        }
    }
}
