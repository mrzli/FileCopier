using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.IO.Abstractions;

namespace FileCopier.Core.Test
{
    [TestClass]
    public class CopyExecutorTests
    {
        private const string SRC_CONTENT = "src";
        private const string DEST_CONTENT = "dest";

        [TestMethod]
        public void TestValidateConfiguration()
        {
            MockFileSystem fileSystem = CreateMockFileSystem();
            CopyConfiguration config;
            bool validationSuccess;

            config = null;
            validationSuccess = InvokeValidateConfiguration(fileSystem, config);
            Assert.IsFalse(validationSuccess);

            config = CreateCopyConfiguration();
            validationSuccess = InvokeValidateConfiguration(fileSystem, config);
            Assert.IsTrue(validationSuccess);

            config = CreateCopyConfiguration();
            config.Name = null;
            validationSuccess = InvokeValidateConfiguration(fileSystem, config);
            Assert.IsFalse(validationSuccess);

            config = CreateCopyConfiguration();
            config.Name = "";
            validationSuccess = InvokeValidateConfiguration(fileSystem, config);
            Assert.IsFalse(validationSuccess);

            config = CreateCopyConfiguration();
            config.SourceDir = null;
            validationSuccess = InvokeValidateConfiguration(fileSystem, config);
            Assert.IsFalse(validationSuccess);

            config = CreateCopyConfiguration();
            config.SourceDir = "";
            validationSuccess = InvokeValidateConfiguration(fileSystem, config);
            Assert.IsFalse(validationSuccess);

            config = CreateCopyConfiguration();
            config.SourceDir = @"D:\invalid_folder";
            validationSuccess = InvokeValidateConfiguration(fileSystem, config);
            Assert.IsFalse(validationSuccess);

            config = CreateCopyConfiguration();
            config.DestDirs = null;
            validationSuccess = InvokeValidateConfiguration(fileSystem, config);
            Assert.IsFalse(validationSuccess);

            config = CreateCopyConfiguration();
            config.DestDirs = new List<string> { };
            validationSuccess = InvokeValidateConfiguration(fileSystem, config);
            Assert.IsFalse(validationSuccess);

            config = CreateCopyConfiguration();
            config.DestDirs = new List<string> { @"D:\invalid_folder" };
            validationSuccess = InvokeValidateConfiguration(fileSystem, config);
            Assert.IsFalse(validationSuccess);

            config = CreateCopyConfiguration();
            config.DestDirs = new List<string> { @"D:\temp\FileCopier\dest1", @"D:\invalid_folder" };
            validationSuccess = InvokeValidateConfiguration(fileSystem, config);
            Assert.IsFalse(validationSuccess);

            config = CreateCopyConfiguration();
            config.IgnorePattern = null;
            validationSuccess = InvokeValidateConfiguration(fileSystem, config);
            Assert.IsTrue(validationSuccess);

            config = CreateCopyConfiguration();
            config.IgnorePattern = "";
            validationSuccess = InvokeValidateConfiguration(fileSystem, config);
            Assert.IsTrue(validationSuccess);

            config = CreateCopyConfiguration();
            config.BackupDir = null;
            validationSuccess = InvokeValidateConfiguration(fileSystem, config);
            Assert.IsFalse(validationSuccess);

            config = CreateCopyConfiguration();
            config.BackupDir = "";
            validationSuccess = InvokeValidateConfiguration(fileSystem, config);
            Assert.IsFalse(validationSuccess);

            config = CreateCopyConfiguration();
            config.BackupDir = @"D:\invalid_folder";
            validationSuccess = InvokeValidateConfiguration(fileSystem, config);
            Assert.IsFalse(validationSuccess);
        }

        [TestMethod]
        public void TestExecuteMock()
        {
            TestExecute(CreateMockFileSystem());
        }

        [TestMethod]
        public void TestExecuteReal()
        {
            TestExecute(CreateRealFileSystem());
        }

        private static void TestExecute(IFileSystem fileSystem)
        {
            CopyConfiguration config = CreateCopyConfiguration();

            CopyExecutor executor = new CopyExecutor(fileSystem, (x, y) => x);
            string message;
            executor.Execute(config, null, null, null, null, null, null, null, out message);

            List<string> expectedExistingDirectories = new List<string>
            {
                @"D:\temp\FileCopier\",
                @"D:\temp\FileCopier\src\",
                @"D:\temp\FileCopier\src\folder\",
                @"D:\temp\FileCopier\src\folder\ignore1\",
                @"D:\temp\FileCopier\src\ignore1\",
                @"D:\temp\FileCopier\src\ignore2\",
                @"D:\temp\FileCopier\dest1\",
                @"D:\temp\FileCopier\dest1\folder\",
                @"D:\temp\FileCopier\dest1\folder\ignore1",
                @"D:\temp\FileCopier\dest2\folder\",
                @"D:\temp\FileCopier\dest2\folder\ignore1",
                @"D:\temp\FileCopier\backup\",
                @"D:\temp\FileCopier\backup\dest1\",
                @"D:\temp\FileCopier\backup\dest1\folder\",
                @"D:\temp\FileCopier\backup\dest2\",
                @"D:\temp\FileCopier\backup\dest2\folder2\"
            };

            foreach (string dir in expectedExistingDirectories)
            {
                Assert.IsTrue(fileSystem.Directory.Exists(dir));
            }

            List<string> expectedExistingFiles = new List<string>
            {
                @"D:\temp\FileCopier\src\folder\ignore1\file.txt",
                @"D:\temp\FileCopier\src\folder\ignore1\file2.txt",
                @"D:\temp\FileCopier\src\folder\ignore1\ignore.txt",
                @"D:\temp\FileCopier\src\folder\file.txt",
                @"D:\temp\FileCopier\src\folder\file2.txt",
                @"D:\temp\FileCopier\src\folder\ignore.txt",
                @"D:\temp\FileCopier\src\ignore1\file.txt",
                @"D:\temp\FileCopier\src\ignore1\file2.txt",
                @"D:\temp\FileCopier\src\ignore1\ignore.txt",
                @"D:\temp\FileCopier\src\ignore2\file.txt",
                @"D:\temp\FileCopier\src\ignore2\file2.txt",
                @"D:\temp\FileCopier\src\ignore2\ignore.txt",
                @"D:\temp\FileCopier\src\file.txt",
                @"D:\temp\FileCopier\src\file2.txt",
                @"D:\temp\FileCopier\src\ignore.txt",
                @"D:\temp\FileCopier\dest1\folder\ignore1\file.txt",
                @"D:\temp\FileCopier\dest1\folder\ignore1\file2.txt",
                @"D:\temp\FileCopier\dest1\folder\ignore1\ignore.txt",
                @"D:\temp\FileCopier\dest1\folder\file.txt",
                @"D:\temp\FileCopier\dest1\folder\ignore.txt",
                @"D:\temp\FileCopier\dest1\file.txt",
                @"D:\temp\FileCopier\dest1\file2.txt",
                @"D:\temp\FileCopier\dest2\folder\ignore1\file.txt",
                @"D:\temp\FileCopier\dest2\folder\ignore1\file2.txt",
                @"D:\temp\FileCopier\dest2\folder\ignore1\ignore.txt",
                @"D:\temp\FileCopier\dest2\folder\ignore.txt",
                @"D:\temp\FileCopier\dest2\file.txt",
                @"D:\temp\FileCopier\dest2\file2.txt",
                @"D:\temp\FileCopier\dest2\filedest.txt",
                @"D:\temp\FileCopier\backup\dest1\folder\file.txt",
                @"D:\temp\FileCopier\backup\dest1\file.txt",
                @"D:\temp\FileCopier\backup\dest2\file2.txt",
                @"D:\temp\FileCopier\backup\dest2\filedest.txt"
            };

            foreach (string file in expectedExistingFiles)
            {
                Assert.IsTrue(fileSystem.File.Exists(file));
            }

            List<string> expectedNotExistingDirectories = new List<string>
            {
                @"D:\temp\FileCopier\dest1\ignore1\",
                @"D:\temp\FileCopier\dest1\ignore2\",
                @"D:\temp\FileCopier\dest2\ignore1\",
                @"D:\temp\FileCopier\dest2\ignore2\"
            };

            foreach (string dir in expectedNotExistingDirectories)
            {
                Assert.IsFalse(fileSystem.Directory.Exists(dir));
            }

            List<string> expectedNotExistingFiles = new List<string>
            {
                @"D:\temp\FileCopier\dest1\folder\file2.txt",
                @"D:\temp\FileCopier\dest1\ignore1\file.txt",
                @"D:\temp\FileCopier\dest1\ignore1\file2.txt",
                @"D:\temp\FileCopier\dest1\ignore1\ignore.txt",
                @"D:\temp\FileCopier\dest1\ignore2\file.txt",
                @"D:\temp\FileCopier\dest1\ignore2\file2.txt",
                @"D:\temp\FileCopier\dest1\ignore2\ignore.txt",
                @"D:\temp\FileCopier\dest1\ignore.txt",
                @"D:\temp\FileCopier\dest2\folder\file.txt",
                @"D:\temp\FileCopier\dest2\folder\file2.txt",
                @"D:\temp\FileCopier\dest2\ignore1\file.txt",
                @"D:\temp\FileCopier\dest2\ignore1\file2.txt",
                @"D:\temp\FileCopier\dest2\ignore1\ignore.txt",
                @"D:\temp\FileCopier\dest2\ignore2\file.txt",
                @"D:\temp\FileCopier\dest2\ignore2\file2.txt",
                @"D:\temp\FileCopier\dest2\ignore2\ignore.txt",
                @"D:\temp\FileCopier\dest2\ignore.txt"
            };

            foreach (string file in expectedNotExistingFiles)
            {
                Assert.IsFalse(fileSystem.File.Exists(file));
            }

            Assert.AreEqual(DEST_CONTENT, fileSystem.File.ReadAllText(@"D:\temp\FileCopier\dest1\folder\file.txt"));
            Assert.AreEqual(SRC_CONTENT, fileSystem.File.ReadAllText(@"D:\temp\FileCopier\dest1\file.txt"));
            Assert.AreEqual(SRC_CONTENT, fileSystem.File.ReadAllText(@"D:\temp\FileCopier\dest2\file2.txt"));
            Assert.AreEqual(DEST_CONTENT, fileSystem.File.ReadAllText(@"D:\temp\FileCopier\dest2\filedest.txt"));
        }

        //[TestMethod]
        //public void TestDirectoryCopy()
        //{
        //    CallDirectoryCopy(@"D:\temp\src", @"D:\temp\dest1", @"ignore1|ignore2|ignore.txt|.*ignore\..*");
        //}

        //private static void CallDirectoryCopy(string sourcePath, string destPath, string ignorePatterns)
        //{
        //    MethodInfo directoryCopyMethod = typeof(CopyExecutor).GetMethod(
        //        "DirectoryCopy",
        //        BindingFlags.Static | BindingFlags.NonPublic,
        //        Type.DefaultBinder,
        //        new[] { typeof(string), typeof(string), typeof(string) },
        //        null);
        //    directoryCopyMethod.Invoke(null, new object[] { sourcePath, destPath, ignorePatterns });
        //}

        private static bool InvokeValidateConfiguration(MockFileSystem fileSystem, CopyConfiguration configuration)
        {
            CopyExecutor executor = new CopyExecutor(fileSystem, null);
            PrivateObject obj = new PrivateObject(executor);

            string message = null;
            object[] args = new object[] { configuration, message };
            return (bool)obj.Invoke("ValidateConfiguration", args);
        }

        private static CopyConfiguration CreateCopyConfiguration()
        {
            return new CopyConfiguration
            {
                Name = "Test",
                SourceDir = @"D:\temp\FileCopier\src",
                DestDirs = new List<string> { @"D:\temp\FileCopier\dest1", @"D:\temp\FileCopier\dest2" },
                IgnorePattern = @"ignore1|ignore2|ignore.txt|folder\\f[A-Za-z0-9].+\.txt",
                BackupDir = @"D:\temp\FileCopier\backup",
                AlwaysBackup = true
            };
        }

        private static MockFileSystem CreateMockFileSystem()
        {
            return new MockFileSystem(
                new Dictionary<string, MockFileData>
                {
                    { @"D:\temp\FileCopier\", new MockDirectoryData() },
                    { @"D:\temp\FileCopier\src\", new MockDirectoryData() },
                    { @"D:\temp\FileCopier\src\folder\", new MockDirectoryData() },
                    { @"D:\temp\FileCopier\src\folder\ignore1\", new MockDirectoryData() },
                    { @"D:\temp\FileCopier\src\folder\ignore1\file.txt", new MockFileData(SRC_CONTENT) },
                    { @"D:\temp\FileCopier\src\folder\ignore1\file2.txt", new MockFileData(SRC_CONTENT) },
                    { @"D:\temp\FileCopier\src\folder\ignore1\ignore.txt", new MockFileData(SRC_CONTENT) },
                    { @"D:\temp\FileCopier\src\folder\file.txt", new MockFileData(SRC_CONTENT) },
                    { @"D:\temp\FileCopier\src\folder\file2.txt", new MockFileData(SRC_CONTENT) },
                    { @"D:\temp\FileCopier\src\folder\ignore.txt", new MockFileData(SRC_CONTENT) },
                    { @"D:\temp\FileCopier\src\ignore1\", new MockDirectoryData() },
                    { @"D:\temp\FileCopier\src\ignore1\file.txt", new MockFileData(SRC_CONTENT) },
                    { @"D:\temp\FileCopier\src\ignore1\file2.txt", new MockFileData(SRC_CONTENT) },
                    { @"D:\temp\FileCopier\src\ignore1\ignore.txt", new MockFileData(SRC_CONTENT) },
                    { @"D:\temp\FileCopier\src\ignore2\", new MockDirectoryData() },
                    { @"D:\temp\FileCopier\src\ignore2\file.txt", new MockFileData(SRC_CONTENT) },
                    { @"D:\temp\FileCopier\src\ignore2\file2.txt", new MockFileData(SRC_CONTENT) },
                    { @"D:\temp\FileCopier\src\ignore2\ignore.txt", new MockFileData(SRC_CONTENT) },
                    { @"D:\temp\FileCopier\src\file.txt", new MockFileData(SRC_CONTENT) },
                    { @"D:\temp\FileCopier\src\file2.txt", new MockFileData(SRC_CONTENT) },
                    { @"D:\temp\FileCopier\src\ignore.txt", new MockFileData(SRC_CONTENT) },
                    { @"D:\temp\FileCopier\dest1\", new MockDirectoryData() },
                    { @"D:\temp\FileCopier\dest1\folder\", new MockDirectoryData() },
                    { @"D:\temp\FileCopier\dest1\folder\file.txt", new MockFileData(DEST_CONTENT) },
                    { @"D:\temp\FileCopier\dest1\file.txt", new MockFileData(DEST_CONTENT) },
                    { @"D:\temp\FileCopier\dest2\folder2\", new MockDirectoryData() },
                    { @"D:\temp\FileCopier\dest2\file2.txt", new MockFileData(DEST_CONTENT) },
                    { @"D:\temp\FileCopier\dest2\filedest.txt", new MockFileData(DEST_CONTENT) },
                    { @"D:\temp\FileCopier\backup\", new MockDirectoryData() }
                });
        }

        private static FileSystem CreateRealFileSystem()
        {
            FileSystem fileSystem = new FileSystem();
            if (fileSystem.Directory.Exists(@"D:\temp\FileCopier\"))
            {
                fileSystem.Directory.Delete(@"D:\temp\FileCopier\", true);
            }

            fileSystem.Directory.CreateDirectory(@"D:\temp\FileCopier\");
            fileSystem.Directory.CreateDirectory(@"D:\temp\FileCopier\src\");
            fileSystem.Directory.CreateDirectory(@"D:\temp\FileCopier\src\folder\");
            fileSystem.Directory.CreateDirectory(@"D:\temp\FileCopier\src\folder\ignore1\");
            fileSystem.Directory.CreateDirectory(@"D:\temp\FileCopier\src\ignore1\");
            fileSystem.Directory.CreateDirectory(@"D:\temp\FileCopier\src\ignore2\");
            fileSystem.Directory.CreateDirectory(@"D:\temp\FileCopier\dest1\");
            fileSystem.Directory.CreateDirectory(@"D:\temp\FileCopier\dest1\folder\");
            fileSystem.Directory.CreateDirectory(@"D:\temp\FileCopier\dest2\folder2\");
            fileSystem.Directory.CreateDirectory(@"D:\temp\FileCopier\backup\");

            fileSystem.File.WriteAllText(@"D:\temp\FileCopier\src\folder\ignore1\file.txt", SRC_CONTENT);
            fileSystem.File.WriteAllText(@"D:\temp\FileCopier\src\folder\ignore1\file2.txt", SRC_CONTENT);
            fileSystem.File.WriteAllText(@"D:\temp\FileCopier\src\folder\ignore1\ignore.txt", SRC_CONTENT);
            fileSystem.File.WriteAllText(@"D:\temp\FileCopier\src\folder\file.txt", SRC_CONTENT);
            fileSystem.File.WriteAllText(@"D:\temp\FileCopier\src\folder\file2.txt", SRC_CONTENT);
            fileSystem.File.WriteAllText(@"D:\temp\FileCopier\src\folder\ignore.txt", SRC_CONTENT);
            fileSystem.File.WriteAllText(@"D:\temp\FileCopier\src\ignore1\file.txt", SRC_CONTENT);
            fileSystem.File.WriteAllText(@"D:\temp\FileCopier\src\ignore1\file2.txt", SRC_CONTENT);
            fileSystem.File.WriteAllText(@"D:\temp\FileCopier\src\ignore1\ignore.txt", SRC_CONTENT);
            fileSystem.File.WriteAllText(@"D:\temp\FileCopier\src\ignore2\file.txt", SRC_CONTENT);
            fileSystem.File.WriteAllText(@"D:\temp\FileCopier\src\ignore2\file2.txt", SRC_CONTENT);
            fileSystem.File.WriteAllText(@"D:\temp\FileCopier\src\ignore2\ignore.txt", SRC_CONTENT);
            fileSystem.File.WriteAllText(@"D:\temp\FileCopier\src\file.txt", SRC_CONTENT);
            fileSystem.File.WriteAllText(@"D:\temp\FileCopier\src\file2.txt", SRC_CONTENT);
            fileSystem.File.WriteAllText(@"D:\temp\FileCopier\src\ignore.txt", SRC_CONTENT);
            fileSystem.File.WriteAllText(@"D:\temp\FileCopier\dest1\folder\file.txt", DEST_CONTENT);
            fileSystem.File.WriteAllText(@"D:\temp\FileCopier\dest1\file.txt", DEST_CONTENT);
            fileSystem.File.WriteAllText(@"D:\temp\FileCopier\dest2\file2.txt", DEST_CONTENT);
            fileSystem.File.WriteAllText(@"D:\temp\FileCopier\dest2\filedest.txt", DEST_CONTENT);

            return fileSystem;
        }
    }
}
