
namespace EncoreCommon.Model
{
    public class Invocation
    {
        public string MethodName { get; set; }
        public string Namespace { get; set; }
        public string MethodSignature { get; set; }
        public string OriginalDefinition { get; set; }
        public InvocationLocation Location { get; set; }
        public string PackageId { get; set; }
        public string Version { get; set; }
    }
}