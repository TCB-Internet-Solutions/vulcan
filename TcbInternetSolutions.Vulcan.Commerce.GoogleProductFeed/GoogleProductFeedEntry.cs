namespace TcbInternetSolutions.Vulcan.Commerce.GoogleProductFeed
{
    public class GoogleProductFeedEntry
    {
        public string Id { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public string Link { get; set; }

        public string ImageLink { get; set; }

        public string Availability { get; set; }

        public string Price { get; set; }

        public string GoogleProductCategory { get; set; }

        public string Brand { get; set; }

        public string GTIN { get; set; }

        public string MPN { get; set; }

        public bool IdentifierExists => !string.IsNullOrWhiteSpace(GTIN) || !string.IsNullOrWhiteSpace(MPN);

        public string Condition { get; set; }

        public string Adult { get; set; }

        public string Shipping { get; set; }

        public string Tax { get; set; }
    }
}
