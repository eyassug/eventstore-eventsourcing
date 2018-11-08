namespace EventStore.EventSourcing
{
    using Fano.CQRS.EventSourcing;

    public static class IEventSourcedExtensions
    {
        public static string GetEventStoreStream<TKey>(this IEventSourced<TKey> aggregate)
        {   
            return string.Format("{0}-{1}", aggregate.GetType().Name, aggregate.Id);
        }
    }
}