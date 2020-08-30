namespace EncoreCommon.Model
{
    public class NamespaceReplaceStrategy : IStrategy
    {
        public string Type {
            get {
                return "namespaceReplace";
            }
        }
        public string PackageId { get; set; }
        public string LatestVersion { get; set; }
        public string OldNamespace { get; set; }
        public string NewNamespace { get; set; }
    }
}