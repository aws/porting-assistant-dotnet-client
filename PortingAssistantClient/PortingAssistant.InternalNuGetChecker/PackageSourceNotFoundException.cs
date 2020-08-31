using System;
namespace PortingAssistant.InternalNuGetChecker
{
    public class PackageSourceNotFoundException : Exception
    {
        public PackageSourceNotFoundException()
        {
        }

        public PackageSourceNotFoundException(string message) :
            base(message)
        {
        }
    }
}
