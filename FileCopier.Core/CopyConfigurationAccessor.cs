using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileCopier.Core
{
    public static class CopyConfigurationAccessor
    {
        public static List<CopyConfiguration> Read(string path)
        {
            string json = File.ReadAllText(path);
            List<CopyConfiguration> configurations = JsonConvert.DeserializeObject<List<CopyConfiguration>>(json);
            return configurations;
        }

        public static void Write(List<CopyConfiguration> configurationData, string path)
        {
            string json = JsonConvert.SerializeObject(configurationData, Formatting.Indented);
            File.WriteAllText(path, json);
        }
    }
}
