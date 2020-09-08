using System;
using System.Collections.Generic;
 
namespace PortingAssistant.Model
{
    public class RecommendationDetails
    {
        public string Name { get; set; } 
        public string EncoreVersion { get; set; } 
        public string Assembly { get; set; }
        public string Reference { get; set; }
        public string RecommendationSource { get; set; }
        public RecommendedActions[] RecommendedActions { get; set; }
 
        public override bool Equals(object obj)
        {
            return obj is RecommendationDetails details &&
                   Name == details.Name &&
                   EncoreVersion == details.EncoreVersion &&
                   Assembly == details.Assembly &&
                   Reference == details.Reference &&
                   RecommendationSource == details.RecommendationSource &&
                   EqualityComparer<RecommendedActions[]>.Default.Equals(RecommendedActions, details.RecommendedActions);
        }
 
        public override int GetHashCode()
        {
            return HashCode.Combine(Name, EncoreVersion, Assembly, Reference, RecommendationSource, RecommendedActions);
        }
    }
 
    public class RecommendedActions
    {
        public string Type { get; set; }
        public string Value { get; set; }
        public Recommendation[] Recommendation { get; set; }
 
        public override bool Equals(object obj)
        {
            return obj is RecommendedActions details &&
                   Type == details.Type &&
                   Value == details.Value &&
                   EqualityComparer<Recommendation[]>.Default.Equals(Recommendation, details.Recommendation);
        }
 
        public override int GetHashCode()
        {
            return HashCode.Combine(Type, Value, Recommendation);
        }
    }
 
    public class Recommendation
    {
        public string Source { get; set; }
        public string Preferred { get; set; }
        public SortedSet<string> Versions { get; set; }
        public string Description { get; set; }
        public Actions[] Actions { get; set; }
 
 
        public override bool Equals(object obj)
        {
            return obj is Recommendation details &&
                   Source == details.Source &&
                   Preferred == details.Preferred &&
                   EqualityComparer<SortedSet<string>>.Default.Equals(Versions, details.Versions) &&
                   Description == details.Description &&
                   EqualityComparer<Actions[]>.Default.Equals(Actions, details.Actions);
        }
 
        public override int GetHashCode()
        {
            return HashCode.Combine(Source, Preferred, Versions, Description, Actions);
        }
    }
 
    public class Actions
    {
        public string Type { get; set; }
        public string Value { get; set; }
        public string Description { get; set; }
 
        public override bool Equals(object obj)
        {
            return obj is Actions details &&
                   Type == details.Type &&
                   Value == details.Value &&
                   Description == details.Description;
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(Type, Value, Description);
        }
    }
}