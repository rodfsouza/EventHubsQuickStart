namespace EventHubsSender
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class EventHubConnectionProperties
    {
        public string EventHubConnectionString { get; set; }

        public string EventHubName { get; set; }
    }
}
