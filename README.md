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
                    .Property(e => e.SomeProperty) // or Property(e => e.SomeProperty, "custom_name") 
                    .Content(e => e.SomeAnotherProperty);
    }
}
```
   - Use methods `PartitionKey` and `RowKey` to configure the mapping of the entity's required keys. In Azure Table storage system entities must have partition and row keys to be properly indexed and queried. These methods allow you to define how these keys should be mapped from the entity's properties
   - Use method  `Property` to configure mapping of searchable properties. It supports mapping nested properties and custom naming, which can be helpful for organizing and querying large datasets.
   - Use method  `Content` to configure mapping of non searchable property
   - Use method  `ToTable` to configure a custom table name for the entity. By default, the name of the entity is used as the table name, but you can use this method to provide a more meaningful or descriptive name
   - Use method  `AutoConfigure` to auto configuration of properties using convention. It determines whether a property should be configured as a searchable property or a non-searchable property based on its type. If the type is a string or primitive, it should be configured as a searchable property. Otherwise, it should be configured as a non-searchable property.

3. Create projection mapping configuration

```c#
class YourProjectionConfiguration : IProjectionConfiguration<YourEntity, Projection>
{
    public void Configure(IProjectionConfigurator<YourEntity, Projection> configurator)
    {
        configurator.Property(e => e.Entity.Property, p => p.Property);
    }
}
```
4. Add the following line to the `Startup`  `Configure` method.

```c#
 services.AddTableClient(options =>
 {
    options.StorageUri = "StorageUri";
    options.StorageAccountKey = "StorageAccountKey";
    options.AccountName = "AccountName";
 }).ConfigureMap(typeof(YourType).Assembly)
   .ConfigureProjections(typeof(YourType).Assembly);
```

5. Inject repository into your service
```c#
IRepository<YourEntity> _repository;

await _repository.CreateAsync(entity); //create entity
await _repository.UpdateAsync(entity); //update entity

var results = await _repository.GetCollectionAsync(); //fetch all
var page = await _repository.GetPageAsync(pageSize: 3); //get page
var entity = await _repository.LoadAsync(entity); //load entity
var searchResult = _repository.SingleAsync(e => e.SomeProperty == "condition"); //search single using expression
var searchResults = await _repository.GetCollectionAsync(e => e.SomeProperty == "condition"); //search using expression

IRepository<YourEntity, Projection> _readOnlyRepository;
IEnumerable<Projection> results = await _readOnlyRepository.GetCollectionAsync(); //fetch all