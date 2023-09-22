using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortingAssistant.Compatibility.Common.Model
{
    // File details of recommendation action files "namespace.json".
    public class RecommendationActionFileDetails
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public ActionFilePackages[] Packages { get; set; }
        public RecommendationActionFileModel[] Recommendations { get; set; }
    }

    public class ActionFilePackages
    {
        public string Type { get; set; }
        public string Name { get; set; }
    }

    public class RecommendationActionFileModel
    {
        public string Type { get; set; }
        public string Value { get; set; }
        public string Name { get; set; }
        public string KeyType { get; set; }
        public RecommendedActionActionFileModel[] RecommendedActions { get; set; }
    }

    public class RecommendedActionActionFileModel
    {
        public string Source { get; set; }
        public string Preferred { get; set; }
        public List<TargetFramework> TargetFrameworks { get; set; }
        public string Description { get; set; }
        public ActionFileActions[] Actions { get; set; }
    }

    public class TargetFramework
    {
        public string Name { get; set; }
        public List<string> TargetCPU { get; set; }
    }

    public class ActionFileActions
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public object Value { get; set; } // For CSharp
        public object VbValue { get; set; } // For Vb
        public string Description { get; set; }
        public ActionValidation ActionValidation { get; set; } // For CSharp
        public ActionValidation VbActionValidation { get; set; } // For Vb
    }

    public class ActionValidation
    {
        public string Contains { get; set; }
        public string NotContains { get; set; }
        public string CheckComments { get; set; }
    }
}