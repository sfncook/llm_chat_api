using Newtonsoft.Json;
using StackExchange.Redis;

public class RedisCacheService<T> : ICacheProvider<T>
{
    private readonly ConnectionMultiplexer redis;
    
    public RedisCacheService() {
        redis = ConnectionMultiplexer.Connect("salesbot.redis.cache.windows.net:6380,password=...,ssl=True,abortConnect=False");
    }

    public T Get(string _id) {
        IDatabase db = redis.GetDatabase();
        string objStr = db.StringGet(_id);
        return JsonConvert.DeserializeObject<T>(objStr);
    }

    public void Set(string _id, T obj) {
        IDatabase db = redis.GetDatabase();
        string objStr = JsonConvert.SerializeObject(obj);
        db.StringSet(_id, objStr);
    }

    public void Clear(string _id) {
        IDatabase db = redis.GetDatabase();
        db.KeyDelete(_id);
    }


}