﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Buildalyzer;
using PortingAssistant.ErrorHandle;
using PortingAssistant.Model;
using PortingAssistant.Utils;
using NuGet.Packaging;
using NuGet.Versioning;
using XmlUtility = NuGet.Common.XmlUtility;

namespace PortingAssistant.FileParser
{
    public class ProjectFileParser
    {
        private static readonly string PackageReferenceFile = "packages.config";
        private readonly AnalyzerManager _manager;
        private readonly string _packageConfigFile;
        private readonly IProjectAnalyzer _projectAnalyzer;
        private readonly XDocument _document;
        private readonly XElement _projectElement;
        private readonly string _path;

        public const string ProjectReference = nameof(ProjectReference);

        public ProjectFileParser(string path)
        {
            _manager = new AnalyzerManager();
            _path = path;
            _projectAnalyzer = _manager.GetProject(_path);
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
                return ProcessPackagesConfigFile().Select(p =>
                    new PackageVersionPair
                    {
                        PackageId = p.PackageIdentity.Id,
                        Version = p.PackageIdentity.Version.ToNormalizedString()
                    }).Where(p => p != null && p.PackageId != null && p.Version != null).ToList();
            }

            // Project References
            if (IsProjectReferenceProject())
            {
                return _projectAnalyzer.ProjectFile.PackageReferences.Select(p =>
                {
                    var version = p.Version;
                    try
                    {
                        version = new NuGetVersion(p.Version).ToNormalizedString();
                    }
                    catch (Exception) { }

                    return new PackageVersionPair
                    {
                        PackageId = p.Name,
                        Version = version
                    };
                }).Where(p => p != null && p.PackageId != null && p.Version != null).ToList();
            }

            // No nuget dependencies found
            return new List<PackageVersionPair>();
        }

        public List<ProjectReference> GetProjectReferences()
        {
            return _projectElement.GetDescendants(ProjectReference).Select(s =>
                PortingAssistant.Model.ProjectReference.Get(s, _path)).ToList();
        }

        private bool IsPackagesConfigProject()
        {
            return File.Exists(_packageConfigFile);
        }

        private bool IsProjectReferenceProject()
        {
            return _projectAnalyzer.ProjectFile.ContainsPackageReferences;
        }

        private List<PackageReference> ProcessPackagesConfigFile()
        {
            try
            {
                XDocument xDocument = XmlUtility.Load(_packageConfigFile);
                var reader = new PackagesConfigReader(xDocument);
                return reader.GetPackages(true).ToList();
            }
            catch (XmlException ex)
            {
                throw new PortingAssistantAssessmentException("Unable to parse packages.config file", ex);
            }
        }
    }
}
