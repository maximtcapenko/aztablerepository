namespace AzureTableAccessor.Builders
{
    using Azure.Data.Tables;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Data.HashFunction.xxHash;
    using System.IO;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Text;

    internal class AnonymousProxyTypeBuilder
    {
        private static AssemblyName _assemblyName = new AssemblyName() { Name = "AnonymousProxyTypes" };
        private static string DefaultTypeNamePrefix = "Dynamic";
        private static ModuleBuilder _moduleBuilder = null;
        private static ConcurrentDictionary<string, Type> _typeCache = new ConcurrentDictionary<string, Type>();

        static AnonymousProxyTypeBuilder()
        {
            _moduleBuilder = AssemblyBuilder
                .DefineDynamicAssembly(_assemblyName, AssemblyBuilderAccess.Run)
                .DefineDynamicModule(_assemblyName.Name);
        }


        public string GetName() => $"{DefaultTypeNamePrefix}_{string.Join(";", _definedMembers.Select(e => $"{e.Key}_{e.Value.Name}")).Hash()}";

        public static AnonymousProxyTypeBuilder GetBuilder() => new AnonymousProxyTypeBuilder();


        private Dictionary<string, Type> _definedMembers = new Dictionary<string, Type>();

        public void DefineField(string name, Type type)
        {
            _definedMembers[name] = type;
        }

        public Type CreateType()
        {
            var key = GetName();
            if (!_typeCache.ContainsKey(key))
            {
                var typeBuilder = GetTypeBuilder(key);

                foreach (var member in _definedMembers)
                {
                    var visitor = new PropertyTypeBuilderVisitor(member.Key, member.Value);
                    visitor.Visit(typeBuilder);
                }
                _typeCache.AddOrUpdate(key, typeBuilder.CreateType(), (k, t) => t);
            }

            return _typeCache[key];
        }

        private TypeBuilder GetTypeBuilder(string typeName)
        {
            var typeBuilder = _moduleBuilder.DefineType(typeName, TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Serializable);
            typeBuilder.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
            typeBuilder.AddInterfaceImplementation(typeof(ITableEntity));

            foreach (var property in typeof(ITableEntity).GetProperties())
            {
                var visitor = new PropertyTypeBuilderVisitor(property.Name, property.PropertyType);
                visitor.Visit(typeBuilder);
            }

            return typeBuilder;
        }
    }

    public class PropertyTypeBuilderVisitor
    {
        private string _fieldName;
        private Type _type;

        public PropertyTypeBuilderVisitor(string name, Type type)
        {
            _fieldName = name;
            _type = type;
        }

        public void Visit(TypeBuilder typeBuilder)
        {
            //build field
            var field = typeBuilder.DefineField(_fieldName, _type, FieldAttributes.Private);

            //define property
            var property = typeBuilder.DefineProperty(_fieldName, PropertyAttributes.None, _type, null);

            //build setter
            var setter = typeBuilder.DefineMethod("set_" + _fieldName, MethodAttributes.Public | MethodAttributes.Virtual, null, new Type[] { _type });
            var setterILG = setter.GetILGenerator();
            setterILG.Emit(OpCodes.Ldarg_0);
            setterILG.Emit(OpCodes.Ldarg_1);
            setterILG.Emit(OpCodes.Stfld, field);
            setterILG.Emit(OpCodes.Ret);
            property.SetSetMethod(setter);


            //build getter
            var getter = typeBuilder.DefineMethod("get_" + _fieldName, MethodAttributes.Public | MethodAttributes.Virtual, _type, Type.EmptyTypes);
            var getterILG = getter.GetILGenerator();
            getterILG.Emit(OpCodes.Ldarg_0);
            getterILG.Emit(OpCodes.Ldfld, field);
            getterILG.Emit(OpCodes.Ret);
            property.SetGetMethod(getter);
        }
    }

    internal static class Helpers
    {
        public static string Hash<T>(this T entity)
        {
            var binFormatter = new BinaryFormatter();
            using (var mStream = new MemoryStream())
            {
                binFormatter.Serialize(mStream, entity);
                var hash = xxHashFactory.Instance.Create();

                return hash.ComputeHash(mStream.ToArray()).AsHexString();
            }
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


        public static Expression AssignAonymous(Type type, params MemberExpression[] initArgs)
        {
            var @new = Expression.New(type);

            return Expression.MemberInit(@new, initArgs.Select(arg =>
            {
                var argMember = arg.Member;
                var argPropertyInfo = argMember as PropertyInfo;

                return Expression.Bind(argPropertyInfo, arg);
            }));
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

        public static void HandleProperty<TModel, TMember>(this Expression<Func<TModel, TMember>> expression)
        {
            var memberExpression = expression.Body as MemberExpression;
            if (memberExpression == null)
            {
                var unaryExpression = (UnaryExpression)expression.Body;
                memberExpression = (MemberExpression)unaryExpression.Operand;
            }

            GetMemberPath(expression.Parameters[0], memberExpression);
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

    internal class MapperDelegate<TFrom0, TTo0, TFrom1, TTo1> : IMapperDelegate 
    {
        public MapperDelegate(Action<TFrom0, TTo0> mapper, Func<TFrom1, TTo1> contentGetter)
        {
            Content = contentGetter;
            Map = mapper;
        }

        public Action<TFrom0, TTo0> Map { get; private set; }
        public Func<TFrom1,TTo1> Content {get; private set;}
    }
}
