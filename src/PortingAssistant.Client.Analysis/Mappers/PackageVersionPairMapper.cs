using PortingAssistant.Client.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PortingAssistant.Compatibility.Common.Interface;
using PortingAssistant.Compatibility.Common.Model;
using PackageVersionPair = PortingAssistant.Client.Model.PackageVersionPair;

namespace PortingAssistant.Client.Analysis.Mappers
{
    public static class PackageVersionPairMapper
    {
        public static PackageVersionPair Convert(Compatibility.Common.Model.PackageVersionPair pvp)
        {
            Enum.TryParse(pvp.PackageSourceType.ToString(), out PortingAssistant.Client.Model.PackageSourceType pst);
            return new PackageVersionPair()
            {
                PackageId = pvp.PackageId,
                Version = pvp.Version,
                PackageSourceType = pst
            };
        }

        public static PortingAssistant.Compatibility.Common.Model.PackageVersionPair Convert(PackageVersionPair pvp)
        {
            Enum.TryParse(pvp.PackageSourceType.ToString(), out PortingAssistant.Compatibility.Common.Model.PackageSourceType pst);
            return new PortingAssistant.Compatibility.Common.Model.PackageVersionPair()
            {
                PackageId = pvp.PackageId,
                Version = pvp.Version,
                PackageSourceType = pst
            };
        }

        public static List<PackageVersionPair> Convert(List<PortingAssistant.Compatibility.Common.Model.PackageVersionPair> pvplist)
        {
            var result = new List<PackageVersionPair>();
            foreach ( var pv in pvplist )
            {
                result.Add( Convert(pv));
            }
            return result;
        
        }
    }
}
