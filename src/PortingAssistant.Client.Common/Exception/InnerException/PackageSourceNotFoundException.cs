using System;

namespace PortingAssistant.Client.Model
{
    public class PackageSourceNotFoundException : Exception
    {
        public PackageSourceNotFoundException(string message) :
            base(message)
        {
        }
    }
}
