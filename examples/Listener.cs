using System;
using System.Threading.Tasks;
using MBClient.CS.Core;
using MBClient.CS.types;

namespace MBClient.CS.Examples
{
    /// <summary>
    /// Demonstrates a client that listens for specific events and responds to them.
    /// </summary>
    public static class Listener
    {
        /// <summary>
        /// Initializes and runs the Listener client.
        /// </summary>
        public static async Task Run()
        {
            Console.WriteLine("Starting listener...");

            // Initialize the WebSocket client with the server URL and reconnection settings.
            var client = new WebSocketClient(
                new ClientConfig
                {
                    Url = Config.Url,
                    Reconnect = new ReconnectConfig { Enabled = true },
                }
            );

            // Define behavior for when the client connects.
            client.Connected.On(async () =>
            {
                Console.WriteLine("Listener connected.");
                // Register the client with the server to identify its role.
                await client.Register(
                    "Test Listener",
                    "[WS] Listens for trigger messages and sends end signal"
                );

                // Subscribe to a 'publish' topic without a callback, just to acknowledge it.
                await client.Subscribe<object>("publish", "test.trigger.publish", 1);
                // Subscribe to a 'request' topic to handle incoming trigger requests.
                await client.Subscribe<object>(
                    "request",
                    "test.trigger.request",
                    1,
                    message =>
                    {
                        // Respond to the trigger message to acknowledge receipt.
                        _ = message.Response(message.Payload);

                        // Create a payload for the new messages.
                        var payload = new
                        {
                            timestamp = DateTime.UtcNow.ToString("o"),
                            triggeredBy = "listener",
                        };

                        // Publish a message to a topic that has no subscribers (fire-and-forget).
                        _ = message.Publish("test.noroute", payload);
                        // Send a request to the 'test.end' topic to signal completion (fire-and-forget).
                        _ = message.Request(
                            "test.end",
                            payload,
                            new RequestOptions { Timeout = 500 }
                        );
                    }
                );
            });
            // Define behavior for when the client disconnects.
            client.Disconnected.On(() => Console.WriteLine("Listener disconnected."));
            // Define behavior for handling errors.
            client.Error.On(error => Console.WriteLine($"Listener error: {error.Message}"));

            // Establish the connection to the server.
            await client.Connect();
        }
    }
}
