using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileCopier.Core
{
    public sealed class CopyConfiguration
    {
        public string Name { get; set; }
        public string SourceDir { get; set; }
        public List<string> DestDirs { get; set; }
        public string IgnorePattern { get; set; }
        public string BackupDir { get; set; }
        public bool AlwaysBackup { get; set; }
    }
}
