using System;
using System.Collections.Generic;

namespace PortingAssistant.Model
{
    public class PackageDetails
    {
        public string Name { get; set; }
        public SortedSet<string> Versions { get; set; }
        public Dictionary<string, SortedSet<string>> Targets { get; set; }
        public LicenseDetails License { get; set; }
        public ApiDetails[] Api { get; set; }
        public bool Deprecated { get; set; }

        public override bool Equals(object obj)
        {
            return obj is PackageDetails details &&
                   Name == details.Name &&
                   EqualityComparer<SortedSet<string>>.Default.Equals(Versions, details.Versions) &&
                   EqualityComparer<Dictionary<string, SortedSet<string>>>.Default.Equals(Targets, details.Targets) &&
                   EqualityComparer<LicenseDetails>.Default.Equals(License, details.License) &&
                   EqualityComparer<ApiDetails[]>.Default.Equals(Api, details.Api);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Versions, Targets, License, Api);
        }
    }

    public class LicenseDetails
    {
        public Dictionary<string, SortedSet<string>> License { get; set; }

        public override bool Equals(object obj)
        {
            return obj is LicenseDetails details &&
                   EqualityComparer<Dictionary<string, SortedSet<string>>>.Default.Equals(License, details.License);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(License); ;
        }
    }
}
