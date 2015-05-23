using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;

namespace FileCopier.Core.Test
{
    [TestClass]
    public class ExecutorTests
    {
        //[TestMethod]
        //public void TestMethod1()
        //{
        //    Executor target = new Executor();
        //    PrivateObject obj = new PrivateObject(target);
        //    var retVal = obj.Invoke("DirectoryCopy", "");
        //    Assert.AreEqual(retVal);
        //}

        [TestMethod]
        public void TestDirectoryCopy()
        {
            CallDirectoryCopy(@"D:\temp\src", @"D:\temp\dest1", @"^ignore1$|^ignore2$|^ignore.txt$|^.*ignore\..*$");
        }

        private static void CallDirectoryCopy(string sourcePath, string destPath, string ignorePatterns)
        {
            MethodInfo directoryCopyMethod = typeof(Executor).GetMethod(
                "DirectoryCopy",
                BindingFlags.Static | BindingFlags.NonPublic,
                Type.DefaultBinder,
                new[] { typeof(string), typeof(string), typeof(string) },
                null);
            directoryCopyMethod.Invoke(null, new object[] { sourcePath, destPath, ignorePatterns });
        }
    }
}
