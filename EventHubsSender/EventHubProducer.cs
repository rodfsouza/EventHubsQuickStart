namespace EventHubsSender
{
    using Azure.Messaging.EventHubs;
    using Azure.Messaging.EventHubs.Producer;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class EventHubProducer : IObserver<EventHubConnectionProperties>
    {
        private IDisposable unsubscriber;

        private EventHubConnectionProperties lastProps;

        // The Event Hubs client types are safe to cache and use as a singleton for the lifetime
        // of the application, which is best practice when events are being published or read regularly.
        //static EventHubProducerClient producerClient;
        private Dictionary<string, EventHubProducerClient> clientCache;

        public EventHubProducer(EventHubConnectionProperties initialConfiguration)
        {
            this.lastProps = initialConfiguration;
            this.clientCache = new Dictionary<string, EventHubProducerClient>();
        }

        public async Task SendAsync(string data)
        {
            EventHubProducerClient producerClient = new EventHubProducerClient(lastProps.EventHubConnectionString, lastProps.EventHubName);
            clientCache.Add(lastProps.EventHubName, producerClient);

            while (true)
            {
                if (lastProps != null)
                {
                    clientCache.TryGetValue(lastProps.EventHubName, out producerClient);

                    // Create a batch of events 
                    using EventDataBatch eventBatch = await producerClient.CreateBatchAsync();

                    for (int i = 1; i <= 3; i++)
                    {
                        if (!eventBatch.TryAdd(new EventData(Encoding.UTF8.GetBytes($"Event {i} with {data}"))))
                        {
                            // if it is too large for the batch
                            throw new Exception($"Event {i} is too large for the batch and cannot be sent.");
                        }
                    }

                    // Use the producer client to send the batch of events to the event hub
                    await producerClient.SendAsync(eventBatch);
                    Console.WriteLine($"A batch of {3} events has been published to {lastProps.EventHubName}.");
                }
                await Task.Delay(10000);
            }
        }
        
        public virtual void Subscribe(IObservable<EventHubConnectionProperties> provider)
        {
            this.unsubscriber = provider.Subscribe(this);
        }

        public virtual async Task Unsubscribe()
        {
            foreach (var client in clientCache.Values)
            {
                if (!client.IsClosed)
                {
                    await client.DisposeAsync();
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
            //Do nothing;
        }

        public void OnNext(EventHubConnectionProperties value)
        {
            this.lastProps = value;
            if (!clientCache.TryGetValue(lastProps.EventHubName, out EventHubProducerClient client))
            {
                clientCache.Add(lastProps.EventHubName, new EventHubProducerClient(lastProps.EventHubConnectionString, lastProps.EventHubName));
            }
        }
    }
}
