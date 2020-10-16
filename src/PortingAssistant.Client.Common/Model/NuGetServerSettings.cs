using System;
namespace PortingAssistant.Client.Model
{
    public class NuGetServerSettings
    {
        public string NugetServerEndpoint { get; set; }

        public NuGetServerSettings DeepCopy()
        {
            return new NuGetServerSettings
            {
                NugetServerEndpoint = this.NugetServerEndpoint
            };
        }
    }
}
