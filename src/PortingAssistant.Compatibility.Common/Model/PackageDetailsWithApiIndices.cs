using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortingAssistant.Compatibility.Common.Model
{
    public class PackageDetailsWithApiIndices
    {
        public PackageDetails PackageDetails { get; set; }
        public Dictionary<string, int> IndexDict { get; set; }
    }
}
