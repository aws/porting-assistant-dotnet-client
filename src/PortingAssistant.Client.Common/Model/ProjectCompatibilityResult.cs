using PortingAssistant.Client.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace PortingAssistant.Client.Common.Model
{
    public class ProjectCompatibilityResult
    {
        public ProjectCompatibilityResult()
        {
            CodeEntityCompatibilityResults = new HashSet<CodeEntityCompatibilityResult>();
            CodeEntityCompatibilityResults.Add(new CodeEntityCompatibilityResult(CodeEntityType.Annotation));
            CodeEntityCompatibilityResults.Add(new CodeEntityCompatibilityResult(CodeEntityType.Method));
            CodeEntityCompatibilityResults.Add(new CodeEntityCompatibilityResult(CodeEntityType.Declaration));
            CodeEntityCompatibilityResults.Add(new CodeEntityCompatibilityResult(CodeEntityType.Enum));
            CodeEntityCompatibilityResults.Add(new CodeEntityCompatibilityResult(CodeEntityType.Struct));
        }
        public string ProjectPath { get; set; }
        public bool IsPorted { get; set; }

        public HashSet<CodeEntityCompatibilityResult> CodeEntityCompatibilityResults { get; set; }

        public override string ToString()
        {
            var str = new StringBuilder();
            if (IsPorted)
            {
                str.AppendLine($"Ported Project Compatibilities for {ProjectPath}:");
            }
            else
            {
                str.AppendLine($"Analyzed Project Compatibilities for {ProjectPath}:");
            }

            CodeEntityCompatibilityResults.ToList().ForEach(result =>
            {
                str.AppendLine($"{ result.CodeEntityType }: Compatible:{result.Compatible}, Incompatible:{result.Incompatible}, Unknown:{result.Unknown}, Deprecated:{result.Deprecated}, Actions:{result.Actions}");
            });
            return str.ToString();
        }
    }
}
