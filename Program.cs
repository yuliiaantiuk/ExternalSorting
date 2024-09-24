using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace External_Sort
{
    internal class Program
    {
        // Invoke declarations for Job Object manipulation
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CreateJobObject(IntPtr lpJobAttributes, string lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetInformationJobObject(IntPtr hJob, JobObjectInfoType infoType, IntPtr lpJobObjectInfo, uint cbJobObjectInfoLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool AssignProcessToJobObject(IntPtr hJob, IntPtr hProcess);

        public enum JobObjectInfoType
        {
            JobObjectExtendedLimitInformation = 9
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct JOBOBJECT_BASIC_LIMIT_INFORMATION
        {
            public long PerProcessUserTimeLimit;
            public long PerJobUserTimeLimit;
            public uint LimitFlags;
            public IntPtr MinimumWorkingSetSize;
            public IntPtr MaximumWorkingSetSize;
            public uint ActiveProcessLimit;
            public long Affinity;
            public uint PriorityClass;
            public uint SchedulingClass;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
        {
            public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
            public IO_COUNTERS IoInfo;
            public IntPtr ProcessMemoryLimit;
            public IntPtr JobMemoryLimit;
            public IntPtr PeakProcessMemoryUsed;
            public IntPtr PeakJobMemoryUsed;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct IO_COUNTERS
        {
            public ulong ReadOperationCount;
            public ulong WriteOperationCount;
            public ulong OtherOperationCount;
            public ulong ReadTransferCount;
            public ulong WriteTransferCount;
            public ulong OtherTransferCount;
        }

        const uint JOB_OBJECT_LIMIT_PROCESS_MEMORY = 0x100;
        const uint JOB_OBJECT_LIMIT_JOB_MEMORY = 0x200;

        static void Main(string[] args)
        {
            // Limit memory to 512 MB 
            IntPtr hJob = CreateJobObject(IntPtr.Zero, null);
            if (hJob == IntPtr.Zero)
            {
                Console.WriteLine("Unable to create job object");
                return;
            }

            JOBOBJECT_EXTENDED_LIMIT_INFORMATION jobInfo = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION();
            jobInfo.BasicLimitInformation.LimitFlags = JOB_OBJECT_LIMIT_PROCESS_MEMORY | JOB_OBJECT_LIMIT_JOB_MEMORY;
            jobInfo.ProcessMemoryLimit = (IntPtr)(512 * 1024 * 1024);  
            jobInfo.JobMemoryLimit = (IntPtr)(512 * 1024 * 1024);    

            int length = Marshal.SizeOf(typeof(JOBOBJECT_EXTENDED_LIMIT_INFORMATION));
            IntPtr jobInfoPtr = Marshal.AllocHGlobal(length);
            Marshal.StructureToPtr(jobInfo, jobInfoPtr, false);

            if (!SetInformationJobObject(hJob, JobObjectInfoType.JobObjectExtendedLimitInformation, jobInfoPtr, (uint)length))
            {
                Console.WriteLine("Unable to set job object information");
                return;
            }

            // Assign the current process to the job object
            IntPtr hProcess = Process.GetCurrentProcess().Handle;
            if (!AssignProcessToJobObject(hJob, hProcess))
            {
                Console.WriteLine("Unable to assign process to job object");
                return;
            }

            int fileSizeMB = 1024; 
            long fileSizeBytes = fileSizeMB * 1024 * 1024;  
            long runSize = 100 * 1024 * 1024; 

            int numWays = (int)(fileSizeBytes / runSize);
            if (fileSizeBytes % runSize != 0)
            {
                numWays++;
            }

            string inputFile = Path.Combine(Environment.CurrentDirectory, "input.txt");
            string outputFile = Path.Combine(Environment.CurrentDirectory, "output.txt");

            try
            {
                using (var writer = new StreamWriter(inputFile))
                {
                    var rand = new Random();
                    long currentSize = 0;
                    const int maxNumber = 10000;
                    const int minNumber = 100;

                    while (currentSize < fileSizeBytes)
                    {
                        int number = rand.Next(minNumber, maxNumber);
                        string numberString = number.ToString();
                        long lineSize = System.Text.Encoding.UTF8.GetByteCount(numberString) + Environment.NewLine.Length;
                        if (currentSize + lineSize > fileSizeBytes)
                        {
                            break;
                        }

                        writer.WriteLine(numberString);
                        currentSize += lineSize;
                    }

                    writer.Flush();
                }
                Console.WriteLine("File generated successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while generating a file: {ex.Message}");
            }


            ExternalSort.ExternalSortFile(inputFile, outputFile, numWays, runSize);

            Console.WriteLine("Sorting completed. Press any key to exit.");
            Console.ReadKey();
        }
    }
}
