namespace TcbInternetSolutions.Vulcan.AttachmentIndexer.Implementation
{
    using EPiServer.ServiceLocation;
    using System;
    using System.Collections.Generic;
    using System.Configuration;

    /// <summary>
    /// Default attachment indexer settings
    /// </summary>
    [ServiceConfiguration(typeof(IVulcanAttachmentIndexerSettings), Lifecycle = ServiceInstanceScope.Singleton)]
    public class VulcanAttachmentIndexerSettings : IVulcanAttachmentIndexerSettings
    {
        private bool? _enableAttachmentFileLimit;
        private bool? _enabledAttachmentPlugins;
        private IEnumerable<string> _supportedFileExtensions;

        /// <summary>
        /// Determines if Elasticsearch has plugins to handle base64 data
        /// </summary>
        public virtual bool EnableAttachmentPlugins
        {
            get
            {
                if (_enabledAttachmentPlugins == null)
                {                    
                    _enabledAttachmentPlugins =
                        GetSetting(ConfigurationManager.AppSettings["VulcanIndexAttachmentPluginsEnabled"], true);
                }

                return _enabledAttachmentPlugins.Value;
            }
        }
        /// <summary>
        /// Determines if file size are considered for indexing.
        /// </summary>
        public virtual bool EnableFileSizeLimit
        {
            get
            {
                if (_enableAttachmentFileLimit == null)
                {                    
                    _enableAttachmentFileLimit =
                        GetSetting(ConfigurationManager.AppSettings["VulcanIndexAttachmentFileLimitEnabled"], false);
                }

                return _enableAttachmentFileLimit.Value;
            }
        }

        /// <summary>
        /// Default max file size limit is 15 MB
        /// </summary>
        public virtual long FileSizeLimit => 15000000;

        /// <summary>
        /// Supported file extensions, by default is pdf,doc,docx,xls,xlsx,ppt,pptx,txt,rtf
        /// </summary>
        public virtual IEnumerable<string> SupportedFileExtensions
        {
            get
            {
                if (_supportedFileExtensions != null) return _supportedFileExtensions;

                var allowedExtensions = ConfigurationManager.AppSettings["VulcanIndexAttachmentFileExtensions"];

                _supportedFileExtensions = string.IsNullOrWhiteSpace(allowedExtensions) ?
                    new[] { "pdf", "doc", "docx", "xls", "xlsx", "ppt", "pptx", "txt", "rtf" } :
                    allowedExtensions.Split(new[] { ',', '|', ';' }, StringSplitOptions.RemoveEmptyEntries);

                return _supportedFileExtensions;
            }
        }

        private static bool GetSetting(string setting, bool defaultValue)
        {
            if (string.IsNullOrWhiteSpace(setting))
            {
                return defaultValue;
            }

            return setting.Equals("true") || setting.Equals("1");
        }
    }
}
