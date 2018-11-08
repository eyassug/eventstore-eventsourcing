namespace EventStore.EventSourcing
{
    using System;
    using Fano.CQRS.EventSourcing;
    using EventStore.ClientAPI;
    using System.Threading.Tasks;
    using System.Net;

    public class EventStoreEventSourcedRepository<T, TKey> : IAsyncEventSourcedRepository<T, TKey> where T : IEventSourced<TKey>
    {
        private readonly IEventStoreRepository _repository;

        public EventStoreEventSourcedRepository(IEventStoreRepository repository) => this._repository = repository;
        public Task<T> FindAsync(TKey id)
        {
            throw new NotImplementedException();
        }

        public Task<T> GetAsync(TKey id)
        {
            throw new NotImplementedException();
        } 

        public Task SaveAsync(T eventSourced, string correlationId)
        {
            throw new NotImplementedException();
        }
    }
}