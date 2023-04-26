namespace AzureTableAccessor.Builders
{
    using System;
    
    public interface IRuntimeTypeBuilder
    {
        void DefineProperty(string name, Type type);
    }
}