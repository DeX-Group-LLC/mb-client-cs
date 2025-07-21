# Message Broker C# WebSocket Client (MBClient.CS)

A C# library for reliable WebSocket communication with the Message Broker server. Features automatic reconnection, heartbeat monitoring, request/response handling, and event-based communication.

## Features

- 🔄 Automatic reconnection with configurable delay
- 💓 Heartbeat monitoring for connection health
- 🤝 `Task<T>`-based request/response pattern
- 📡 Event-based message handling using `SingleEmitter`
- 📊 Connection state monitoring and events
- ⚡ Efficient message serialization
- 📝 Fully documented with XML comments

## Installation

This library is intended to be used as a NuGet package. Once published, you can install it using the .NET CLI:

```bash
dotnet add package MBClient.CS
```

## Quick Start

```csharp
using MBClient.CS.Core;
using MBClient.CS.types;
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        // Create a client instance
        var client = new WebSocketClient(new ClientConfig
        {
            Url = "ws://localhost:8000",
            Reconnect = new ReconnectConfig { Enabled = true }
        });

        // Listen for connection events
        client.Connected.On(() => Console.WriteLine("Connected to server!"));
        client.Disconnected.On(() => Console.WriteLine("Disconnected from server."));
        client.Error.On((error) => Console.WriteLine($"An error occurred: {error.Message}"));

        // Connect to the server
        await client.Connect();

        // Make a request
        try
        {
            var response = await client.Request("my.topic", new { some = "data" });
            Console.WriteLine($"Response payload: {response.Payload}");
        }
        catch (Exception error)
        {
            Console.WriteLine($"Request failed: {error.Message}");
        }

        // Publish a message
        await client.Publish("my.topic", new { hello = "world" });

        // Keep the application running to listen for messages
        await Task.Delay(-1);
    }
}
```

## API Reference

### `WebSocketClient`

The main client class for WebSocket communication.

#### Constructor

```csharp
new WebSocketClient(ClientConfig config)
```

`ClientConfig` options:
- `Url` (required `string`): WebSocket server URL.
- `Reconnect` (optional `ReconnectConfig`): Configuration for reconnection behavior.
    - `Enabled` (`bool`): Whether to automatically reconnect on disconnect.
    - `MaxAttempts` (optional `int`): Maximum number of reconnection attempts.
    - `Delay` (optional `int`): Delay between reconnection attempts in milliseconds.

#### Methods

- `Task Connect()`: Connects to the server.
- `Task Disconnect()`: Disconnects from the server.
- `Task<Message<BrokerHeader, object>> Request(string topic, object payload, RequestOptions? options = null)`: Makes a request and waits for a response.
- `Task Publish(string topic, object payload, PublishOptions? options = null)`: Publishes a message.
- `Task Subscribe<P>(string action, string topic, int priority, Action<Message<BrokerHeader, P>>? callback)`: Subscribes to a topic.
- `Task Unsubscribe(string topic, string action)`: Unsubscribes from a topic.

#### Events (SingleEmitters)

- `Connected`: `SingleEmitter<Action>` - Fires when a connection is established.
- `Disconnected`: `SingleEmitter<Action>` - Fires when the connection is lost.
- `Error`: `SingleEmitter<Action<Exception>>` - Fires when a connection or message error occurs.
- `Reconnecting`: `SingleEmitter<Action<(int attempt, int maxAttempts)>>` - Fires when a reconnection attempt is made.
- `Message`: `SingleEmitter<Action<Message<BrokerHeader, object>>>` - Fires for every message received from the server.

### Message Format

Messages are serialized in the following format:
```
action:topic:version[:requestId[:parentRequestId[:timeout]]]
{"key": "value"}
```

Where:
- First line: Header with action, topic, version, and optional requestId.
- Second line: JSON payload.

### Connection States

- `CONNECTED` - Successfully connected to the server.
- `CONNECTING` - Initial connection attempt.
- `RECONNECTING` - Attempting to reconnect.
- `DISCONNECTED` - Not connected to the server.

## Development

```bash
# Restore dependencies
dotnet restore

# Build the library
dotnet build

# Run tests (once a test project is set up)
dotnet test
```

## Contributing

1. Fork the repository.
2. Create your feature branch (`git checkout -b feature/amazing-feature`).
3. Commit your changes (`git commit -m 'Add some amazing feature'`).
4. Push to the branch (`git push origin feature/amazing-feature`).
5. Open a Pull Request.

## License

This project is licensed under the Apache-2.0 License - see the [LICENSE](LICENSE) file for details.