using System.Collections.Concurrent;

public class InMemoryCacheService<T> : ICacheProvider<T>
{

    private readonly ConcurrentDictionary<string, T> objsById = new ConcurrentDictionary<string, T>();

    public T Get(string _id) {
        objsById.TryGetValue(_id, out var obj);
        return obj;
    }

    public void Set(string _id, T obj) {
        objsById.AddOrUpdate(_id, obj, (key, oldValue) => obj);
    }

    public void Clear(string _id) {
        objsById.TryRemove(_id, out _);
    }


}