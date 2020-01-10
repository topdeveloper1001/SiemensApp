using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Platform.XL.Common.Extensions
{
    public static class ObjectExtensions
    {

        public static IDictionary<string, string> ToDictionary(this object source, bool ignoreNulls = false)
        {
            if (source == null)
            {
                throw new NullReferenceException("Unable to convert anonymous object to a dictionary. The source anonymous object is null.");
            }

            var dictionary = new Dictionary<string, string>();
            foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(source))
            {
                var value = property.GetValue(source);
                if (ignoreNulls && value == null)
                {
                    continue;
                }

                dictionary.Add(property.Name, value?.ToString());
            }
            return dictionary;
        }

    }
}