namespace KVAK.Networking.Protocol;

/// <summary>
/// Represents a client that can connect to a connection listener at the other end
/// <seealso cref="IProtocolListener"/>
/// </summary>
interface IProtocolSessionClient
{
    /// <summary>
    /// Connects with the protocol session server
    /// </summary>
    /// <returns>A protocol session</returns>
    static abstract IProtocolSession Connect();
}