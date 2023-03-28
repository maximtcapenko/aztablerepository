namespace AzureTableAccessor.Examples
{
    using Data;
    using Microsoft.Extensions.DependencyInjection;

    class BatchExample
    {
        public static async Task Run(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var repository = unitOfWork.CreateRepository<Message>();

             for(int i = 0; i < 5; i++)
            {
                var message = Generator.Generate();
                await repository.CreateAsync(message);
            }

            await unitOfWork.SaveChangesAsync();
        }
    }
}