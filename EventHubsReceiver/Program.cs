namespace EventHubsReceiver
{
    using System;
    using System.Text;
    using System.Threading.Tasks;
    using Azure.Storage.Blobs;
    using Azure.Messaging.EventHubs;
    using Azure.Messaging.EventHubs.Consumer;
    using Azure.Messaging.EventHubs.Processor;
    using System.Collections.Generic;

    class Program
    {
        static async Task Main(string[] args)
        {
            ConfigurationMonitor provider = new();
            EventHubConsumer consumer = new(provider.ConsumerProperties);

            consumer.Subscribe(provider);

            while (true)
            {
                await consumer.StartAsync();
                Console.WriteLine(provider.ConsumerProperties.EventHubName);
                // Wait for 30 seconds for the events to be processed
                await Task.Delay(TimeSpan.FromSeconds(30));
            }
        }
    }
}
