namespace PortingAssistant.Model
{
    public class NamespaceDetails
    {
        public PackageDetails Package { get; set; }
        public string[] Namespaces { get; set; }
        public string[] Assemblies { get; set; }
    }
}
