namespace LarryWisherMan.ApiUtils.Domain.Models
{
    /// <summary>
    /// Specifies runtime behavior flags for processing API requests.
    /// </summary>
    public class ApiRequestOptions
    {
        /// <summary>
        /// If true, the raw response is returned instead of a structured object.
        /// </summary>
        public bool PassThrough { get; set; }

        /// <summary>
        /// If true, exceptions will be thrown on non-success HTTP responses.
        /// </summary>
        public bool ThrowOnError { get; set; } = false;

        /// <summary>
        /// If true, the response body will be parsed according to the content type.
        /// </summary>
        public bool ParseContent { get; set; } = true;

        /// <summary>
        /// If true, the session's last-used timestamp will be updated after the request.
        /// </summary>
        public bool UpdateSessionLastUsed { get; set; } = true;
    }
}
