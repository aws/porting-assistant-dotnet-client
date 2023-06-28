using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;


namespace PortingAssistant.Client.Common.Utils
{
    public static class MemoryUtils
    {
        private static bool _disabledMetrics = false;

        private static string ConvertBytesToMegabytes(long bytes)
        {
            return ((bytes / 1024f) / 1024f).ToString("0.00");
        }
        //
        // Summary:
        //     This method takes a logger and logs the number that is the
        //     best available approximation of the number of bytes currently
        //     allocated in managed memory. It also logs the total amount
        //     of memory, in bytes, allocated for the associated process.
        public static void LogMemoryConsumption(ILogger logger)
        {
            if (_disabledMetrics) { return; }

            // Determine the best available approximation of the number
            // of bytes currently allocated in managed memory.
            logger.LogInformation(
                "GC total memory in MB: {0}",
                ConvertBytesToMegabytes(GC.GetTotalMemory(false)));

            Process currentProc = Process.GetCurrentProcess();
            currentProc.Refresh();
            // The most recently refreshed size of memory used by the
            // process, in bytes, that cannot be shared with other processes.
            logger.LogInformation(
                "Total private memory allocated for current process that cannot be shared with other processes in MB: {0}",
                ConvertBytesToMegabytes(currentProc.PrivateMemorySize64));
            // The working set of a process is the set of memory pages
            // currently visible to the process in physical RAM memory.
            // These pages are resident and available for an application
            // to use without triggering a page fault. The working set
            // includes both shared and private data. The shared data
            // includes the pages that contain all the instructions that
            // the process executes, including instructions from the
            // process modules and the system libraries.
            logger.LogInformation(
                "Total working set (physical) memory including both shared and private data used by current process in MB: {0}",
                ConvertBytesToMegabytes(currentProc.WorkingSet64));
            logger.LogInformation(
                "Peak working set (physical) memory including both shared and private data used by current process in MB: {0}",
                ConvertBytesToMegabytes(currentProc.PeakWorkingSet64));
            // the maximum size of virtual memory used by the process
            // since it started, in bytes. The operating system maps
            // the virtual address space for each process either to
            // pages loaded in physical memory, or to pages stored in
            // the virtual memory paging file on disk.
            logger.LogInformation(
                "Peak virtual memory used by current process in MB: {0}",
                ConvertBytesToMegabytes(currentProc.PeakVirtualMemorySize64));

        }

        //
        // Summary:
        //     This method takes a logger, path to a solution file, and logs
        //     the total size of the solution, in bytes, by suming up the
        //     size of each cs file contained in the solution. Then, it
        //     returns the size.
        public static long LogSolutionSize(ILogger logger, string SolutionPath)
        {
            DirectoryInfo solutionDir = Directory.GetParent(SolutionPath);
            var size = solutionDir.EnumerateFiles(
                "*.cs", SearchOption.AllDirectories).Sum(fi => fi.Length);

            if (!_disabledMetrics)
            {
                logger.LogInformation(
                    "Total size for {0} in bytes: {1}",
                    SolutionPath, size);
            }

            return size;
        }

        //
        // Summary:
        //     This method takes a logger and logs the system information
        //     including type of operating system if it's 32 bit or 64 bit;
        //     and the type of current running process if it's 32 bit or
        //     64 bit.
        public static void LogSystemInfo(ILogger logger)
        {
            if (_disabledMetrics) { return; }

            int systemType = Environment.Is64BitOperatingSystem ? 64 : 32;
            logger.LogInformation("Operating system is {0}bit.", systemType);

            int processType = Environment.Is64BitProcess? 64 : 32;
            logger.LogInformation("Current process is {0}bit.", processType);

        }

        public static void ToggleMetrics(bool disabledMetrics)
        {
            _disabledMetrics = disabledMetrics;
        }
    }
}
