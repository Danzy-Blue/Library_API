using System.Collections.Generic;

namespace Library_API.Services
{
    public interface IPropertyMappingService
    {
        Dictionary<string, PropertyMappingValue> GetPropertyMapping<TSource, TDestination>();
        bool ValidateMappingExistsFor<TSource, TDestination>(string fields);
    }
}