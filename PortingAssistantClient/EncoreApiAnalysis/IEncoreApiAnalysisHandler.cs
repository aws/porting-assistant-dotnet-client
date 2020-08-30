using System;
using System.Collections.Generic;
using EncoreApiAnalysis.Model;
using EncoreCommon.Model;

namespace EncoreApiAnalysis
{
    public interface IEncoreApiAnalysisHandler
    {
        SolutionAnalysisResult AnalyzeSolution(string solutionFilename, List<Project> projects);
    }
}
