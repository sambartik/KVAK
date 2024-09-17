# KVAKProtocol specification v1

This custom binary protocol is designed to be used in the client-server communications between the server and clients of the KVAK in-memory store.

Each packet is prefixed with a fixed 10 byte header that may be followed by a payload of the packet. All multibyte values are strictly BigEndian.

___

## Packet header

Each packet is prefixed by this header and its format stays the same.

It includes the version of the protocol used, a packet type number that distinguishes between different types of packets and its payload size.  Additionally, It includes a packet ID that will be used to match response packets to the originating request packets that were sent in response to.

### Header fields

| Field name       | Type | Size    | Description                                                                                                                                                                                                       |
|------------------|------|---------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Protocol version | byte | 1 byte  | Represents the protocol specification version being used, currently this must always be `0x01`                                                                                                                    |
| Packet ID        | uint | 4 bytes | Will be used to match response packets to the originating request packets that were sent in response to. Value `0x00` is reserved for meta packets, i.e. packets that don't have a corresponding response packet. |
| Packet type      | byte | 1 bytes | Distinguishes between different types of packets                                                                                                                                                                  |
| Payload length   | uint | 4 bytes | Length of the payload that is followed after this header. `0x00` for no payload.                                                                                                                                  |


```
 0                   1                   2                   3
 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
| Protocol ver. |                   Packet ID                   |
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
|  Packet type  |         Payload Length        |
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
```

---

## Payload

Each type of packet has a different payload. The following section will describe each of them individually.

### Authentication request packet (type `0x01`)
Used to authenticate to the server. Most of the request packets require to be authenticated prior sending them, otherwise the server will turn them down by sending an error.

The payload consists of a UTF-8 encoded string:

| Field name | Type   | Size      | Description                                                                                                             |
|------------|--------|-----------|-------------------------------------------------------------------------------------------------------------------------|
| API Key    | string | Arbitrary | The key used to authenticate to the server. _Note: the length of the string can be calculated from the payload length._ |


### Authentication response packet (type `0x02`)
Used to authenticate to the server. Most of the request packets require to be authenticated prior sending them, otherwise the server will turn them down by sending an error.

| Field name | Type | Size   | Description                            |
|------------|------|--------|----------------------------------------|
| Status     | byte | 1 byte | `0x01` for success, `0x00` for failure |


### Data request packet (type `0x03`)
**The sender must be authenticated prior sending this packet!** Represents a request for data, given a key.

| Field name | Type   | Size      | Description                             |
|------------|--------|-----------|-----------------------------------------|
| Key        | string | Arbitrary | The key under which the data is stored. |


### Data response packet (type `0x04`)
Returns the requested data.

1. _In case of a failure:_

| Field name | Type | Size   | Description                              |
|------------|------|--------|------------------------------------------|
| Status     | byte | 1 byte | The value is `0x00`, indicating failure. |
| ErrorCode  | byte | 1 byte | Indicates what went wrong.               |

Following ErrorCodes are supported by the current protocol spec.:


| ErrorCode | Description                                  |
|-----------|----------------------------------------------|
| `0x01`    | Authentication is required                   |
| `0x02`    | The requested key was not found in the store |
| `0x03`    | An unexpected error occured                  |

The rest of the packet payload shall be ignored.

2. _In case of a success:_

| Field name   | Type | Size   | Description                                                                                                                                                                             |
|--------------|------|--------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Status       | byte | 1 byte | The value is `0x01`, indicating success, `0x02` meaning the key was not found and any other for a general failure.  **In case of a failure, the rest of the payload shall be ignored.** |
| DataUnitType | byte | 1 byte | A number representing the type of the stored value. Used by the client to make sense of the data.                                                                                       |
| Data         | byte | -      | The rest of the packet contains the binary data of the stored value type. Note: these bytes are still BigEndian.                                                                        |

Following DataUnitTypes are supported by the current protocol spec.:

| DataUnitType | Name   | Size    | Description                                 |
|--------------|--------|---------|---------------------------------------------|
| `0x01`       | String | -       | UTF-8 encoded string with an arbitrary size |
| `0x02`       | Int    | 4 bytes | A signed integer in two's complement        |
| `0x03`       | Bool   | 1 byte  | True = `0x01`, False = `0x00`               |

### Data addition request packet (type `0x05`)
**The sender must be authenticated prior sending this packet!** Represents a request to associate passed data with a given key.

| Field name   | Type   | Size      | Description                                               |
|--------------|--------|-----------|-----------------------------------------------------------|
| KeyLength    | int    | 4 bytes   | The length of the key passed in the next field.           |
| Key          | string | Arbitrary | The key under which the data is stored.                   |
| DataUnitType | byte   | 1 byte    | Number representing the type of data that will be stored. |
| Data         | bytes  | Arbitrary | The key to be used to retrieve the data                   |


### Data addition response packet (type `0x06`)

In case of a success:

| Field name   | Type | Size   | Description                                                                                                                          |
|--------------|------|--------|--------------------------------------------------------------------------------------------------------------------------------------|
| Status       | byte | 1 byte | The value is `0x01`, indicating success, any other for failure.  **In case of a failure, the rest of the payload shall be ignored.** |

In case of a failure:

| Field name | Type | Size   | Description                                                                                                                          |
|------------|------|--------|--------------------------------------------------------------------------------------------------------------------------------------|
| Status     | byte | 1 byte | The value is `0x01`, indicating success, any other for failure.  **In case of a failure, the rest of the payload shall be ignored.** |
| ErrorCode  | byte | 1 byte | Error code values are described in an earlier section                                                                                |


### Data removal request packet (type `0x07`)
**The sender must be authenticated prior sending this packet!** Represents a request to remove data associated to the given key.

| Field name   | Type   | Size      | Description                                               |
|--------------|--------|-----------|-----------------------------------------------------------|
| Key          | string | Arbitrary | The key under which the data is stored.                   |

### Data removal response packet (type `0x08`)
Represents a response to a request to delete data associated with a given key.

In case of a success:

| Field name   | Type | Size   | Description                                                                                                                          |
|--------------|------|--------|--------------------------------------------------------------------------------------------------------------------------------------|
| Status       | byte | 1 byte | The value is `0x01`, indicating success, any other for failure.  **In case of a failure, the rest of the payload shall be ignored.** |

In case of a failure:

| Field name | Type | Size   | Description                                                                                                                          |
|------------|------|--------|--------------------------------------------------------------------------------------------------------------------------------------|
| Status     | byte | 1 byte | The value is `0x01`, indicating success, any other for failure.  **In case of a failure, the rest of the payload shall be ignored.** |
| ErrorCode  | byte | 1 byte | Error code values are described in an earlier section                                                                                |
