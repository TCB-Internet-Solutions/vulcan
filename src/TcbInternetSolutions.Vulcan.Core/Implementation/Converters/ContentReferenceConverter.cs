using EPiServer.Core;
using Newtonsoft.Json;
using System;

namespace TcbInternetSolutions.Vulcan.Core.Implementation.Converters
{
    /// <summary>
    /// Converts content reference properties to use references without work ID/version set
    /// </summary>
    public class ContentReferenceConverter : JsonConverter
    {
        private static readonly Type ContentReferenceType = typeof(ContentReference);

        /// <summary>
        /// Can read, default is true
        /// </summary>
        public override bool CanRead { get; } = true;

        /// <summary>
        /// Can write, default is true
        /// </summary>
        public override bool CanWrite { get; } = true;

        /// <summary>
        /// Determines if convert supports given type
        /// </summary>
        /// <param name="objectType"></param>
        /// <returns></returns>
        public override bool CanConvert(Type objectType)
        {
            return ContentReferenceType.IsAssignableFrom(objectType);
        }

        /// <summary>
        /// Creates content reference from value
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="objectType"></param>
        /// <param name="existingValue"></param>
        /// <param name="serializer"></param>
        /// <returns></returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            switch (reader.TokenType)
            {
                case JsonToken.Null:
                    return null;
                case JsonToken.String:
	                if (reader.Value == null || string.IsNullOrEmpty(reader.Value.ToString()))
	                {
		                return null;
	                }
                    return new ContentReference((string)reader.Value);
                case JsonToken.Integer:
                    return new ContentReference((int)reader.Value);
            }

            throw new JsonSerializationException($"Cannot convert token of type {reader.TokenType} to {objectType}.");
        }

        /// <summary>
        /// Writes value without work ID/version
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        /// <param name="serializer"></param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var contentRef = value as ContentReference;

            if (contentRef == null)
                writer.WriteNull();
            else
                writer.WriteValue(contentRef.ToReferenceWithoutVersion().ToString());
        }
    }
}
