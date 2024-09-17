using System.Buffers.Binary;
using System.Text;

namespace KVAK.Networking.Protocol.KVAKProtocol;
public sealed class AuthRequestPacket : BaseKVAKPacket, IRequestPacket
{
    public string ApiKey;
    public AuthRequestPacket(string apiKey)
    {
        ApiKey = apiKey; 
        Header = new FixedPacketHeader(0, PacketType.AuthRequest, (uint)DecodePayload().Length);
    }
    
    public AuthRequestPacket(FixedPacketHeader header, byte[] payload)
    {
        ApiKey = Encoding.UTF8.GetString(payload);
        Header = header;
    }

    protected override byte[] DecodePayload()
    {
        return Encoding.UTF8.GetBytes(ApiKey);
    }
}

public sealed class AuthResponsePacket : BaseKVAKPacket, IResponsePacket
{
    public PacketStatus Status;
    public ErrorCode? ErrorCode;
    
    public AuthResponsePacket(ErrorCode errorCode)
    {
        Status = PacketStatus.Failure;
        ErrorCode = errorCode;
        Header = new FixedPacketHeader(0, PacketType.AuthResponse, (uint)DecodePayload().Length);
    }
    
    public AuthResponsePacket(PacketStatus status)
    {
        Status = status;
        Header = new FixedPacketHeader(0, PacketType.AuthResponse, (uint)DecodePayload().Length);
    }
    
    public AuthResponsePacket(FixedPacketHeader header, byte[] payload)
    {
        Status = (PacketStatus)payload[0];

        if (Status != PacketStatus.Success)
        {
            ErrorCode = (ErrorCode)payload[1];
        }
        
        Header = header;
    }

    protected override byte[] DecodePayload()
    {
        return Status == PacketStatus.Success ? [(byte)Status] : [(byte) Status, (byte)ErrorCode!];
    }
}

public sealed class DataRequestPacket : BaseKVAKPacket, IRequestPacket
{
    public string Key;
    public DataRequestPacket(string key)
    {
        Key = key; 
        Header = new FixedPacketHeader(0, PacketType.DataRequest, (uint)DecodePayload().Length);
    }
    
    public DataRequestPacket(FixedPacketHeader header, byte[] payload)
    {
        Key = Encoding.UTF8.GetString(payload);
        Header = header;
    }

    protected override byte[] DecodePayload()
    {
        return Encoding.UTF8.GetBytes(Key);
    }
}

public sealed class DataResponsePacket : BaseKVAKPacket, IResponsePacket
{
    public PacketStatus Status;
    public ErrorCode? ErrorCode;
    public DataUnitType? DataUnitType;
    public byte[]? Data;

    public DataResponsePacket(DataUnitType dataUnitType, byte[] rawValue)
    {
        Status = PacketStatus.Success;
        DataUnitType = dataUnitType;
        Data = rawValue;
        Header = new FixedPacketHeader(0, PacketType.DataResponse, (uint)DecodePayload().Length);
    }
    
    public DataResponsePacket(ErrorCode errorCode)
    {
        Status = PacketStatus.Failure;
        ErrorCode = errorCode;
        Header = new FixedPacketHeader(0, PacketType.DataResponse, (uint)DecodePayload().Length);
    }
    
    public DataResponsePacket(FixedPacketHeader header, byte[] payload)
    {
        Status = (PacketStatus)payload[0];
        
        if (Status == PacketStatus.Success)
        {
            DataUnitType = (DataUnitType)payload[1];
            Data = payload.Skip(2).ToArray();
        }
        else
        {
            ErrorCode = (ErrorCode)payload[1];
        }
        
        Header = header;
    }

    protected override byte[] DecodePayload()
    {
        if (Status == PacketStatus.Success)
        {
            byte[] output = [(byte)Status, (byte)DataUnitType!];
            return output.Concat(Data!).ToArray();
        }
        else
        {
            return [(byte)Status, (byte)ErrorCode!];
        }
    }
}
public sealed class DataAdditionRequestPacket : BaseKVAKPacket, IRequestPacket
{
    public string Key;
    public DataUnitType DataUnitType;
    public byte[] Data;

    public DataAdditionRequestPacket(string key, DataUnitType dataUnitType, byte[] rawValue)
    {
        Key = key;
        DataUnitType = dataUnitType;
        Data = rawValue;
        Header = new FixedPacketHeader(0, PacketType.DataAdditionRequest, (uint)DecodePayload().Length);
    }
    
    public DataAdditionRequestPacket(FixedPacketHeader header, byte[] payload)
    {
        int keyLength = BinaryPrimitives.ReadInt32BigEndian(payload);
        Key = Encoding.UTF8.GetString(payload, 4, (int) keyLength);
        DataUnitType = (DataUnitType)payload[4 + keyLength];
        Data = payload.Skip(4 + keyLength + 1).ToArray();
        
        Header = header;
    }

    protected override byte[] DecodePayload()
    {
        byte[] keyLengthBytes = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(keyLengthBytes, Key.Length);
        
        var output = keyLengthBytes.Concat(Encoding.UTF8.GetBytes(Key)).Append((byte)DataUnitType);
        return output.Concat(Data).ToArray();
    }
}

public sealed class DataAdditionResponsePacket : BaseKVAKPacket, IResponsePacket
{
    public PacketStatus Status;
    public ErrorCode? ErrorCode;

    public DataAdditionResponsePacket(ErrorCode errCode)
    {
        Status = PacketStatus.Failure;
        ErrorCode = errCode;
        
        Header = new FixedPacketHeader(0, PacketType.DataAdditionResponse, (uint)DecodePayload().Length);
    }
    
    public DataAdditionResponsePacket(PacketStatus status)
    {
        Status = status;
        Header = new FixedPacketHeader(0, PacketType.DataAdditionResponse, (uint)DecodePayload().Length);
    }
    
    public DataAdditionResponsePacket(FixedPacketHeader header, byte[] payload)
    {
        Status = (PacketStatus) payload[0]; // nasty.

        if (Status == PacketStatus.Failure)
        {
            ErrorCode = (ErrorCode) payload[1];
        }
        
        Header = header;
    }

    protected override byte[] DecodePayload()
    {
        if (Status == PacketStatus.Success)
        {
            return [(byte)Status];
        }
        else
        {
            return [(byte)Status, (byte)ErrorCode!];
        }
    }
}

public sealed class DataRemovalRequestPacket : BaseKVAKPacket, IRequestPacket
{
    public string Key;

    public DataRemovalRequestPacket(string key)
    {
        Key = key;
        
        Header = new FixedPacketHeader(0, PacketType.DataRemovalRequest, (uint)DecodePayload().Length);
    }
    
    public DataRemovalRequestPacket(FixedPacketHeader header, byte[] payload)
    {
        Key = Encoding.UTF8.GetString(payload);
        
        Header = header;
    }

    protected override byte[] DecodePayload()
    {
        return Encoding.UTF8.GetBytes(Key);
    }
}

public sealed class DataRemovalResponsePacket : BaseKVAKPacket, IResponsePacket
{
    public PacketStatus Status;
    public ErrorCode? ErrorCode;

    public DataRemovalResponsePacket(ErrorCode errCode)
    {
        Status = PacketStatus.Failure;
        ErrorCode = errCode;
        
        Header = new FixedPacketHeader(0, PacketType.DataRemovalResponse, (uint)DecodePayload().Length);
    }
    
    public DataRemovalResponsePacket(PacketStatus status)
    {
        Status = status;
        Header = new FixedPacketHeader(0, PacketType.DataRemovalResponse, (uint)DecodePayload().Length);
    }
    
    public DataRemovalResponsePacket(FixedPacketHeader header, byte[] payload)
    {
        Status = (PacketStatus) payload[0]; // nasty.
        
        if (Status == PacketStatus.Failure)
        {
            ErrorCode = (ErrorCode) payload[1];
        }
        
        Header = header;
    }

    protected override byte[] DecodePayload()
    {
        if (Status == PacketStatus.Success)
        {
            return [(byte)Status];
        }
        else
        {
            return [(byte)Status, (byte)ErrorCode!];
        }
    }
}
