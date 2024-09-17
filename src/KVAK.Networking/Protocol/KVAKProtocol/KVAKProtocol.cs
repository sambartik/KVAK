using System.Net;
using KVAK.Networking.Transport;

namespace KVAK.Networking.Protocol.KVAKProtocol;

// An example definition of custom application protocol
public class KVAKProtocolSession : BaseProtocolSession
{
    private FixedPacketHeader? _nextPacketHeader = null;
    
    public KVAKProtocolSession(IConnection connection) : base(connection) { }

    protected override IPacket? TryGetNextPacketFromBuffer()
    {
        try
        {
            if (_nextPacketHeader == null && Buffer.Length >= FixedPacketHeader.Size)
            {
                var header = FixedPacketHeader.FromByteArray(Buffer.RemoveFirst(FixedPacketHeader.Size));
                _nextPacketHeader = header;
            }

            if (_nextPacketHeader != null && Buffer.Length >= _nextPacketHeader.PayloadLength)
            {
                var packet = KVAKPacketFactory.CreatePacket(_nextPacketHeader,
                    Buffer.RemoveFirst((int)_nextPacketHeader.PayloadLength));
                _nextPacketHeader = null;
                return packet;
            }
        }
        catch (Exception e)
        {
            _nextPacketHeader = null;
            throw new PacketParsingException("An error occured while parsing the next packet", e);
        }

        return null;
    }
}

/// <summary>
/// A listener implementing the KVAKProtocol over TCP transport
/// </summary>
public class KVAKTcpListener : BaseProtocolSessionTcpListener
{
    protected override KVAKProtocolSession CreateNewSession(BaseConnection connection)
    {
        return new KVAKProtocolSession(connection);
    }
}

/// <summary>
/// A client manager that can connect to a KVAK server and return a protocol session
/// </summary>
public class KVAKProtocolTcpClient
{
    public static IProtocolSession Connect(IPAddress ipAddress, int port)
    {
        return new KVAKProtocolSession(TcpConnectionClient.Connect(ipAddress, port));
    }
}