# TcbInternetSolutions.Vulcan.AttachmentIndexer Read Me

## Support for elasticsearch mapper-attachments

By default 'mapper-attachments' is required and enabled via the interface 'IVulcanAttachmentIndexerSettings'. The default implementation uses the 
 appsetting key 'VulcanIndexAttachmentPluginsEnabled' to determine if its enabled.

## Support for media data parsing in Episerver

Media data can now be passed to Elasticsearch as raw text, but an interface must be implemented and registered to Episerver's service locator.

### Tika media parser

```cs
    /// <summary>
    /// Requires TikaOnDotnet.TextExtractor to extract PDF,Doc(x), PPT(X), and XLS(X) documents
    /// </summary>
    [ServiceConfiguration(typeof(IVulcanBytesToStringConverter), Lifecycle = ServiceInstanceScope.Singleton)]
    public class TikaBytesToStringConverter : IVulcanBytesToStringConverter
    {
        public string ConvertToString(byte[] bytes, string mimeType)
        {
            // using TikaOnDotNet.TextExtraction
            return new TikaOnDotNet.TextExtraction.TextExtractor().Extract(bytes).Text?.Trim();
        }
    }
}
```

### iTextSharp pdf media parser

```cs
    /// <summary>
    /// Requires a NuGet package iTextSharp for PDF files only
    /// </summary>
    [ServiceConfiguration(typeof(IVulcanBytesToStringConverter), Lifecycle = ServiceInstanceScope.Singleton)]
    public class PdfBytesToStringConverter : IVulcanBytesToStringConverter
    {
        public string ConvertToString(byte[] bytes, string mimeType)
        {
            var mType = mimeType.ToLowerInvariant();

            if (mType == "application/pdf")
            {
                using (var reader = new iTextSharp.text.pdf.PdfReader(bytes))
                {
                    StringBuilder s = new StringBuilder();
                    var parserStrategy = new iTextSharp.text.pdf.parser.SimpleTextExtractionStrategy();
                    var totalPages = reader.NumberOfPages;

                    for (int i = 1; i <= totalPages; i++)
                    {
                        s.AppendFormat(" {0}", iTextSharp.text.pdf.parser.PdfTextExtractor.GetTextFromPage(reader, i, parserStrategy));
                    }

                    return s.ToString().Trim();
                }
            }

            return null;
        }
    }
}
```