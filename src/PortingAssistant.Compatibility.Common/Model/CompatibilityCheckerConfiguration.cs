using Amazon.S3.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortingAssistant.Compatibility.Common.Model
{
    public class CompatibilityCheckerConfiguration
    {
        public DataStoreSettings DataStoreSettings { get; set; }
        public CompatibilityCheckerConfiguration()
        {

            this.DataStoreSettings = new DataStoreSettings
            {
                HttpsEndpoint = "https://s3.us-west-2.amazonaws.com/preprod.aws.portingassistant.service.datastore.uswest2/",
                S3Endpoint = "preprod.aws.portingassistant.service.datastore.uswest2",
                GitHubEndpoint = "https://raw.githubusercontent.com/aws/porting-assistant-dotnet-datastore/master/",
            };
        }
    }

    public class DataStoreSettings
    {
        public string HttpsEndpoint { get; set; }
        public string S3Endpoint { get; set; }
        public string GitHubEndpoint { get; set; }

        
        public DataStoreSettings DeepCopy()
        {
            return new DataStoreSettings
            {
                HttpsEndpoint = this.HttpsEndpoint,
                S3Endpoint = this.S3Endpoint,
                GitHubEndpoint = this.GitHubEndpoint
            };
        }
    }
}