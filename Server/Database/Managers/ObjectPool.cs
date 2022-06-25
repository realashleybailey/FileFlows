using System.Collections.Concurrent;

namespace FileFlows.Server.Database.Managers;

public class ObjectPool<T>
{
    private readonly ConcurrentBag<T> Objects;
    private readonly Func<T> ObjectGenerator;
    private int ObjectsTaken;
    private int Max;

    public ObjectPool(int max, Func<T> objectGenerator)
    {
        this.Max = max;
        this.ObjectGenerator = objectGenerator ?? throw new ArgumentNullException(nameof(objectGenerator));
        this.Objects = new ConcurrentBag<T>();
    }

    public async Task<T> Get()
    {
        while (ObjectsTaken >= Max)
        {
            await Task.Delay(25);
        }

        lock (this)
        {
            ++ObjectsTaken;
        }

        return Objects.TryTake(out T item) ? item : ObjectGenerator();
    }



    public void Return(T item)
    {
        lock (this)
        {
            if (--ObjectsTaken < 0)
                ObjectsTaken = 0;
        }

        Objects.Add(item);
    }

    public int Count => ObjectsTaken;

}