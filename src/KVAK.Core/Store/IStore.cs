using KVAK.Networking.Protocol.KVAKProtocol;

namespace KVAK.Core.Store;

/// <summary>
/// Holds information about the stored (binary) dataUnit
/// </summary>
public struct StoreDataUnit
{
    public readonly DataUnitType Type;
    public readonly byte[] Data;

    public StoreDataUnit(DataUnitType type, byte[] data)
    {
        Type = type;
        Data = data;
    }
}

/// <summary>
/// Stores <see cref="StoreDataUnit"/> and associates it with a string key.
/// It has set-like properties and all operations are at most logarithmic.
/// </summary>
public interface IBinaryStore
{
    /// <summary>
    /// Stores a <see cref="StoreDataUnit"/> in the store under the given key. If the key already exists, it overwrites whatever data it had associated.
    /// </summary>
    /// <param name="key">A unique key</param>
    /// <param name="dataUnit">Data to be associated with the key</param>
    void Add(string key, StoreDataUnit dataUnit);

    /// <summary>
    /// Removes <see cref="StoreDataUnit"/> associated with the given key. If the key does not exist, it succeeds nonetheless.
    /// </summary>
    /// <param name="key">A key to remove</param>
    void Remove(string key);

    /// <summary>
    /// Finds the associated <see cref="StoreDataUnit"/>  within the structure
    /// </summary>
    /// <param name="key">Key to find data by</param>
    /// <returns>Returns <see cref="StoreDataUnit"/> associated with the key or null if there is no such dataUnit</returns>
    StoreDataUnit? Find(string key);
}


