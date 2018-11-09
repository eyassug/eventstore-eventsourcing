namespace EventStore.EventSourcing
{
    using System;
    using Fano.CQRS.EventSourcing;
    using EventStore.ClientAPI;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using ServiceStack.Text;
    using ServiceStack;
    using System.Reflection;
    using System.Linq;
    public class EventStoreEventSourcedRepository<T, TKey> : IAsyncEventSourcedRepository<T, TKey> where T : IEventSourced<TKey>
    {
        private readonly IEventStoreConnection _eventStore;
        private readonly Func<TKey, IEnumerable<IVersionedEvent>, T> entityFactory;

        private static Dictionary<string, Type> _eventTypesHash;
        static EventStoreEventSourcedRepository()
        {
            var versionedEventType = typeof(IVersionedEvent);
            var typesInAssemblies = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()).ToList();//.ToDictionary<(t => t.FullName, t => t);
            _eventTypesHash = typesInAssemblies.Where(t => versionedEventType.IsAssignableFrom(t)).ToDictionary(t => t.FullName, t => t);
        }
        public EventStoreEventSourcedRepository(IEventStoreConnection eventStore)
        {
            this._eventStore = eventStore;
            // TODO: could be replaced with a compiled lambda
            var constructor = typeof(T).GetConstructor(new[] { typeof(TKey), typeof(IEnumerable<IVersionedEvent>) });
            if (constructor == null)
            {
                throw new InvalidCastException("Type T must have a constructor with the following signature: .ctor(TKey, IEnumerable<IVersionedEvent>)");
            }
            this.entityFactory = (id, events) => (T)constructor.Invoke(new object[] { id, events });
        }
        public async Task<T> FindAsync(TKey id)
        {
            string streamName = string.Format("{0}-{1}", typeof(T).Name, id);
            
            var eventsSlice = await _eventStore.ReadStreamEventsForwardAsync(streamName, StreamPosition.Start, 4096, false);
            
            if(!eventsSlice.Events.Any()) return default(T);

            var resolved = eventsSlice.Events.ToList();
            var recordedEvents = resolved.Select(e => e.Event).ToList();
            var versionedEvents = recordedEvents.Select(e => Deserialize(e)).ToList();
            
            return entityFactory.Invoke(id, versionedEvents);
        }

        public async Task<T> GetAsync(TKey id)
        {
            var entity = await FindAsync(id);
            if(entity == null)
                throw new EntityNotFoundException();

            return entity;
        } 

        public async Task SaveAsync(T eventSourced, string correlationId)
        {
            // Guarantee incremental versions
            var expectedVersion = eventSourced.Version - eventSourced.Events.Count();
            
            var events = eventSourced.Events.Select(e => Serialize(e, correlationId));
            
            using(var transaction = await _eventStore.StartTransactionAsync(eventSourced.GetEventStoreStream(), expectedVersion))
            {
                await transaction.WriteAsync(events);
                await transaction.CommitAsync();
            }
        }

        static EventData Serialize(IVersionedEvent @event, string correlationId)
        {
            if(@event == null) throw new ArgumentNullException(nameof(@event));
            var json = JsonSerializer.SerializeToString(@event);
            var metadata = new Dictionary<string, object>{ {"correlationId", correlationId}};
            return new EventData(Guid.NewGuid(), @event.GetType().FullName, true, json.ToUtf8Bytes(), metadata.ToJson().ToUtf8Bytes());
        }

        static IVersionedEvent Deserialize(RecordedEvent @event)
        {
            if(@event == null) throw new ArgumentNullException(nameof(@event));
            Console.WriteLine("EventType: {0}", @event.EventType);
            Type eventType;
            _eventTypesHash.TryGetValue(@event.EventType, out eventType);
            if (eventType != null)
            {
                var json = @event.Data.FromUtf8Bytes();      
                return (IVersionedEvent) JsonSerializer.DeserializeFromString(json, eventType);
            }
            //TODO: return an empty event that doesn't affect aggregates.
            return null;
        }
    }
}