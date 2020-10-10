using System;
namespace PortingAssistant.Client.Model
{
    public class DataStoreSettings
    {
        public string HttpsEndpoint { get; set; }
        public string S3Endpoint { get; set; }

        public DataStoreSettings DeepCopy()
        {
            return new DataStoreSettings
            {
                HttpsEndpoint = this.HttpsEndpoint,
                S3Endpoint = this.S3Endpoint
            };
        }
    }
}
