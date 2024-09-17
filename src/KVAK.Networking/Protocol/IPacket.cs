namespace KVAK.Networking.Protocol;

/// <summary>
/// Represents any packet of a binary protocol.
/// </summary>
public interface IPacket
{
    /// <summary>
    /// Decodes the packet into bytes that can be sent over the network
    /// </summary>
    /// <returns>A byte array that represents the packet</returns>
    byte[] Decode();
    
    /// <summary>
    /// Gets the unique identifier of the packet during the span of a single session.
    /// The value gets dynamically assigned by the corresponding <see cref="BaseProtocolSession"/> either during the sending or receipt of the packet, see <see cref="SetId"/>.
    /// The value 0 is specifically reserved for packets that have yet not been dynamically assigned ID by the <see cref="BaseProtocolSession"/> and/or for meta packets.
    /// </summary>
    /// <returns>A unique integer</returns>
    uint GetId();

    /// <summary>
    /// Sets the ID of the packet.
    /// </summary>
    void SetId(uint id);
}

/// <summary>
/// A marker interface to mark a request packet classes
/// </summary>
public interface IRequestPacket : IPacket { }

/// <summary>
/// A marker interface to mark a response packet classes
/// </summary>
public interface IResponsePacket : IPacket  {}