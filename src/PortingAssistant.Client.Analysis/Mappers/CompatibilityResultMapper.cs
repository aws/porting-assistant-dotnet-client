using PortingAssistant.Client.Model;
using PortingAssistant.Compatibility.Common.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CompatibilityResult = PortingAssistant.Compatibility.Common.Model.CompatibilityResult;

namespace PortingAssistant.Client.Analysis.Mappers
{
    public static class CompatibilityResultMapper
    {

        public static PortingAssistant.Client.Model.CompatibilityResult Convert(CompatibilityResult result)
        {
            if (result == null)
            {
                return new PortingAssistant.Client.Model.CompatibilityResult()
                {
                    Compatibility =  Model.Compatibility.UNKNOWN,
                    CompatibleVersions = new List<string>(),
                };

            }
            Enum.TryParse(result.Compatibility.ToString(), out PortingAssistant.Client.Model.Compatibility p);
            return new PortingAssistant.Client.Model.CompatibilityResult()
            {
                Compatibility = p,
                CompatibleVersions = result.CompatibleVersions,
            };
        }

        public static CompatibilityResult Convert(PortingAssistant.Client.Model.CompatibilityResult result)
        {
            if (result == null)
            {
                return new CompatibilityResult()
                {
                    Compatibility = Compatibility.Common.Model.Compatibility.UNKNOWN,
                    CompatibleVersions = new List<string>(),
                };
            }

            Enum.TryParse(result.Compatibility.ToString(), out PortingAssistant.Compatibility.Common.Model.Compatibility p);
            return new CompatibilityResult()
            {
                Compatibility = p,
                CompatibleVersions = result.CompatibleVersions,
            };
        }

        public static Dictionary<string, PortingAssistant.Client.Model.CompatibilityResult> Convert(Dictionary<string, CompatibilityResult> compatibilityResults)
        {
            var result = new Dictionary<string, PortingAssistant.Client.Model.CompatibilityResult>();
            if (compatibilityResults == null)
            {
                return result;
            }
            
            foreach (var item in compatibilityResults)
            {
                result.Add(item.Key, Convert(item.Value));
            }

            return result;
        }
    }
}
