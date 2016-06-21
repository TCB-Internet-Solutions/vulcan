using Nest;

namespace TcbInternetSolutions.Vulcan.Core
{
    public interface IVulcanClientConnectionSettings
    {
        string Index { get; }

        ConnectionSettings ConnectionSettings { get; }
    }
}
