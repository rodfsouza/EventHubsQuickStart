namespace EventHubsReceiver
{
    using Azure.Messaging.EventHubs;
    using Azure.Messaging.EventHubs.Consumer;
    using Azure.Messaging.EventHubs.Processor;
    using Azure.Storage.Blobs;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class EventHubConsumer : IObserver<EventHubConsumerProperties>
    {
        private IDisposable unsubscriber;

        private EventHubConsumerProperties lastProps;

        private readonly Dictionary<string, EventProcessorClient> clientCache;

        private readonly Dictionary<string, BlobContainerClient> blobCacheClient;

        public EventHubConsumer(EventHubConsumerProperties initialConfiguration)
        {
            this.lastProps = initialConfiguration;
            this.clientCache = new();
            this.blobCacheClient = new();
        }

        public EventHubConsumerProperties ConsumerProperties { get => lastProps; }

        public async Task StartAsync()
        {
            EventProcessorClient processor;
            if (!clientCache.TryGetValue(lastProps.EventHubName, out processor))
            {
                BlobContainerClient blobClient = new BlobContainerClient(lastProps.BlobStorageConnectionString, lastProps.BlobContainerName);
                
                processor = new EventProcessorClient(blobClient, EventHubConsumerClient.DefaultConsumerGroupName,
                    lastProps.EventHubConnectionString, lastProps.EventHubName);
                
                clientCache.TryAdd(lastProps.EventHubName, processor);

                blobCacheClient.TryAdd(lastProps.BlobContainerName, blobClient);
            }

            if (!processor.IsRunning)
            {
                // Register handlers for processing events and handling errors
                processor.ProcessEventAsync += ProcessEventHandler;
                processor.ProcessErrorAsync += ProcessErrorHandler;

                // Starts the processing
                await processor.StartProcessingAsync();
            }
        }

        public virtual void Subscribe(IObservable<EventHubConsumerProperties> provider)
        {
            this.unsubscriber = provider.Subscribe(this);
        }

        public virtual async Task Unsubscribe()
        {
            foreach (var client in clientCache.Values)
            {
                if (client.IsRunning)
                {
                    await client.StopProcessingAsync();
                }
            }
            unsubscriber.Dispose();
        }

        public void OnCompleted()
        {
            Console.WriteLine("Nothing else to be processed");
        }

        public void OnError(Exception error)
        {
            Console.WriteLine(error.Message);
        }

        public void OnNext(EventHubConsumerProperties value)
        {
            this.lastProps = value;
            
            BlobContainerClient blobClient;
            if (!blobCacheClient.TryGetValue(lastProps.BlobContainerName, out blobClient))
            {
                blobClient = new BlobContainerClient(lastProps.BlobStorageConnectionString, lastProps.BlobContainerName);
                blobCacheClient.Add(lastProps.BlobContainerName, blobClient);
            }

            if (!clientCache.TryGetValue(lastProps.EventHubName, out EventProcessorClient _))
            {
                clientCache.Add(lastProps.EventHubName, new EventProcessorClient(blobClient, EventHubConsumerClient.DefaultConsumerGroupName, 
                    lastProps.EventHubConnectionString, lastProps.EventHubName));
            }
        }

        private async Task ProcessEventHandler(ProcessEventArgs eventArgs)
        {
            // Write the body of the event to the console window
            Console.WriteLine("\tReceived event: {0}", Encoding.UTF8.GetString(eventArgs.Data.Body.ToArray()));

            // Update checkpoint in the blob storage so that the app receives only new events the next time it's run
            await eventArgs.UpdateCheckpointAsync(eventArgs.CancellationToken);
        }

        private Task ProcessErrorHandler(ProcessErrorEventArgs eventArgs)
        {
            // Write details about the error to the console window
            Console.WriteLine($"\tPartition '{ eventArgs.PartitionId}': an unhandled exception was encountered. This was not expected to happen.");
            Console.WriteLine(eventArgs.Exception.Message);
            return Task.CompletedTask;
        }
    }
}
