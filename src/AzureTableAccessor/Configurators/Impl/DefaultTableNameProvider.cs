namespace AzureTableAccessor.Configurators.Impl
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Infrastructure;

    internal class DefaultTableNameProvider<TEntity> : ITableNameProvider
        where TEntity : class
    {
        private readonly List<string> _names = new List<string>();
        private const string _rulePattern = "^[A-Za-z][A-Za-z0-9]{2,62}$";

        public void AddName(string name)
        {
            _names.Add(name);
        }

        public string GetTableName()
        {
            if (!_names.Any())
            {
                return typeof(TEntity).Name.ToLower();
            }
            var name = string.Join(null, _names);
            var match = Regex.Match(name, _rulePattern);

            if (!match.Success)
                throw new System.ArgumentException($"Table name [{name}] is not valid");

            return name;
        }
    }
}