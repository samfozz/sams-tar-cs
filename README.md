# Sam's Tar-CS

Forked from : code.google.com/p/tar-cs

A library for extracting tar archives in .net - implemented in C#. 

Most of the clever stuff comes from the original tar-cs library, This package just adds the following helper
method that can be used to extract to a directory :

``` 
    UnTar.Extract(Stream stream, DirectoryInfo outputDir)
```


## Small Files 
Sometimes when unpacking a packages with many small files, the file creation becomes the performance bottleneck.

To help mitigate this, the optional small file handling parameters are provided.

``` 
    UnTar.Extract(Stream stream, DirectoryInfo outputDir, AsyncFileWriteQueue smallFileQueue = null, int smallFileSize = -1)
```

AsyncFileWriteQueue simply buffers files to memory and writes them using a background task. 

* The 'smallFileQueue' parameter allows you to provide a shared write buffer for small files ( see below )
* The 'smallFileSize' parameter allows you to specify the max size of files to add to the queue. This value should be 
less than the size of the buffer ( probably considerably  smaller ...). When not provided, the full size of the
buffer is used - This is not optimal so should usually be explicitly provided.


A full use of AsyncFileWriteQueue is shown here :

```  
            var writeQueue = new AsyncFileWriteQueue(10 * 1024 * 1024);
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

            // Wait for queued files to write...
            writeQueue.WaitComplete().Wait();
```


## Notes

* PaxHeaders are ignored because they cause an error when unpacking.