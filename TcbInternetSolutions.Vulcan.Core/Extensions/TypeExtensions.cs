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

        public static List<Type> GetSearchTypesFor<T>(bool removeAbstractClasses = true) where T : class, IContent =>
            GetSearchTypesFor(typeof(T), removeAbstractClasses);

        public static List<Type> GetSearchTypesFor(this Type type, bool classesOnly = true, bool removeAbstractClasses = true)
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

            if (removeAbstractClasses)
                allTypesForGiven = allTypesForGiven.Where(x => !x.IsAbstract).ToList();

            if (classesOnly)
                allTypesForGiven = allTypesForGiven.Where(x => x.IsClass).ToList();

            return allTypesForGiven;
        }

    }
}
