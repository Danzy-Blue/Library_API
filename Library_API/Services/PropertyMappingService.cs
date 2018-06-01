using Library.API.Entities;
using Library_API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Library_API.Services
{
    public class PropertyMappingService : IPropertyMappingService
    {
        private Dictionary<string, PropertyMappingValue> authorPropertyMapping =
            new Dictionary<string, PropertyMappingValue>(StringComparer.OrdinalIgnoreCase)
            {
                {"Id",new PropertyMappingValue (new List<string>(){"Id"}) },
                {"Genre",new PropertyMappingValue (new List<string>(){"Genre"}) },
                {"Age",new PropertyMappingValue (new List<string>(){"DateOfBirth"},true) },
                {"Name",new PropertyMappingValue (new List<string>(){"FirstName","LastName"}, true) }
            };

        private IList<IPropertyMapping> propertyMapping = new List<IPropertyMapping>();

        public PropertyMappingService()
        {
            propertyMapping.Add(new PropertyMapping<AuthorDto, Author>(authorPropertyMapping));
        }

        public Dictionary<string, PropertyMappingValue> GetPropertyMapping<TSource, TDestination>()
        {
            // GetHashCode matching mapping
            var matchingProperty = propertyMapping.OfType<PropertyMapping<TSource, TDestination>>();
            if (matchingProperty.Count() == 1)
            {
                return matchingProperty.First().mappingDictionary;
            }

            throw new Exception($"can not find exact property mapping instance for <{typeof(TSource)}, {typeof(TDestination)}>");
        }

        public bool ValidateMappingExistsFor<TSource, TDestination>(string fields)
        {
            var propertyMapping = GetPropertyMapping<TSource, TDestination>();

            if (string.IsNullOrWhiteSpace(fields))
            {
                return true;
            }

            var fieldsAfterSplit = fields.Split(',');
            foreach (var field in fieldsAfterSplit)
            {
                var trimmedField = field.Trim();
                var indexOfFirstSpace = trimmedField.IndexOf(" ");
                var propertyName = indexOfFirstSpace == -1 ? trimmedField : trimmedField.Remove(indexOfFirstSpace);

                if (!propertyMapping.ContainsKey(propertyName))
                {
                    return false;
                }

            }

            return true;
        }
    }
}
