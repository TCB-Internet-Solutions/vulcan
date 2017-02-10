using EPiServer.Core;
using EPiServer.Data.Dynamic;
using EPiServer.DataAbstraction;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    /// <summary>
    /// Vulcan helper
    /// </summary>
    public static class VulcanHelper
    {
        /// <summary>
        /// Get index name for language
        /// </summary>
        /// <param name="IndexNameBase"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        public static string GetIndexName(string IndexNameBase, CultureInfo language)
        {
            var suffix = "_";

            if (language == CultureInfo.InvariantCulture)
            {
                suffix += "invariant";
            }
            else
            {
                suffix += language.Name.ToLowerInvariant(); // causing invalid index name w/o tolowerstring
            }

            return IndexNameBase + suffix;
        }

        internal static Type[] IgnoredTypes =>
            new Type[]
            {
                typeof(PropertyDataCollection),
                typeof(ContentArea),
                typeof(CultureInfo),
                typeof(IEnumerable<CultureInfo>),
                typeof(PageType)
            };

        /// <summary>
        /// Get analyzer for cultureinfo
        /// </summary>
        /// <param name="cultureInfo"></param>
        /// <returns></returns>
        public static string GetAnalyzer(CultureInfo cultureInfo)
        {
            if (cultureInfo != CultureInfo.InvariantCulture) // check if we have non-language data
            {
                if (!cultureInfo.IsNeutralCulture)
                {
                    // try something specific first

                    switch (cultureInfo.Name.ToUpper())
                    {
                        case "PT-BR":
                            return "brazilian";
                    }
                }

                // nothing specific matched, go with generic

                switch (cultureInfo.TwoLetterISOLanguageName.ToUpper())
                {
                    case "AR":
                        return "arabic";
                    case "HY":
                        return "armenian";
                    case "EU":
                        return "basque";
                    case "BG":
                        return "bulgarian";
                    case "CA":
                        return "catalan";
                    case "ZH":
                        return "chinese";
                    case "KO":
                        return "cjk"; // generic chinese-japanese-korean                            
                    case "JP":
                        return "cjk"; // generic chinese-japanese-korean                            
                    case "CS":
                        return "czech";
                    case "DA":
                        return "danish";
                    case "NL":
                        return "dutch";
                    case "EN":
                        return "english";
                    case "FI":
                        return "finnish";
                    case "FR":
                        return "french";
                    case "GL":
                        return "galician";
                    case "DE":
                        return "german";
                    case "GR":
                        return "greek";
                    case "HI":
                        return "hindi";
                    case "HU":
                        return "hungarian";
                    case "ID":
                        return "indonesian";
                    case "GA":
                        return "irish";
                    case "IT":
                        return "italian";
                    case "LV":
                        return "latvian";
                    case "NO":
                        return "norwegian";
                    case "FA":
                        return "persian";
                    case "PT":
                        return "portuguese";
                    case "RO":
                        return "romanian";
                    case "RU":
                        return "russian";
                    case "KU":
                        return "sorani"; // Kurdish                            
                    case "ES":
                        return "spanish";
                    case "SV":
                        return "swedish";
                    case "TR":
                        return "turkish";
                    case "TH":
                        return "thai";
                }
            }

            // couldn't find a match (or invariant culture)
            return "standard";
        }

        internal static Nest.Language? GetLanguage(CultureInfo cultureInfo)
        {
            if (cultureInfo != CultureInfo.InvariantCulture) // check if we have non-language data
            {
                if (!cultureInfo.IsNeutralCulture)
                {
                    // try something specific first

                    switch (cultureInfo.Name.ToUpper())
                    {
                        case "PT-BR":
                            return Nest.Language.Brazilian;
                    }
                }

                // nothing specific matched, go with generic

                switch (cultureInfo.TwoLetterISOLanguageName.ToUpper())
                {
                    case "AR":
                        return Nest.Language.Arabic;
                    case "HY":
                        return Nest.Language.Armenian;
                    case "EU":
                        return Nest.Language.Basque;
                    case "BG":
                        return Nest.Language.Bulgarian;
                    case "CA":
                        return Nest.Language.Catalan;
                    case "ZH":
                        return Nest.Language.Chinese;
                    case "KO":
                        return Nest.Language.Cjk; // generic chinese-japanese-korean                            
                    case "JP":
                        return Nest.Language.Cjk; // generic chinese-japanese-korean                            
                    case "CS":
                        return Nest.Language.Czech;
                    case "DA":
                        return Nest.Language.Danish;
                    case "NL":
                        return Nest.Language.Dutch;
                    case "EN":
                        return Nest.Language.English;
                    case "FI":
                        return Nest.Language.Finnish;
                    case "FR":
                        return Nest.Language.French;
                    case "GL":
                        return Nest.Language.Galician;
                    case "DE":
                        return Nest.Language.German;
                    case "GR":
                        return Nest.Language.Greek;
                    case "HI":
                        return Nest.Language.Hindi;
                    case "HU":
                        return Nest.Language.Hungarian;
                    case "ID":
                        return Nest.Language.Indonesian;
                    case "GA":
                        return Nest.Language.Irish;
                    case "IT":
                        return Nest.Language.Italian;
                    case "LV":
                        return Nest.Language.Latvian;
                    case "NO":
                        return Nest.Language.Norwegian;
                    case "FA":
                        return Nest.Language.Persian;
                    case "PT":
                        return Nest.Language.Portuguese;
                    case "RO":
                        return Nest.Language.Romanian;
                    case "RU":
                        return Nest.Language.Russian;
                    case "KU":
                        return Nest.Language.Sorani; // Kurdish                            
                    case "ES":
                        return Nest.Language.Spanish;
                    case "SV":
                        return Nest.Language.Swedish;
                    case "TR":
                        return Nest.Language.Turkish;
                    case "TH":
                        return Nest.Language.Thai;
                }
            }

            // couldn't find a match (or invariant culture)
            return null;
        }

        internal static void AddSynonym(string language, string term, string[] synonyms, bool biDirectional)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                throw new Exception("Cannot add a blank synonym term");
            }

            if (synonyms == null || synonyms.Length == 0)
            {
                throw new Exception("Cannot add a synonym term with no synonyms");
            }

            term = term.ToLower().Trim();

            var store = DynamicDataStoreFactory.Instance.CreateStore(typeof(VulcanSynonym));

            var synonym = store.LoadAll<VulcanSynonym>().Where(s => s.Term == term && s.Language == language).FirstOrDefault();

            if (synonym == null)
            {
                synonym = new VulcanSynonym();
            }

            synonym.Language = language;
            synonym.Term = term;
            synonym.Synonyms = synonyms.Select(s => s.ToLower().Trim()).ToArray();
            synonym.BiDirectional = biDirectional;

            store.Save(synonym);
        }

        internal static void DeleteSynonym(string language, string term)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                throw new Exception("Cannot delete a blank synonym term");
            }

            term = term.ToLower().Trim();

            var store = DynamicDataStoreFactory.Instance.CreateStore(typeof(VulcanSynonym));

            var synonym = store.LoadAll<VulcanSynonym>().Where(s => s.Term == term && s.Language == language).FirstOrDefault();

            if (synonym != null)
            {
                store.Delete(synonym);
            }
        }

        internal static Dictionary<string, KeyValuePair<string[], bool>> GetSynonyms(string language)
        {
            var store = DynamicDataStoreFactory.Instance.CreateStore(typeof(VulcanSynonym));

            return store.LoadAll<VulcanSynonym>().Where(s => s.Language == language).ToDictionary(s => s.Term, s => new KeyValuePair<string[], bool>(s.Synonyms, s.BiDirectional));
        }
    }
}
