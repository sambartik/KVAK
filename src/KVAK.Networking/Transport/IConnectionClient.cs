namespace KVAK.Networking.Transport;


/// <summary>
/// Represents a client that can connect to a connection listener at the other end
/// <seealso cref="IConnectionListener"/>
/// </summary>
interface IConnectionClient
{
    /// <summary>
    /// Connects with the connection listener
    /// </summary>
    /// <returns>A connection</returns>
    static abstract IConnection Connect();
}