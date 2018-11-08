namespace EventStore.EventSourcing
{
    using EventStore.ClientAPI;
    using System.Threading.Tasks;
    
    public interface IEventStoreRepository
    {
        Task<EventData[]> GetAllEventsAsync(string stream);

        Task AppendToStreamAsync(string stream, long expectedVersion, EventData[] events);
    }
}