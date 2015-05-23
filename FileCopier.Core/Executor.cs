using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace FileCopier.Core
{
    public sealed class Executor
    {
        private const int TICK_INTERVAL = 1000;

        public bool Execute(
            CopyConfiguration configuration,
            Func<bool> backupQueryFunc,
            Action backupStartedFunc,
            Action backupTickFunc,
            Action backupEndedFunc,
            Action copyStartedFunc,
            Action copyTickFunc,
            Action copyEndedFunc,
            out string message)
        {
            message = "";

            if (!ValidateConfiguration(configuration, out message))
            {
                return false;
            }

            if (configuration.AlwaysBackup || (backupQueryFunc != null && backupQueryFunc()))
            {
                StartTickEndExecute(backupStartedFunc, backupTickFunc, backupEndedFunc, () => CreateBackup(configuration));
            }

            StartTickEndExecute(copyStartedFunc, copyTickFunc, copyEndedFunc, () => DirectoryCopy(configuration));

            return true;
        }

        private static bool ValidateConfiguration(CopyConfiguration configuration, out string message)
        {
            message = "";

            if (configuration == null)
            {
                message = "Configuration is null.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(configuration.Name))
            {
                message = "Configuration has no name.";
                return false;
            }

            if (!IsDirValid(configuration.SourceDir))
            {
                message = "'SourceDir' is invalid or missing.";
                return false;
            }

            if (configuration.DestDirs == null || configuration.DestDirs.Count == 0)
            {
                message = "'DestDirs' has no entries.";
                return false;
            }

            if (configuration.DestDirs.Any(x => !IsDirValid(x)))
            {
                message = "'DestDirs' has an invalid entry.";
                return false;
            }

            if (!IsDirValid(configuration.BackupDir))
            {
                message = "'BackupDir' is invalid or missing.";
                return false;
            }

            return true;
        }

        private static bool IsDirValid(string path)
        {
            return !string.IsNullOrWhiteSpace(path) && Directory.Exists(path);
        }

        private static void StartTickEndExecute(Action startedFunc, Action tickFunc, Action endedFunc, Action doWork)
        {
            if (startedFunc != null)
            {
                startedFunc();
            }
            Task backupTask = Task.Run(doWork);
            while (!backupTask.IsCompleted && !backupTask.IsCanceled && !backupTask.IsFaulted)
            {
                Thread.Sleep(TICK_INTERVAL);
                if (tickFunc != null)
                {
                    tickFunc();
                }
            }
            if (endedFunc != null)
            {
                endedFunc();
            }
        }

        private static void CreateBackup(CopyConfiguration configuration)
        {
            DateTime currentTime = DateTime.UtcNow;
            foreach (string destDir in configuration.DestDirs)
            {
                CreateBackup(destDir, configuration.BackupDir, currentTime);
            }
        }

        private static void CreateBackup(string sourcePath, string backupParentPath, DateTime currentTime)
        {
            string sourceDirName = new DirectoryInfo(sourcePath).Name;
            string backupPath = Path.Combine(backupParentPath, currentTime.ToString("yyyy-MM-dd_HH-mm-ss") + "_" + sourceDirName);
            Directory.CreateDirectory(backupPath);
            DirectoryCopy(sourcePath, backupPath, null);
        }

        private static void DirectoryCopy(CopyConfiguration configuration)
        {
            foreach (string destDir in configuration.DestDirs)
            {
                DirectoryCopy(configuration.SourceDir, destDir, configuration.IgnorePattern);
            }
        }

        private static void DirectoryCopy(
            string sourcePath,
            string destPath,
            string ignorePattern)
        {
            DirectoryInfo src = new DirectoryInfo(sourcePath);
            FileSystemInfo[] fsInfos = GetFileSystemInfos(src, ignorePattern, true);

            string parentFullName = src.FullName;
            IEnumerable<DirectoryInfo> directories = fsInfos
                .Where(x => x is DirectoryInfo)
                .Select(x => (DirectoryInfo)x);

            foreach (DirectoryInfo dir in directories)
            {
                string destDir = Path.Combine(destPath, dir.GetRelativePath(parentFullName));
                Directory.CreateDirectory(destDir);
            }

            IEnumerable<FileInfo> files = fsInfos
                .Where(x => x is FileInfo)
                .Select(x => (FileInfo)x);

            foreach (FileInfo file in files)
            {
                string destFile = Path.Combine(destPath, file.GetRelativePath(parentFullName));
                file.CopyTo(destFile, true);
            }
        }

        private static FileSystemInfo[] GetFileSystemInfos(DirectoryInfo parentDir, string searchPattern, bool invertSearchPattern)
        {
            FileSystemInfo[] allFsInfos = parentDir.GetFileSystemInfos("*", SearchOption.AllDirectories);

            string parentFullName = parentDir.FullName;
            FileSystemInfo[] searchedFsInfos;
            if (!string.IsNullOrEmpty(searchPattern))
            {
                searchedFsInfos = allFsInfos
                    .Where(x => Regex.IsMatch(x.GetRelativePath(parentFullName), searchPattern))
                    .ToArray();
            }
            else
            {
                searchedFsInfos = new FileSystemInfo[0];
            }

            FileSystemInfo[] resultFsInfos;
            if (invertSearchPattern)
            {
                resultFsInfos = allFsInfos
                    .Where(x => !searchedFsInfos.Contains(x))
                    .ToArray();
            }
            else
            {
                resultFsInfos = searchedFsInfos;
            }

            resultFsInfos = RemoveOrphanedFileSystemInfos(parentDir, resultFsInfos);

            return resultFsInfos;
        }

        private static FileSystemInfo[] RemoveOrphanedFileSystemInfos(DirectoryInfo parentDir, FileSystemInfo[] infos)
        {
            List<FileSystemInfoWithOrphaned> infosWithOrphaned = infos
                .Select(x => new FileSystemInfoWithOrphaned { Info = x, IsOrphaned = null })
                .ToList();

            foreach (FileSystemInfoWithOrphaned infoWithOrphaned in infosWithOrphaned)
            {
                SetOrphaned(infoWithOrphaned, parentDir, infosWithOrphaned);
            }

            return infosWithOrphaned
                .Where(x => !x.IsOrphaned.Value)
                .Select(x => x.Info)
                .ToArray();
        }

        private static bool SetOrphaned(FileSystemInfoWithOrphaned infoWithOrphaned, DirectoryInfo parentDir, List<FileSystemInfoWithOrphaned> infosWithOrphaned)
        {
            if (!infoWithOrphaned.IsOrphaned.HasValue)
            {
                DirectoryInfo parent = infoWithOrphaned.Info.GetParentDirectory();
                if (parent.EqualsTo(parentDir))
                {
                    infoWithOrphaned.IsOrphaned = false;
                }
                else if (infosWithOrphaned.Any(x => x.Info.EqualsTo(parent)))
                {
                    FileSystemInfoWithOrphaned parentFsInfoWithOrphaned = infosWithOrphaned.First(x => x.Info.EqualsTo(parent));
                    infoWithOrphaned.IsOrphaned = SetOrphaned(parentFsInfoWithOrphaned, parentDir, infosWithOrphaned);
                }
                else
                {
                    infoWithOrphaned.IsOrphaned = true;
                }
            }

            return infoWithOrphaned.IsOrphaned.Value;
        }

        private class FileSystemInfoWithOrphaned
        {
            public FileSystemInfo Info { get; set; }
            public bool? IsOrphaned { get; set; }
        }
    }
}
