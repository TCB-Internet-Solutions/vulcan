using EPiServer.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace TcbInternetSolutions.Vulcan.Core.Extensions
{
    internal static class TypeExtensions
    {
        private static ConcurrentDictionary<Type, List<Type>> ResolvedTypes = new ConcurrentDictionary<Type, List<Type>>();

        internal static IEnumerable<Type> GetAllTypesFor<T>(bool filterAbstracts = true) where T : class, IContent =>
            GetAllTypesFor(typeof(T), filterAbstracts);

        internal static IEnumerable<Type> GetAllTypesFor(this Type type, bool filterAbstracts = true)
        {
            List<Type> allTypesForGiven;

            if (!ResolvedTypes.TryGetValue(type, out allTypesForGiven))
            {
                List<Type> resolvedTypes = new List<Type>();

                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    resolvedTypes.AddRange(assembly.GetTypes()
                        .Where(t => type.IsAssignableFrom(t) && !t.FullName.EndsWith("Proxy")));
                }

                ResolvedTypes.TryAdd(type, resolvedTypes);
            }

            if (filterAbstracts)
                return allTypesForGiven.Where(x => !x.IsAbstract);

            return allTypesForGiven;
        }

    }
}
