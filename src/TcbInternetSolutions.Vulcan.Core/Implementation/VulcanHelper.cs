﻿using EPiServer.Data.Dynamic;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EPiServer.ServiceLocation;
// ReSharper disable InvertIf
// ReSharper disable SwitchStatementMissingSomeCases

namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    /// <summary>
    /// Vulcan helper
    /// </summary>
    public static class VulcanHelper
    {
        /// <summary>
        /// Master Alias
        /// </summary>
        public static readonly string MasterAlias = "master";

        /// <summary>
        /// Temp Alias
        /// </summary>
        public static readonly string TempAlias = "temp";

        private static IServiceLocator _assignedServiceLocator;

        /// <summary>
        ///  Get indexAlias-based name for index
        /// </summary>
        /// <param name="indexNameBase"></param>
        /// <param name="language"></param>
        /// <param name="alias"></param>
        /// <returns></returns>
        public static string GetAliasName(string indexNameBase, CultureInfo language, string alias)
        {
            if (language == null)
                throw new ArgumentNullException(nameof(language));

            var suffix = "_";

            if (language.Equals(CultureInfo.InvariantCulture))
            {
                suffix += "invariant";
            }
            else
            {
                suffix += language.Name.ToLowerInvariant(); // causing invalid index name w/o tolowerstring
            }

            return indexNameBase + "-" + (string.IsNullOrWhiteSpace(alias) ? MasterAlias : alias) + suffix;
        }

        /// <summary>
        /// Wrapper for Service Locator GetAllInstances
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IEnumerable<T> GetAllServices<T>() => ResolveServiceLocator().GetAllInstances<T>();

        /// <summary>
        /// Get analyzer for cultureinfo
        /// </summary>
        /// <param name="cultureInfo"></param>
        /// <returns></returns>
        public static string GetAnalyzer(CultureInfo cultureInfo)
        {
            if (!cultureInfo.Equals(CultureInfo.InvariantCulture)) // check if we have non-language data
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

        /// <summary>
        /// Get index name for language
        /// </summary>
        /// <param name="indexNameBase"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        public static string GetRawIndexName(string indexNameBase, CultureInfo language)
        {
            if (language == null)
                throw new ArgumentNullException(nameof(language));

            var suffix = "_";

            if (language.Equals(CultureInfo.InvariantCulture))
            {
                suffix += "invariant";
            }
            else
            {
                suffix += language.Name.ToLowerInvariant(); // causing invalid index name w/o tolowerstring
            }

            return indexNameBase + "_" + DateTime.UtcNow.ToString("yyyyMMddhhmmss") + suffix;
        }

        /// <summary>
        /// Wrapper for Service Locator GetInstance
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T GetService<T>() => ResolveServiceLocator().GetInstance<T>();

        /// <summary>
        /// Guards for null alias
        /// </summary>
        /// <param name="alias"></param>
        public static void GuardForNullAlias(ref string alias)
        {
            if (string.IsNullOrWhiteSpace(alias)) alias = MasterAlias;
        }

        /// <summary>
        /// Allows for service locator to be swapped for things such as unit testing.
        /// </summary>
        /// <param name="serviceLocator"></param>
        public static void SetServiceLocator(IServiceLocator serviceLocator)
        {
            _assignedServiceLocator = serviceLocator;
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
            var store = CreateVulcanStore();
            var synonym = store.LoadAll<VulcanSynonym>().FirstOrDefault(s => s.Term == term && s.Language == language) ?? new VulcanSynonym();

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
            var store = CreateVulcanStore();
            var synonym = store.LoadAll<VulcanSynonym>().FirstOrDefault(s => s.Term == term && s.Language == language);

            if (synonym != null)
            {
                store.Delete(synonym);
            }
        }

        internal static Dictionary<string, KeyValuePair<string[], bool>> GetSynonyms(string language)
        {
            return CreateVulcanStore()
                .LoadAll<VulcanSynonym>()
                .Where(s => s.Language == language)
                .ToDictionary(s => s.Term, s => new KeyValuePair<string[], bool>(s.Synonyms, s.BiDirectional));
        }

        private static DynamicDataStore CreateVulcanStore() => DynamicDataStoreFactory.Instance.CreateStore(typeof(VulcanSynonym));

        private static IServiceLocator ResolveServiceLocator() => _assignedServiceLocator ?? ServiceLocator.Current;
    }
}