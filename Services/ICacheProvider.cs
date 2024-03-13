public interface ICacheProvider<T>
{
    public T Get(string _id);
    public void Set(string _id, T obj);
    public void Clear(string _id);
}
