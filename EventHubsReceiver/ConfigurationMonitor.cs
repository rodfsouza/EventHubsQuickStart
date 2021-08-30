namespace EventHubsReceiver
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class ConfigurationMonitor : IObservable<EventHubConsumerProperties>
    {
        private EventHubConsumerProperties toBeMonitored;

        private ISet<IObserver<EventHubConsumerProperties>> observers;

        public ConfigurationMonitor()
        {
            this.toBeMonitored = FromTo(ReadSettingsFile());
            this.observers = new HashSet<IObserver<EventHubConsumerProperties>>();
            Task.Run(() => PeriodicLoadSettings());
        }
        
        public IDisposable Subscribe(IObserver<EventHubConsumerProperties> observer)
        {
            if (!observers.Contains(observer))
            {
                observers.Add(observer);
            }

            return new Unsubscriber(observers, observer);
        }

        public EventHubConsumerProperties ConsumerProperties { get => toBeMonitored; }

        private class Unsubscriber : IDisposable
        {
            private ISet<IObserver<EventHubConsumerProperties>> _observers;
            private IObserver<EventHubConsumerProperties> _observer;

            public Unsubscriber(ISet<IObserver<EventHubConsumerProperties>> observers, IObserver<EventHubConsumerProperties> observer)
            {
                this._observers = observers;
                this._observer = observer;
            }

            public void Dispose()
            {
                if (!(_observer == null)) _observers.Remove(_observer);
            }
        }

        private async Task PeriodicLoadSettings()
        {            
            var dueTime = GenerateComingDueTime(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(10));
            JObject settings;

            while (true)
            {
                try
                {
                    if (DateTimeOffset.UtcNow >= dueTime)
                    {
                        dueTime = GenerateComingDueTime(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(10));
                        settings = ReadSettingsFile();

                        EventHubConsumerProperties props = FromTo(settings);
                        if (!toBeMonitored.EventHubName.Equals(props.EventHubName) && !toBeMonitored.BlobContainerName.Equals(props.BlobContainerName))
                        {
                            toBeMonitored = props;
                            foreach (var observer in observers)
                            {
                                observer.OnNext(toBeMonitored);
                            }
                        }

                    }

                    await Task.Delay(5000);
                }
                catch (Exception ex)
                {
                    throw;
                }

            }
        }

        private JObject ReadSettingsFile()
        {
            try
            {
                using (StreamReader reader = File.OpenText(@"C:\Users\rosou\source\repos\EventHubsQuickStart\EventHubsReceiver\Settings.json"))
                {
                    JObject o = (JObject)JToken.ReadFrom(new JsonTextReader(reader));
                    return o;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }

        private EventHubConsumerProperties FromTo(JObject obj)
        {
            return new EventHubConsumerProperties
            {
                EventHubName = obj["EventHubName"].ToObject<string>(),
                EventHubConnectionString = obj["EventHubConnectionString"].ToObject<string>(),
                BlobContainerName = obj["BlobContainerName"].ToObject<string>(),
                BlobStorageConnectionString = obj["BlobStorageConnectionString"].ToObject<string>()
            };
        }

        private static DateTimeOffset GenerateComingDueTime(DateTimeOffset schedule, TimeSpan interval)
        {
            var utcNow = DateTimeOffset.UtcNow;

            if (utcNow <= schedule)
            {
                return schedule;
            }

            return schedule.AddMinutes(Math.Ceiling((utcNow - schedule).TotalMinutes / interval.TotalMinutes) * interval.TotalMinutes);
        }
    }
}
