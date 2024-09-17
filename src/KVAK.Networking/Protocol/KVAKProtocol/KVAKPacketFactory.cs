namespace KVAK.Networking.Protocol.KVAKProtocol;

public enum PacketType : byte
{
    AuthRequest = 0x01,
    AuthResponse = 0x02,
    DataRequest = 0x03,
    DataResponse = 0x04,
    DataAdditionRequest = 0x05,
    DataAdditionResponse = 0x06,
    DataRemovalRequest = 0x07,
    DataRemovalResponse = 0x08
}

public enum DataUnitType : byte
{
    String = 0x01,
    Int = 0x02,
    Bool = 0x03
}

public enum ErrorCode : byte
{
    AuthRequired = 0x01,
    KeyNotFound = 0x02,
    UnexpectedError = 0x03
}

public enum PacketStatus : byte
{
    Success = 0x01,
    Failure = 0x02
}

public class KVAKPacketFactory
{
    private delegate BaseKVAKPacket PacketCreatorHandler(FixedPacketHeader header, byte[] payload);
    private static readonly Dictionary<PacketType, PacketCreatorHandler> CreatorHandlers = new();

    
    static KVAKPacketFactory()
    {
        RegisterPacketTypes();
    }
    
    // Registers all packet types
    private static void RegisterPacketTypes()
    {
        RegisterNewPacketType<AuthRequestPacket>(PacketType.AuthRequest);
        RegisterNewPacketType<AuthResponsePacket>(PacketType.AuthResponse);
        RegisterNewPacketType<DataRequestPacket>(PacketType.DataRequest);
        RegisterNewPacketType<DataResponsePacket>(PacketType.DataResponse);
        RegisterNewPacketType<DataAdditionRequestPacket>(PacketType.DataAdditionRequest);
        RegisterNewPacketType<DataAdditionResponsePacket>(PacketType.DataAdditionResponse);
        RegisterNewPacketType<DataRemovalRequestPacket>(PacketType.DataRemovalRequest);
        RegisterNewPacketType<DataRemovalResponsePacket>(PacketType.DataRemovalResponse);
    }
    
    // Register a packet type to its handler delegate that instantiates it based on header and payload
    private static void RegisterNewPacketType<TPacketClass>(PacketType type) where TPacketClass : BaseKVAKPacket
    {
        if (!CreatorHandlers.TryAdd(type, Handler))
        {
            throw new ArgumentException("A packet creator handler for the specified type already exists!");
        }

        BaseKVAKPacket Handler(FixedPacketHeader header, byte[] payload) => ((TPacketClass)Activator.CreateInstance(typeof(TPacketClass), header, payload)!)!;
    }
    
    /// <summary>
    /// Creates a new instance of a packet class based on the header and payload of the packet
    /// </summary>
    /// <remarks>Given that we have a FixedPacketHeader object, the packet mush have a valid PacketType</remarks>
    /// <param name="header">The header of the packet</param>
    /// <param name="payload">Binary payload data</param>
    /// <returns>The instance of the packet</returns>
    /// <exception cref="ArgumentException">When the passed packet has a valid type, but it was not registered</exception>
    public static BaseKVAKPacket CreatePacket(FixedPacketHeader header, byte[] payload)
    {
        CreatorHandlers.TryGetValue(header.Type, out var handler);

        if (handler == null)
        {
            throw new ArgumentException("The packet type of the passed packet has not been registered!");
        }
        
        return handler(header, payload);;
    }
}