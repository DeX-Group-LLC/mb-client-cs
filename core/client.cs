using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MBClient.CS.Serialization;
using MBClient.CS.types;
using MBClient.CS.Utils;

namespace MBClient.CS.Core
{
    /// <summary>
    /// Configuration for the client.
    /// </summary>
    public class ClientConfig
    {
        /// <summary>
        /// The URL of the server to connect to.
        /// </summary>
        public required string Url { get; set; }

        /// <summary>
        /// Configuration for reconnection behavior.
        /// </summary>
        public ReconnectConfig? Reconnect { get; set; }
    }

    /// <summary>
    /// Configuration for reconnection behavior.
    /// </summary>
    public class ReconnectConfig
    {
        /// <summary>
        /// Whether reconnection is enabled.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// The maximum number of reconnection attempts.
        /// </summary>
        public int? MaxAttempts { get; set; }

        /// <summary>
        /// The delay between reconnection attempts, in milliseconds.
        /// </summary>
        public int? Delay { get; set; }
    }

    /// <summary>
    /// Options for a request message.
    /// </summary>
    public class RequestOptions
    {
        /// <summary>
        /// The ID of the parent request, for tracing.
        /// </summary>
        public string? ParentRequestId { get; set; }

        /// <summary>
        /// The version of the protocol to use.
        /// </summary>
        public string? Version { get; set; }

        /// <summary>
        /// The timeout for the request, in milliseconds.
        /// </summary>
        public int? Timeout { get; set; }
    }

    /// <summary>
    /// Options for a publish message.
    /// </summary>
    public class PublishOptions : RequestOptions
    {
        /// <summary>
        /// Whether to include a request ID in the message.
        /// </summary>
        public bool WithRequestId { get; set; }
    }

    /// <summary>
    /// An abstract base class for client implementations, defining core functionality.
    /// </summary>
    public abstract class Client
    {
        private const int DefaultTimeout = 10000;
        private const int DefaultReconnectDelay = 1000;
        private const int DefaultMaxReconnectAttempts = int.MaxValue;

        protected bool IsConnected = false;
        protected (string name, string description)? RegisterInfo;
        protected readonly Dictionary<
            string,
            (Action<Message<BrokerHeader, object>> callback, string action, int priority)
        > Subscriptions = new();
        protected readonly Dictionary<string, PendingRequest> Requests = new();
        protected Timer? ReconnectTimer;
        protected int ReconnectAttempts = 0;

        /// <summary>
        /// An emitter that fires when the client connects.
        /// </summary>
        public readonly SingleEmitter<Action> Connected = new();

        /// <summary>
        /// An emitter that fires when the client disconnects.
        /// </summary>
        public readonly SingleEmitter<Action> Disconnected = new();

        /// <summary>
        /// An emitter that fires for every message received.
        /// </summary>
        public readonly SingleEmitter<Action<Message<BrokerHeader, object>>> Message = new();

        /// <summary>
        /// An emitter that fires when an error occurs.
        /// </summary>
        public readonly SingleEmitter<Action<Exception>> Error = new();

        /// <summary>
        /// An emitter that fires when a reconnection attempt is made.
        /// </summary>
        public readonly SingleEmitter<Action<(int attempt, int maxAttempts)>> Reconnecting = new();

        protected readonly ClientConfig Config;

        /// <summary>
        /// Initializes a new instance of the <see cref="Client"/> class.
        /// </summary>
        /// <param name="config">The client configuration.</param>
        protected Client(ClientConfig config)
        {
            Config = config;
            // Initialize reconnect configuration with default values if not provided.
            Config.Reconnect ??= new ReconnectConfig();
            Config.Reconnect.Enabled = Config.Reconnect.Enabled;
            Config.Reconnect.Delay ??= DefaultReconnectDelay;
            Config.Reconnect.MaxAttempts ??= DefaultMaxReconnectAttempts;
        }

        /// <summary>
        /// Handles a disconnection event and initiates reconnection if enabled.
        /// </summary>
        protected void HandleDisconnect()
        {
            // Clean up any existing reconnect timer.
            ReconnectTimer?.Dispose();
            ReconnectTimer = null;

            // Update connection state and notify listeners.
            IsConnected = false;
            Disconnected.Emit();

            // Attempt to reconnect if enabled.
            if (Config.Reconnect?.Enabled == true)
            {
                AttemptReconnect();
            }
        }

        /// <summary>
        /// Attempts to reconnect to the server, with delays and limits.
        /// </summary>
        private void AttemptReconnect()
        {
            var delay = Config.Reconnect?.Delay ?? DefaultReconnectDelay;
            var maxAttempts = Config.Reconnect?.MaxAttempts ?? DefaultMaxReconnectAttempts;

            // Stop reconnecting if the maximum number of attempts is reached.
            if (ReconnectAttempts >= maxAttempts)
            {
                Error.Emit(
                    new Exception($"Maximum reconnection attempts ({maxAttempts}) exceeded")
                );
                return;
            }

            // Increment attempts and notify listeners.
            ReconnectAttempts++;
            Reconnecting.Emit((ReconnectAttempts, maxAttempts));

            // Schedule the next reconnection attempt.
            ReconnectTimer = new Timer(
                async _ =>
                {
                    try
                    {
                        await Connect();
                        // Reset attempts on successful connection.
                        ReconnectAttempts = 0;
                    }
                    catch (Exception ex)
                    {
                        Error.Emit(ex);
                        // If connection fails, try again.
                        AttemptReconnect();
                    }
                },
                null,
                delay,
                Timeout.Infinite
            );
        }

        /// <summary>
        /// Handles a successful connection event, re-establishing subscriptions and state.
        /// </summary>
        protected async Task HandleConnect()
        {
            // Clean up any existing reconnect timer.
            ReconnectTimer?.Dispose();
            ReconnectTimer = null;

            // Reset connection state.
            ReconnectAttempts = 0;
            IsConnected = true;

            // Re-register if necessary.
            if (RegisterInfo.HasValue)
            {
                await Register(RegisterInfo.Value.name, RegisterInfo.Value.description);
            }

            // Re-subscribe to all topics.
            var promises = new List<Task>();
            foreach (var (topic, (callback, action, priority)) in Subscriptions)
            {
                promises.Add(
                    Request(
                        "system.topic.subscribe",
                        new
                        {
                            action,
                            topic,
                            priority,
                        }
                    )
                );
            }
            await Task.WhenAll(promises);

            // Notify listeners of the successful connection.
            Connected.Emit();
        }

        /// <summary>
        /// Connects the client to the server.
        /// </summary>
        public abstract Task Connect();

        /// <summary>
        /// Disconnects the client from the server.
        /// </summary>
        public abstract Task Disconnect();

        /// <summary>
        /// Handles an incoming message, deserializing it and routing it appropriately.
        /// </summary>
        /// <param name="data">The raw message data.</param>
        protected void HandleMessage(string data)
        {
            try
            {
                var deserializedMessage = MessageSerializer.Deserialize(data);
                var message = new Message<BrokerHeader, object>(
                    this,
                    deserializedMessage.Header,
                    deserializedMessage.Payload
                );

                // Respond to system heartbeats.
                if (message.Header.Topic == "system.heartbeat")
                {
                    _ = Response(message, new { });
                }

                // Resolve pending requests.
                if (
                    message.Header.RequestId != null
                    && Requests.TryGetValue(message.Header.RequestId, out var request)
                )
                {
                    if (
                        message.Payload is JsonElement payloadElement
                        && payloadElement.TryGetProperty("error", out var errorElement)
                    )
                    {
                        var error = errorElement.Deserialize<types.Error>();
                        request.Reject(new Exception(error?.Message ?? "Unknown error"));
                    }
                    else
                    {
                        request.Resolve(message);
                    }
                }

                // Invoke subscription callbacks.
                var actionKey =
                    $"{message.Header.Action.ToString().ToLower()}:{message.Header.Topic}";
                if (Subscriptions.TryGetValue(actionKey, out var subscription))
                {
                    subscription.callback(message);
                }

                var allKey = $"all:{message.Header.Topic}";
                if (Subscriptions.TryGetValue(allKey, out var allSubscription))
                {
                    allSubscription.callback(message);
                }

                // Notify general message listeners.
                Message.Emit(message);
            }
            catch (Exception ex)
            {
                Error.Emit(ex);
            }
        }

        /// <summary>
        /// Sends a data string to the server.
        /// </summary>
        /// <param name="data">The data to send.</param>
        protected abstract Task Send(string data);

        /// <summary>
        /// Publishes a message to a topic.
        /// </summary>
        /// <param name="topic">The topic to publish to.</param>
        /// <param name="payload">The message payload.</param>
        /// <param name="options">Optional parameters for the publish message.</param>
        public Task Publish(string topic, object payload, PublishOptions? options = null)
        {
            if (!Topic.IsValid(topic))
            {
                throw new ArgumentException($"Invalid topic name: {topic}");
            }

            var requestId = options?.WithRequestId == true ? Uuid4.Generate() : null;

            var header = new ClientHeader
            {
                Action = ActionType.PUBLISH,
                Topic = topic,
                Version = options?.Version ?? CS.Version.Current,
                RequestId = requestId,
                ParentRequestId = options?.ParentRequestId,
                Timeout = options?.Timeout,
            };
            var message = MessageSerializer.Serialize(header, payload);

            var tcs = new TaskCompletionSource<object?>();

            if (requestId != null)
            {
                var timeout = options?.Timeout ?? DefaultTimeout;
                var cts = new CancellationTokenSource(timeout);
                var token = cts.Token;

                var pendingRequest = new PendingRequest(
                    resolve: (response) =>
                    {
                        if (tcs.TrySetResult(response))
                        {
                            Requests.Remove(requestId);
                            cts.Dispose();
                        }
                    },
                    reject: (error) =>
                    {
                        if (tcs.TrySetException(new Exception(error.ToString())))
                        {
                            Requests.Remove(requestId);
                            cts.Dispose();
                        }
                    }
                );

                token.Register(() =>
                {
                    if (tcs.TrySetException(new TimeoutException("Request timed out")))
                    {
                        Requests.Remove(requestId);
                        cts.Dispose();
                    }
                });

                Requests[requestId] = pendingRequest;
            }
            else
            {
                tcs.SetResult(null);
            }

            _ = Send(message);
            return tcs.Task;
        }

        /// <summary>
        /// Sends a request to a topic and waits for a response.
        /// </summary>
        /// <param name="topic">The topic to send the request to.</param>
        /// <param name="payload">The message payload.</param>
        /// <param name="options">Optional parameters for the request.</param>
        /// <returns>The response message.</returns>
        public Task<Message<BrokerHeader, object>> Request(
            string topic,
            object payload,
            RequestOptions? options = null
        )
        {
            if (!Topic.IsValid(topic))
            {
                throw new ArgumentException($"Invalid topic name: {topic}");
            }

            var requestId = Uuid4.Generate();

            var header = new ClientHeader
            {
                Action = ActionType.REQUEST,
                Topic = topic,
                Version = options?.Version ?? CS.Version.Current,
                RequestId = requestId,
                ParentRequestId = options?.ParentRequestId,
                Timeout = options?.Timeout,
            };
            var message = MessageSerializer.Serialize(header, payload);

            var tcs = new TaskCompletionSource<Message<BrokerHeader, object>>();
            var timeout = options?.Timeout ?? DefaultTimeout;
            var cts = new CancellationTokenSource(timeout);
            var token = cts.Token;

            var pendingRequest = new PendingRequest(
                resolve: (response) =>
                {
                    if (tcs.TrySetResult((Message<BrokerHeader, object>)response))
                    {
                        Requests.Remove(requestId);
                        cts.Dispose();
                    }
                },
                reject: (error) =>
                {
                    if (tcs.TrySetException(new Exception(error.ToString())))
                    {
                        Requests.Remove(requestId);
                        cts.Dispose();
                    }
                }
            );

            token.Register(() =>
            {
                if (tcs.TrySetException(new TimeoutException("Request timed out")))
                {
                    Requests.Remove(requestId);
                    cts.Dispose();
                }
            });

            Requests[requestId] = pendingRequest;

            _ = Send(message);
            return tcs.Task;
        }

        /// <summary>
        /// Sends a response to a received message.
        /// </summary>
        /// <param name="message">The original message to respond to.</param>
        /// <param name="payload">The response payload.</param>
        public Task Response(Message<BrokerHeader, object> message, object payload)
        {
            if (!Topic.IsValid(message.Header.Topic))
            {
                throw new ArgumentException($"Invalid topic name: {message.Header.Topic}");
            }

            var header = new ClientHeader
            {
                Action = ActionType.RESPONSE,
                Topic = message.Header.Topic,
                Version = message.Header.Version,
                RequestId = message.Header.RequestId,
            };
            var response = MessageSerializer.Serialize(header, payload);
            return Send(response);
        }

        /// <summary>
        /// Registers the client with a service name and description.
        /// </summary>
        /// <param name="name">The name of the service.</param>
        /// <param name="description">A description of the service.</param>
        /// <param name="options">Optional parameters for the registration request.</param>
        /// <returns>The response from the system service.</returns>
        public Task<Message<BrokerHeader, object>> Register(
            string name,
            string description,
            RequestOptions? options = null
        )
        {
            RegisterInfo = (name, description);
            return Request("system.service.register", new { name, description }, options);
        }

        /// <summary>
        /// Subscribes to a topic with a callback.
        /// </summary>
        /// <typeparam name="P">The expected payload type for the callback.</typeparam>
        /// <param name="action">The action to subscribe to ('publish', 'request', or 'all').</param>
        /// <param name="topic">The topic to subscribe to.</param>
        /// <param name="priority">The priority of the subscription.</param>
        /// <param name="callback">The callback to invoke for incoming messages.</param>
        public async Task Subscribe<P>(
            string action,
            string topic,
            int priority = 0,
            Action<Message<BrokerHeader, P>>? callback = null
        )
            where P : class
        {
            var key = $"{action}:{topic}";
            if (!Subscriptions.ContainsKey(key) || Subscriptions[key].priority != priority)
            {
                // Add or update the subscription.
                Subscriptions[key] = (
                    (message) =>
                    {
                        try
                        {
                            if (message.Payload is JsonElement payload)
                            {
                                var specificPayload = payload.Deserialize<P>();
                                if (specificPayload != null)
                                {
                                    callback?.Invoke(
                                        new Message<BrokerHeader, P>(
                                            this,
                                            message.Header,
                                            specificPayload
                                        )
                                    );
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Error.Emit(
                                new Exception(
                                    $"Failed to deserialize payload for topic {message.Header.Topic} into {typeof(P).Name}",
                                    ex
                                )
                            );
                        }
                    },
                    action,
                    priority
                );
                // Send a subscription request to the server.
                await Request(
                    "system.topic.subscribe",
                    new
                    {
                        action,
                        topic,
                        priority,
                    }
                );
            }
        }

        /// <summary>
        /// Unsubscribes from a topic.
        /// </summary>
        /// <param name="topic">The topic to unsubscribe from.</param>
        /// <param name="action">The action to unsubscribe from.</param>
        public async Task Unsubscribe(string topic, string action)
        {
            var key = $"{action}:{topic}";
            if (Subscriptions.Remove(key))
            {
                // Send an unsubscribe request to the server.
                await Request("system.topic.unsubscribe", new { topic, action });
            }
        }
    }
}
