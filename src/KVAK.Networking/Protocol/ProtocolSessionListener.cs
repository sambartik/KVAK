using KVAK.Networking.Transport;

namespace KVAK.Networking.Protocol;

public delegate void ListenerNewSessionHandler(IProtocolSession session);
public delegate void ListenerNewPacketHandler(IProtocolSession session, IPacket packet);
public delegate void ListenerSessionEnded(IProtocolSession session, Exception e);
/// <summary>
/// A server that is also protocol-aware and manages and creates new <see cref="IProtocolSession"/> instances from new connections of an internal <see cref="IConnectionListener"/>.
/// </summary>
public interface IProtocolListener : IConnectionListener 
{
    /// <summary>
    /// Fired when a new session has been established from an accepted incoming connection.
    /// </summary>
    public event ListenerNewSessionHandler ListenerNewSession;
    
    /// <summary>
    /// Fired when one of the connected sessions receives a new packet
    /// </summary>
    public event ListenerNewPacketHandler ListenerNewPacket;
    
    /// <summary>
    /// Fired when session that has previously connected has ended.
    /// </summary>
    public event ListenerSessionEnded ListenerSessionEnded;
}

/// <summary>
/// A base class for custom application protocol listeners that use TCP connections as communication transport.
/// </summary>
/// <typeparam name="TSession">A subtype of <see cref="IProtocolSession"/> defining application protocol logic</typeparam>
/// <seealso cref="TcpConnectionListener"/>
public abstract class BaseProtocolSessionTcpListener : TcpConnectionListener, IProtocolListener
{
    public event ListenerNewSessionHandler? ListenerNewSession;
    public event ListenerNewPacketHandler? ListenerNewPacket;
    public event ListenerSessionEnded? ListenerSessionEnded;

    protected override void OnNewConnection(BaseConnection baseConnection)
    {
        base.OnNewConnection(baseConnection);
        if (!baseConnection.IsClosed())
        {
            var session = CreateNewSession(baseConnection);
            // Sugar-code, make it easier for the consumer of this class, they could have done the same.
            session.NewPacket += packet => ListenerNewPacket?.Invoke(session, packet);
            session.SessionEnded += e => ListenerSessionEnded?.Invoke(session, e);
            ListenerNewSession?.Invoke(session);
        }
    }
    
    /// <summary>
    /// Create a new <see cref="IProtocolSession"/> object from the given connection.
    /// </summary>
    /// <param name="connection">The underlying connection the session is communicating over</param>
    /// <returns>A session object</returns>
    protected abstract IProtocolSession CreateNewSession(BaseConnection connection);
    
}