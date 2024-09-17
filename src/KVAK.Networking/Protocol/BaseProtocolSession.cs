using System.Collections.Concurrent;
using KVAK.Networking.Transport;

namespace KVAK.Networking.Protocol;

class PacketParsingException : Exception
{
    public PacketParsingException(string? message) : base(message)
    {
    }

    public PacketParsingException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}

public delegate void NewPacketHandler(IPacket packet);
public delegate void SessionClosedHandler(Exception e);
/// <summary>
/// It is a wrapper around <see cref="IConnection"/> that is protocol-aware. <br/>
/// In contrast to <see cref="IConnection"/> it deals with packets of a given application protocol.
/// </summary>
/// <seealso cref="IProtocolListener"/>
public interface IProtocolSession : IDisposable
{
    /// <summary>
    /// Sends a packet without awaiting a response. It is a fire-and-forget.
    /// </summary>
    /// <param name="packet">Packet to be sent</param>
    /// <returns>A task that completes when the packet was sent</returns>
    /// <exception cref="ObjectDisposedException">When the object was already disposed</exception>
    /// <exception cref="ConnectionClosed">When the underlying connect has been closed</exception>
    /// <exception cref="ConnectionException">An unexpected error while sending the data over the connection</exception>
    Task SendPacket(IPacket packet);
    
    /// <summary>
    /// Sends a response packet to a passed request packet. Internally it sets the packet ID of the response to match the one in request packet header.
    /// </summary>
    /// <param name="originalRequestPacket">Packet we are responding to</param>
    /// <param name="responsePacket">Packet we are responding with</param>
    /// <returns>Task that completes when the packet has been sent</returns>
    Task SendResponsePacket(IPacket originalRequestPacket, IPacket responsePacket);
    
    /// <summary>
    /// Sends a request packet asynchronously and waits for a corresponding response packet.
    /// </summary>
    /// <param name="packet">Packet to be sent</param>
    /// <returns>Task with the resulting response packet that completes when it is received</returns>
    /// <exception cref="ObjectDisposedException">When the object was already disposed</exception>
    /// <exception cref="ConnectionClosed">When the underlying connect has been closed</exception>
    /// <exception cref="ConnectionException">An unexpected error while sending the data over the connection</exception>
    /// <exception cref="InvalidOperationException">When a collision happens in the packet request id</exception>
    /// <exception cref="OverflowException">When there are too many pending requests</exception>
    Task<IResponsePacket> SendRequestPacket(IRequestPacket packet);

    /// <summary>
    /// Should be called to start receiving data, after setting up all necessary event handlers
    /// </summary>
    Task StartPolling();
    
    /// <summary>
    /// Ends the session and underlying transport connection. This action is desctructive and the session can't be reopened.
    /// </summary>
    void End();
    
    /// <summary>
    /// Fired for all incoming packets, including the response packets from <see cref="SendRequestPacket"/>.
    /// </summary>
    /// <remarks>In case of a response packet resulting from a call to <see cref="SendRequestPacket"/>, it should fire after the corresponding task completes.</remarks>
    event NewPacketHandler NewPacket;
    
    /// <summary>
    /// Fired when the session is being closed for whatever reason.
    /// </summary>
    event SessionClosedHandler SessionEnded;
}

/// <inheritdoc />
/// <remarks>
/// Acts as a base class to inherit from when defining a new application protocol.
/// </remarks>
public abstract class BaseProtocolSession : IProtocolSession
{
    public event NewPacketHandler? NewPacket;
    public event SessionClosedHandler? SessionEnded;
    
    private bool _disposed = false;
    private bool _sessionClosed = false; // Used to prevent firing session closed multiple times because of Dispose() method
    
    /// <summary> Stores the binary data received so far in the order they were received (and sent by the other party) </summary>
    protected readonly DynamicArrayBuffer<byte> Buffer = new();
    /// <summary> The underlying transport connection that is being used by the session </summary>
    protected readonly IConnection Connection;
    // Used to keep track of requests that are currently awaiting a response.
    private readonly ConcurrentDictionary<uint, TaskCompletionSource<IResponsePacket>> _pendingRequests = new();
    private uint _nextRequestId = 0;
    
    public BaseProtocolSession(IConnection connection)
    {
        Connection = connection;
        Connection.NewData += NewChunkHandler;
        Connection.ConnectionClosed += OnConnectionClosed;
        if (connection.IsClosed())
        {
            OnConnectionClosed(new ConnectionClosed("Connection closed prematurely"));
        }
    }

    public Task SendPacket(IPacket packet)
    {
        if (_disposed) throw new ObjectDisposedException("The object has been already disposed!");
        
        return Connection.Send(packet.Decode());
    }

    public Task SendResponsePacket(IPacket originalRequestPacket, IPacket responsePacket)
    {
        responsePacket.SetId(originalRequestPacket.GetId());
        return SendPacket(responsePacket);
    }

    public async Task<IResponsePacket> SendRequestPacket(IRequestPacket packet)
    {
        if (_disposed) throw new ObjectDisposedException("The object has been already disposed!");
        
        // Atomically increase the counter so the request ID is unique
        uint requestId = Interlocked.Increment(ref _nextRequestId);
        
        var responseReceived = new TaskCompletionSource<IResponsePacket>();
        if (!_pendingRequests.TryAdd(requestId, responseReceived))
        {
            throw new InvalidOperationException($"There is already a request pending with the ID: {requestId}");
        }
        
        packet.SetId(requestId);
        
        try
        {
            await Connection.Send(packet.Decode()).ConfigureAwait(false);
            return await responseReceived.Task.ConfigureAwait(false);
        }
        finally
        {
            if (!_pendingRequests.TryRemove(requestId, out _))
            {
                await Console.Error.WriteLineAsync($"There was a problem removing the pending request (id: {requestId}) from dictionary").ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Starts processing received data. Call after adding all needed event handlers, otherwise data might be lost.
    /// </summary>
    /// <returns>Task that completes when no longer can read any more data.</returns>
    public Task StartPolling()
    {
        return Connection.PollData();
    }

    /// <summary>
    /// This method uses the internal data buffer <see cref="Buffer"/> comprised of received binary data so far and tries to translate the into an appropriate packet object.
    /// </summary>
    /// <returns>If there is a parsable packet in the head of the buffer an instance of <see cref="IPacket"/>, otherwise null.</returns>
    /// <exception cref="PacketParsingException">When an error occurs while parsing a packet from the buffer</exception>
    protected abstract IPacket? TryGetNextPacketFromBuffer();
    
    // Each time we receive a new binary chunk of data, we try to decode packets from bytes
    private void NewChunkHandler(ArraySegment<byte> chunk)
    {
        Buffer.Add(chunk);
        try
        {
            IPacket? packet = null;
            while ((packet = TryGetNextPacketFromBuffer()) != null)
            {
                OnNewPacket(packet);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"An error occured while handling a new chunk from transport: {e}");
        }
    }
    
    // When an underlying connection has been closed the session ceases to exist too.
    // This also handles the case when the session itself is disposed first, as that will trigger the closure of connection.
    private void OnConnectionClosed(Exception e)
    {
        if (_sessionClosed) return;
        _sessionClosed = true;
        
        SessionEnded?.Invoke(e);
        End();
    }
    
    private void OnNewPacket(IPacket packet)
    {
        // Check if this is a response to one of the request packets we sent earlier and waited for the response
        if (packet is IResponsePacket responsePacket)
        {
            var id = responsePacket.GetId();

            _pendingRequests.TryGetValue(id, out TaskCompletionSource<IResponsePacket>? responseSignal);

            if (responseSignal == null)
            {
                Console.Error.WriteLine($"Received a response packet, but no request was pending with the id: {id}");
                return;
            }

            if (!responseSignal.TrySetResult(responsePacket))
            {
                throw new InvalidOperationException("Unable to send signal to the originating request sender!");
            }
        }
        
        NewPacket?.Invoke(packet);
    } 
    
    public void End()
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

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            Connection.Dispose();
            Buffer.Dispose();
        }

        _disposed = true;
    }
}
