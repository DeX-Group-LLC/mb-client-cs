using System;
using System.Threading.Tasks;
using MBClient.CS.Core;
using MBClient.CS.types;

namespace MBClient.CS.Examples
{
    /// <summary>
    /// Demonstrates a client that periodically sends requests and handles responses.
    /// </summary>
    public static class Requester
    {
        /// <summary>
        /// Initializes and runs the Requester client.
        /// </summary>
        public static async Task Run()
        {
            Console.WriteLine("Starting requester...");

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
                Console.WriteLine("Requester connected.");
                // Register the client with the server to identify its role.
                await client.Register("Test Requester", "[WS] Sends test requests periodically");

                // Subscribe to the 'test.end' topic to listen for completion signals.
                await client.Subscribe<object>(
                    "request",
                    "test.end",
                    1,
                    message =>
                    {
                        Console.WriteLine($"Requester received message: {message.Header.Topic}");
                        // Respond to the 'test.end' message to acknowledge receipt.
                        message.Response(message.Payload);
                    }
                );
            });
            // Define behavior for when the client disconnects.
            client.Disconnected.On(() => Console.WriteLine("Requester disconnected."));
            // Define behavior for handling errors.
            client.Error.On(error => Console.WriteLine($"Requester error: {error.Message}"));

            // Establish the connection to the server.
            await client.Connect();

            // Set up a timer to send a request every 2 seconds.
            var timer = new System.Threading.Timer(
                async _ =>
                {
                    try
                    {
                        // Create a payload with a timestamp and a message.
                        var payload = new
                        {
                            timestamp = DateTime.UtcNow.ToString("o"),
                            message = "Hello from requester!",
                        };
                        // Send a request to the 'test.message' topic and wait for a response.
                        var response = await client.Request(
                            "test.message",
                            payload,
                            new RequestOptions { Timeout = 2000 }
                        );
                        // Log the topic of the response.
                        Console.WriteLine($"Requester received response: {response.Header.Topic}");
                    }
                    catch (Exception ex)
                    {
                        // Log any errors that occur during the request.
                        Console.WriteLine($"Requester error on request: {ex.Message}");
                    }
                },
                null,
                0,
                2000
            );
        }
    }
}
