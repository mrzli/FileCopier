using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileCopier.Core
{
    internal static class FileSystemInfoBaseExtensions
    {
        public static string GetRelativePath(this FileSystemInfoBase info, string parentDir)
        {
            return info.FullName.Remove(0, parentDir.Length).Trim('\\');
        }

        public static DirectoryInfoBase GetParentDirectory(this FileSystemInfoBase info)
        {
            DirectoryInfoBase parent;
            if (info is FileInfoBase)
            {
                parent = ((FileInfoBase)info).Directory;
            }
            else if (info is DirectoryInfoBase)
            {
                parent = ((DirectoryInfoBase)info).Parent;
            }
            else
            {
                parent = null;
            }

            return parent;
        }
    }
}
