namespace EncoreCommon.Model
{
    public class UpgradeStrategy : IStrategy
    {
        public string Type {
            get {
                return "upgrade";
            }
        }
        public string OldestVersion { get; set; }
        public string LatestVersion { get; set; }
    }
}
