namespace MBClient.CS.types;

/// <summary>
/// Specifies the type of action a message represents.
/// </summary>
public enum ActionType
{
    /// <summary>
    /// A message that publishes information to a topic without expecting a direct response.
    /// </summary>
    PUBLISH,

    /// <summary>
    /// A message that requests data or action from a topic and expects a response.
    /// </summary>
    REQUEST,

    /// <summary>
    /// A message that serves as a reply to a previous request message.
    /// </summary>
    RESPONSE,
}

/// <summary>
/// Represents the header of a message, containing metadata for routing and handling.
/// </summary>
public class BrokerHeader
{
    /// <summary>
    /// The action this message performs (e.g., publish, request, response).
    /// </summary>
    public ActionType Action { get; set; }

    /// <summary>
    /// The dot-notation topic this message is associated with (e.g., 'service.event').
    /// </summary>
    public required string Topic { get; set; }

    /// <summary>
    /// The version of the message protocol (e.g., '1.0.0').
    /// </summary>
    public required string Version { get; set; }

    /// <summary>
    /// A unique identifier for correlating request and response messages.
    /// </summary>
    public string? RequestId { get; set; }
}

/// <summary>
/// Extends the BrokerHeader with additional client-specific metadata.
/// </summary>
public class ClientHeader : BrokerHeader
{
    /// <summary>
    /// The ID of a parent request, used for tracing related messages.
    /// </summary>
    public string? ParentRequestId { get; set; }

    /// <summary>
    /// An optional timeout for request-response pairs, in milliseconds.
    /// </summary>
    public int? Timeout { get; set; }
}

/// <summary>
/// Represents a standardized error structure for communicating issues.
/// </summary>
public class Error
{
    /// <summary>
    /// A unique code identifying the error.
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// A human-readable message describing the error.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// The ISO 8601 timestamp when the error occurred.
    /// </summary>
    public string? Timestamp { get; set; } // ISO 8601 format (e.g., "2023-10-27T10:30:00Z")

    /// <summary>
    /// An optional object containing additional, structured error details.
    /// </summary>
    public object? Details { get; set; }
}

/// <summary>
/// Represents an error payload in a message.
/// </summary>
public class PayloadError : Error { }

/// <summary>
/// Represents a successful payload, containing arbitrary data in a key-value format.
/// </summary>
public class PayloadSuccess : Dictionary<string, object> { }

/// <summary>
/// Represents a complete message, including both header and payload.
/// </summary>
/// <typeparam name="T">The type of the message header (must be a BrokerHeader or derived class).</typeparam>
/// <typeparam name="U">The type of the message payload (must be a class).</typeparam>
public class Message<T, U>
    where T : BrokerHeader
    where U : class
{
    /// <summary>
    /// The header containing message metadata.
    /// </summary>
    public required T Header { get; set; }

    /// <summary>
    /// The payload containing the message's data or error information.
    /// </summary>
    public required U Payload { get; set; }
}

/// <summary>
/// Defines the possible states of a WebSocket connection.
/// </summary>
public enum ConnectionState
{
    /// <summary>
    /// The client is successfully connected to the server.
    /// </summary>
    CONNECTED,

    /// <summary>
    /// The client is attempting to establish an initial connection.
    /// </summary>
    CONNECTING,

    /// <summary>
    /// The client is attempting to reconnect after a disconnection.
    /// </summary>
    RECONNECTING,

    /// <summary>
    /// The client is not connected to the server.
    /// </summary>
    DISCONNECTED,
}

/// <summary>
/// Defines the types of connection-related events that can occur.
/// </summary>
public enum ConnectionEventType
{
    /// <summary>
    /// Fired when the client successfully connects to the server.
    /// </summary>
    CONNECTED,

    /// <summary>
    /// Fired when the client begins its initial connection attempt.
    /// </summary>
    CONNECTING,

    /// <summary>
    /// Fired when the client begins a reconnection attempt.
    /// </summary>
    RECONNECTING,

    /// <summary>
    /// Fired when the client's connection is lost.
    /// </summary>
    DISCONNECTED,

    /// <summary>
    /// Fired when a connection-related error occurs.
    /// </summary>
    ERROR,
}

/// <summary>
/// Represents a connection-related event, with details about the event.
/// </summary>
public class ConnectionEvent
{
    /// <summary>
    /// The type of connection event.
    /// </summary>
    public ConnectionEventType Type { get; set; }

    /// <summary>
    /// The timestamp when the event occurred.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// An optional error message associated with the event.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// The attempt number for a reconnection event.
    /// </summary>
    public int? Attempt { get; set; }
}

/// <summary>
/// Provides detailed information about the current connection status.
/// </summary>
public class ConnectionDetails
{
    /// <summary>
    /// The current state of the connection (e.g., connected, disconnected).
    /// </summary>
    public ConnectionState State { get; set; }

    /// <summary>
    /// The URL of the WebSocket server.
    /// </summary>
    public required string Url { get; set; }

    /// <summary>
    /// The timestamp of the last successful connection.
    /// </summary>
    public DateTime? LastConnected { get; set; }

    /// <summary>
    /// The number of reconnection attempts made.
    /// </summary>
    public int ReconnectAttempts { get; set; }

    /// <summary>
    /// The most recently measured connection latency in milliseconds.
    /// </summary>
    public int? Latency { get; set; }

    /// <summary>
    /// A list of recent connection events, for diagnostic purposes.
    /// </summary>
    public required List<ConnectionEvent> RecentEvents { get; set; }
}

/// <summary>
/// Configuration options for the WebSocket client.
/// </summary>
public class WebSocketConfig
{
    /// <summary>
    /// The URL of the WebSocket server to connect to.
    /// </summary>
    public required string Url { get; set; }

    /// <summary>
    /// The interval, in milliseconds, at which to send heartbeat messages.
    /// </summary>
    public int? HeartbeatInterval { get; set; }

    /// <summary>
    /// The delay, in milliseconds, before attempting to reconnect.
    /// </summary>
    public int? ReconnectDelay { get; set; }

    /// <summary>
    /// The maximum number of recent connection events to store.
    /// </summary>
    public int? MaxRecentEvents { get; set; }

    /// <summary>
    /// The version of the communication protocol to use.
    /// </summary>
    public string? ProtocolVersion { get; set; }
}

/// <summary>
/// Represents a pending request that is awaiting a response.
/// </summary>
public class PendingRequest
{
    /// <summary>
    /// The callback to execute when the request is successfully resolved.
    /// </summary>
    public Action<object> Resolve { get; set; }

    /// <summary>
    /// The callback to execute when the request is rejected or fails.
    /// </summary>
    public Action<object> Reject { get; set; }

    /// <summary>
    /// An optional timer to handle request timeouts.
    /// </summary>
    public Timer? Timeout { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PendingRequest"/> class.
    /// </summary>
    /// <param name="resolve">The callback for successful resolution.</param>
    /// <param name="reject">The callback for failed resolution.</param>
    /// <param name="timeout">The optional timeout timer.</param>
    public PendingRequest(Action<object> resolve, Action<object> reject, Timer? timeout = null)
    {
        Resolve = resolve;
        Reject = reject;
        Timeout = timeout;
    }
}
