namespace KVAK.Networking.Transport;

class ConnectionClosed : Exception
{
    public ConnectionClosed(string? message) : base(message)
    {
    }

    public ConnectionClosed(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}

class ConnectionException : Exception
{
    public ConnectionException(string? message) : base(message)
    {
    }

    public ConnectionException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}

public delegate void NewDataHandler(ArraySegment<byte> data);

public delegate void ConnectionClosedHandler(Exception e);
/// <summary>
/// Represents a single established low-level network connection that is TCP-like,
/// i.e. one that is reliable and ensures the data get to the other side in correct order. <br/>
/// It deals with raw bytes and low-level transport protocols.
/// </summary>
/// <seealso cref="IConnectionListener"/>
public interface IConnection : IDisposable
{
    /// <summary>
    /// Sends binary data <i>asynchronously</i> over the established connection.
    /// </summary>
    /// <param name="data">Bytes to be sent</param>
    /// <returns>A task that completes when the data are sent</returns>
    /// <exception cref="ObjectDisposedException">When the object was already disposed</exception>
    /// <exception cref="ConnectionClosed">When the connection is no longer open</exception>
    /// <exception cref="ConnectionException">When an unexpected error occurs during the sending of data</exception>
    Task Send(byte[] data);
    
    /// <summary>
    /// Receives at most <paramref name="size"/> bytes.
    /// </summary>
    /// <param name="size">The maximum of data to return at once, defaults to 1024 bytes</param>
    /// <returns>A task that completes when data has been received and as its result has a binary array segment of size at most <paramref name="size"/> with the data.</returns>
    /// <exception cref="ObjectDisposedException">When the object was already disposed</exception>
    /// <exception cref="ConnectionClosed">When the connection is no longer open</exception>
    /// <exception cref="ConnectionException">When an unexpected error occurs during the receipt of data</exception>
    Task<ArraySegment<byte>> Receive(int size = 1024);
    
    /// <summary>
    /// This method must be called before any calls to the <see cref="Receive"/> method, otherwise the returned tasks will not be completed ever.
    /// This starts the internal loop to handle incoming data, handles the disposal of the connection.
    /// </summary>
    /// <returns>A task that completes when no longer watching for new data.</returns>
    /// <exception cref="ObjectDisposedException">When the object was already disposed</exception>
    /// <exception cref="ConnectionException">When an unexpected error occurs during the receipt of data</exception>
    Task PollData();
    
    /// <summary>
    /// Closes the connection. This action is destructive and the connection can't be reopened.
    /// </summary>
    void Close();

    /// <summary>
    /// Checks if the connections has been closed
    /// </summary>
    /// <returns>A boolean indicating connectivity</returns>
    bool IsClosed();
    
    /// <summary>
    /// Fired when the connection has received a new chunk of data
    /// </summary>
    event NewDataHandler NewData;
    
    /// <summary>
    /// Fired when the connection has been closed for whatever reason
    /// </summary>
    event ConnectionClosedHandler ConnectionClosed;
}

/// <inheritdoc />
/// <remarks>
/// Acts as a base class to derive from when implementing a new network connection listener for a given transport protocol.
/// </remarks>
/// <seealso cref="BaseConnectionListener"/>
public abstract class BaseConnection : IConnection
{
    public event NewDataHandler? NewData;
    public event ConnectionClosedHandler? ConnectionClosed;

    public bool Closed { get; private set; } = false;

    public BaseConnection()
    {
        ConnectionClosed += _ => Closed = true;
    }
    
    public async Task PollData()
    {
        try
        {
            while (true)
            {
                var data = await Receive(1024).ConfigureAwait(false);
                NewData?.Invoke(data);
            }
        }
        catch (ConnectionClosed e) // Bubbles up when the connection has been closed
        {
            ConnectionClosed?.Invoke(e);
        }
        catch (Exception e)
        {
            ConnectionClosed?.Invoke(e);
            throw;
        }
        finally
        {
            Close();
        }
    }

    public abstract Task Send(byte[] data);
    public abstract Task<ArraySegment<byte>> Receive(int size = 1024);

    public void Close()
    {
        Dispose(true);
    }
    
    public bool IsClosed()
    {
        return Closed;
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