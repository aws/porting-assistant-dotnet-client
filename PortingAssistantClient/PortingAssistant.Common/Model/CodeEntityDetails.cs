using System;
namespace PortingAssistant.Model
{
    public class CodeEntityDetails
    {
        public CodeEntityType CodeEntityType { get; set; }
        public string Name { get; set; }
        public string Namespace { get; set; }
        public string Signature { get; set; }  //valid for method
        public string OriginalDefinition { get; set; } //valid for method
        public TextSpan TextSpan { get; set; }
        public PackageVersionPair Package { get; set; }
    }
}
