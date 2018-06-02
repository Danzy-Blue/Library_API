using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Library_API.Helpers
{
    public static class IEnumerableExtension
    {
        public static IEnumerable<ExpandoObject> ShapeData<T>(this IEnumerable<T> source, string fields)
        {

            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            var expendableOjectList = new List<ExpandoObject>();
            var propertyInfoList = new List<PropertyInfo>();
            if (string.IsNullOrWhiteSpace(fields))
            {
                var propertyInfos = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public);
                propertyInfoList.AddRange(propertyInfos);
            }
            else
            {
                var fieldAfterSplit = fields.Split(',');
                foreach (var field in fieldAfterSplit)
                {
                    var propertyName = field.Trim();
                    var propertyInfo = typeof(T).GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                    if (propertyInfo == null)
                    {
                        throw new Exception($"Property {propertyName} not found on {typeof(T)}");
                    }

                    propertyInfoList.Add(propertyInfo);
                }
            }

            foreach (var sourceObject in source)
            {
                var dataShapedObject = new ExpandoObject();
                foreach (var propertyInfo in propertyInfoList)
                {
                    var propertyValue = propertyInfo.GetValue(sourceObject);
                    ((IDictionary<string, object>)dataShapedObject).Add(propertyInfo.Name, propertyValue);
                }

                expendableOjectList.Add(dataShapedObject);
            }

            return expendableOjectList;
        }
    }
}




