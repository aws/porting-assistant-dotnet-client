using System;
using System.Collections.Generic;
using System.Text;

namespace PortingAssistant.Client.Model
{
    public class PortingAction
    {
        public TextSpan TextSpan { get; set; }
        public RecommendedAction RecommendedAction { get; set; }
        public Dictionary<string, List<string>> TargetFramework { get; set; }

        public override bool Equals(object obj)
        {
            return obj is PortingAction pair &&
                   TextSpan == pair.TextSpan &&
                   RecommendedAction == pair.RecommendedAction;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(TextSpan, RecommendedAction);
        }
    }
}
