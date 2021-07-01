using PortingAssistant.Client.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace PortingAssistant.Client.Common.Model
{
    public class CodeEntityCompatibilityResult
    {
        public CodeEntityCompatibilityResult(CodeEntityType codeEntityType)
        {
            CodeEntityType = codeEntityType;
        }
        public CodeEntityType CodeEntityType { get; set; }
        public int Compatible { get; set; }
        public int Incompatible { get; set; }
        public int Unknown { get; set; }
        public int Actions { get; set; }
        public int Deprecated { get; set; }

        public override bool Equals(object obj)
        {
            return base.Equals(obj as CodeEntityCompatibilityResult);
        }

        public bool Equals(CodeEntityCompatibilityResult compareResult)
        {
            return compareResult?.CodeEntityType.Equals(CodeEntityType) == true;
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(CodeEntityType);
        }
    }
}
