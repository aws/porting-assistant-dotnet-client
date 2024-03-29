﻿using System;
using System.IO;
using NUnit.Framework;
using PortingAssistant.Client.Client.FileParser;

namespace PortingAssistant.Client.UnitTests
{
    public class GitConfigFileParserTest
    {
        [Test]
        public void getGitRepositoryRootPath_Returns_Expected_Path()
        {
            string gitRootPath = GitConfigFileParser.getGitRepositoryRootPath(
                Directory.GetCurrentDirectory());
            string expectedRootPathEnding = Path.Combine("porting-assistant-dotnet-client", ".git");
            Assert.IsTrue(gitRootPath.Contains(expectedRootPathEnding, StringComparison.OrdinalIgnoreCase));
        }

        [Test]
        public void getGitRepositoryRootPath_Returns_Null_On_NonExisting_Path()
        {
            string gitRootPath = GitConfigFileParser.getGitRepositoryRootPath(@"C:\\RandomFile\\Path\\solution.sln");
            Assert.AreEqual(null, gitRootPath);
        }

        [Test]
        public void getGitRepositoryUrl_Returns_Null_On_Invalid_Path()
        {
            string gitUrl = GitConfigFileParser.getGitRepositoryUrl(@"C:\\RandomFile\\Path\\solution\\.git\");
            Assert.AreEqual(null, gitUrl);
        }

        [Test]
        public void getGitRepositoryUrl_Returns_Null_On_Null_Path()
        {
            string gitUrl = GitConfigFileParser.getGitRepositoryUrl(null);
            Assert.AreEqual(null, gitUrl);
        }
    }
}
