using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using SamsTarCS;
using Xunit;

namespace Tests
{
    public class UnTarTests
    {
        
        DirectoryInfo _testDataDir = new DirectoryInfo("../../../../../TestData");
        
        



        [Fact]
        public void SimpleUnpack()
        {

            var testRuns = 2;
             

            var tmpDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
            
            Stopwatch sw = new Stopwatch();
            
            foreach (var testFile in _testDataDir.GetFiles("*.tgz"))
            {
                sw.Reset();
                sw.Start();
                
                for (int i = 0; i < testRuns; i++)
                {
                    
                    var testOutDir = tmpDir.CreateSubdirectory(testFile.Name.Replace(".", "_")+"__"+i);
                
                    using (var tarStream = new GZipStream(File.OpenRead(testFile.FullName), CompressionMode.Decompress))
                    {
                        UnTar.Extract(tarStream, testOutDir);
                    }
                }
                
                Debug.WriteLine($"To unpack '{testFile.Name}' {testRuns} times took '{sw.Elapsed}' - avg - '{sw.Elapsed / testRuns} per go.'");
            }

            Assert.True(tmpDir.Exists);
            Assert.True(tmpDir.EnumerateFileSystemInfos().Any());

            Directory.Delete(tmpDir.FullName, true);
        }

        [Fact]
        public void UnpackWithSharedBuffer1MB()
        {

            var testRuns = 2;

            // 1 MB buffer
            using (var writeQueue = new AsyncFileWriteQueue(1 * 1024 * 1024))
            {

                
                    writeQueue.Start();

                var tmpDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));


                Stopwatch sw = new Stopwatch();

                foreach (var testFile in _testDataDir.GetFiles("*.tgz"))
                {
                    sw.Reset();
                    sw.Start();

                    for (int i = 0; i < testRuns; i++)
                    {

                        var testOutDir = tmpDir.CreateSubdirectory(testFile.Name.Replace(".", "_") + "__" + i);

                        using (var tarStream = new GZipStream(File.OpenRead(testFile.FullName), CompressionMode.Decompress))
                        {
                            UnTar.Extract(tarStream, testOutDir, writeQueue);
                        }
                    }

                    Debug.WriteLine($"To unpack '{testFile.Name}' {testRuns} times took '{sw.Elapsed}' - avg - '{sw.Elapsed / testRuns} per go.'");
                }

                writeQueue.WaitComplete().Wait();

                writeQueue.Stop().Wait();

                Assert.True(tmpDir.Exists);
                Assert.True(tmpDir.EnumerateFileSystemInfos().Any());
                Directory.Delete(tmpDir.FullName, true);
            }
        }

        [Fact]
        public void UnpackWithSharedBuffer10MB()
        {

            var testRuns = 2;

            // 10 MB buffer
            var writeQueue = new AsyncFileWriteQueue(10 * 1024 * 1024);
            writeQueue.Start();

            var tmpDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));


            Stopwatch sw = new Stopwatch();

            foreach (var testFile in _testDataDir.GetFiles("*.tgz"))
            {
                sw.Reset();
                sw.Start();

                for (int i = 0; i < testRuns; i++)
                {

                    var testOutDir = tmpDir.CreateSubdirectory(testFile.Name.Replace(".", "_") + "__" + i);

                    using (var tarStream = new GZipStream(File.OpenRead(testFile.FullName), CompressionMode.Decompress))
                    {
                        UnTar.Extract(tarStream, testOutDir, writeQueue);
                    }
                }

                Debug.WriteLine($"To unpack '{testFile.Name}' {testRuns} times took '{sw.Elapsed}' - avg - '{sw.Elapsed / testRuns} per go.'");
            }

            writeQueue.WaitComplete().Wait();
            Assert.True(tmpDir.Exists);
            Assert.True(tmpDir.EnumerateFileSystemInfos().Any());
            Directory.Delete(tmpDir.FullName, true);
        }

        [Fact]
        public void UnpackWithSharedBuffer100MB()
        {

            var testRuns = 2;

            // 100 MB buffer
            var writeQueue = new AsyncFileWriteQueue(100 * 1024 * 1024);
            writeQueue.Start();

            var tmpDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));


          

            foreach (var testFile in _testDataDir.GetFiles("*.tgz"))
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();

                for (int i = 0; i < testRuns; i++)
                {

                    var testOutDir = tmpDir.CreateSubdirectory(testFile.Name.Replace(".", "_") + "__" + i);

                    using (var tarStream = new GZipStream(File.OpenRead(testFile.FullName), CompressionMode.Decompress))
                    {
                        UnTar.Extract(tarStream, testOutDir, writeQueue);
                    }
                }

                Debug.WriteLine($"To unpack '{testFile.Name}' {testRuns} times took '{sw.Elapsed}' - avg - '{sw.Elapsed / testRuns} per go.'");
            }

            writeQueue.WaitComplete().Wait();
            Assert.True(tmpDir.Exists);
            Assert.True(tmpDir.EnumerateFileSystemInfos().Any());
            Directory.Delete(tmpDir.FullName, true);
        }


        [Fact]
        public void UnpackWithSharedBuffer100MB_2()
        {
            var testRuns = 2;

            // 100 MB buffer
            var writeQueue = new AsyncFileWriteQueue(100 * 1024 * 1024);
            writeQueue.Start();

            var tmpDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
            

            foreach (var testFile in _testDataDir.GetFiles("*.tgz"))
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();

                for (int i = 0; i < testRuns; i++)
                {

                    var testOutDir = tmpDir.CreateSubdirectory(testFile.Name.Replace(".", "_") + "__" + i);

                    using (var tarStream = new GZipStream(File.OpenRead(testFile.FullName), CompressionMode.Decompress))
                    {
                        UnTar.Extract(tarStream, testOutDir, writeQueue, 1024 * 1024);
                    }
                }

                Debug.WriteLine($"To unpack '{testFile.Name}' {testRuns} times took '{sw.Elapsed}' - avg - '{sw.Elapsed / testRuns} per go.'");
            }

            writeQueue.WaitComplete().Wait();
            Assert.True(tmpDir.Exists);
            Assert.True(tmpDir.EnumerateFileSystemInfos().Any());
            Directory.Delete(tmpDir.FullName, true);
        }

        [Fact]
        public void UnpackWithSharedBuffer100MB_3()
        {
            var testRuns = 2;

            // 100 MB buffer
            var writeQueue = new AsyncFileWriteQueue(100 * 1024 * 1024);
            writeQueue.Start();

            var tmpDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));


            foreach (var testFile in _testDataDir.GetFiles("*.tgz"))
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();

                for (int i = 0; i < testRuns; i++)
                {

                    var testOutDir = tmpDir.CreateSubdirectory(testFile.Name.Replace(".", "_") + "__" + i);

                    using (var tarStream = new GZipStream(File.OpenRead(testFile.FullName), CompressionMode.Decompress))
                    {
                        UnTar.Extract(tarStream, testOutDir, writeQueue, 512 * 1024);
                    }
                }

                Debug.WriteLine($"To unpack '{testFile.Name}' {testRuns} times took '{sw.Elapsed}' - avg - '{sw.Elapsed / testRuns} per go.'");
            }

            writeQueue.WaitComplete().Wait();
            Assert.True(tmpDir.Exists);
            Assert.True(tmpDir.EnumerateFileSystemInfos().Any());
            Directory.Delete(tmpDir.FullName, true);
        }
    }
}