using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using tar_cs;

namespace BenchmarkingApp
{
    internal class Program
    {
        private static AsyncFileWriter writer;

        public static void Main(string[] args)
        {
            writer = new AsyncFileWriter(2, 100000000);
            writer.Start();

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
                    createdDirs = new HashSet<string>();

                    dirsCreated = filesCreated = 0;

                    var testOutDir = tmpDir.CreateSubdirectory(testFile.Name.Replace(".", "_")+"__"+i);


                    Console.WriteLine($"File : {testFile.Name} / Pass : {i}");

                    using (var tarStream = new GZipStream(File.OpenRead(testFile.FullName), CompressionMode.Decompress))
                    {
                        
                        var tarReader = new TarReader(tarStream);
                        while (tarReader.MoveNext(skip)) // Moves pointer to the next file in the tar archive.
                        {
                            ExtractTarEntry(tarReader, testOutDir.FullName, null);

                            //hack: skip paxheaders
                            skip = tarReader.FileInfo.FileName.Contains("PaxHeader");
                        }
                        
                    }


                    Console.WriteLine($"\tTo unpack '{testFile.Name}' took '{sw.Elapsed}' - {filesCreated} files & {dirsCreated} dirs created.");
                }
                
                Console.WriteLine($"To unpack '{testFile.Name}' {testRuns} times took '{sw.Elapsed}' - avg - '{sw.Elapsed.TotalMinutes / testRuns} mins per go.'");
            }

            Console.WriteLine("Waiting for file writes to finish ...");
            sw.Reset();
            sw.Start();
            writer.WaitComplete().Wait();

            Console.WriteLine($"Took '{sw.ElapsedMilliseconds}'ms o finish.");


            Console.WriteLine("Files here :\r\n " + tmpDir.FullName);
            Console.ReadKey(true);

            Console.WriteLine("Press to delete folder...");

            Directory.Delete(tmpDir.FullName, true);


            Console.ReadKey(true);
        }
                
        static DirectoryInfo _testDataDir = new DirectoryInfo("../../../../TestData");
     
        
        
        static HashSet<string> createdDirs = new HashSet<string>();


        private static int dirsCreated = 0;
        private static int filesCreated = 0;





        private static void ExtractTarEntry(TarReader tarReader, string outputDirectory, string packName)
        {
            var relativePath = tarReader.FileInfo.FileName;

            if (relativePath.StartsWith("package/"))
            {
                relativePath = relativePath.Substring("package/".Length);
            }
            else if (relativePath.StartsWith(packName + "/"))
            {
                relativePath = relativePath.Substring((packName + "/").Length);
            }


            // Relative path can contain slash, not backslash.
            // Use Path.GetFullPath() method to convert path.
            var fullPath = Path.GetFullPath(Path.Combine(outputDirectory, relativePath));


            switch (tarReader.FileInfo.EntryType)
            {
                case EntryType.File:
                case EntryType.FileObsolete:

                    var file = new FileInfo(fullPath);

                    if (!createdDirs.Contains(file.DirectoryName))
                    {
                        dirsCreated++;
                        file.Directory.Create();
                        createdDirs.Add(file.DirectoryName);
                    }

                  // using (var outputStream = File.Create(fullPath))
                    using(var outputStream = new MemoryStream())
                    {
                        filesCreated++;
                        // Read data from a current file to a Stream.
                         
                        tarReader.Read(outputStream);

                        writer.Enqueue(fullPath, outputStream.ToArray()).Wait();
                    }
                    
                    break;
                case EntryType.Directory:
                    Directory.CreateDirectory(fullPath);
                    break;
                default:
                    break;
                //     throw new NotSupportedException("Not supported entry type: " + tarReader.FileInfo.EntryType);
            }

             
        }

       
    }



    class AsyncFileWriter
    {
        private int _workerThreads;

        private long _maxSize;

        private long _currentSize = 0;

        public AsyncFileWriter(int workerThreads, long maxSize)
        {
            _workerThreads = workerThreads;
            _maxSize = maxSize;
        }

        private Dictionary<string, byte[]> _work  =new Dictionary<string, byte[]>();

        private object _lock = new object();

        private Thread[] _workers;

        public long CurrentSize
        {
            get { return _currentSize; }
        }

        public bool HasWork
        {
            get { return _work.Count > 0; }
        }

        public async Task Enqueue(string fileName, byte[] data)
        {
             await WaitForRoom(data.Length, TimeSpan.FromSeconds(0.1));

            lock (_lock)
            {
                if (_work.ContainsKey(fileName))
                {
                    throw new DuplicateNameException();
                }

                _work[fileName] = data;
                _currentSize += data.Length;
            }
        }


        public void Start()
        {
            _workers = new Thread[_workerThreads];

            for (var i = 0; i < _workerThreads; i++)
            {
                _workers[i] = new Thread(new ThreadStart(DoWork));
                _workers[i].Start();

            }
        }

        public void Stop()
        {
            for (var i = 0; i < _workerThreads; i++)
            {
                _workers[i].Abort();
            }
        }


        private void DoWork()
        {
            while (true)
            {
                KeyValuePair<string, byte[]> val;

                lock (_lock)
                {
                    val = _work.FirstOrDefault();
                    if (val.Key != null)
                    {
                        _work.Remove(val.Key);
                    }
                }

                if (val.Key == null)
                    {
                        Thread.Sleep(100);
                        continue;
                    }


                

                var len = val.Value.Length;
                File.WriteAllBytes(val.Key, val.Value);
                 


                lock (_lock)
                {
                    this._currentSize -= len;
                }

                //Thread.Sleep(100);
            }
           
        }


        public async Task WaitForRoom(long allocate, TimeSpan timeout )
        {
            long waited = 0;

            if (allocate > this._maxSize)
            {
                throw new InvalidOperationException();
            }

            while ((_maxSize - CurrentSize) < allocate)
            {
                if (waited > timeout.TotalMilliseconds)
                {
                    throw new TimeoutException();
                }
                await Task.Delay(TimeSpan.FromMilliseconds(10));
                waited += 10;
            }
        }

        public async Task WaitComplete()
        {
            while (HasWork)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100));
            }
        }

    }

}