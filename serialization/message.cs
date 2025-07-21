using System;
using System.Text;
using System.Text.Json;
using MBClient.CS.types;
using MBClient.CS.Utils;
using NuGet.Versioning;

namespace MBClient.CS.Serialization
{
    /// <summary>
    /// Provides methods for serializing and deserializing messages for network transmission.
    /// </summary>
    public static class MessageSerializer
    {
        // The version of the serialization protocol.
        private const string Version = CS.Version.Current;

        // Defines the compatible version range for deserialization.
        private static readonly VersionRange VersionRange = VersionRange.Parse(
            $"[{Version}, {int.Parse(Version.Split('.')[0]) + 1}.0.0)"
        );

        /// <summary>
        /// Serializes a message into a string format for transmission.
        /// The format is: "{action}:{topic}:{version}[:{requestId}[:{parentRequestId}[:{timeout}]]]\n{payload_json}"
        /// </summary>
        /// <param name="header">The message header.</param>
        /// <param name="payload">The message payload.</param>
        /// <returns>The serialized message string.</returns>
        public static string Serialize(ClientHeader header, object payload)
        {
            var headerString = $"{header.Action}:{header.Topic}:{header.Version}";

            // Append optional header fields if they exist.
            if (header.Timeout.HasValue)
            {
                headerString +=
                    $":{header.RequestId ?? ""}:{header.ParentRequestId ?? ""}:{header.Timeout}";
            }
            else if (!string.IsNullOrEmpty(header.ParentRequestId))
            {
                headerString += $":{header.RequestId ?? ""}:{header.ParentRequestId}";
            }
            else if (!string.IsNullOrEmpty(header.RequestId))
            {
                headerString += $":{header.RequestId}";
            }

            // Serialize the payload to a JSON string.
            var payloadString = JsonSerializer.Serialize(payload);

            // Combine the header and payload with a newline separator.
            return $"{headerString}\n{payloadString}";
        }

        /// <summary>
        /// Deserializes a message from its string format into a message object.
        /// </summary>
        /// <typeparam name="T">The expected type of the payload.</typeparam>
        /// <param name="data">The raw message data string.</param>
        /// <returns>The deserialized message object.</returns>
        /// <exception cref="ArgumentException">Thrown when the message format is invalid or unsupported.</exception>
        public static Message<BrokerHeader, object> Deserialize(string data)
        {
            // The message is split into header and payload by the first newline character.
            var parts = data.Split(new[] { '\n' }, 2);
            if (parts.Length < 2)
            {
                throw new ArgumentException("Invalid message format: missing header or payload");
            }

            // The header is split into its components by colons.
            var headerParts = parts[0].Split(':');
            if (headerParts.Length < 3)
            {
                throw new ArgumentException(
                    "Invalid message format: missing required header fields"
                );
            }

            var actionStr = headerParts[0];
            var topic = headerParts[1];
            var versionStr = headerParts[2];
            var requestId = headerParts.Length > 3 ? headerParts[3] : null;
            var parentRequestId = headerParts.Length > 4 ? headerParts[4] : null;
            var timeout =
                headerParts.Length > 5 && int.TryParse(headerParts[5], out var t) ? (int?)t : null;

            // Validate the action type.
            if (
                string.IsNullOrEmpty(actionStr)
                || !Enum.TryParse<ActionType>(actionStr, true, out var action)
            )
            {
                throw new ArgumentException($"Invalid action type: {actionStr}");
            }

            // Validate the topic.
            if (string.IsNullOrEmpty(topic) || !Topic.IsValid(topic))
            {
                throw new ArgumentException($"Invalid topic: {topic}");
            }

            // Validate the protocol version.
            if (
                string.IsNullOrEmpty(versionStr)
                || !NuGetVersion.TryParse(versionStr, out var version)
                || !VersionRange.Satisfies(version)
            )
            {
                throw new ArgumentException($"Unsupported protocol version: {versionStr}");
            }

            // Validate the request ID.
            if (!string.IsNullOrEmpty(requestId) && !Uuid4.IsUuid4(requestId))
            {
                throw new ArgumentException($"Invalid requestId: {requestId}");
            }

            try
            {
                // Deserialize the payload from JSON.
                var parsedPayload = JsonSerializer.Deserialize<object>(parts[1]);
                if (parsedPayload == null)
                {
                    throw new ArgumentException("Failed to parse payload: payload is null");
                }

                return new Message<BrokerHeader, object>
                {
                    Header = new BrokerHeader
                    {
                        Action = action,
                        Topic = topic,
                        Version = versionStr,
                        RequestId = requestId,
                    },
                    Payload = parsedPayload,
                };
            }
            catch (JsonException ex)
            {
                throw new ArgumentException($"Failed to parse payload: {ex.Message}");
            }
        }

        /// <summary>
        /// Calculates the size of a message in bytes.
        /// </summary>
        /// <param name="header">The message header.</param>
        /// <param name="payload">The message payload.</param>
        /// <returns>The size of the message in bytes.</returns>
        public static int GetMessageSize(ClientHeader header, object payload)
        {
            return Encoding.UTF8.GetByteCount(Serialize(header, payload));
        }
    }
}
