using System;
using System.Threading;

namespace MBClient.CS.Utils
{
    /// <summary>
    /// Provides utility methods for debouncing function calls.
    /// </summary>
    public static class DebounceUtils
    {
        /// <summary>
        /// Creates a debounced version of a function that delays invoking the function until after a specified wait time has elapsed
        /// since the last time the debounced function was invoked.
        /// </summary>
        /// <param name="func">The function to debounce.</param>
        /// <param name="wait">The number of milliseconds to delay. If less than 0, the function executes immediately. If 0, it executes after the next tick.</param>
        /// <returns>A debounced version of the function.</returns>
        public static Action<object[]> Debounce(Action<object[]> func, int wait)
        {
            Timer? timeoutId = null;
            object[]? lastArgs = null;
            // Initialize lastTime to a very small number to ensure the first call is not delayed.
            long lastTime = long.MinValue;

            Action<object[]> debounced = (object[] args) =>
            {
                // Cache the latest arguments.
                lastArgs = args;
                long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                // Calculate the actual delay based on the time since the last execution.
                long actualDelay = wait - (now - lastTime);

                // Clear any existing timeout to reset the debounce timer.
                timeoutId?.Dispose();

                // If the calculated delay is negative, execute the function immediately.
                if (actualDelay < 0)
                {
                    lastTime = now;
                    func(args);
                    return;
                }

                // Schedule a new timeout to execute the function after the actual delay.
                timeoutId = new Timer(
                    _ =>
                    {
                        timeoutId = null;
                        lastTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        if (lastArgs != null)
                        {
                            func(lastArgs);
                        }
                    },
                    null,
                    Math.Max(0, (int)actualDelay),
                    Timeout.Infinite
                );
            };

            // If wait time is negative, return the original function without debouncing.
            return wait < 0 ? func : debounced;
        }
    }
}
