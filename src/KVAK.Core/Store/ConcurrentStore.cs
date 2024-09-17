namespace KVAK.Core.Store;

/// <summary>
/// Handles concurrent access to a <see cref="IBinaryStore"/> instance
/// </summary>
public class ConcurrentStore
{
    private IBinaryStore _store; 
    private int _readerCount = 0;
    private readonly SemaphoreSlim _readerCountLock = new SemaphoreSlim(1, 1);
    private readonly SemaphoreSlim _writerLock = new SemaphoreSlim(1, 1);
    
    public ConcurrentStore(IBinaryStore store)
    {
        _store = store;
    }

    private async Task EnterReadCriticalSection()
    {
        try
        {
            await _readerCountLock.WaitAsync().ConfigureAwait(false); // Make sure _readerCount variable is safe before we read & change it
        
            if (_readerCount == 0)
            {
                await _writerLock.WaitAsync().ConfigureAwait(false); // Block writers until we exit from reading critical section
            }
            _readerCount++;
        }
        finally
        {
            _readerCountLock.Release();
        }
    }
    
    private async Task ExitReadCriticalSection()
    {
        try
        {
            await _readerCountLock.WaitAsync().ConfigureAwait(false); // Make sure _readerCount variable is safe before we read & change it

            _readerCount--;
            if (_readerCount == 0)
            {
                _writerLock.Release(); // Enable
            }
        }
        finally
        {
            _readerCountLock.Release();   
        }
    }

    private Task EnterWriteCriticalSection()
    {
        return _writerLock.WaitAsync();
    }
    
    private void ExitWriteCriticalSection()
    {
        _writerLock.Release();
    }
    
    /// <summary>
    /// Stores a <see cref="StoreDataUnit"/> in the store under the given key. If the key already exists, it overwrites whatever data it had associated.
    /// </summary>
    /// <param name="key">A unique key</param>
    /// <param name="dataUnit">Data to be associated with the key</param>
     public async Task Add(string key, StoreDataUnit dataUnit)
    {
        await EnterWriteCriticalSection().ConfigureAwait(false);
        _store.Add(key, dataUnit);
        ExitWriteCriticalSection();
    }

    /// <summary>
    /// Removes <see cref="StoreDataUnit"/> associated with the given key. If the key does not exist, it succeeds nonetheless.
    /// </summary>
    /// <param name="key">A key to remove</param>
    public async Task Remove(string key)
    {
        await EnterWriteCriticalSection().ConfigureAwait(false);
        _store.Remove(key);
        ExitWriteCriticalSection();
    }

    /// <summary>
    /// Finds the associated <see cref="StoreDataUnit"/>  within the structure
    /// </summary>
    /// <param name="key">Key to find data by</param>
    /// <returns>Returns <see cref="StoreDataUnit"/> associated with the key or null if there is no such dataUnit</returns>
    public async Task<StoreDataUnit?> Find(string key)
    {
        await EnterReadCriticalSection().ConfigureAwait(false);
        var result = _store.Find(key);
        await ExitReadCriticalSection().ConfigureAwait(false);

        return result;
    }
}