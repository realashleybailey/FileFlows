using System.Collections.Concurrent;

namespace FileFlows.Server.Database.Managers;

/// <summary>
/// A object pool that will reuse the same objects and will only allow a maximum number
/// of instances to be created
/// </summary>
/// <typeparam name="T"></typeparam>
public class ObjectPool<T>
{
    private readonly ConcurrentBag<T> Objects;
    private readonly Func<T> ObjectGenerator;
    private int ObjectsTaken;
    private readonly int Max;

    /// <summary>
    /// Creates an object pool
    /// </summary>
    /// <param name="max">the maximum number of items</param>
    /// <param name="objectGenerator">a function that creates new instances</param>
    /// <exception cref="ArgumentNullException">if objectGenerator is null</exception>
    public ObjectPool(int max, Func<T> objectGenerator)
    {
        this.Max = max;
        this.ObjectGenerator = objectGenerator ?? throw new ArgumentNullException(nameof(objectGenerator));
        this.Objects = new ConcurrentBag<T>();
    }

    /// <summary>
    /// Gets an item from of type
    /// Will wait if at maximum number of items
    /// </summary>
    /// <returns>an item instance</returns>
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

    /// <summary>
    /// Disposes of an item so it cannot be reused
    /// </summary>
    /// <param name="item">the item to dispose of</param>
    public void DisposeOf(T item)
    {
        lock (this)
        {
            --ObjectsTaken;
        }
        if(item is IDisposablePooledObject disposablePooledObject)
            disposablePooledObject.DisposePooledObject();
        else if(item is IDisposable disposable)
            disposable.Dispose();
    }


    /// <summary>
    /// Returns an object back to the pool
    /// </summary>
    /// <param name="item">the item to return</param>
    public void Return(T item)
    {
        lock (this)
        {
            if (--ObjectsTaken < 0)
                ObjectsTaken = 0;
        }

        Objects.Add(item);
    }

    /// <summary>
    /// Gets the number of objects taken
    /// </summary>
    public int Count => ObjectsTaken;
}

/// <summary>
/// Interface thats is called when disposing of the object from the ObjectPooler
/// </summary>
public interface IDisposablePooledObject
{
    /// <summary>
    /// Called to permanently dispose of an object
    /// </summary>
    void DisposePooledObject();
} 