using System;
using System.Linq;
using System.Threading.Tasks;
using MBClient.CS.Core;
using MBClient.CS.types;

namespace MBClient.CS.Examples
{
    /// <summary>
    /// Demonstrates a client that provides a sorting service.
    /// </summary>
    public static class SortClient
    {
        /// <summary>
        /// Initializes and runs the SortClient.
        /// </summary>
        public static async Task Run()
        {
            Console.WriteLine("Starting sort client...");

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
                Console.WriteLine("Sort client connected.");
                // Register the client with the server to identify its role as a sorting service.
                await client.Register(
                    "Sort Responder Service",
                    "[WS] Responds to common.sort.request messages"
                );

                // Subscribe to the 'common.sort.request' topic to handle incoming sort requests.
                await client.Subscribe<SortRequestPayload>(
                    "request",
                    "common.sort.request",
                    1,
                    message =>
                    {
                        // Validate the payload to ensure it has the expected properties.
                        if (message.Payload.Iata == null || message.Payload.Barcodes == null)
                        {
                            Console.WriteLine(
                                $"Sort Client: Received sort request with unknown payload format: {message.Payload}"
                            );
                            return;
                        }

                        // Determine the number of items to sort.
                        var itemCount =
                            message.Payload.Iata.Length > 0
                                ? message.Payload.Iata.Length
                                : message.Payload.Barcodes.Length;

                        // Send a response with mock destination data.
                        message.Response(
                            new
                            {
                                destinations = Enumerable
                                    .Range(1, itemCount)
                                    .Select(i => i.ToString())
                                    .ToArray(),
                            }
                        );
                    }
                );
            });
            // Define behavior for when the client disconnects.
            client.Disconnected.On(() => Console.WriteLine("Sort client disconnected."));
            // Define behavior for handling errors.
            client.Error.On(error => Console.WriteLine($"Sort client error: {error.Message}"));

            // Establish the connection to the server.
            await client.Connect();
        }
    }

    /// <summary>
    /// Defines the structure of the payload for a sort request.
    /// </summary>
    public class SortRequestPayload
    {
        /// <summary>
        /// An array of IATA codes to be sorted.
        /// </summary>
        public string[]? Iata { get; set; }

        /// <summary>
        /// An array of barcodes to be sorted.
        /// </summary>
        public string[]? Barcodes { get; set; }
    }
}
