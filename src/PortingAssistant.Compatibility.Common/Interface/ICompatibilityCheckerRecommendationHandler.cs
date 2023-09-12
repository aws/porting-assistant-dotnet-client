using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using PortingAssistant.Compatibility.Common.Model;

namespace PortingAssistant.Compatibility.Common.Interface
{
    public interface ICompatibilityCheckerRecommendationHandler
    {
        public Dictionary<string, Task<RecommendationDetails>> GetApiRecommendation(IEnumerable<string> namespaces);
    }
}
