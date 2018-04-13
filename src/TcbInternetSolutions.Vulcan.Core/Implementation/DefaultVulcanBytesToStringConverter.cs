namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    using EPiServer.ServiceLocation;

    /// <summary>
    /// Default implementation does nothing
    /// </summary>
    [ServiceConfiguration(typeof(IVulcanBytesToStringConverter), Lifecycle = ServiceInstanceScope.Singleton)]
    public class DefaultVulcanBytesToStringConverter : IVulcanBytesToStringConverter
    {
        string IVulcanBytesToStringConverter.ConvertToString(byte[] bytes, string mimeType)
        {
            return string.Empty;
        }
    }
}
