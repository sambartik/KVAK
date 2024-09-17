# Developers guide to KVAK

This document contains the developers guide to KVAK explaining the code's internals and the overall architecture. For regular user's guide please check out [README.md](README.md).

## Architecture

The entire project is split into 4 subprojects for better division of concerns:
- KVAK.Client
- KVAK.Core
- KVAK.Networking
- KVAK.Interactive

For network communication between the server and clients we are utilizing a custom binary TCP/IP protocol [KVAK Protocol](src/KVAK.Networking/protocol_specification.md) that is based on the server-client networking architecture.

To handle a large (concurrent) load, we heavily utilize [async approach](https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/).

___

## KVAK.Networking
This component deals with networking and is used by both the KVAK.Client and KVAK.Core projects. Specifically it handles: 
- underlying low-level socket networking
- handling new connections
- establishing new connections
- parsing chunked data into new packets

We're using the following terminology:
- **Transport**: Generally refers to any low-level networking protocol that handles the transmission of data between 2 endpoints in chunks, reliably and ensures the ordering of chunks to be the same on the recipient and as it was on the sending end. _Example: TCP, Websockets_
- **Connection**: Refers to an interface to a **Transport** protocol, that can receive chunks and send chunks
- **Protocol**: Refers to a high-level application protocol. It is above **Transport** in the networking stack. Deals with whole packets as defined by application. _Example: KVAKProtocol_
- **Session**: Similar as **Connection**, but for **Protocol**. It's an interface to a Protocol that can send and receive packets.

Most of the API of this component is event based to better align with the asynchronous nature of project. Connection listeners classes fire events when a new connection has been established, Connection classes fire an event when a new data chunk has been received and so on...

You can think of `Protocol`/`Session` as application-protocol-aware `Transport`/`Connection`.

### `TcpTransport.cs`

This file shows how to implement a common interface to a _low-level_ transport protocol: TCP. It includes all necessary classes:
- `TcpConnection` - handles data manipulation, both sending & receiving
- `TcpConnectionListener` - handles creating new instances of `TcpConnection` class when a new connection has been established
- `TcpConnectionClient` - a simple wrapper around `TcpConnection`, to be able to create a new instance of it, given connection details

Defining a new interface to another low-level transport protocol such as Websockets would require a similar setup:
- A respective `Connection` class that inherits from `BaseConnection`
- A respective `ConnectionListener` class that inherits from `BaseConnectionListener
- A respective `Client` class that has a static method to create a new instance of the respective `Connection` class, given connection details

### `KVAKProtocol.cs`

This file shows the working of implementation of a _high-level_ application protocol: KVAKProtocol. It includes these classes:
- `KVAKProtocolSession` - handles parsing packets from raw binary data and sending packet class instances
- `KVAKTcpListener` - handles creating new instances of `KVAKProtocolSession` by utilizing the `TcpConnectionListener` interface to be able to listen for TCP connections
- `KVAKProtocolTcpClient` - a wrapper around `KVAKProtocolSession` that sends and receives data over TCP connection

The packet parsing in the `KVAKProtocolSession` class is actually decomposed into the `KVAKPacketFactory` class for better separation of concerns. It works by reading the header, which is fixed for all types of KVAK packets and then finding the right packet class that can handle it and create a new instance. These classes are defined in the `KVAKPacketDefinitions.cs` file.


### Adding new KVAK packets

To extend the KVAK protocol by adding new packets, you need to first extend the protocol specification [here](src/KVAK.Networking/protocol_specification.md).

The new packet type needs to have a new unique packet type number based on the edited protocol specification. To reflect it in code, you will need to add it into the `PacketType` enum with the matching value.

Then, you create a new packet class in the `KVAKPacketDefinitions.cs` file. It needs to extend the `BaseKVAKPacket` class and thus implement the `DecodePayload` static method. Additionally, the class needs a constructor that accepts the header and its raw binary payload: `FixedPacketHeader header` and `byte[] payload`.

With both the enum entry added and new class defined, the last step is to register it with the packet factory in the `KVAKPacketFactory.cs` by adding additional line in the `RegisterPacketTypes` static method:

```c#
RegisterNewPacketType<NewPacket>(PacketType.NewPacket);
```

Where `NewPacket` is the class of the new packet.

___

## KVAK.Core

Implements an effective data structure to store key-value data, makes it safe for concurrency and proceeds to make it available by creating a server that listens on a port, utilizing the `KVAK.Networking` project.

### `ABTree.cs`

It implements a data structure called an (a,b)-tree. It is a generalised search tree with external nodes that can have multiple keys in nodes. 

```
By definition (a,b)-tree, where a >= 2, b >= 2a-1 is a general search tree for which following holds true:
- The root has at least 2 and at most b children, other nodes at least a and at most b children
- All external nodes are in the same depth 
```

### Algorithm overview

How are the operations implemented, algorithmically?

#### 1. Add

We continue down the search tree similarly as in a Binary Search Tree, except we consider compare with multiple keys in the node. We continue until we get into an external node or find a node that has a matching key - in such case we reassign the data(value) associated with the key and stop.

If the key was not in the tree, we stop at an external node. We try to add the key to the parent node of the external node we just got to. If the node has _overflowed_ - it has too much keys, we split that node into two by lifting the middle key into its parent, creating 2 new nodes with the resulting halves and attaching them to the parent.

We then check whether the node where we inserted the middle key overflows and if so, we continue fixing it. If the root node overflows, we just create a new root node.

#### 2. Find

We start at the root node and check if the node contains the key we are looking for. If not, based on the key comparison choose a child node to go to next. And we do it until we either find the node with the key or until we end up in the external node, meaning the key does not exist in the tree.

#### 3. Remove

We first search for the node in the tree. If it does not contain the key, we end early.

In case the key is a node that is not on the last internal layer, we transform the problem by finding its successor the same way as in a binary search tree and deleting that one instead. By definition, it must have been on the last internal layer. We replace the key we wanted to delete originally with the successor key.

Either way, we try to delete a key in the last internal layer. If the node _underflows_ - it has too few keys, we try to fix the problem:
- If its sibling node has minimum number of keys allowed, we merge the sibling nodes and add the pivot key they are connected with to their shared parent in the middle of the newly created merged node. We continually check the condition of min. keys up the path, since we have taken the pivot key form the parent.
- Otherwise, we kind of rotate keys. Assuming the sibling node is to the right of the pivot key, we take the first key of the sibling and put it in place of the pivot key. We add the pivot key to the end of the keys of the node we deleted one from. The number of keys in the parent hasn't changed, so we don't have to check other nodes.

### `ConcurrentStore.cs`

Makes the key-value store concurrency safe by utilizing locking in such way that multiple readers can read at the same time, but writers are exclusive and need to wait until readers have finished reading.

___

## KVAK.Client

Utilizes `KVAK.Networking` to connect to the KVAK Server and expose a simple C# developer interface.

We are using the `dynamic` return type for returning a data type based on the stored value on the server.

Example usage can be seen in the `TestingPlayground` project.

___

## KVAK.Interactive

Utilizes `KVAK.Client` behind the scenes to expose an interactive interface for people without any development experience. Easily controllable through standard input commands.

---

## Possible extensions / ideas into the future

- Adding extra data types
- HTTP API
- Web interactive interface, similar to `KVAK.Interactive`
- Developer API for other languages

# Subjective conclusion

Our initial vision was to implement a simplified project similar to [Redis](https://redis.io/) using a balanced search tree as its internal storage solution. 

The networking part of the implementation turned out to be the most challenging because of the necessity to handle and actively manipulate data coming from multiple sources in a packet based communication, while still maintaining the ease of extensibility in the future.
Looking back, we are content with choosing the async model instead of the threads-based one as that helped us focus more on the problem of packet parsing and offloaded the thread pooling and context switching to the .NET runtime.

Thanks to our previous experience of implementing a packet based networking in Python, we learned to handle matching response packets to their initial request packets by utilizing a special field in the packet header - the packet ID.

In the future we would like to take a peak at handling the locking of the tree more granularly by locking individual nodes, instead of the whole tree, but because this was our first time dealing with shared resources, we opted out for a more simplified solution to avoid issues such as deadlocks.

This project made us appreciate Redis and its whole ecosystem of libraries as the complexity of maintaining and implementing of a protocol and sharing data across multiple systems while still ensuring data are interpreted correctly. 