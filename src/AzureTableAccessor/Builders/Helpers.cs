namespace AzureTableAccessor.Builders
{
    using System.Collections.Generic;
    using System.Data.HashFunction.xxHash;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Text;
    using System;

    internal static class Helpers
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

        public static (string, Type) GetPropertyOrFieldInfo(this MemberExpression member)
        {
            var property = member.Member as PropertyInfo;
            if (property != null)
                return (property.Name, property.PropertyType);

            var field = member.Member as FieldInfo;
            if (field != null)
                return (field.Name, field.FieldType);

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

        public static void GetMemberPath(ParameterExpression parameter, MemberExpression memberExpression)
        {
            var property = memberExpression;
            var path = new Stack<MemberInfo>();
            do
            {
                path.Push(memberExpression.Member);
                memberExpression = memberExpression.Expression as MemberExpression;
            }
            while (memberExpression != null);

            if (path.Count > 2) throw new NotSupportedException();

            while (path.Count > 0)
            {
                var memberInfo = path.Pop();
                var propertyInfo = memberInfo as PropertyInfo;

                if (propertyInfo.PropertyType.IsClass && propertyInfo.PropertyType != typeof(string))
                {
                    // define new expression
                    var @new = Expression.New(propertyInfo.PropertyType);
                    var nestedProperty = Expression.Property(parameter, propertyInfo);

                    var init = Init(@new);
                    var op = Expression.Assign(nestedProperty, init);
                }
            }

            Expression Init(NewExpression @new)
            {
                var valueMemberInfo = path.Pop();
                var valuePropertyInfo = valueMemberInfo as PropertyInfo;

                return Expression.MemberInit(@new, Expression.Bind(valuePropertyInfo, property));
            }
        }
    }

    internal interface IMapperDelegate { }

    internal class MapperDelegate<TFrom, TTo> : IMapperDelegate where TFrom : class
        where TTo : class
    {
        public MapperDelegate(Action<TFrom, TTo> mapper)
        {
            Map = mapper;
        }

        public Action<TFrom, TTo> Map { get; private set; }
    }
}
