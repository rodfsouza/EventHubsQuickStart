namespace EventHubsReceiver
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public class EventHubConsumerProperties
    {
        public string EventHubConnectionString { get; set; }

        public string EventHubName { get; set; }

        public string BlobStorageConnectionString { get; set; }

        public string BlobContainerName { get; set; }

        public string ConsumerGroup { get; set; }
        
    }
}
