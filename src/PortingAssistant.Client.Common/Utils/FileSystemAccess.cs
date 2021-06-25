using System;
using System.IO;
using System.Security.Principal;
using System.Security.AccessControl;

namespace PortingAssistant.Client.Common.Utils
{
    public static class FileSystemAccess
    {
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
                if (!string.IsNullOrEmpty(result))
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
