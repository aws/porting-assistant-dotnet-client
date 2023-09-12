using Amazon.Lambda.Core;
using PortingAssistant.Compatibility.Common.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortingAssistant.Compatibility.Common.Interface
{
    public interface ICompatibilityCheckerRecommendationActionHandler
    {
        public Task<Dictionary<string, RecommendationActionFileDetails>> GetRecommendationActionFileAsync(IEnumerable<string> namespaces);
    }
}
