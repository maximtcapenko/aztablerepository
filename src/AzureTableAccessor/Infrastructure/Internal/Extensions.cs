namespace AzureTableAccessor.Infrastructure.Internal
{
    using System;
    using System.Collections.Generic;
    using System.Data.HashFunction.xxHash;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Text;

    public static class Extensions
    {
        public static string Hash(this string str)
        {
            var data = Encoding.Default.GetBytes(str);
            var hash = xxHashFactory.Instance.Create();
            return hash.ComputeHash(data).AsHexString();
        }

        public static Type GetMemberType(this MemberInfo member)
        {
            var property = member as PropertyInfo;
            if (property != null)
                return property.PropertyType;

            var field = member as FieldInfo;
            if (field != null)
                return field.FieldType;

            throw new NotSupportedException();
        }

        public static string GetMemberPath(MemberExpression memberExpression, Func<string, string> transformer = null)
        {
            var path = new List<string>();
            do
            {
                var memberName = memberExpression.Member.Name;
                path.Add(transformer == null ? memberName : transformer(memberName));
                memberExpression = memberExpression.Expression as MemberExpression;
            }
            while (memberExpression != null);

            var sb = new StringBuilder();
            var i = path.Count - 1;
            for (; i > 0; --i)
            {
                sb.Append(path[i]);
                sb.Append('.');
            }
            sb.Append(path[i]);
            return sb.ToString();
        }

        public static string GetMemberPath<TModel, TMember>(this Expression<Func<TModel, TMember>> expression, Func<string, string> transformer = null)
        {
            var memberExpression = expression.Body as MemberExpression;
            if (memberExpression == null)
            {
                var unaryExpression = (UnaryExpression)expression.Body;
                memberExpression = (MemberExpression)unaryExpression.Operand;
            }

            return GetMemberPath(memberExpression, transformer);
        }
    }
}