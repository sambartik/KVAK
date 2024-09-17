using System.Buffers.Binary;
using System.Net;
using System.Text;
using KVAK.Networking.Protocol;
using KVAK.Networking.Protocol.KVAKProtocol;

namespace KVAK.Client;

/// <summary>
/// This is a client meant to be used by the end-user to access the KVAKProtocol Server key-value store.
/// </summary>
public class KVAKClient
{
    private IProtocolSession? _session;
    private readonly string _ipAddress;
    private readonly int _port;
    private readonly string _apiKey;
    
    public KVAKClient(string ipAddress, string apiKey, int port = 3000)
    {
        _ipAddress = ipAddress;
        _apiKey = apiKey;
        _port = port;
    }

    /// <summary>
    /// Connects to the remote server and authenticates via API key.
    /// </summary>
    /// <exception cref="InvalidOperationException">If this instance has already successfully connected before</exception>
    /// <exception cref="Exception">When failed to successfully initiate connection and authenticate</exception>
    public async Task Connect()
    {
        if (_session != null)
        {
            throw new InvalidOperationException("Already connected");
        }

        try
        {
            _session = KVAKProtocolTcpClient.Connect(IPAddress.Parse(_ipAddress), _port);
            _ = _session.StartPolling();
            var response = (AuthResponsePacket) await _session.SendRequestPacket(new AuthRequestPacket(_apiKey)).ConfigureAwait(false);

            if (response.Status != PacketStatus.Success)
            {
                throw new Exception($"Failed to authenticate with the server: {Enum.GetName(response.ErrorCode ?? ErrorCode.UnexpectedError)}");
            }
        }
        catch (Exception e)
        {
            _session = null;
            throw new Exception($"Failed to connect to the server: {e.Message}");
        }
    }

    private async Task AddBase(string key, DataUnitType type, byte[] data)
    {
        if (_session == null)
        {
            throw new InvalidOperationException("Not connected");
        }

        try
        {
            var originalResponse = await _session.SendRequestPacket(new DataAdditionRequestPacket(key, type, data)).ConfigureAwait(false);
            var response = (DataAdditionResponsePacket) originalResponse;

            if (response.Status != PacketStatus.Success)
            {
                throw new Exception($"Failed to store the key-value pair: {Enum.GetName(response.ErrorCode ?? ErrorCode.UnexpectedError)}");
            }
        }
        catch (Exception e)
        {
            throw new Exception($"Failed to communicate with the server: {e.Message}");
        }
    }
    
    /// <summary>
    /// Adds a string value to the store and associates it with the key
    /// </summary>
    /// <param name="key">Key</param>
    /// <param name="value">String data</param>
    /// <exception cref="InvalidOperationException">If this instance has already successfully connected before</exception>
    /// <exception cref="Exception">When failed to successfully initiate connection and authenticate</exception>
    /// <returns>Task that completes when the network communication is over</returns>
    public Task Add(string key, string value)
    {
        return AddBase(key, DataUnitType.String, Encoding.UTF8.GetBytes(value));
    }
    
    /// <summary>
    /// Adds a integer value to the store and associates it with the key
    /// </summary>
    /// <param name="key">Key</param>
    /// <param name="value">Integer data</param>
    /// <exception cref="InvalidOperationException">If this instance has already successfully connected before</exception>
    /// <exception cref="Exception">When failed to successfully initiate connection and authenticate</exception>
    /// <returns>Task that completes when the network communication is over</returns>
    public Task Add(string key, int value)
    {
        byte[] rawData = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(rawData, value);
        return AddBase(key, DataUnitType.Int, rawData);
    }
    
    /// <summary>
    /// Adds a boolean value to the store and associates it with the key
    /// </summary>
    /// <param name="key">Key</param>
    /// <param name="value">Boolean data</param>
    /// <exception cref="InvalidOperationException">If this instance has already successfully connected before</exception>
    /// <exception cref="Exception">When failed to successfully initiate connection and authenticate</exception>
    /// <returns>Task that completes when the network communication is over</returns>
    public Task Add(string key, bool value)
    {
        return AddBase(key, DataUnitType.Bool, BitConverter.GetBytes(value));
    }

    /// <summary>
    /// Finds the data associated with the given key in the store.
    /// </summary>
    /// <param name="key">Key under which is the requested data stored</param>
    /// <exception cref="InvalidOperationException">If this instance has already successfully connected before</exception>
    /// <exception cref="Exception">When failed to successfully initiate connection and authenticate</exception>
    /// <returns>Dynamically typed data, null if not found</returns>
    public async Task<dynamic?> Find(string key)
    {
        if (_session == null)
        {
            throw new InvalidOperationException("Not connected");
        }

        try
        {
            var originalResponse = await _session.SendRequestPacket(new DataRequestPacket(key)).ConfigureAwait(false);
            var response = (DataResponsePacket) originalResponse;

            if (response.Status != PacketStatus.Success && response.ErrorCode == ErrorCode.KeyNotFound)
            {
                return null;
            }
            
            if (response.Status != PacketStatus.Success)
            {
                throw new Exception($"There was an error while finding the key-value pair: {Enum.GetName(response.ErrorCode ?? ErrorCode.UnexpectedError)}");
            }

            dynamic typedData;
            switch (response.DataUnitType)
            {
                case DataUnitType.String:
                    typedData = Encoding.UTF8.GetString(response.Data!);
                    break;
                case DataUnitType.Int:
                    typedData = BinaryPrimitives.ReadInt32BigEndian(response.Data!);
                    break;
                case DataUnitType.Bool:
                    typedData = BitConverter.ToBoolean(response.Data!);
                    break;
                default:
                    throw new Exception($"Failed to find the data type from the server: {response.DataUnitType}");
            }
            
            return typedData;
        }
        catch (Exception e)
        {
            throw new Exception($"Failed to communicate with the server: {e.Message}");
        }
    }

    /// <summary>
    /// Removes the data from the store, given a key
    /// </summary>
    /// <param name="key">Key under which the data are stored under</param>
    /// <exception cref="InvalidOperationException">If not connected</exception>
    /// <exception cref="Exception">If the operation goes unsuccessful</exception>
    public async Task Remove(string key)
    {
        if (_session == null)
        {
            throw new InvalidOperationException("Not connected");
        }

        try
        {
            var response = (DataRemovalResponsePacket) await _session.SendRequestPacket(new DataRemovalRequestPacket(key)).ConfigureAwait(false);

            if (response.Status != PacketStatus.Success)
            {
                throw new Exception($"Failed to remove the key-value pair: {Enum.GetName(response.ErrorCode ?? ErrorCode.UnexpectedError)}");
            }
        }
        catch (Exception e)
        {
            throw new Exception($"Failed to communicate with the server: {e.Message}");
        }
    }
}