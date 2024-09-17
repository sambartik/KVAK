namespace KVAK.Networking.Transport;

class ConnectionListenerException : Exception
{
    public ConnectionListenerException(string? message) : base(message)
    {
    }

    public ConnectionListenerException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}

public delegate void NewConnectionHandler(IConnection connection);
public delegate void ConnectionLostHandler(IConnection connection);

/// <summary>
/// Acts as a network listener and manages the creation of new <see cref="IConnection"/> instances.
/// </summary>
public interface IConnectionListener : IDisposable
{ 
    /// <summary>
    /// Starts listening on all network interfaces on the specified port.
    /// </summary>
    /// <param name="port">Port to listen on</param>
    /// <returns>Task that isn't completed until the listener stops listening</returns>
    /// <exception cref="ConnectionListenerException">An unexpected error when trying to start listening for connections</exception>
    Task Listen(int port = 3000);
    
    /// <summary>
    /// Stops the listener.
    /// </summary>
    /// <remarks>
    /// This does not dispose any connections. It just disposes of the listener itself.
    /// </remarks>
    void Stop();
    
    /// <summary>
    /// Fired when a new connection has been established. 
    /// </summary>
    event NewConnectionHandler NewConnection;
    
    /// <summary>
    /// Fired in case a connection has been closed gracefully or unexpectedly.
    /// </summary>
    event ConnectionLostHandler ConnectionLost;
}

/// <inheritdoc />
/// <remarks>
/// Acts as a base class to derive from when implementing a new network connection listener for a given transport protocol. <br/>
/// A corresponding derived class of <see cref="BaseConnection"/> must be also implemented that will be passed as argument to <see cref="OnNewConnection"/> method.
/// </remarks>
public abstract class BaseConnectionListener : IConnectionListener
{
    public event NewConnectionHandler? NewConnection;
    public event ConnectionLostHandler? ConnectionLost;
    
    public abstract Task Listen(int port = 3000);
    
    /// <summary>
    /// This method should be called when a new connection has been established. 
    /// </summary>
    /// <param name="baseConnection">An object representing the connection</param>
    protected virtual void OnNewConnection(BaseConnection baseConnection)
    {
        NewConnection?.Invoke(baseConnection);
    }

    public void Stop()
    {
        Dispose(true);
    }
    
    public void Dispose()
    {
        // Dispose of managed & unmanaged resources.
        Dispose(true);
        // Suppress finalization.
        GC.SuppressFinalize(this);
    }
    
    protected abstract void Dispose(bool disposing);
}