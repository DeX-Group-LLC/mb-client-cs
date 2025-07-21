using System.Threading.Tasks;
using MBClient.CS.types;

namespace MBClient.CS.Core
{
    /// <summary>
    /// Represents a message, containing a header and payload, and provides methods for interaction.
    /// </summary>
    /// <typeparam name="T">The type of the message header.</typeparam>
    /// <typeparam name="U">The type of the message payload.</typeparam>
    public class Message<T, U>
        where T : BrokerHeader
        where U : class
    {
        private readonly Client _client;

        /// <summary>
        /// The header of the message.
        /// </summary>
        public T Header { get; }

        /// <summary>
        /// The payload of the message.
        /// </summary>
        public U Payload { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Message{T,U}"/> class.
        /// </summary>
        /// <param name="client">The client instance associated with this message.</param>
        /// <param name="header">The message header.</param>
        /// <param name="payload">The message payload.</param>
        public Message(Client client, T header, U payload)
        {
            _client = client;
            Header = header;
            Payload = payload;
        }

        /// <summary>
        /// Publishes a new message as a child of the current message.
        /// </summary>
        /// <param name="topic">The topic to publish to.</param>
        /// <param name="payload">The payload of the new message.</param>
        /// <param name="options">Optional parameters for the publish message.</param>
        public Task Publish(string topic, object payload, PublishOptions? options = null)
        {
            options ??= new PublishOptions();
            // Set the parent request ID to trace the message chain.
            options.ParentRequestId = Header.RequestId;
            return _client.Publish(topic, payload, options);
        }

        /// <summary>
        /// Sends a new request as a child of the current message and awaits a response.
        /// </summary>
        /// <param name="topic">The topic to send the request to.</param>
        /// <param name="payload">The payload of the request.</param>
        /// <param name="options">Optional parameters for the request.</param>
        /// <returns>The response message.</returns>
        public Task<Message<BrokerHeader, object>> Request(
            string topic,
            object payload,
            RequestOptions? options = null
        )
        {
            options ??= new RequestOptions();
            // Set the parent request ID to trace the message chain.
            options.ParentRequestId = Header.RequestId;
            return _client.Request(topic, payload, options);
        }

        /// <summary>
        /// Sends a response to the current message.
        /// </summary>
        /// <param name="payload">The payload of the response.</param>
        public Task Response(object payload)
        {
            return _client.Response(
                new Message<BrokerHeader, object>(_client, Header, (object)payload),
                payload
            );
        }
    }
}
