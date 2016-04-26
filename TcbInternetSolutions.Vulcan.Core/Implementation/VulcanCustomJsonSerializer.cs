using Elasticsearch.Net;
using EPiServer.ServiceLocation;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    public class VulcanCustomJsonSerializer : JsonNetSerializer
    {
        public VulcanCustomJsonSerializer(IConnectionSettingsValues settings)
            : base(settings)
        {}

        public override Elasticsearch.Net.IPropertyMapping CreatePropertyMapping(System.Reflection.MemberInfo memberInfo)
        {
            if (memberInfo.MemberType == System.Reflection.MemberTypes.Property && IsSubclassOfRawGeneric(typeof(Injected<>), (memberInfo as System.Reflection.PropertyInfo).PropertyType))
            {
                return new PropertyMapping() { Ignore = true };
            }
            else
            {
                return base.CreatePropertyMapping(memberInfo);
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