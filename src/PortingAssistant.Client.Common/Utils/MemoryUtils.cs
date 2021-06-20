using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;

namespace PortingAssistant.Client.Common.Utils
{
    public static class MemoryUtils
    {
        public static void LogMemoryConsumption(ILogger logger)
        {
            // Determine the best available approximation of the number
            // of bytes currently allocated in managed memory.
            logger.LogInformation("Total Memory thought to be allocated in bytes: {0}", GC.GetTotalMemory(false));

            // Gets the amount of private memory, in bytes, allocated for the associated process.
            Process currentProc = Process.GetCurrentProcess();
            logger.LogInformation("Total Memory allocated for current process in bytes: {0}", currentProc.PrivateMemorySize64);

        }
    }
}
