namespace TcbInternetSolutions.Vulcan.AttachmentIndexer
{
    /// <summary>
    /// Converts byte array to string
    /// </summary>
    public interface IVulcanBytesToStringConverter
    {
        /// <summary>
        /// Converts bytes to string
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="mimeType"></param>
        /// <returns></returns>
        string ConvertToString(byte[] bytes, string mimeType);
    }
}
