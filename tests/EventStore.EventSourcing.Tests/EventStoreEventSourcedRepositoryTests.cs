namespace EventStore.EventSourcing.Tests
{
    using System;
    using Fano.CQRS.EventSourcing;
    using Xunit;
    using Moq;
    using System.Linq;
    using EventStore.ClientAPI;
    using ServiceStack;
    using ServiceStack.Text;
    using System.Collections.Generic;
    using System.Reflection;

    class AggregateWithoutValidConstructor : EventSourced<Guid>
    {
        AggregateWithoutValidConstructor(Guid id) : base(id)
        {

        }
    }
    public class AggregateA : EventSourced<Guid>
    {
        AggregateA(Guid id) : base(id)
        {
            Handles<OrderPlaced>(this.On);
        }
        public AggregateA(): this(Guid.NewGuid()) {}

        public AggregateA(Guid id, IEnumerable<IVersionedEvent> pastEvents) : this(id)
        {
            LoadFrom(pastEvents);
        }
        public void PlaceOrder(Guid orderId, string name)
        {
            this.Update(new OrderPlaced(orderId, name));
        }
        
        public Guid OrderId { get; private set; }
        public string CustomerName { get; private set; }
        void On(OrderPlaced @event)
        {
            OrderId = @event.OrderId;
            CustomerName = @event.CustomerName;
        }
    }
    public class OrderPlaced : VersionedEvent
    {
        public OrderPlaced(Guid orderId, string customerName)
        {
            OrderId = orderId;
            CustomerName = customerName;
        }
        public Guid OrderId { get; set;}
        public string CustomerName {get; set;}
    }

    public class EventStoreEventSourcedRepositoryTests
    {
        [Fact]
        public void Ctor_NoValidConstructor_ThrowsInvalidCastException()
        {
            var mockStore = new Mock<IEventStoreConnection>();
            
            Assert.Throws<InvalidCastException>(() => { var repo = new EventStoreEventSourcedRepository<AggregateWithoutValidConstructor, Guid>(mockStore.Object);});
            

        }

        [Fact]
        public void Save_NewAggregate_ShouldSaveToNewStream()
        {
            var uri = @"tcp://admin:changeit@localhost:1113/";
            var connection = EventStoreConnection.Create(new Uri(uri));
            connection.ConnectAsync().Wait();
            var repository = new EventStoreEventSourcedRepository<AggregateA, Guid>(connection);
            
            var agg = new AggregateA();
            var correlation = Guid.NewGuid();
            agg.PlaceOrder(Guid.NewGuid(), "John Doe");
            agg.PlaceOrder(Guid.NewGuid(), "Jane Doe");

            repository.SaveAsync(agg, correlation.ToString()).Wait();
            Assert.True(true);
        }

        [Fact]
        public void Get_InvalidId_ThrowsEntityNotFoundException()
        {
            //TODO: Mock IEventStore
            var uri = @"tcp://admin:changeit@localhost:1113/";
            var connection = EventStoreConnection.Create(new Uri(uri));
            connection.ConnectAsync().Wait();
            var repository = new EventStoreEventSourcedRepository<AggregateA, Guid>(connection);

            var id = Guid.NewGuid();
            Assert.ThrowsAsync<EntityNotFoundException>(async () => await repository.GetAsync(id));
        }

        [Fact]
        public void Find_InvalidId_ReturnsNull()
        {
            //TODO: Mock IEventStore
            var uri = @"tcp://admin:changeit@localhost:1113/";
            var connection = EventStoreConnection.Create(new Uri(uri));
            connection.ConnectAsync().Wait();
            var repository = new EventStoreEventSourcedRepository<AggregateA, Guid>(connection);

            var id = Guid.NewGuid();
            var entity = repository.FindAsync(id).Result;

            Assert.Null(entity);
        }

        [Fact]
        public void Find_ValidAggregateId_ShouldReturnHydratedAggregate()
        {
            //TODO: Mock IEventStore
            var uri = @"tcp://admin:changeit@localhost:1113/";
            var connection = EventStoreConnection.Create(new Uri(uri));
            connection.ConnectAsync().Wait();
            var repository = new EventStoreEventSourcedRepository<AggregateA, Guid>(connection);
            
            var eventTypeName = typeof(OrderPlaced).FullName;
            var eventType = Type.GetType(eventTypeName);
            //var deserialized = (OrderPlaced) JsonSerializer.DeserializeFromString(json, eventType);

            var streamName = "AggregateA-5a0ca157-3ca9-4a9e-b755-e93ea0fa7aa1";
            
            var eventsSlice = connection.ReadStreamEventsForwardAsync(streamName, StreamPosition.Start, 4096, false).Result;
            var events = eventsSlice.Events.Select(e => e.Event).ToList();
            Assert.NotNull(events);
            Assert.NotEmpty(events);
            Assert.Equal(2, events.Count);
            Assert.Equal(typeof(OrderPlaced).FullName, events[0].EventType);
            //var deserialized = (OrderPlaced)EventStoreEventSourcedRepository<AggregateA, Guid>.Deserialize(events[0]);
            //var versioned = events.Select(e => EventStoreEventSourcedRepository<AggregateA, Guid>.Deserialize(e)).ToList();

            //var deserialized = (IVersionedEvent) EventStoreEventSourcedRepository<AggregateA, Guid>.Deserialize(events[0]);
            // JsonSerializer.DeserializeFromString(events[0].Data.FromUtf8Bytes(), eventType);
            //var deserialized = (OrderPlaced) EventStoreEventSourcedRepository<AggregateA, Guid>.Deserialize(events[0]);
             //Assert.NotNull(deserialized);
             //Assert.NotNull(deserialized.CustomerName);
             //Assert.Equal(deserialized.CustomerName, "John Doe");

            var repo = new EventStoreEventSourcedRepository<AggregateA, Guid>(connection);
            var id = Guid.Parse("5a0ca157-3ca9-4a9e-b755-e93ea0fa7aa1");
            var agg = repo.FindAsync(id).Result;
            Assert.NotNull(agg);
            Assert.Equal(agg.Id, id);
            // Assert.Equal(typeof(OrderPlaced), deserialized.GetType());
            // //Assert.Equal(1, deserialized.Version);
            // Assert.Equal("Jane Doe", deserialized.CustomerName);
            //Assert.Equal(deserialized.)
            /* 
            var uri = @"tcp://admin:changeit@localhost:1113/";
            var connection = EventStoreConnection.Create(new Uri(uri));
            connection.ConnectAsync().Wait();
            var repository = new EventStoreEventSourcedRepository<AggregateA, Guid>(connection);
            
            var id = Guid.Parse("3c560e76-238e-4f33-a8ec-4994dadfe6bb");

            var agg = repository.FindAsync(id).Result;
            
            Assert.NotNull(agg);
            Assert.Equal(1, agg.Version);
            Assert.Equal(id, agg.Id); */
        }

       /*  [Fact]
        public void Serialize_ReturnsEventData()
        {
            var @event = new OrderPlaced{OrderId = Guid.NewGuid(), CustomerName = "John Doe", SourceId = Guid.NewGuid().ToString(), Version = 1};
            
            var data = EventStoreEventSourcedRepository<AggregateWithoutValidConstructor, Guid>.Serialize(@event);

            Assert.True(data.IsJson);
            var json = JsonSerializer.SerializeToString(@event);            
            Assert.Equal(@event.GetType().FullName, data.Type);
            Assert.Equal(json.ToUtf8Bytes().ToString(), data.Data.ToString());
            
        }

        [Fact]
        public void Deerialize_ReturnsEventObject()
        {
            var @event = new OrderPlaced{OrderId = Guid.NewGuid(), CustomerName = "John Doe", SourceId = Guid.NewGuid().ToString(), Version = 1};
            
            var data = EventStoreEventSourcedRepository<AggregateWithoutValidConstructor, Guid>.Serialize(@event);
            var byteArray = data.Data;

            Assert.True(data.IsJson);
            var json = JsonSerializer.SerializeToString(@event);            
            Assert.Equal(@event.GetType().FullName, data.Type);
            Assert.Equal(json.ToUtf8Bytes().ToString(), data.Data.ToString());
            
        } */
    }
}