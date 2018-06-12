using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.IO.Pipes;
using SamsTarCS;

namespace BenchmarkingApp
{
    internal class Program
    {
        private static AsyncFileWriteQueue _writeQueue;

        public static void Main(string[] args)
        {
            //10MB buffer
            _writeQueue = new AsyncFileWriteQueue(1024 * 1024);
            _writeQueue.Start();

            var testRuns = 2;
            
            var skip = false;

            var tmpDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
            
            
            Stopwatch sw = new Stopwatch();
            
            foreach (var testFile in _testDataDir.GetFiles("*.tgz"))
            {
                sw.Reset();
                sw.Start();
                
                for (int i = 0; i < testRuns; i++)
                { 

                    var testOutDir = tmpDir.CreateSubdirectory(testFile.Name.Replace(".", "_")+"__"+i);


                    Console.WriteLine($"File : {testFile.Name} / Pass : {i}");

                    using (var tarStream = new GZipStream(File.OpenRead(testFile.FullName), CompressionMode.Decompress))
                    {
                        
                        UnTar.Extract(tarStream, testOutDir, _writeQueue);
                        
                    }

                    Console.WriteLine($"\tTo unpack '{testFile.Name}' took '{sw.Elapsed}' ");

                }
                
                Console.WriteLine($"To unpack '{testFile.Name}' {testRuns} times took '{sw.Elapsed}' - avg - '{sw.Elapsed.TotalMinutes / testRuns} mins per go.'");
            }

            Console.WriteLine("Waiting for file writes to finish ...");
            sw.Reset();
            sw.Start();
            _writeQueue.WaitComplete().Wait();

            Console.WriteLine($"Took '{sw.ElapsedMilliseconds}'ms o finish.");


            Console.WriteLine("Files here :\r\n " + tmpDir.FullName);
            Console.ReadKey(true);

            Console.WriteLine("Press to delete folder...");

            Directory.Delete(tmpDir.FullName, true);

            _writeQueue.Dispose();
            Console.ReadKey(true);
        }
                
        static DirectoryInfo _testDataDir = new DirectoryInfo("../../../../TestData");
     
        
        

       
    }
}