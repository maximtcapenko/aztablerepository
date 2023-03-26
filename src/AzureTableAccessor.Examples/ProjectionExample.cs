namespace AzureTableAccessor.Examples
{
    using Data;
    using Microsoft.Extensions.DependencyInjection;

    class ProjectionExample
    {
        public static async Task Run(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var projectionRepo = scope.ServiceProvider.GetRequiredService<IRepository<Message, PhoneOnly>>();
            var projections = await projectionRepo.GetCollectionAsync();
        }
    }
}