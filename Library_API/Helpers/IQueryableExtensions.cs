using Library_API.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;

namespace Library_API.Helpers
{
    public static class IQueryableExtensions
    {
        public static IQueryable<T> ApplySort<T>(this IQueryable<T> source, string OrderBy, Dictionary<string, PropertyMappingValue> mappingDictionary)
        {
            if (source == null)
            {
                throw new ArgumentException("source");
            }

            if (mappingDictionary == null)
            {
                throw new ArgumentException("mappingDictionary");
            }

            if (string.IsNullOrWhiteSpace(OrderBy))
            {
                return source;
            }

            var orderBySplit = OrderBy.Split(',');

            foreach (var orderByCaluse in orderBySplit)
            {
                var trimOrderByCaluse = orderByCaluse.Trim();
                var orderDesending = trimOrderByCaluse.EndsWith(" desc");
                var indexOfFirstSpace = trimOrderByCaluse.IndexOf(" ");
                var propertyName = indexOfFirstSpace == -1 ?
                    trimOrderByCaluse : trimOrderByCaluse.Remove(indexOfFirstSpace);

                if (!mappingDictionary.ContainsKey(propertyName))
                {
                    throw new ArgumentException($"key mapping for {propertyName} is mmising");
                }

                var propertyMappingValue = mappingDictionary[propertyName];
                if (propertyMappingValue == null)
                {
                    throw new ArgumentException("propertyMappingValue");
                }

                foreach (var destinationProperty in propertyMappingValue.DestinationProperties.Reverse())
                {
                    if (propertyMappingValue.Revert)
                    {
                        orderDesending = !orderDesending;
                    }

                    source = source.OrderBy(destinationProperty + (orderDesending ? " desc" : " asc"));
                }
            }

            return source;
        }
    }
}
