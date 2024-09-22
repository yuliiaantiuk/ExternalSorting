using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace External_Sort
{
    class ExternalSort
    {
        // Merge files using K-way merge
        public static void MergeFiles(string outputFile, int k, int bufferSize = 1024 * 1024)
        {
            var minHeap = new SortedSet<(int Element, int Index)>(); // Heap to store the smallest elements

            using (var outStream = new BufferedStream(new FileStream(outputFile, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize)))
            using (var writer = new StreamWriter(outStream))
            {
                var readers = new StreamReader[k];

                // Open input files in read mode with large buffering
                for (int i = 0; i < k; i++)
                {
                    var fileStream = new FileStream(i.ToString(), FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize);
                    var bufferedStream = new BufferedStream(fileStream, bufferSize);
                    readers[i] = new StreamReader(bufferedStream);

                    string line = readers[i].ReadLine();
                    if (line != null)
                    {
                        minHeap.Add((int.Parse(line), i));
                    }
                }

                while (minHeap.Count > 0)
                {
                    var root = minHeap.Min;
                    minHeap.Remove(root);

                    writer.WriteLine(root.Element);

                    string line = readers[root.Index].ReadLine();
                    if (line != null)
                    {
                        minHeap.Add((int.Parse(line), root.Index));
                    }
                }

                // Close input files
                for (int i = 0; i < k; i++)
                {
                    readers[i].Close();
                }

                // Delete temporary files after merging
                Parallel.For(0, k, (i) =>
                {
                    File.Delete(i.ToString());
                });
            }
        }

        // Create initial runs and divide them among output files
        public static void CreateInitialRuns(string inputFile, long runSizeBytes, int numWays, int bufferSize = 1024 * 1024)
        {
            using (var inStream = new BufferedStream(new FileStream(inputFile, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize)))
            using (var reader = new StreamReader(inStream))
            {
                var writers = new StreamWriter[numWays];
                for (int i = 0; i < numWays; i++)
                {
                    var fileStream = new FileStream(i.ToString(), FileMode.Create, FileAccess.Write, FileShare.None, bufferSize);
                    var bufferedStream = new BufferedStream(fileStream, bufferSize);
                    writers[i] = new StreamWriter(bufferedStream);
                }

                bool moreInput = true;
                int nextOutputFile = 0;

                while (moreInput)
                {
                    var data = new List<int>();
                    long currentRunSize = 0;

                    // Read lines until the run size in bytes is reached
                    while (currentRunSize < runSizeBytes)
                    {
                        string line = reader.ReadLine();
                        if (line != null)
                        {
                            data.Add(int.Parse(line));
                            currentRunSize += System.Text.Encoding.UTF8.GetByteCount(line);
                        }
                        else
                        {
                            moreInput = false;
                            break;
                        }
                    }

                    data.Sort();

                    // Write sorted data to appropriate output file
                    using (var writer = writers[nextOutputFile])
                    {
                        foreach (var item in data)
                        {
                            writer.WriteLine(item);
                        }
                    }

                    nextOutputFile = (nextOutputFile + 1) % numWays;
                }

                // Close output files
                for (int i = 0; i < numWays; i++)
                {
                    writers[i].Close();
                }
            }
        }

        public static void ExternalSortFile(string inputFile, string outputFile, int numWays, long runSize)
        {
            // Create initial runs from the input file and distribute them to temporary files
            CreateInitialRuns(inputFile, runSize, numWays);

            // Merge the runs using the K-way merging technique with large buffer size
            MergeFiles(outputFile, numWays);
        }
    }
}
