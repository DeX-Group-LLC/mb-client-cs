using System;
using System.Collections.Generic;

namespace MBClient.CS.Utils
{
    /// <summary>
    /// Manages a collection of <see cref="SingleEmitter{T}"/> instances to handle multiple, named event streams.
    /// </summary>
    /// <typeparam name="T">The delegate type for the event handlers.</typeparam>
    public class MultiEmitter<T>
        where T : Delegate
    {
        /// <summary>
        /// A dictionary of named event emitters.
        /// </summary>
        public readonly Dictionary<string, SingleEmitter<T>> Events = new();

        /// <summary>
        /// An emitter that fires when a new event stream is created.
        /// </summary>
        public readonly SingleEmitter<Action<string>> CreatedEmitter = new();

        /// <summary>
        /// An emitter that fires when an event stream is removed.
        /// </summary>
        public readonly SingleEmitter<Action<string>> RemovedEmitter = new();

        /// <summary>
        /// Gets the <see cref="SingleEmitter{T}"/> for a specific event, creating it if it doesn't exist.
        /// </summary>
        /// <param name="eventName">The name of the event.</param>
        /// <returns>The emitter for the specified event.</returns>
        public SingleEmitter<T> GetEmitter(string eventName)
        {
            if (!Events.TryGetValue(eventName, out var emitter))
            {
                // If the emitter does not exist, create a new one, add it to the dictionary,
                // and notify listeners that a new event stream has been created.
                emitter = new SingleEmitter<T>();
                Events[eventName] = emitter;
                CreatedEmitter.Emit(eventName);
            }
            return emitter;
        }

        /// <summary>
        /// Checks if an emitter exists for a given event.
        /// </summary>
        /// <param name="eventName">The name of the event.</param>
        /// <returns><c>true</c> if an emitter exists; otherwise, <c>false</c>.</returns>
        public bool Has(string eventName)
        {
            return Events.ContainsKey(eventName);
        }

        /// <summary>
        /// Subscribes a listener to a specific event.
        /// </summary>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="callback">The callback to invoke when the event is emitted.</param>
        public void On(string eventName, T callback)
        {
            GetEmitter(eventName).On(callback);
        }

        /// <summary>
        /// Subscribes a one-time listener to a specific event.
        /// </summary>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="callback">The callback to invoke once when the event is emitted.</param>
        public void Once(string eventName, T callback)
        {
            GetEmitter(eventName).Once(callback);
        }

        /// <summary>
        /// Unsubscribes a listener from a specific event.
        /// </summary>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="callback">The callback to remove.</param>
        public void Off(string eventName, T callback)
        {
            if (Events.TryGetValue(eventName, out var emitter))
            {
                emitter.Off(callback);
                // If the emitter becomes empty after removing the listener,
                // clean it up to conserve resources.
                if (emitter.IsEmpty)
                {
                    Events.Remove(eventName);
                    RemovedEmitter.Emit(eventName);
                }
            }
        }

        /// <summary>
        /// Emits a specific event, invoking all its subscribed listeners.
        /// </summary>
        /// <param name="eventName">The name of the event to emit.</param>
        /// <param name="args">The arguments to pass to the listeners.</param>
        public void Emit(string eventName, params object[] args)
        {
            if (Events.TryGetValue(eventName, out var emitter))
            {
                // Only emit if there are active listeners.
                if (emitter.Size > 0)
                {
                    emitter.Emit(args);
                }

                // Clean up the emitter if it has become empty (e.g., from 'once' listeners).
                if (emitter.IsEmpty)
                {
                    Events.Remove(eventName);
                    RemovedEmitter.Emit(eventName);
                }
            }
        }

        /// <summary>
        /// Removes all listeners from a specific event or all events.
        /// </summary>
        /// <param name="eventName">The optional name of the event to clear. If null, all events are cleared.</param>
        public void Clear(string? eventName = null)
        {
            if (eventName != null)
            {
                // Clear a single event.
                if (Events.TryGetValue(eventName, out var emitter))
                {
                    emitter.Clear();
                    Events.Remove(eventName);
                    RemovedEmitter.Emit(eventName);
                }
            }
            else
            {
                // Clear all events, notifying listeners for each one.
                foreach (var key in Events.Keys)
                {
                    RemovedEmitter.Emit(key);
                }
                Events.Clear();
            }
        }
    }
}
