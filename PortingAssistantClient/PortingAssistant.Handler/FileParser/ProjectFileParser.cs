using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Buildalyzer;
using PortingAssistant.Model;
using PortingAssistant.Utils;
using NuGet.Packaging;
using NuGet.Versioning;
using XmlUtility = NuGet.Common.XmlUtility;

namespace PortingAssistant.Handler.FileParser
{
    public class ProjectFileParser
    {
        private static readonly string PackageReferenceFile = "packages.config";
        private readonly AnalyzerManager _analyzerManager;
        private readonly string _packageConfigFile;
        private readonly IProjectAnalyzer _projectAnalyzer;
        private readonly XDocument _document;
        private readonly XElement _projectElement;
        private readonly string _path;

        public const string ProjectReference = nameof(ProjectReference);

        public ProjectFileParser(string path)
        {
            _analyzerManager = new AnalyzerManager();
            _path = path;
            _projectAnalyzer = _analyzerManager.GetProject(_path);
            _packageConfigFile = Path.Combine(Path.GetDirectoryName(_path), PackageReferenceFile);
            _document = XDocument.Load(_path);
            _projectElement = _document.GetDescendants("Project").FirstOrDefault();
        }

        public List<string> GetTargetFrameworks()
        {
            return _projectAnalyzer.ProjectFile.TargetFrameworks.ToList();
        }

        public List<PackageVersionPair> GetPackageReferences()
        {
            // packages.config
            if (IsPackagesConfigProject())
            {
                return GetPackageReferencesFromConfigFile().Select(p =>
                    new PackageVersionPair
                    {
                        PackageId = p.PackageIdentity.Id,
                        Version = p.PackageIdentity.Version.ToNormalizedString()
                    })
                    .Where(p => p.PackageId != null && p.Version != null)
                    .ToList();
            }

            // Project References
            if (IsProjectReferenceProject())
            {
                return _projectAnalyzer.ProjectFile.PackageReferences
                    .Select(p =>
                    {
                        var version = p.Version;
                        try
                        {
                            version = new NuGetVersion(p.Version).ToNormalizedString();
                        }
                        catch (Exception)
                        {
                            // Throwing away exceptions is a code smell, but sometimes it is valid and necessary.
                            // If it is valid to throw away the exception, we should leave a comment explaining why
                        }

                        return new PackageVersionPair
                        {
                            PackageId = p.Name,
                            Version = version
                        };
                    })
                    .Where(p => p.PackageId != null && p.Version != null)
                    .ToList();
            }

            // No nuget dependencies found
            return new List<PackageVersionPair>();
        }

        public List<ProjectReference> GetProjectReferences()
        {
            return _projectElement.GetDescendants(ProjectReference)
                .Select(s => PortingAssistant.Model.ProjectReference.Get(s, _path))
                .ToList();
        }

        private bool IsPackagesConfigProject()
        {
            return File.Exists(_packageConfigFile);
        }

        private bool IsProjectReferenceProject()
        {
            return _projectAnalyzer.ProjectFile.ContainsPackageReferences;
        }

        private List<PackageReference> GetPackageReferencesFromConfigFile()
        {
            try
            {
                XDocument xDocument = XmlUtility.Load(_packageConfigFile);
                var reader = new PackagesConfigReader(xDocument);
                return reader.GetPackages(true).ToList();
            }
            catch (XmlException ex)
            {
                throw new PortingAssistantClientException("Unable to parse packages.config file", ex);
            }
        }
    }
}
