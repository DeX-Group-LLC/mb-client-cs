using System.Text.RegularExpressions;

namespace MBClient.CS.Utils
{
    /// <summary>
    /// Provides utility methods for working with message topics.
    /// </summary>
    public static class Topic
    {
        // A valid topic name must:
        // - Start with a letter.
        // - Contain only letters, numbers, and dots.
        // - Have a hierarchical structure with up to 5 levels.
        // - Not have consecutive dots or start/end with a dot.
        private static readonly Regex TopicNameRegex = new(
            "^[a-zA-Z][a-zA-Z0-9]*(\\.[a-zA-Z][a-zA-Z0-9]*){0,4}$"
        );

        /// <summary>
        /// Validates a topic name against the defined rules.
        /// </summary>
        /// <param name="name">The topic name to validate.</param>
        /// <returns><c>true</c> if the topic name is valid; otherwise, <c>false</c>.</returns>
        public static bool IsValid(string name)
        {
            if (string.IsNullOrEmpty(name) || name.Length > 255)
            {
                return false;
            }
            return TopicNameRegex.IsMatch(name);
        }

        /// <summary>
        /// Converts a topic name to its canonical form (lowercase).
        /// </summary>
        /// <param name="name">The topic name to canonicalize.</param>
        /// <returns>The canonicalized topic name.</returns>
        public static string GetCanonical(string name)
        {
            return name.ToLower();
        }

        /// <summary>
        /// Gets the parent topic of a given topic.
        /// </summary>
        /// <param name="name">The topic name.</param>
        /// <returns>The parent topic name, or <c>null</c> if it's a top-level topic or invalid.</returns>
        public static string? GetParent(string name)
        {
            if (!IsValid(name))
            {
                return null;
            }

            var lastDotIndex = name.LastIndexOf('.');
            if (lastDotIndex == -1)
            {
                return null;
            }

            return GetCanonical(name[..lastDotIndex]);
        }

        /// <summary>
        /// Checks if a topic is a direct child of another topic.
        /// </summary>
        /// <param name="child">The topic name to check.</param>
        /// <param name="parent">The potential parent topic name.</param>
        /// <returns><c>true</c> if 'child' is a direct child of 'parent'; otherwise, <c>false</c>.</returns>
        public static bool IsDirectChild(string child, string parent)
        {
            if (!IsValid(child) || !IsValid(parent))
            {
                return false;
            }

            var canonicalChild = GetCanonical(child);
            var canonicalParent = GetCanonical(parent);

            var actualParent = GetParent(canonicalChild);
            return actualParent == canonicalParent;
        }

        /// <summary>
        /// Checks if a topic is a descendant of another topic.
        /// </summary>
        /// <param name="descendant">The topic name to check.</param>
        /// <param name="ancestor">The potential ancestor topic name.</param>
        /// <returns><c>true</c> if 'descendant' is a descendant of 'ancestor'; otherwise, <c>false</c>.</returns>
        public static bool IsDescendant(string descendant, string ancestor)
        {
            if (!IsValid(descendant) || !IsValid(ancestor))
            {
                return false;
            }

            var canonicalDescendant = GetCanonical(descendant);
            var canonicalAncestor = GetCanonical(ancestor);

            if (canonicalDescendant == canonicalAncestor)
            {
                return false;
            }

            return canonicalDescendant.StartsWith(canonicalAncestor + ".");
        }

        /// <summary>
        /// Tests if a topic matches a given wildcard pattern.
        /// Supports '*' to match a single level and '>' to match multiple levels at the end.
        /// </summary>
        /// <param name="name">The topic name to check.</param>
        /// <param name="pattern">The wildcard pattern to match against.</param>
        /// <returns><c>true</c> if the topic matches the pattern; otherwise, <c>false</c>.</returns>
        public static bool Test(string name, string pattern)
        {
            if (!IsValid(name))
            {
                return false;
            }

            var canonicalTopic = GetCanonical(name);
            var canonicalPattern = GetCanonical(pattern);

            // Convert the wildcard pattern to a regex pattern.
            // Escape dots, and replace '*' with a pattern for one level and '>' with a pattern for multiple levels.
            var regexPattern =
                "^"
                + Regex.Escape(canonicalPattern).Replace("\\*", "[^.]+").Replace(">", ".*")
                + "$";

            return Regex.IsMatch(canonicalTopic, regexPattern);
        }
    }
}
