using KVAK.Core.Store;
using KVAK.Networking.Protocol;
using KVAK.Networking.Protocol.KVAKProtocol;

namespace KVAK.Core;

/// <summary>
/// Owns networking listener and the actual store. Acts as an orchestrator between these components.
/// </summary>
public class KVAKServer
{
      private readonly string _apiKey;
      private readonly KVAKTcpListener _listener;
      private readonly ConcurrentStore _store;
      private readonly int _port;
      /// <summary>
      /// Contains open sessions and their auth status
      /// </summary>
      private readonly Dictionary<IProtocolSession, bool> _sessions = new();
      
      public KVAKServer(KVAKTcpListener listener, string apiKey, ConcurrentStore store, int port)
      {
            _apiKey = apiKey;
            _listener = listener;
            _store = store;
            _port = port;
            _listener.ListenerNewPacket += OnNewPacket;
            _listener.ListenerSessionEnded += OnSessionEnded;
            _listener.ListenerNewSession += OnNewSession;
      }
      
      /// <summary>
      /// Each time a new session is made with the server, we keep track of it and register an event handler to receive packets from that session.
      /// </summary>
      /// <param name="session">The session that was just established</param>
      private void OnNewSession(IProtocolSession session)
      {
            Console.WriteLine("New session established!");
            _sessions.TryAdd(session, false);
            session.StartPolling();
      }

      /// <summary>
      /// Handles new packets received from a connected session
      /// </summary>
      /// <param name="session">Session that sent the packet</param>
      /// <param name="packet">The packet that the session sent</param>
      private async void OnNewPacket(IProtocolSession session, IPacket packet)
      {
            try
            {
                  if (packet is not BaseKVAKPacket kvakPacket)
                  {
                        Console.WriteLine("Received an invalid packet! Dropping the packet!");
                        return;
                  }

                  switch (kvakPacket.Header.Type) 
                  {
                        case PacketType.AuthRequest:
                              await HandleAuthRequest(session, (AuthRequestPacket) kvakPacket).ConfigureAwait(false);
                              break;
                        case PacketType.DataRequest:
                              await HandleDataRequest(session, (DataRequestPacket) kvakPacket).ConfigureAwait(false);
                              break;
                        case PacketType.DataAdditionRequest:
                              await HandleDataAdditionRequest(session, (DataAdditionRequestPacket) kvakPacket).ConfigureAwait(false);
                              break;
                        case PacketType.DataRemovalRequest:
                              await HandleDataRemovalRequest(session, (DataRemovalRequestPacket) kvakPacket).ConfigureAwait(false);
                              break;
                        default:
                              throw new Exception($"Unknown packet type: {kvakPacket.Header.Type}");
                  }
            }
            catch(Exception ex)
            {
                  Console.WriteLine($"An error occured while handling a new packet: {ex.Message}");
            }
      }
      
      /// <summary>
      /// Handles expected/unexpected session end
      /// </summary>
      /// <param name="session">Session that ended</param>
      /// <param name="e">An error that triggered the end</param>
      private void OnSessionEnded(IProtocolSession session, Exception e)
      {
            Console.WriteLine("Session ended.");
            _sessions.Remove(session);
      }

      private async Task HandleAuthRequest(IProtocolSession session, AuthRequestPacket authRequestPacket)
      {
            Console.WriteLine("Handling auth request");
            if (authRequestPacket.ApiKey == _apiKey)
            {
                  Console.WriteLine("Session authenticated");
                  _sessions[session] = true;
                  await session.SendResponsePacket(authRequestPacket, new AuthResponsePacket(PacketStatus.Success)).ConfigureAwait(false);
            }
            else
            {
                  Console.WriteLine("Session not authenticated");
                  await session.SendResponsePacket(authRequestPacket, new AuthResponsePacket(ErrorCode.AuthRequired)).ConfigureAwait(false);
            }
      }
      
      private async Task HandleDataRequest(IProtocolSession session, DataRequestPacket dataRequestPacket)
      {
            Console.WriteLine("Handling data request");
            
            if (_sessions[session] == false)
            {
                  await session.SendResponsePacket(dataRequestPacket, new DataResponsePacket(ErrorCode.AuthRequired)).ConfigureAwait(false);
                  return;
            }

            try
            {
                  var result = await _store.Find(dataRequestPacket.Key).ConfigureAwait(false);
            
                  if (result == null)
                  {
                        await session.SendResponsePacket(dataRequestPacket, new DataResponsePacket(ErrorCode.KeyNotFound)).ConfigureAwait(false);
                  }
                  else
                  {
                        await session.SendResponsePacket(dataRequestPacket, new DataResponsePacket(result.Value.Type, result.Value.Data)).ConfigureAwait(false);
                  }
            }
            catch (Exception)
            {
                  await session.SendResponsePacket(dataRequestPacket, new DataResponsePacket(ErrorCode.UnexpectedError)).ConfigureAwait(false);
            }
      }

      private async Task HandleDataAdditionRequest(IProtocolSession session, DataAdditionRequestPacket dataAdditionRequestPacket)
      {
            Console.WriteLine("Handling data addition request");
            if (_sessions[session] == false)
            {
                  await session.SendResponsePacket(dataAdditionRequestPacket, new DataAdditionResponsePacket(ErrorCode.AuthRequired)).ConfigureAwait(false);
                  return;
            }

            try
            {
                  await _store.Add(dataAdditionRequestPacket.Key, new StoreDataUnit(dataAdditionRequestPacket.DataUnitType, dataAdditionRequestPacket.Data)).ConfigureAwait(false);
                  await session.SendResponsePacket(dataAdditionRequestPacket, new DataAdditionResponsePacket(PacketStatus.Success)).ConfigureAwait(false);
            }
            catch (Exception)
            {
                  await session.SendResponsePacket(dataAdditionRequestPacket, new DataAdditionResponsePacket(ErrorCode.UnexpectedError)).ConfigureAwait(false);
            }
      }
      
      private async Task HandleDataRemovalRequest(IProtocolSession session, DataRemovalRequestPacket dataRemovalRequestPacket)
      {
            Console.WriteLine("Handling data removal request");
            if (_sessions[session] == false)
            {
                  await session.SendResponsePacket(dataRemovalRequestPacket, new DataAdditionResponsePacket(ErrorCode.AuthRequired)).ConfigureAwait(false);
                  return;
            }
            
            try
            {
                  await _store.Remove(dataRemovalRequestPacket.Key).ConfigureAwait(false);
                  await session.SendResponsePacket(dataRemovalRequestPacket, new DataRemovalResponsePacket(PacketStatus.Success)).ConfigureAwait(false);
            }
            catch (Exception)
            {
                  await session.SendResponsePacket(dataRemovalRequestPacket, new DataRemovalResponsePacket(ErrorCode.UnexpectedError)).ConfigureAwait(false);
            }
      }
      
      /// <summary>
      /// Start listening for connections
      /// </summary>
      public Task Run()
      {
            return _listener.Listen(_port);
      }

      /// <summary>
      /// Stop listening for connections
      /// </summary>
      public void Stop()
      {
            _listener.Stop();
      }
}