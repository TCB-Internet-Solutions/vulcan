using Nest;
using Newtonsoft.Json.Serialization;
using System;
using System.Linq.Expressions;

namespace TcbInternetSolutions.Vulcan.Core.Extensions
{
    /// <summary>
    /// Field Extensions
    /// </summary>
    public static class FieldExtensions
    {
        internal static DefaultContractResolver FallbackNameResolver = new CamelCasePropertyNamesContractResolver();

        /// <summary>
        /// Creates field search for all analyzed fields.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="descriptor"></param>
        /// <returns></returns>
        public static FieldsDescriptor<T> AllAnalyzed<T>(this FieldsDescriptor<T> descriptor) where T : class =>
            descriptor.Field($"*.{VulcanFieldConstants.AnalyzedModifier}");

        /// <summary>
        /// Creates analyzed field descriptor from given object's property name
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="descriptor"></param>
        /// <param name="field"></param>
        /// <param name="boost"></param>
        /// <param name="resolver">CamelCasePropertyNamesContractResolver by default</param>
        /// <returns></returns>
        [Obsolete("Please use f => f.PropertyName.Suffix(VulcanFieldConstants.AnalyzedModifier) instead.", false)]
        public static FieldsDescriptor<T> FieldAnalyzed<T>(this FieldsDescriptor<T> descriptor, Expression<Func<T, object>> field, double? boost = null, DefaultContractResolver resolver = null) where T : class
        {
            MemberExpression memberExpression = null;

            switch (field.Body.NodeType)
            {
                case ExpressionType.Convert:
                    memberExpression = ((UnaryExpression)field.Body).Operand as MemberExpression;
                    break;
                case ExpressionType.MemberAccess:
                    memberExpression = field.Body as MemberExpression;
                    break;
            }

            if (memberExpression == null) return descriptor;

            resolver = resolver ?? FallbackNameResolver;
            var name = resolver.GetResolvedPropertyName(memberExpression.Member.Name);

            return descriptor.Field(memberExpression.Type == typeof(string) ? $"{name}.{VulcanFieldConstants.AnalyzedModifier}" : name, boost);
        }
    }
}