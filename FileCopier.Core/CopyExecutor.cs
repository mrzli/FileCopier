using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace FileCopier.Core
{
    public sealed class CopyExecutor
    {
        private const int TICK_INTERVAL = 1000;

        private IFileSystem fileSystem;
        private Func<string, object[], string> backupFolderNamingFunc;

        public CopyExecutor(
            IFileSystem fileSystem,
            Func<string, object[], string> backupFolderNamingFunc)
        {
            this.fileSystem = fileSystem;
            this.backupFolderNamingFunc = backupFolderNamingFunc ?? DefaultBackupFolderNamingFunc;
        }

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
                bool isBackupOk = StartTickEndExecute(backupStartedFunc, backupTickFunc, backupEndedFunc, () => CreateBackup(configuration));
                if (!isBackupOk)
                {
                    message = "Error while doing backup.";
                    return false;
                }
            }

            bool isCopyOk = StartTickEndExecute(copyStartedFunc, copyTickFunc, copyEndedFunc, () => DirectoryCopy(configuration));
            if (!isCopyOk)
            {
                message = "Error while copying files.";
                return false;
            }

            return true;
        }

        private bool ValidateConfiguration(CopyConfiguration configuration, out string message)
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

        private bool IsDirValid(string path)
        {
            return !string.IsNullOrWhiteSpace(path) && fileSystem.Directory.Exists(path);
        }

        private static bool StartTickEndExecute(Action startedFunc, Action tickFunc, Action endedFunc, Action doWork)
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

            if (backupTask.IsCanceled || backupTask.IsFaulted)
            {
                return false;
            }

            if (endedFunc != null)
            {
                endedFunc();
            }

            return true;
        }

        private void CreateBackup(CopyConfiguration configuration)
        {
            DateTime currentTime = DateTime.UtcNow;
            foreach (string destDir in configuration.DestDirs)
            {
                CreateBackup(destDir, configuration.BackupDir, currentTime);
            }
        }

        private void CreateBackup(string sourcePath, string backupParentPath, DateTime currentTime)
        {
            string sourceDirName = fileSystem.DirectoryInfo.FromDirectoryName(sourcePath).Name;
            string backupPath = fileSystem.Path.Combine(backupParentPath, backupFolderNamingFunc(sourceDirName, new object[] { currentTime }));
            fileSystem.Directory.CreateDirectory(backupPath);
            DoCopy(sourcePath, backupPath, null);
        }

        private void DirectoryCopy(CopyConfiguration configuration)
        {
            foreach (string destDir in configuration.DestDirs)
            {
                DoCopy(configuration.SourceDir, destDir, configuration.IgnorePattern);
            }
        }

        private void DoCopy(string sourcePath, string destPath, string ignorePattern)
        {
            DirectoryInfoBase src = fileSystem.DirectoryInfo.FromDirectoryName(sourcePath);
            FileSystemInfoBase[] fsInfos = GetFileSystemInfos(src, ignorePattern, true);

            string srcParentFullName = src.FullName;
            Parallel.ForEach(fsInfos, fsi => CopyFileSystemInfo(fsi, fileSystem, srcParentFullName, destPath));
        }

        private static void CopyFileSystemInfo(FileSystemInfoBase fsi, IFileSystem fileSystem, string srcParentFullName, string destPath)
        {
            if (fsi is DirectoryInfoBase)
            {
                DirectoryInfoBase dir = (DirectoryInfoBase)fsi;
                string destDir = fileSystem.Path.Combine(destPath, dir.GetRelativePath(srcParentFullName));
                fileSystem.Directory.CreateDirectory(destDir);
            }
            else if (fsi is FileInfoBase)
            {
                FileInfoBase file = (FileInfoBase)fsi;
                string destFile = fileSystem.Path.Combine(destPath, file.GetRelativePath(srcParentFullName));
                string destFileParentDir = fileSystem.Path.GetDirectoryName(destFile);
                fileSystem.Directory.CreateDirectory(destFileParentDir); // create dir just in case it isn't already created
                file.CopyTo(destFile, true);
            }
        }

        private FileSystemInfoBase[] GetFileSystemInfos(DirectoryInfoBase parentDir, string searchPattern, bool invertSearchPattern)
        {
            FileSystemInfoBase[] allFsInfos = parentDir.GetFileSystemInfos("*", System.IO.SearchOption.AllDirectories);

            if (searchPattern == null)
            {
                searchPattern = "";
            }

            searchPattern = "^(" + searchPattern + ")$";

            string parentFullName = parentDir.FullName;
            FileSystemInfoBase[] searchedFsInfos;
            searchedFsInfos = allFsInfos
                .Where(x => Regex.IsMatch(x.GetRelativePath(parentFullName), searchPattern))
                .ToArray();

            FileSystemInfoBase[] resultFsInfos;
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

        private FileSystemInfoBase[] RemoveOrphanedFileSystemInfos(DirectoryInfoBase parentDir, FileSystemInfoBase[] infos)
        {
            List<FileSystemInfoBaseWithOrphaned> infosWithOrphaned = infos
                .Select(x => new FileSystemInfoBaseWithOrphaned { Info = x, IsOrphaned = null })
                .ToList();

            foreach (FileSystemInfoBaseWithOrphaned infoWithOrphaned in infosWithOrphaned)
            {
                SetOrphaned(infoWithOrphaned, parentDir, infosWithOrphaned);
            }

            return infosWithOrphaned
                .Where(x => !x.IsOrphaned.Value)
                .Select(x => x.Info)
                .ToArray();
        }

        private bool SetOrphaned(
            FileSystemInfoBaseWithOrphaned infoWithOrphaned,
            DirectoryInfoBase parentDir,
            List<FileSystemInfoBaseWithOrphaned> infosWithOrphaned)
        {
            if (!infoWithOrphaned.IsOrphaned.HasValue)
            {
                DirectoryInfoBase parent = infoWithOrphaned.Info.GetParentDirectory();
                if (Equals(parent, parentDir))
                {
                    infoWithOrphaned.IsOrphaned = false;
                }
                else if (infosWithOrphaned.Any(x => Equals(x.Info, parent)))
                {
                    FileSystemInfoBaseWithOrphaned parentFsInfoWithOrphaned = infosWithOrphaned.First(x => Equals(x.Info, parent));
                    infoWithOrphaned.IsOrphaned = SetOrphaned(parentFsInfoWithOrphaned, parentDir, infosWithOrphaned);
                }
                else
                {
                    infoWithOrphaned.IsOrphaned = true;
                }
            }

            return infoWithOrphaned.IsOrphaned.Value;
        }

        private bool Equals(FileSystemInfoBase info1, FileSystemInfoBase info2)
        {
            string path1 = fileSystem.Path.Combine(info1.GetParentDirectory().FullName, info1.Name);
            string path2 = fileSystem.Path.Combine(info2.GetParentDirectory().FullName, info2.Name);
            return path1.Equals(path2, StringComparison.InvariantCultureIgnoreCase);
        }

        private static string DefaultBackupFolderNamingFunc(string originalName, object[] parameters)
        {
            DateTime time = (DateTime)parameters[0];
            return time.ToString("yyyy-MM-dd_HH-mm-ss") + "_" + originalName;
        }

        private class FileSystemInfoBaseWithOrphaned
        {
            public FileSystemInfoBase Info { get; set; }
            public bool? IsOrphaned { get; set; }
        }
    }
}
