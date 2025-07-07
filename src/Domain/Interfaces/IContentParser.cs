namespace LarryWisherMan.ApiUtils.Domain.Interfaces
{
    /// <summary>
    /// Interface for content parsing services
    /// </summary>
    public interface IContentParser
    {
        bool CanParse(string contentType);
        object Parse(string content, string contentType);
    }

}
