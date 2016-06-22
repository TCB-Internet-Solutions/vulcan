namespace TcbInternetSolutions.Vulcan.Core.Extensions
{
    using EPiServer.Core;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;

    public static class TypeExtensions
    {
        private static ConcurrentDictionary<Type, List<Type>> resolvedTypes = new ConcurrentDictionary<Type, List<Type>>();        

        public static List<Type> GetSearchTypesFor<T>(Func<Type, bool> filter = null) where T : class, IContent =>
            GetSearchTypesFor(typeof(T), filter);

        public static List<Type> GetSearchTypesFor(this Type type, Func<Type,bool> filter = null)
        {
            List<Type> allTypesForGiven;

            if (!resolvedTypes.TryGetValue(type, out allTypesForGiven))
            {
                allTypesForGiven = new List<Type>();

                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    allTypesForGiven.AddRange(assembly.GetTypes()
                        .Where(t => type.IsAssignableFrom(t) && !t.FullName.EndsWith("Proxy")));
                }

                resolvedTypes.TryAdd(type, allTypesForGiven);
            }

            if (filter != null)
                allTypesForGiven = allTypesForGiven.Where(filter).ToList();

            return allTypesForGiven;
        }

    }
}
