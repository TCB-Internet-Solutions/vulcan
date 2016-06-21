using Nest;

namespace TcbInternetSolutions.Vulcan.Core
{
    public interface IVulcanClientConnectionSettings
    {
        ConnectionSettings ConnectionSettings { get; }
    }
}
