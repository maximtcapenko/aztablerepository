namespace AzureTableAccessor.Infrastructure.Internal
{
    using System;
    
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