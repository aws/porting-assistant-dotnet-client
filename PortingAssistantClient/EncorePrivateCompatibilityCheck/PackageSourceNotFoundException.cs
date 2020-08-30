using System;
namespace EncorePrivateCompatibilityCheck
{
    public class PackageSourceNotFoundException : Exception
    {
        public PackageSourceNotFoundException()
        {
        }

        public PackageSourceNotFoundException(string message):
            base(message)
        {
        }
    }
}
