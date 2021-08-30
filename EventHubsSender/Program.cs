namespace EventHubsSender
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using Azure.Messaging.EventHubs;
    using Azure.Messaging.EventHubs.Producer;

    class Program
    {
        static async Task Main(string[] args)
        {
            var provider = new ConfigurationMonitor();
            var producer = new EventHubProducer(provider.ConnectionProperties);

            producer.Subscribe(provider);
            await producer.SendAsync($"rosou test at {DateTimeOffset.UtcNow}");
        }
    }
}
