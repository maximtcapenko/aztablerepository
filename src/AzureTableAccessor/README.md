# AzureTableAccessor
The abstraction layer for working with Azure DataTables. Implements a repository that contains methods for creating and retrieving entities from the Azure Table Storage

## Getting Started
1. Install the package into your project
```
dotnet add package AzureTableAccessor
```
2. Create entity to table mapping configuration

```c#
class YourEntityTableMappingConfiguration : IMappingConfiguration<YourEntity>
{
    public void Configure(IMappingConfigurator<YourEntity> configurator)
    {
        configurator.PartitionKey(e => e.Id)
                    .RowKey(e => e.RowId)
                    .Property(e => e.SomeProperty)
                    .Content(e => e.SomeAnotherProperty);
    }
}
```
   - Use methods `PartitionKey` and `RowKey` to configure mapping of required keys
   - Use method  `Property` to configure mapping of searchable property, method supports mapping nested properties
   - Use method  `Content` to configure mapping of non searchable property
 
3. Add the following line to the `Startup`  `Configure` method.

```c#
 services.AddTableClient(options =>
 {
    options.StorageUri = "StorageUri";
    options.StorageAccountKey = "StorageAccountKey";
    options.AccountName = "AccountName";
 }).ConfigureMap(configurator => configurator.Register(new YourEntityTableMappingConfiguration()));

```
4. Inject repository into your service
```c#
IRepository<YourEntity> _repository;

await _repository.CreateAsync(entity); //create entity
var results = await _repository.GetCollectionAsync(); //fetch all
var searchResults = await _repository.GetCollectionAsync(e => e.SomeProperty == "condition"); //search using expression