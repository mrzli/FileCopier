using FileCopier.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileCopier.ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                RunProgram();
            }
            catch (Exception e)
            {
                Console.WriteLine("\n\nException: " + e.ToString());
            }

            Console.ReadLine();
        }

        private static void RunProgram()
        {
            //CopyConfigurationAccessor.Write(CopyConfigurationSeed.CreateDefault(), "copyconfig.json");
            List<CopyConfiguration> configurations = CopyConfigurationAccessor.Read("copyconfig.json");

            string input = QueryOption(configurations);

            if (input == "e")
            {
                return;
            }

            int configurationIndex = GetConfigurationChoice(input, configurations.Count) - 1;

            CopyConfiguration configuration = configurations[configurationIndex];
            PrintConfiguration(configuration);

            if (!QueryYesNo("Proceed (y/n)? "))
            {
                return;
            }

            Executor executor = new Executor();

            string message;
            bool success = executor.Execute(
                configuration,
                () => QueryYesNo("Create Backup (y/n)? "),
                () => Console.Write("Creating Backup"),
                () => Console.Write("."),
                () => Console.WriteLine("\nBackup Created"),
                () => Console.Write("Copy Started"),
                () => Console.Write("."),
                () => Console.WriteLine("\nFiles Copied"),
                out message);

            if (success)
            {
                Console.WriteLine("Copy operation successful!");
            }
            else
            {
                Console.WriteLine("Copy operation failed! Message: " + message);
            }
        }

        private static string QueryOption(List<CopyConfiguration> configurations)
        {
            Console.WriteLine("Options:");
            int index = 1;
            foreach (CopyConfiguration configuration in configurations)
            {
                Console.WriteLine("{0} - {1}", index, configuration.Name);
            }
            Console.WriteLine("e - Exit");
            Console.WriteLine();

            string input;
            do
            {
                Console.Write("Select option: ");
                input = Console.ReadLine();
            } while (!IsInputValid(input, configurations.Count));

            Console.WriteLine();

            return input;
        }

        private static void PrintConfiguration(CopyConfiguration configuration)
        {
            Console.WriteLine("Name: " + configuration.Name);
            Console.WriteLine("Source Dir: " + configuration.SourceDir);
            foreach (string destDir in configuration.DestDirs)
            {
                Console.WriteLine("Dest Dir: " + destDir);
            }
            Console.WriteLine("Ignore Pattern: " + configuration.IgnorePattern);
            Console.WriteLine("Backup Dir: " + configuration.BackupDir);
            Console.WriteLine("Always Backup: " + configuration.AlwaysBackup);
            Console.WriteLine();
        }

        private static bool QueryYesNo(string text)
        {
            bool? answer;
            do
            {
                Console.Write(text);
                string input = Console.ReadLine();
                answer = GetYesNo(input);
            } while (!answer.HasValue);

            return answer.Value;
        }

        private static bool IsInputValid(string input, int numConfigurations)
        {
            input = input.Trim();
            return input == "e" || GetConfigurationChoice(input, numConfigurations) != -1;
        }

        private static int GetConfigurationChoice(string input, int numConfigurations)
        {
            int configurationChoice;
            if (int.TryParse(input, out configurationChoice) && configurationChoice >= 1 && configurationChoice < configurationChoice + numConfigurations)
            {
                return configurationChoice;
            }
            else
            {
                return -1;
            }
        }

        private static bool? GetYesNo(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return null;
            }

            input = input.ToLower();
            if (input == "y" || input == "yes")
            {
                return true;
            }
            else if (input == "n" || input == "no")
            {
                return false;
            }
            else
            {
                return null;
            }
        }
    }
}
