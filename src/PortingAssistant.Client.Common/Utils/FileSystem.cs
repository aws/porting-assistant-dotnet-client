using System;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Collections.Generic;
using System.Security.AccessControl;
using PortingAssistant.Client.Model;

namespace PortingAssistant.Client.Common.Utils
{
    public static class FileSystem
    {
        /// <summary>
        /// Checks solution and project folders to make sure we have write access
        /// </summary>
        /// <param name="projectPaths">List of project file paths</param>
        /// <param name="solutionPath">Solution file path</param>
        /// <returns>
        /// (ProjectsWithoutAccess, ProjectsWithAccess)
        /// If solution folder has no access, treats all projects as if no write access
        /// </returns>
        public static (List<string>, List<string>) VerifyFileAccess(List<string> projectPaths, string solutionPath)
        {
            var projectsWithoutAccess = new List<string>();
            var projectsWithAccess = new List<string>();

            // Check Solution Path
            if (!HaveDirectoryWriteAccess(Path.GetDirectoryName(solutionPath)))
            {
                return (projectPaths, projectsWithAccess);
            }
            // Check Projects
            foreach (string project in projectPaths)
            {
                string projectFolderPath = Path.GetDirectoryName(project);
                // assuming that a project doesn't exist in the root directory so won't be null
                if (CheckWriteAccessForDirectory(projectFolderPath))
                {
                    projectsWithAccess.Add(project);
                }
                else
                {
                    projectsWithoutAccess.Add(project);
                }
            }
            return (projectsWithoutAccess, projectsWithAccess);
        }

        /// <summary>
        /// Checks directory and all content for write access
        /// </summary>
        /// <param name="path">Directory path</param>
        /// <returns>First file/directory with no write access found if any, blank if all contents have access</returns>
        public static string CheckWriteAccessForDirectory(string path)
        {
            foreach (string file in Directory.GetFiles(path))
            {
                if (!HaveFileWriteAccess(file))
                {
                    Console.WriteLine($"File {file} does not have write access");
                    return file;
                }
            }
            foreach (string subDirectory in Directory.GetDirectories(path))
            {
                if (!HaveDirectoryWriteAccess(subDirectory))
                {
                    Console.WriteLine($"Directory {subDirectory} does not have write access");
                    return subDirectory;
                }
                string result = CheckWriteAccessForDirectory(subDirectory);
                if (result != string.Empty)
                {
                    return result;
                }
            }
            return "";
        }

        private static bool HaveFileWriteAccess(string path)
        {
            var fileInfo = new FileInfo(path);
            FileSecurity accessControl = fileInfo.GetAccessControl();
            AuthorizationRuleCollection rules = accessControl.GetAccessRules(true, true, typeof(SecurityIdentifier));
            return CheckAuthorizationRules(rules);
        }

        private static bool HaveDirectoryWriteAccess(string path)
        {
            var directoryInfo = new DirectoryInfo(path);
            DirectorySecurity accessControl = directoryInfo.GetAccessControl();
            AuthorizationRuleCollection rules = accessControl.GetAccessRules(true, true, typeof(SecurityIdentifier));
            return CheckAuthorizationRules(rules);
        }

        // Checks rules for write permission
        private static bool CheckAuthorizationRules(AuthorizationRuleCollection rules)
        {
            bool hasAllowWrite = false;
            bool hasDenyWrite = false;

            var identity = WindowsIdentity.GetCurrent();

            foreach (AuthorizationRule rule in rules)
            {
                if (identity.User.Equals(rule.IdentityReference) || identity.Groups.Contains(rule.IdentityReference))
                {
                    var filesystemAccessRule = (FileSystemAccessRule)rule;
                    if ((filesystemAccessRule.FileSystemRights & FileSystemRights.Write) > 0 && filesystemAccessRule.AccessControlType != AccessControlType.Deny)
                    {
                        hasAllowWrite = true;
                    }
                    else if ((filesystemAccessRule.FileSystemRights & FileSystemRights.Write) > 0 && filesystemAccessRule.AccessControlType == AccessControlType.Deny)
                    {
                        hasDenyWrite = true;
                    }
                }
            }
            return hasAllowWrite && !hasDenyWrite;
        }
    }
}
