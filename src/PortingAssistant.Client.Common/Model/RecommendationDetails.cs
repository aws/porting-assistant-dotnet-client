using System;
using System.Collections.Generic;

namespace PortingAssistant.Client.Model
{
    public class RecommendationDetails
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public RecommendationModel[] Recommendations { get; set; }

        public Packages[] Packages { get; set; }

        public override bool Equals(object obj)
        {
            return obj is RecommendationDetails details &&
                   Name == details.Name &&
                   Version == details.Version &&
                   EqualityComparer<RecommendationModel[]>.Default.Equals(Recommendations, details.Recommendations);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Version, Recommendations);
        }
    }

    public class Packages
    {
        public string Type { get; set; }
        public string Name { get; set; }

        public override bool Equals(object obj)
        {
            return obj is Packages model &&
                   Type == model.Type &&
                   Name == model.Name;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Type);
        }
    }

    public class RecommendationModel
    {
        public string Type { get; set; }
        public string Value { get; set; }
        public string Name { get; set; }
        public string KeyType { get; set; }
        public RecommendedActionModel[] RecommendedActions { get; set; }

        public override bool Equals(object obj)
        {
            return obj is RecommendationModel details &&
                   Type == details.Type &&
                   Value == details.Value &&
                   EqualityComparer<RecommendedActionModel[]>.Default.Equals(RecommendedActions, details.RecommendedActions);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Type, Value, RecommendedActions);
        }
    }

    public class RecommendedActionModel
    {
        public string Source { get; set; }
        public string Preferred { get; set; }
        public SortedSet<string> TargetFrameworks { get; set; }
        public string Description { get; set; }
        public Actions[] Actions { get; set; }

        public override bool Equals(object obj)
        {
            return obj is RecommendedActionModel details &&
                   Source == details.Source &&
                   Preferred == details.Preferred &&
                   EqualityComparer<SortedSet<string>>.Default.Equals(TargetFrameworks, details.TargetFrameworks) &&
                   Description == details.Description &&
                   EqualityComparer<Actions[]>.Default.Equals(Actions, details.Actions);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Source, Preferred, TargetFrameworks, Description, Actions);
        }
    }

    public class Actions
    {
        public string Type { get; set; }
        public string Value { get; set; }

        public override bool Equals(object obj)
        {
            return obj is Actions details &&
                   Type == details.Type &&
                   Value == details.Value;
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(Type, Value);
        }
    }
}