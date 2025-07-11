namespace LarryWisherMan.ApiUtils.Domain.Interfaces
{
    /// <summary>
    /// Interface for content parsing services.
    /// </summary>
    public interface IContentParser
    {
        /// <summary>
        /// Determines whether this parser can handle the specified content type.
        /// </summary>
        /// <param name="contentType">The MIME type of the content (e.g., application/json).</param>
        /// <returns><c>true</c> if this parser can handle the content type; otherwise, <c>false</c>.</returns>
        bool CanParse(string contentType);

        /// <summary>
        /// Parses the content string based on the specified content type.
        /// </summary>
        /// <param name="content">The raw content to parse.</param>
        /// <param name="contentType">The MIME type of the content.</param>
        /// <returns>The parsed object representation of the content.</returns>
        object Parse(string content, string contentType);
    }
}
