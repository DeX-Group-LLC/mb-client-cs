using System;

namespace MBClient.CS.Utils
{
    /// <summary>
    /// Provides utility methods for working with UUIDv4.
    /// </summary>
    public static class Uuid4
    {
        /// <summary>
        /// Generates a new UUIDv4 string.
        /// </summary>
        /// <returns>A new UUIDv4 string.</returns>
        public static string Generate()
        {
            return Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Validates if a string is a valid UUID.
        /// </summary>
        /// <param name="uuid">The string to validate.</param>
        /// <returns><c>true</c> if the string is a valid UUID; otherwise, <c>false</c>.</returns>
        public static bool IsUuid4(string uuid)
        {
            if (string.IsNullOrEmpty(uuid))
            {
                return false;
            }
            return Guid.TryParse(uuid, out _);
        }
    }
}
