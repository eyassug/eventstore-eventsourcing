namespace Fano.FPS.Domain.Tests
{
    using System;
    using Fano.CQRS.EventSourcing;
    using EventStore.EventSourcing;
    using Xunit;

    public class ExtensionsTests
    {
        public class AggregateA : EventSourced<Guid>
        {
            AggregateA(Guid id) : base(id)
            {

            }
            public AggregateA(): this(Guid.NewGuid()) {}
        }


        [Fact]
        public void GetEventStoreStream_ReturnsClassName_hyphen_Id()
        {
            var entity = new AggregateA();

            var actual = entity.GetEventStoreStream();
            var expected = string.Format("AggregateA-{0}", entity.Id);

            Assert.Equal(expected, actual);
        }
    }
}