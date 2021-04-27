using CTA.Rules.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PortingAssistant.Client.Model
{
    public class RecommendedAction
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public RecommendedActionType RecommendedActionType { get; set; }
        public string Description { get; set; }
        public TextSpan TextSpan { get; set; }
        public List<string> TargetCPU { get; set; }

        public IList<TextChange> TextChanges { get; set; }

        public override bool Equals(object obj)
        {
            return obj is RecommendedAction pair &&
                   RecommendedActionType == pair.RecommendedActionType &&
                   TextSpan.Equals(pair.TextSpan) &&
                   Description == pair.Description &&
                   ((TargetCPU == null && pair.TargetCPU == null) || TargetCPU.OrderBy(t => t).SequenceEqual(pair.TargetCPU.OrderBy(t => t)));
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(RecommendedActionType.ToString(), TextSpan, Description, TargetCPU);
        }
    }
}
