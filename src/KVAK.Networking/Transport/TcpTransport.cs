using System.Net;
using System.Net.Sockets;

namespace KVAK.Networking.Transport;

/// <inheritdoc />
/// <summary>
/// An implementation of a single connection over TCP protocol.
/// </summary>
/// <remarks>
/// Implemented via .Net TcpClient API
/// </remarks>
/// <seealso cref="TcpConnectionListener"/>
public class TcpConnection : BaseConnection
{
    private bool _disposed = false;
    
    private readonly TcpClient _tcpClient;
    private readonly NetworkStream _stream;
    
    public TcpConnection(TcpClient tcpClient)
    {
        _tcpClient = tcpClient;
        _stream = tcpClient.GetStream();
    }

    public override async Task Send(byte[] data)
    {
        if (_disposed) throw new ObjectDisposedException("The object has been already disposed!");
        if (!_tcpClient.Connected) throw new ConnectionClosed("The connection has been closed!");

        try
        {
            await _stream.WriteAsync(data).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            throw new ConnectionException("An error occured while sending data through a connection", e);
        }
    }

    public override async Task<ArraySegment<byte>> Receive(int size = 1024)
    {
        if (_disposed) throw new ObjectDisposedException("The object has been already disposed!");
        if (!_tcpClient.Connected) throw new ConnectionClosed("The connection has been closed!");
        
        var buffer = new byte[size];
        int readSize;
        try
        {
            readSize = await _stream.ReadAsync(buffer).ConfigureAwait(false);
        }
        catch (IOException e)
        {
            throw new ConnectionClosed("The connection has been closed!", e);
        }
        catch (Exception e)
        {
            throw new ConnectionException("There was an error reading data from a connection", e);
        }
        
        if (readSize == 0) throw new ConnectionClosed("The connection has been closed!");
        
        return new ArraySegment<byte>(buffer, 0, readSize);
    }
    
    protected override void Dispose(bool disposing)
    {
        if (_disposed) return;

        // Disposing of managed resources
        if (disposing)
        {
            _stream.Dispose();
            _tcpClient.Dispose();
        }

        _disposed = true;
    }
}

/// <inheritdoc />
/// <summary>
/// An implementation of a connection listener communicating over TCP.
/// </summary>
/// <remarks>
/// Implemented via .Net TcpListener API
/// </remarks>
public class TcpConnectionListener : BaseConnectionListener
{
    private bool _disposed = false;
    
    private TcpListener? _listener;
    
    public override async Task Listen(int port = 3000)
    {
        Console.WriteLine($"Listening on port {port}");
        
        var ipEndPoint = new IPEndPoint(IPAddress.Any, port);
        _listener = new TcpListener(ipEndPoint);

        try
        {
            _listener.Start();

            while (!_disposed)
            {
                var tcpClient = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);
                var connection = new TcpConnection(tcpClient);
                OnNewConnection(connection);
            }
        }
        catch (Exception e)
        {
            throw new ConnectionListenerException("An unexpected error occured while listening for connections", e);
        }
        finally
        {
            Stop();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (_disposed) return;

        // Disposing of managed resources
        if (disposing)
        {
            if (_listener != null)
            {
                _listener.Dispose();
                _listener = null;
            }
        }

        _disposed = true;
    }
}

public class TcpConnectionClient
{
    public static IConnection Connect(IPAddress ipAddress, int port)
    {
        var tcpClient = new TcpClient();
        tcpClient.Connect(new IPEndPoint(ipAddress, port));
        
        return new TcpConnection(tcpClient);
    }
}