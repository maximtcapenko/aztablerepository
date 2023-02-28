namespace AzureTableAccessor.Configurators.Exceptions
{
    using System;

    public class PropertyConfigurationException : Exception
    {
        public PropertyConfigurationException()
        { }

        public PropertyConfigurationException(string message) : base(message)
        { }
    }
}