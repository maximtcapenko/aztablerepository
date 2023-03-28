namespace AzureTableAccessor.Infrastructure
{
    public class AutoKeyGenerator
    {
        public static IAutoKeyGenerator Guid => new AutoGuidKeyGenerator();

        class AutoGuidKeyGenerator : IAutoKeyGenerator
        {
            public string Generate() => System.Guid.NewGuid().ToString();
        }
    }
}