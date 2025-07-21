using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MBClient.CS.Core
{
    /// <summary>
    /// A client implementation that communicates over WebSockets.
    /// </summary>
    public class WebSocketClient : Client
    {
        private ClientWebSocket? _ws;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketClient"/> class.
        /// </summary>
        /// <param name="config">The client configuration.</param>
        public WebSocketClient(ClientConfig config)
            : base(config) { }

        /// <summary>
        /// Establishes a WebSocket connection to the server.
        /// </summary>
        public override async Task Connect()
        {
            if (IsConnected)
            {
                return;
            }

            _ws = new ClientWebSocket();
            try
            {
                // Connect to the WebSocket server.
                await _ws.ConnectAsync(new Uri(Config.Url), CancellationToken.None);
                // Handle the successful connection, which includes re-subscribing to topics.
                await HandleConnect();
                // Start the loop to receive messages.
                _ = ReceiveLoop();
            }
            catch (Exception ex)
            {
                Error.Emit(ex);
                HandleDisconnect();
            }
        }

        /// <summary>
        /// Disconnects from the WebSocket server.
        /// </summary>
        public override async Task Disconnect()
        {
            if (!IsConnected || _ws == null)
            {
                return;
            }

            await _ws.CloseAsync(
                WebSocketCloseStatus.NormalClosure,
                "Closing",
                CancellationToken.None
            );
            HandleDisconnect();
        }

        /// <summary>
        /// Sends a data string over the WebSocket connection.
        /// </summary>
        /// <param name="data">The data to send.</param>
        protected override async Task Send(string data)
        {
            if (!IsConnected || _ws == null)
            {
                throw new InvalidOperationException("Client is not connected");
            }

            var bytes = Encoding.UTF8.GetBytes(data);
            await _ws.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None
            );
        }

        /// <summary>
        /// A long-running task that listens for incoming messages.
        /// </summary>
        private async Task ReceiveLoop()
        {
            if (_ws == null)
            {
                return;
            }

            var buffer = new byte[1024 * 4];
            // Continue listening as long as the WebSocket is open.
            while (_ws.State == WebSocketState.Open)
            {
                try
                {
                    var result = await _ws.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        CancellationToken.None
                    );
                    // If the server closes the connection, disconnect gracefully.
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await Disconnect();
                        break;
                    }

                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    HandleMessage(message);
                }
                catch (Exception ex)
                {
                    Error.Emit(ex);
                    await Disconnect();
                    break;
                }
            }
        }
    }
}
