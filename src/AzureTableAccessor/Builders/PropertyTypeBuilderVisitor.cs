namespace AzureTableAccessor.Builders
{
    using System.Reflection.Emit;
    using System.Reflection;
    using System;
    
    internal class PropertyTypeBuilderVisitor
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
}