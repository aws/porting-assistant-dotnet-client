using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PortingAssistant.Client.IntegrationTests.TestUtils
{
    class DirectoryUtils
    {
        public static bool AreTwoDirectoriesEqual(
            string dirPath1, string dirPath2, string[] filesToIgnore)
        {
            DirectoryInfo dir1 = new DirectoryInfo(dirPath1);
            DirectoryInfo dir2 = new DirectoryInfo(dirPath2);

            // Take a snapshot of the file system.  
            IEnumerable<FileInfo> list1 = dir1.GetFiles(
                "*.*", SearchOption.AllDirectories)
                .Where(x => !filesToIgnore.Any(s => x.FullName.Contains(s)))
                .OrderBy(e => e.Name)
                .ToList<FileInfo>();
            IEnumerable<FileInfo> list2 = dir2.GetFiles(
                "*.*", SearchOption.AllDirectories)
                .Where(x => !filesToIgnore.Any(s => x.FullName.Contains(s)))
                .OrderBy(e => e.Name)
                .ToList<FileInfo>();

            Console.WriteLine("---------FILES IN DIR 1-----------");
            PrintFileInfos(list1);
            Console.WriteLine("---------FILES IN DIR 2-----------");
            PrintFileInfos(list2);

            // This query determines whether the two folders contain  
            // identical file lists, based on the custom file comparer  
            // that is defined in the FileCompare class.
            return Enumerable.SequenceEqual(list1, list2, new FileCompare());

        }

        static void PrintFileInfos(IEnumerable<FileInfo> fis)
        {
            foreach (FileInfo fi in fis)
            {
                Console.WriteLine("{0} | {1}", fi.Name, fi.Length);
            }
        }
    }

    // This implementation defines a very simple comparison  
    // between two FileInfo objects. It only compares the name  
    // of the files being compared and their length in bytes.  
    class FileCompare : IEqualityComparer<FileInfo>
    {
        public FileCompare() { }

        public bool Equals(FileInfo f1, FileInfo f2)
        {
            if (f1.Name != f2.Name)
            {
                return false;
            }
            if (f1.Length != f2.Length)
            {
                return false;
            }
            // TODO: Potentially compare the content of the files
            return true;
        }

        // Return a hash that reflects the comparison criteria.
        // According to the rules for IEqualityComparer<T>, if
        // Equals is true, then the hash codes must also be
        // equal. Because equality as defined here is a simple
        // value equality, not reference identity, it is possible
        // that two or more objects will produce the same  
        // hash code.  
        public int GetHashCode(FileInfo fi)
        {
            string s = $"{fi.Name}{fi.Length}";
            return s.GetHashCode();
        }
    }
}
