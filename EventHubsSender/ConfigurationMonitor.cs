namespace EventHubsSender
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

    public class ConfigurationMonitor : IObservable<EventHubConnectionProperties>
    {
        private EventHubConnectionProperties toBeMonitored;

        private readonly ISet<IObserver<EventHubConnectionProperties>> observers;

        public ConfigurationMonitor()
        {
            this.toBeMonitored = new EventHubConnectionProperties();
            this.observers = new HashSet<IObserver<EventHubConnectionProperties>>();            
            this.toBeMonitored = FromTo(ReadSettingsFile());

            Task.Run(() => PeriodicLoadSettings());
        }
        public IDisposable Subscribe(IObserver<EventHubConnectionProperties> observer)
        {
            if (!observers.Contains(observer))
            {
                observers.Add(observer);
            }

            return new Unsubscriber(observers, observer);
        }

        public EventHubConnectionProperties ConnectionProperties { get => toBeMonitored; }

        private async Task PeriodicLoadSettings()
        {
            var dueTime = GenerateComingDueTime(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(10));
            JObject settings;

            while (true)
            {
                if (DateTimeOffset.UtcNow >= dueTime)
                {
                    dueTime = GenerateComingDueTime(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(10));
                    settings = ReadSettingsFile();

                    EventHubConnectionProperties prop = FromTo(settings);
                    if (!prop.EventHubName.Equals(toBeMonitored.EventHubName))
                    {
                        toBeMonitored = prop;
                        foreach (var observer in observers)
                        {
                            observer.OnNext(toBeMonitored);
                        }
                    }
                }
                else
                {
                    await Task.Delay(5000);
                }
            }
        }

        private EventHubConnectionProperties FromTo(JObject obj)
        {
            return new EventHubConnectionProperties
            {
                EventHubName = obj["EventHubName"].ToObject<string>(),
                EventHubConnectionString = obj["EventHubConnectionString"].ToObject<string>()
            };
        }

        private JObject ReadSettingsFile()
        {
            try
            {
                using (StreamReader reader = File.OpenText(@"C:\Users\rosou\source\repos\EventHubsQuickStart\EventHubsSender\Settings.json"))
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

        private static DateTimeOffset GenerateComingDueTime(DateTimeOffset schedule, TimeSpan interval)
        {
            var utcNow = DateTimeOffset.UtcNow;

            if (utcNow <= schedule)
            {
                return schedule;
            }

            return schedule.AddMinutes(Math.Ceiling((utcNow - schedule).TotalMinutes / interval.TotalMinutes) * interval.TotalMinutes);
        }

        private class Unsubscriber : IDisposable
        {
            private ISet<IObserver<EventHubConnectionProperties>> _observers;
            private IObserver<EventHubConnectionProperties> _observer;

            public Unsubscriber(ISet<IObserver<EventHubConnectionProperties>> observers, IObserver<EventHubConnectionProperties> observer)
            {
                this._observers = observers;
                this._observer = observer;
            }

            public void Dispose()
            {
                if (!(_observer == null)) _observers.Remove(_observer);
            }
        }
    }
}
