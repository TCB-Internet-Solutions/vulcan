namespace TcbInternetSolutions.Vulcan.AttachmentIndexer
{
    internal static class Helpers
    {
        static IVulcanBytesToStringConverter _Converter;

        internal static IVulcanBytesToStringConverter GetBytesToStringConverter()
        {
            if (_Converter == null)
            {
                try
                {
                    _Converter = EPiServer.ServiceLocation.ServiceLocator.Current.GetInstance<IVulcanBytesToStringConverter>();
                }
                catch { }
            }

            return _Converter;
        }
    }
}
