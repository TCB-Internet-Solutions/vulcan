using Elasticsearch.Net;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using Nest;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    /// <summary>
    /// Serializer for Vulcan content
    /// </summary>
    public class VulcanCustomJsonSerializer : JsonNetSerializer
    {
        Injected<IVulcanHandler> VulcanHandler;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="settings"></param>
        public VulcanCustomJsonSerializer(IConnectionSettingsValues settings) : base(settings) { }

        /// <summary>
        /// Creates property mapping
        /// </summary>
        /// <param name="memberInfo"></param>
        /// <returns></returns>
        public override IPropertyMapping CreatePropertyMapping(MemberInfo memberInfo)
        {
            if (memberInfo.Name.Equals("PageName", StringComparison.InvariantCultureIgnoreCase) ||
                memberInfo.Name.Contains(".") || (
                    memberInfo.MemberType == MemberTypes.Property &&
                    (IsSubclassOfRawGeneric(typeof(Injected<>), (memberInfo as PropertyInfo).PropertyType)
                    || VulcanHelper.IgnoredTypes.Contains((memberInfo as PropertyInfo).PropertyType)
                    || memberInfo.Name.Equals("DefaultMvcController", StringComparison.InvariantCultureIgnoreCase))))
            {
                return new PropertyMapping() { Ignore = true };
            }
            else
            {
                return base.CreatePropertyMapping(memberInfo);
            }
        }

        /// <summary>
        /// Serialize data 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="writableStream"></param>
        /// <param name="formatting"></param>
        public override void Serialize(object data, Stream writableStream, SerializationFormatting formatting = SerializationFormatting.Indented)
        {
            if (data is IndexDescriptor<IContent>)
            {
                var stream = new MemoryStream();

                base.Serialize(data, stream, formatting);

                stream.Seek(0, SeekOrigin.Begin);

                var bytes = Convert.ToInt32(stream.Length) - 1; // trim the closing brace

                var buffer = new byte[32768];
                int read;
                while (bytes > 0 &&
                       (read = stream.Read(buffer, 0, Math.Min(buffer.Length, bytes))) > 0)
                {
                    writableStream.Write(buffer, 0, read);
                    bytes -= read;
                }

                stream.Flush();

                var content = data.GetType().GetProperty("Nest.IIndexRequest.UntypedDocument", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(data) as IContent;

                if (VulcanHandler.Service.IndexingModifers != null)
                {
                    foreach (var indexingModifier in VulcanHandler.Service.IndexingModifers)
                    {
                        indexingModifier.ProcessContent(content, writableStream);
                    }
                }

                var streamWriter = new StreamWriter(writableStream);
                streamWriter.Write("}");

                streamWriter.Flush();
            }
            else
            {
                base.Serialize(data, writableStream, formatting);
            }
        }

        static bool IsSubclassOfRawGeneric(Type generic, Type toCheck)
        {
            while (toCheck != null && toCheck != typeof(object))
            {
                var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (generic == cur)
                {
                    return true;
                }
                toCheck = toCheck.BaseType;
            }
            return false;
        }
    }
}