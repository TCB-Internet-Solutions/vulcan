using Elasticsearch.Net;
using EPiServer.Core;
using EPiServer.Core.Html.StringParsing;
using EPiServer.ServiceLocation;
using Nest;
using System;
using System.Collections.Generic;
using System.Globalization;
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
            if (memberInfo.Name.Equals("PageName", StringComparison.InvariantCultureIgnoreCase) || (
                    memberInfo.MemberType == System.Reflection.MemberTypes.Property && 
                    (IsSubclassOfRawGeneric(typeof(Injected<>), (memberInfo as System.Reflection.PropertyInfo).PropertyType)
                    || VulcanHelper.IgnoredTypes.Contains((memberInfo as System.Reflection.PropertyInfo).PropertyType)
                    || memberInfo.Name.Equals("DefaultMvcController", StringComparison.InvariantCultureIgnoreCase))))
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