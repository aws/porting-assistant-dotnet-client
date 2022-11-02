using System;
using System.Collections.Generic;

namespace PortingAssistant.Client.Common.Model
{
    public class SupportedVersion : IComparable<SupportedVersion>
    {
        public string DisplayName { get; set; }
        public string VersionKey { get; set; }
        public string RequiredVisualStudioVersion { get; set; }
        public string RecommendOrder { get; set; }

        public SupportedVersion()
        { }

        public SupportedVersion(SupportedVersion other)
        {
            DisplayName = other.DisplayName;
            VersionKey = other.VersionKey;
            RequiredVisualStudioVersion = other.RequiredVisualStudioVersion;
            RecommendOrder = other.RecommendOrder;
        }

        public int CompareTo(SupportedVersion other)
        {
            return this.RecommendOrder.CompareTo(other.RecommendOrder);
        }
    }

    public class SupportedVersionConfiguration
    {
        /// <summary>
        /// Porting Assitant tools (standalone tool and IDE extensions) would require internet connection to function,
        /// including assess, port and deploy. Adding a dependency to a public S3 bucket configuration file is not 
        /// introducing extra internet dependency.
        /// </summary>
        public const string S3Region = "us-west-2";
        public const string S3BucketName = "mingxue-global-test";
        public const string S3File = "PAConfigurations/SupportedVersion.json";
        public const string ExpectedBucketOwnerId = "412081997838";
        public string FormatVersion { get; set; }
        public List<SupportedVersion> Versions { get; set; }

        /// <summary>
        /// Default values are for backward compatibility purpose.
        /// These values are needed for both Standlone tool and IDE extensions.
        /// Adding them here so we don't need to worry about the configuration files for all of them.
        /// </summary>
        public SupportedVersionConfiguration()
        {
            Versions = new List<SupportedVersion>();
        }

        public static SupportedVersionConfiguration CreateDefaultConfiguration()
        {
            SupportedVersionConfiguration defaultConfig = new SupportedVersionConfiguration()
            {
                FormatVersion = "1.0",
                Versions = new List<SupportedVersion>
                {
                    new SupportedVersion()
                    {
                        DisplayName = ".NET 6 (Microsoft LTS)",
                        VersionKey = "net6.0",
                        RequiredVisualStudioVersion = "17.0.0",
                        RecommendOrder = "1"
                    },

                    new SupportedVersion()
                    {
                        DisplayName = ".NET Core 3.1 (Microsoft LTS)",
                        VersionKey = "netcoreapp3.1",
                        RequiredVisualStudioVersion = "16.0.0",
                        RecommendOrder = "2"
                    },

                    new SupportedVersion()
                    {
                        DisplayName = ".NET 5 (Microsoft out of support)",
                        VersionKey = "net5.0",
                        RequiredVisualStudioVersion = "16.0.0",
                        RecommendOrder = "3"
                    }
                }
            };

            return defaultConfig;
        }

        public SupportedVersionConfiguration DeepCopy()
        {
            var result = new SupportedVersionConfiguration()
            {
                FormatVersion = this.FormatVersion,
                Versions = new List<SupportedVersion>(),
            };

            this.Versions.ForEach(v =>
            {
                result.Versions.Add(new SupportedVersion(v));
            });

            return result;
        }
    }
}
