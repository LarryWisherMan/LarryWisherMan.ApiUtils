namespace LarryWisherMan.ApiUtils.Domain.Models
{

    /// <summary>
    /// Configuration for API request processing
    /// </summary>
    public class ApiRequestOptions
    {
        public bool PassThrough { get; set; }
        public bool ThrowOnError { get; set; } = false;
        public bool ParseContent { get; set; } = true;
        public bool UpdateSessionLastUsed { get; set; } = true;

    }
}
