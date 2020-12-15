using CTA.Rules.Models;
using PortingAssistant.Client.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PortingAssistant.Client.Analysis.Utils
{
    public static class ProjectActionsToRecommendedActions
    {
        public static Dictionary<string, List<RecommendedAction>> Convert(ProjectActions projectActions)
        {
            return projectActions.FileActions.Select(fileAction =>
            {
                var RecommendedActions = fileAction.NodeTokens.ConvertAll(t => new RecommendedAction
                {
                    TargetCPU = t.TargetCPU,
                    TextSpan = new TextSpan
                    {
                        EndCharPosition = t.TextSpan.EndCharPosition,
                        EndLinePosition = t.TextSpan.EndLinePosition,
                        StartCharPosition = t.TextSpan.StartCharPosition,
                        StartLinePosition = t.TextSpan.StartLinePosition
                    },
                    Description = t.Description,
                    RecommendedActionType = RecommendedActionType.ReplaceNamespace
                }).ToHashSet();

                return new Tuple<string, List<RecommendedAction>>(fileAction.FilePath, RecommendedActions.ToList());
            }).ToDictionary(t => t.Item1, t => t.Item2);
        }
    }
}
