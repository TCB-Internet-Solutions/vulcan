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
        private IEnumerable<string> _SupportedFileExtensions = null;

        private bool? _EnabledAttachmentPlugins;

        private bool? _EnableAttachmentFileLimit;

        /// <summary>
        /// Determines if Elasticsearch has plugins to handle base64 data
        /// </summary>
        public virtual bool EnableAttachmentPlugins
        {
            get
            {
                if (_EnabledAttachmentPlugins == null)
                {
                    string settings = ConfigurationManager.AppSettings["VulcanIndexAttachmentPluginsEnabled"];

                    if (string.IsNullOrWhiteSpace(settings))
                    {
                        _EnabledAttachmentPlugins = true;
                    }
                    else
                    {
                        return settings == "true" || settings == "1";
                    }
                }

                return _EnabledAttachmentPlugins.Value;
            }
        }
        /// <summary>
        /// Determines if file size are considered for indexing.
        /// </summary>
        public virtual bool EnableFileSizeLimit
        {
            get
            {
                if (_EnableAttachmentFileLimit == null)
                {
                    string settings = ConfigurationManager.AppSettings["VulcanIndexAttachmentFileLimitEnabled"];

                    if (string.IsNullOrWhiteSpace(settings))
                    {
                        _EnableAttachmentFileLimit = false;
                    }
                    else
                    {
                        _EnableAttachmentFileLimit = settings == "true" || settings == "1";

                    }
                }

                return _EnableAttachmentFileLimit.Value;
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
                if (_SupportedFileExtensions == null)
                {
                    var allowedExtensions = ConfigurationManager.AppSettings["VulcanIndexAttachmentFileExtensions"];

                    if (string.IsNullOrWhiteSpace(allowedExtensions))
                    {
                        _SupportedFileExtensions = new string[] { "pdf", "doc", "docx", "xls", "xlsx", "ppt", "pptx", "txt", "rtf" };
                    }
                    else
                    {
                        _SupportedFileExtensions = allowedExtensions.Split(new char[] { ',', '|', ';' }, StringSplitOptions.RemoveEmptyEntries);
                    }
                }

                return _SupportedFileExtensions;
            }
        }
    }
}
