using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;


namespace PortingAssistant.Client.Common.Utils
{
    public static class MemoryUtils
    {
        //
        // Summary:
        //     This method takes a logger and logs the number that is the
        //     best available approximation of the number of bytes currently
        //     allocated in managed memory. It also logs the total amount
        //     of memory, in bytes, allocated for the associated process.
        public static void LogMemoryConsumption(ILogger logger)
        {
            // Determine the best available approximation of the number
            // of bytes currently allocated in managed memory.
            logger.LogInformation(
                "Total Memory thought to be allocated in bytes: {0}",
                GC.GetTotalMemory(false));

            // Gets the amount of private memory, in bytes, allocated for the associated process.
            Process currentProc = Process.GetCurrentProcess();
            logger.LogInformation(
                "Total Memory allocated for current process in bytes: {0}",
                currentProc.PrivateMemorySize64);

        }

        //
        // Summary:
        //     This method takes a logger, path to a solution file, and logs
        //     the total size of the solution, in bytes, by suming up the
        //     size of each cs file contained in the solution.
        public static void LogSolutiontSize(ILogger logger, string SolutionPath)
        {
            DirectoryInfo solutionDir = Directory.GetParent(SolutionPath);
            var size = solutionDir.EnumerateFiles(
                "*.cs", SearchOption.AllDirectories).Sum(fi => fi.Length);
            logger.LogInformation(
                "Total size for {0} in bytes: {1}",
                SolutionPath, size);
        }
    }
}
