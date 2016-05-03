using Elasticsearch.Net;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAbstraction.RuntimeModel;
using EPiServer.Logging;
using EPiServer.ServiceLocation;
using Nest;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    [ServiceConfiguration(typeof(IVulcanHandler),Lifecycle=ServiceInstanceScope.Singleton)]
    public class VulcanHandler : IVulcanHandler
    {
        private static ILogger Logger = LogManager.GetLogger();

        public Injected<IContentLoader> ContentLoader { get; set; }

        private Dictionary<CultureInfo, VulcanClient> clients = new Dictionary<CultureInfo, VulcanClient>();

        private MethodCallExpression AddPropertyToIgnore(Type type, Expression exp, string PropertyName)
        {
            var propertyParameter = Expression.Parameter(type, "p");
            var propertyName = Expression.Property(propertyParameter, PropertyName);

            var innerDelegateType = typeof(Func<,>).MakeGenericType(type, typeof(object));
            var innerLambda = Expression.Lambda(innerDelegateType, propertyName, propertyParameter);

            return Expression.Call(exp, "Ignore", null, new Expression[] { innerLambda });
        }

        private IEnumerable<string> WalkProperties(PropertyInfo[] properties, string path, List<Type> seenTypes) // seenTypes used to prevent recursion... passes the seen types up a path
        {
            var seenTypesCopy = new List<Type>(seenTypes); // we need to copy as otherwise caller would get our changes too (it's by reference)

            var ignore = new List<string>();

            if (properties == null) return ignore;

            foreach(var property in properties)
            {
                seenTypesCopy.Add(property.PropertyType);                

                var prop = path == "" ? property.Name : path + "." + property.Name;

                if (VulcanHelper.IgnoredTypes.Contains(property.PropertyType)
                    || property.Name == "PageName")
                {
                    ignore.Add(prop);
                }
                else
                {
                    var childProperties = property.PropertyType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                    if(childProperties != null)
                    {
                        //ignore.AddRange(WalkProperties(childProperties.Where(p => !seenTypesCopy.Contains(p.PropertyType)).ToArray(), prop, seenTypesCopy)); right now infer mapping only supports direct properties
                    }
                }
            }

            return ignore;
        }

        private object lockObject = new object();

        /// <summary>
        /// Get a Vulcan client
        /// </summary>
        /// <param name="language">Pass in null for current culture, a specific culture or CultureInfo.InvariantCulture to get a client for non-language specific data</param>
        /// <returns>A Vulcan client</returns>
        public IVulcanClient GetClient(CultureInfo language = null)
        {
            var cultureInfo = language == null ? CultureInfo.CurrentUICulture : language;

            lock (lockObject)
            {
                if (clients.ContainsKey(cultureInfo)) return clients[cultureInfo];

                // we now know what our culture is (current culture or invariant), but we need to choose the language analyzer

                var languageAnalyzer = VulcanHelper.GetAnalyzer(cultureInfo);

                var connectionPool = new SingleNodeConnectionPool(new Uri(ConfigurationManager.AppSettings["VulcanUrl"]));
                var settings = new ConnectionSettings(connectionPool, s => new VulcanCustomJsonSerializer(s));
                settings.InferMappingFor<ContentMixin>(pd => pd.Ignore(p => p.MixinInstance));
                settings.DefaultIndex(GetIndexName(cultureInfo));

                var client = new VulcanClient(settings, cultureInfo);

                if (!client.IndexExists(GetIndexName(cultureInfo)).Exists)
                {
                    client.CreateIndex(GetIndexName(cultureInfo));
                }

                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (var type in assembly.GetTypes().Where(t => typeof(IContent).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract && !t.FullName.EndsWith("Proxy")))
                    {
                        var propertyMappingDescriptorType = typeof(PropertyMappingDescriptor<>).MakeGenericType(new Type[] { type });
                        var objectParameter = Expression.Parameter(propertyMappingDescriptorType, "o");
                        MethodCallExpression exp = null;

                        var ignore = WalkProperties(type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance), "", new List<Type> { type });

                        if (ignore != null && ignore.Any())
                        {
                            foreach (var property in ignore)
                            {
                                exp = AddPropertyToIgnore(type, exp == null ? objectParameter as Expression : exp, property);
                            }

                            var outerDelegateType = typeof(Action<>).MakeGenericType(propertyMappingDescriptorType);
                            var outerLambda = Expression.Lambda(outerDelegateType, exp, objectParameter);

                            var mapPropertiesForMethod = settings.GetType().GetMethod("MapPropertiesFor").MakeGenericMethod(new Type[] { type });

                            mapPropertiesForMethod.Invoke(settings, new object[] { outerLambda.Compile() });
                        }
                    }
                }

                var tasks = new List<Task>();

                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (var type in assembly.GetTypes().Where(t => typeof(IContent).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract && !t.FullName.EndsWith("Proxy")))
                    {
                        tasks.Add(Task.Factory.StartNew(() =>
                        {
                            var typeName = new TypeName() { Name = type.FullName, Type = type };

                            var putMappingDescriptorType = typeof(PutMappingDescriptor<>).MakeGenericType(new Type[] { type });

                            var contentExpression = Expression.Parameter(putMappingDescriptorType, "o");
                            var methodCall = Expression.Call(contentExpression, "Type", null, Expression.Constant(typeName));
                            methodCall = Expression.Call(methodCall, "Dynamic", null, Expression.Constant(DynamicMapping.Allow));
                            methodCall = Expression.Call(methodCall, "Analyzer", null, Expression.Constant(languageAnalyzer));
                             //methodCall = Expression.Call(methodCall, "AutoMap", null, Expression.Constant(new VulcanPropertyVisitor(languageAnalyzer)), Expression.Constant(0)); // this causes all sorts of trouble. avoiding for now

                            var del = typeof(Func<,>).MakeGenericType(putMappingDescriptorType, typeof(IPutMappingRequest));
                            var lambda = Expression.Lambda(del, methodCall, contentExpression);

                            var mapMethod = client.GetType().GetMethods().Where(m => m.Name == "Map" && m.IsGenericMethod).FirstOrDefault().MakeGenericMethod(type);

                            IPutMappingResponse response = mapMethod.Invoke(client, new object[] { lambda.Compile() }) as IPutMappingResponse;

                            if(!response.IsValid)
                            {
                                Logger.Error("Could not map type: " + type.FullName + ": " + response.DebugInformation);
                            }
                        }));
                    }
                }
                
                Task.WaitAll(tasks.ToArray());

                clients.Add(cultureInfo, client);

                return client;
            }
        }

        private string Index
        {
            get
            {
                return ConfigurationManager.AppSettings["VulcanIndex"];
            }
        }

        public void DeleteIndex(CultureInfo language = null)
        {
            var client = new ElasticClient(new Uri(ConfigurationManager.AppSettings["VulcanUrl"])); // use a raw elasticclient because we just need this to be quick

            client.DeleteIndex(GetIndexName(language));
        }

        public string GetIndexName(CultureInfo language)
        {
            var suffix = "_";

            if (language == null)
            {
                suffix += "*";
            }
            else if (language == CultureInfo.InvariantCulture)
            {
                suffix += "invariant";
            }
            else
            {
                suffix += language.Name;
            }

            return Index + suffix;
        }


        public void DeleteContentByLanguage(IContent content)
        {
            IVulcanClient client;

            if (!(content is ILocalizable))
            {
                client = GetClient(CultureInfo.InvariantCulture);

            }
            else
            {
                client = GetClient((content as ILocalizable).Language);
            }

            client.DeleteContent(content);
        }

        public void DeleteContentEveryLanguage(IContent content)
        {
            if (!(content is ILocalizable))
            {
                var client = GetClient(CultureInfo.InvariantCulture);

                client.DeleteContent(content);
            }
            else
            {
                foreach (var language in (content as ILocalizable).ExistingLanguages)
                {
                    var client = GetClient(language);

                    client.DeleteContent(content);
                }
            }
        }

        public void IndexContentByLanguage(IContent content)
        {
            IVulcanClient client;
            
            if(!(content is ILocalizable))
            {
                client = GetClient(CultureInfo.InvariantCulture);

            }
            else
            {
                client = GetClient((content as ILocalizable).Language);
            }

            client.IndexContent(content);
        }

        public void IndexContentEveryLanguage(IContent content)
        {
            if (!(content is ILocalizable))
            {
                var client = GetClient(CultureInfo.InvariantCulture);

                client.IndexContent(content);
            }
            else
            {
                foreach(var language in (content as ILocalizable).ExistingLanguages)
                {
                    var client = GetClient(language);

                    client.IndexContent(ContentLoader.Service.Get<IContent>(content.ContentLink.ToReferenceWithoutVersion(), language));
                }
            }
        }
    }
}
