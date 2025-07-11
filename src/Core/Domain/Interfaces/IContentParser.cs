namespace LarryWisherMan.ApiUtils.Domain.Interfaces
{
    namespace LarryWisherMan.ApiUtils.Domain.Interfaces
    {
        /// <summary>
        /// Base interface for runtime content parsing.
        /// Enables discovery and routing of parsing logic by content type.
        /// </summary>
        public interface IContentParser
        {
            /// <summary>
            /// Gets the supported MIME type this parser handles.
            /// </summary>
            string ContentType { get; }

            /// <summary>
            /// Determines whether this parser can handle the specified content type.
            /// </summary>
            /// <param name="contentType">The MIME type of the content (e.g., application/json).</param>
            /// <returns><c>true</c> if this parser can handle the content type; otherwise, <c>false</c>.</returns>
            bool CanParse(string contentType);

            /// <summary>
            /// Parses the content string based on the specified content type.
            /// </summary>
            /// <param name="content">The raw content string.</param>
            /// <param name="contentType">The MIME type of the content.</param>
            /// <returns>The parsed object.</returns>
            object Parse(string content, string contentType);
        }

        /// <summary>
        /// Strongly typed content parser interface.
        /// </summary>
        /// <typeparam name="T">The type the content should be parsed into.</typeparam>
        public interface IContentParser<T> : IContentParser
        {
            /// <summary>
            /// Parses the content into the specified generic type.
            /// </summary>
            /// <param name="content">The raw content string.</param>
            /// <param name="contentType">The MIME type of the content.</param>
            /// <returns>The parsed result as type <typeparamref name="T"/>.</returns>
            new T Parse(string content, string contentType);
        }
    }


}
