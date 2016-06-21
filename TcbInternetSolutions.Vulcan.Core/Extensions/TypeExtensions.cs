using EPiServer.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace TcbInternetSolutions.Vulcan.Core.Extensions
{
    public static class TypeExtensions
    {
        private static ConcurrentDictionary<Type, List<Type>> ResolvedTypes = new ConcurrentDictionary<Type, List<Type>>();

        public static IEnumerable<Type> GetSearchTypesFor<T>(bool removeAbstractClasses = true) where T : class, IContent =>
            GetSearchTypesFor(typeof(T), removeAbstractClasses);

        public static IEnumerable<Type> GetSearchTypesFor(this Type type, bool removeAbstractClasses = true)
        {
            List<Type> allTypesForGiven;

            if (!ResolvedTypes.TryGetValue(type, out allTypesForGiven))
            {
                allTypesForGiven = new List<Type>();

                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    allTypesForGiven.AddRange(assembly.GetTypes()
                        .Where(t => type.IsAssignableFrom(t) && !t.FullName.EndsWith("Proxy")));
                }

                ResolvedTypes.TryAdd(type, allTypesForGiven);
            }

            if (removeAbstractClasses)
                return allTypesForGiven.Where(x => !x.IsAbstract);

            return allTypesForGiven;
        }

    }
}
