using System;
using System.Collections.Generic;

namespace MBClient.CS.Utils
{
    /// <summary>
    /// Provides a simple event emitter for a single event type.
    /// </summary>
    /// <typeparam name="T">The delegate type of the event handler.</typeparam>
    public class SingleEmitter<T>
        where T : Delegate
    {
        private readonly HashSet<T> _listeners = new();

        /// <summary>
        /// Gets the number of subscribed listeners.
        /// </summary>
        public int Size => _listeners.Count;

        /// <summary>
        /// Gets a value indicating whether the emitter has any listeners.
        /// </summary>
        public bool IsEmpty => _listeners.Count == 0;

        /// <summary>
        /// Subscribes a listener to the event.
        /// </summary>
        /// <param name="callback">The callback to invoke when the event is emitted.</param>
        public void On(T callback)
        {
            _listeners.Add(callback);
        }

        /// <summary>
        /// Subscribes a one-time listener that will be removed after its first invocation.
        /// </summary>
        /// <param name="callback">The callback to invoke once when the event is emitted.</param>
        public void Once(T callback)
        {
            // Create a wrapper callback that removes itself after execution.
            var onceCallback = (T)
                (Delegate)(
                    (object[] args) =>
                    {
                        // Unsubscribe the wrapper to ensure it only runs once.
                        Off(callback);
                        // Invoke the original callback.
                        return callback.DynamicInvoke(args);
                    }
                );
            On(onceCallback);
        }

        /// <summary>
        /// Unsubscribes a listener from the event.
        /// </summary>
        /// <param name="callback">The callback to remove.</param>
        public void Off(T callback)
        {
            _listeners.Remove(callback);
        }

        /// <summary>
        /// Emits the event, invoking all subscribed listeners with the provided arguments.
        /// </summary>
        /// <param name="args">The arguments to pass to the listeners.</param>
        public void Emit(params object[] args)
        {
            // Iterate over a copy of the listeners to avoid issues with modification during iteration.
            foreach (var callback in new List<T>(_listeners))
            {
                callback.DynamicInvoke(args);
            }
        }

        /// <summary>
        /// Removes all listeners from the event.
        /// </summary>
        public void Clear()
        {
            _listeners.Clear();
        }
    }
}
