
namespace PortingAssistant.Model
{
    public class Invocation
    {
        public string MethodName { get; set; }
        public string Namespace { get; set; }
        public string MethodSignature { get; set; }
        public string OriginalDefinition { get; set; }
        public TextSpan Location { get; set; }
        public PackageVersionPair Package { get; set; }
    }
}