using System.Buffers.Binary;

namespace KVAK.Networking.Protocol.KVAKProtocol;

class UnknownPacketType : Exception
{
    public UnknownPacketType(string? message) : base(message)
    {
    }

    public UnknownPacketType(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}

class ProtocolVersionMismatch : Exception
{
    public ProtocolVersionMismatch(string? message) : base(message)
    {
    }

    public ProtocolVersionMismatch(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Represents a header of the KVAKProtocol protocol. Its size is fixed and it has the same format for all packets.
/// </summary>
public class FixedPacketHeader
{
    public uint PacketId;
    public readonly PacketType Type;
    public readonly uint PayloadLength;

    /// <summary>
    /// The fixed size of the header in bytes
    /// </summary>
    public static readonly int Size = 10;
    
    public static readonly byte ProtocolVersion = 0x01;

    public FixedPacketHeader(uint packetId, PacketType type, uint payloadLength)
    {
        PacketId = packetId;
        Type = type;
        PayloadLength = payloadLength;
    }
    
    /// <summary>
    /// Constructs a packet header from bytes.
    /// </summary>
    /// <param name="bytes">The byte array containing the header binary data</param>
    /// <returns>A corresponding instance of <see cref="FixedPacketHeader"/></returns>
    /// <exception cref="ArgumentException">In case the supplied data size does not match the size of the header exactly</exception>
    /// <exception cref="UnknownPacketType">When the type of the packet is not recognised</exception>
    /// <exception cref="ProtocolVersionMismatch">When the received packet protocol version does not match the implementation packet version</exception>
    public static FixedPacketHeader FromByteArray(byte[] bytes)
    {
        if (bytes.Length != Size)
        {
            throw new ArgumentException($"The argument does not have the correct length for a packet header: {Size}");
        }
        
        if (bytes[0] != ProtocolVersion)
        {
            throw new ProtocolVersionMismatch($"Protocol version mismatch. Received {bytes[0]}, expected {ProtocolVersion}");
        }
        
        uint packetId = BinaryPrimitives.ReadUInt32BigEndian(new ReadOnlySpan<byte>(bytes, 1, 4));
        byte packetTypeByte = bytes[5];
        uint payloadLength = BinaryPrimitives.ReadUInt32BigEndian(new ReadOnlySpan<byte>(bytes, 6, 4));

        if (!Enum.IsDefined(typeof(PacketType), packetTypeByte))
        {
            throw new UnknownPacketType($"The packet type {packetTypeByte} is not recognised as valid.");
        }

        var packetType = (PacketType) packetTypeByte; 
        
        return new FixedPacketHeader(packetId, packetType, payloadLength);
    }

    /// <summary>
    /// Decodes the packet header into bytes (Big-Endian)
    /// </summary>
    /// <returns>Packet header in a byte array</returns>
    public byte[] Decode()
    {
        byte[] bytes = new byte[Size];

        bytes[0] = ProtocolVersion;
        BinaryPrimitives.WriteUInt32BigEndian(new Span<byte>(bytes, 1, 4), PacketId);
        bytes[5] = (byte)Type;
        BinaryPrimitives.WriteUInt32BigEndian(new Span<byte>(bytes, 6, 4), PayloadLength);
        
        return bytes;
    }
}

/// <summary>
/// An abstract implementation of the <see cref="IPacket"/> interface for the KVAKProtocol packets meant to be extended.
/// </summary>
public abstract class BaseKVAKPacket : IPacket
{
    #pragma warning disable CS8618 // Disable the warning for uninitialized non-nullable fields
    public FixedPacketHeader Header;

    /// <summary>
    /// Decodes the payload of the packet to a byte array
    /// </summary>
    /// <returns>Packet's payload as a byte array</returns>
    protected abstract byte[] DecodePayload();

    public byte[] Decode()
    {
        return Header.Decode().Concat(DecodePayload()).ToArray();
    }

    public uint GetId()
    {
        return Header.PacketId;
    }

    public void SetId(uint id)
    {
        Header.PacketId = id;
    }
}