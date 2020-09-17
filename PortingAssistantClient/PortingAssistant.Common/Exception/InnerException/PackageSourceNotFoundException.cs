using System;

namespace PortingAssistant.Model
{
    public class PackageSourceNotFoundException : Exception
    {
        public PackageSourceNotFoundException(string message) :
            base(message)
        {
        }
    }
}
