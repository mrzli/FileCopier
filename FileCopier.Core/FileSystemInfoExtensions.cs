using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileCopier.Core
{
    internal static class FileSystemInfoExtensions
    {
        public static string GetRelativePath(this FileSystemInfo info, string parentDir)
        {
            return info.FullName.Remove(0, parentDir.Length).Trim('\\');
        }

        public static DirectoryInfo GetParentDirectory(this FileSystemInfo info)
        {
            DirectoryInfo parent;
            if (info is FileInfo)
            {
                parent = ((FileInfo)info).Directory;
            }
            else if (info is DirectoryInfo)
            {
                parent = ((DirectoryInfo)info).Parent;
            }
            else
            {
                parent = null;
            }

            return parent;
        }

        public static bool EqualsTo(this FileSystemInfo info1, FileSystemInfo info2)
        {
            string path1 = Path.Combine(info1.GetParentDirectory().FullName, info1.Name);
            string path2 = Path.Combine(info2.GetParentDirectory().FullName, info2.Name);
            return path1.Equals(path2, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
