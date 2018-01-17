using Elasticsearch.Net;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using Nest;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
        private readonly IEnumerable<IVulcanIndexingModifier> _VulcanModifiers;

        /// <summary>
        /// DI
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="modifiers"></param>
        /// <param name="settingsModifier"></param>
        public VulcanCustomJsonSerializer
            (
                IConnectionSettingsValues settings,
                IEnumerable<IVulcanIndexingModifier> modifiers,
                Action<JsonSerializerSettings, IConnectionSettingsValues> settingsModifier
            ) : base(settings, settingsModifier)
        {
            _VulcanModifiers = modifiers;
        }

        /// <summary>
        /// Creates property mapping
        /// </summary>
        /// <param name="memberInfo"></param>
        /// <returns></returns>
        public override IPropertyMapping CreatePropertyMapping(MemberInfo memberInfo)
        {
            if (memberInfo.Name.Equals("PageName", StringComparison.OrdinalIgnoreCase) ||
                memberInfo.Name.Contains(".") || (
                    memberInfo.MemberType == MemberTypes.Property &&
                    (IsSubclassOfRawGeneric(typeof(Injected<>), (memberInfo as PropertyInfo).PropertyType)
                    || VulcanHelper.IgnoredTypes.Contains((memberInfo as PropertyInfo).PropertyType)
                    || memberInfo.Name.Equals("DefaultMvcController", StringComparison.OrdinalIgnoreCase))))
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
            if (data is IndexDescriptor<IContent> descriptedData)
            {
                // write all but ending }
                CopyDataToStream(data, writableStream, formatting, trimLast: true);

                var content = (descriptedData as IIndexRequest<IContent>)?.Document;

                if (content != null && _VulcanModifiers != null)
                {
                    // try to inspect to see if a pipeline was enabled
                    var requestAccessor = data as IRequest<IndexRequestParameters>;

                    if (requestAccessor == null)
                    {
                        throw new NotSupportedException($"{data.GetType().FullName} doesn't implement {typeof(IRequest<IndexRequestParameters>).FullName}!");
                    }

                    string pipelineId = requestAccessor.RequestParameters?.GetQueryStringValue<string>("pipeline") ?? null; // returns null if key not found                    
                    var args = new VulcanIndexingModifierArgs(content, pipelineId);

                    foreach (var indexingModifier in _VulcanModifiers)
                    {
                        try
                        {
                            indexingModifier.ProcessContent(args);
                        }
                        catch (Exception e)
                        {
                            throw new Exception($"{indexingModifier.GetType().FullName} failed to process content ID {content.ContentLink.ID} with name {content.Name}!", e);
                        }
                    }

                    // add separator for additional items if any
                    if (args.AdditionalItems.Any())
                    {
                        WriteToStream(" , " , writableStream); 
                    }

                    // copy all but starting {
                    CopyDataToStream(args.AdditionalItems, writableStream, formatting, trimLast: false);
                }
                else
                {
                    WriteToStream( "}", writableStream); // add back closing
                }
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

        static void WriteToStream(string data, Stream writeableStream)
        {
            var streamWriter = new StreamWriter(writeableStream);

            streamWriter.Write(data);

            streamWriter.Flush();
        }

        //copies all but first or last byte
        void CopyDataToStream(object data, Stream writableStream, SerializationFormatting formatting, bool trimLast = true)
        {
            var stream = new MemoryStream();
            base.Serialize(data, stream, formatting);
            stream.Seek(trimLast ? 0 : 1, SeekOrigin.Begin);
            var bytes = Convert.ToInt32(stream.Length);
            var buffer = new byte[32768];
            int read = 0;

            if (trimLast)
                bytes--;

            while (bytes > 0 &&
                   (read = stream.Read(buffer, 0, Math.Min(buffer.Length, bytes))) > 0)
            {
                writableStream.Write(buffer, 0, read);
                bytes -= read;
            }

            stream.Flush();
        }
    }
}