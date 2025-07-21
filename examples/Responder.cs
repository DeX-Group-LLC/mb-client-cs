using System;
using System.Threading.Tasks;
using MBClient.CS.Core;
using MBClient.CS.types;

namespace MBClient.CS.Examples
{
    /// <summary>
    /// Demonstrates a client that responds to requests and triggers other events.
    /// </summary>
    public static class Responder
    {
        /// <summary>
        /// Initializes and runs the Responder client.
        /// </summary>
        public static async Task Run()
        {
            Console.WriteLine("Starting responder...");

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
                Console.WriteLine("Responder connected.");
                // Register the client with the server to identify its role.
                await client.Register(
                    "Test Responder",
                    "[WS] Responds to test messages and triggers additional events"
                );

                // Subscribe to the 'test.message' topic to handle incoming requests.
                await client.Subscribe<object>(
                    "request",
                    "test.message",
                    1,
                    message =>
                    {
                        Console.WriteLine($"Responder received: {message.Payload}");
                        // Ensure the message has a request ID before processing.
                        if (message.Header.RequestId == null)
                            return;

                        // Send a confirmation response.
                        _ = message.Response(
                            new { confirmation = "message processed by responder" }
                        );

                        // Create a payload for the trigger messages.
                        var payload = new
                        {
                            timestamp = DateTime.UtcNow.ToString("o"),
                            triggeredBy = "responder",
                        };

                        // Publish a message to a topic that has no subscribers (fire-and-forget).
                        _ = message.Publish("test.trigger.publish", payload);
                        // Send a request to trigger the listener (fire-and-forget).
                        _ = message.Request("test.trigger.request", payload);
                    }
                );

                // Subscribe to the 'test.end' topic without a callback, simply to acknowledge the topic.
                await client.Subscribe<object>("request", "test.end");
            });
            // Define behavior for when the client disconnects.
            client.Disconnected.On(() => Console.WriteLine("Responder disconnected."));
            // Define behavior for handling errors.
            client.Error.On(error => Console.WriteLine($"Responder error: {error.Message}"));

            // Establish the connection to the server.
            await client.Connect();
        }
    }
}
