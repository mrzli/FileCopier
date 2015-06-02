using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileCopier.Core
{
    public static class CopyConfigurationSeed
    {
        public static List<CopyConfiguration> CreateDefault()
        {
            List<CopyConfiguration> configurations = new List<CopyConfiguration>();

            CopyConfiguration c1 = new CopyConfiguration
            {
                Name = "Test",
                SourceDir = @"D:\temp\src",
                DestDirs = new List<string> { @"D:\temp\dest1", @"D:\temp\dest2" },
                IgnorePattern = "ignore1|ignore2|ignore.txt",
                BackupDir = @"D:\temp\backup",
                AlwaysBackup = false
            };
            configurations.Add(c1);

            return configurations;
        }
    }
}
