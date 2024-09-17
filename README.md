# 🦆 KVAK: Key-Value Access Kernel

This repository is home to **KVAK**, a simple in-memory store that works over the TCP/IP network and has a programmable C# interface!

___

This project consists of three main components:
- **KVAK Server** that actually manages the store itself and serves clients
- **KVAK Developer Interface** that connects to the server for data operations from code
- **KVAK Interactive Interface** that connects to the server for data operations interactively from standard input

Network communication between server and clients is done over TCP via a custom binary protocol: *KVAKProtocol*. You can read up on its [specification file](src/KVAK.Networking/protocol_specification.md).

## ✨ Features
- Efficient operations with **logarithmic** big-O complexity thanks to **(a,b)-trees**!
- **Async** heavy implementation, keeping the server more reliant under load
- C# programming interface

# ▶️ How to get started:

This section helps you get familiar with **KVAK**!

## ⚙️ KVAK Server

Server starts off with an empty data store implemented by an effective data structure - (a,b)-tree. It proceeds to make this data structure available to mutate by remote clients over the network. That way, clients can store their data on server and later manipulate them effectively!

### Installation

To run a server, firstly, have .NET 8 SDK environment ready on your machine. Having a `dotnet` CLI tool is a pre-requisite.

1. Clone the repo: `git clone https://github.com/sambartik/KVAK.git`
2. Change directories: `cd KVAK/src`
3. Build the projects: `dotnet build --configuration Release`
4. Navigate over to `KVAK.Core/bin/Release/net8.0` subdirectory and grab your executable
5. You can now start the executable and start the server. You can configure the server via environmental variables. For more info, visit section bellow.

### Configuration

The server is configured via following environment variables:

| Variable     | Type   | Default | Description                                                 | Required |
|--------------|--------|---------|-------------------------------------------------------------|----------|
| KVAK_API_KEY | string | -       | This is a way to authorize network clients with the server. | Yes      |
| KVAK_A       | int    | 2       | The A parameter for the (a,b)-tree                          | No       |
| KVAK_B       | int    | 3       | The B parameter for the (a,b)-tree                          | No       |
| KVAK_PORT    | int    | 3000    | TCP port for the server to listen on                        | No       |

Setting environmental variables for the server can be done multiple ways and differ on what Operating System you are using.

For example, assuming the server's binary `KVAKServer.exe` is in the current working directory, to start a server on Windows using CMD with custom a,b parameters you execute this in your command line:
```
set KVAK_API_KEY=MY-CUSTOM-API-KEY && set KVAK_A=2 && set KVAK_B=4 && KVAKServer.exe
```

That's it! Server does everything on its own from the point it was started with the configuration in the environment.

## 🤖 KVAK Developer Interface:

This is the component that the end user (a developer) is intended to work with very often. It is in form of a C# library that exposes a C# interface that can communicate with a server via network.

The interface is purely asynchronous. To get started, first connect to your server by creating a new instance of the client with appropriate information:

### 0. Create a new instance 
```c#
var client = new KVAKClient("127.0.0.1", "MY-KVAK-API-KEY", 3000);
```

Creating a new instance of the client does not automatically trigger network connection to establish.
That's why before doing anything else, you need to connect to the KVAKServer. To do it, just:

### 1. Connect to the server

```c#
await client.Connect();
```

Note: This call is asynchronous, as the rest of the API is. Keep that in mind.

### Storing your first data into KVAK

Let's say you have a piece of data somewhere in your application: `string dearData = "this is a string that holds some data";`

To store it in KVAK under the key `dear-data`, you can:

### ➕ 2. Add a new key-value pair

```c#
await client.Add("dear-data", dearData);
```

#### Other operations

In a similar fashion, KVAK supports these standard operations:

### 3. 🗑️ Remove a key-value pair

```c#
await client.Remove("dear-data");
```

### 4. 🔍 Find a key-value pair by key

```c#
await client.Find("dear-data");
```

This operation is special however, as its return type is dynamic. It will return the data type of the actual stored data. However, if the key doesn't exist in the store, it returns null.

## 📖 KVAK Interactive:
This component allows users to interact with KVAK Server through a command line standard input. It allows users without any developer experience to access the data and feel the power of KVAK.

### Installation

To run a server, firstly, have .NET 8 SDK environment ready on your machine. Having a `dotnet` CLI tool is a pre-requisite.

1. Clone the repo: `git clone https://github.com/sambartik/KVAK.git`
2. Change directories: `cd KVAK/src`
3. Build the projects: `dotnet build --configuration Release`
4. Navigate over to `KVAK.Interactive/bin/Release/net8.0` subdirectory and grab your executable
5. You can now start the executable and the interactive mode starts.

### Usage:

Upon starting the interactive mode, you are freely able to interact with the server by issuing commands through the standard input. Commands are case-insensitive and the arguments can't contain a whitespace.

Firstly, you need to connect to the server before being able to do anything useful. Make sure you have your server turned on.

#### 0. Help command

```
HELP
```

Prints the help message.


#### 1. Exit command

```
EXIT
```

Exits the interactive mode.

#### 2. Connect command

```
CONNECT [IP Address] [Port] [API Key]
```

Connects to the KVAK Server.

#### 3. Add command

```
ADD [Key] [Value]
```

Adds the key with the value to the server. **Requires authentication.**

#### 3. Find command

```
FIND [Key]
```

Finds the value associated with the given key. **Requires authentication.**


#### 4. Remove command

```
REMOVE [Key]
```

Removes the key-value pair from the store. **Requires authentication.**


## ❗ Data type support

As of right now, the data you can store in the store is limited to:
- string
- int
- bool

These exactly coincide with c# types with the same name.
