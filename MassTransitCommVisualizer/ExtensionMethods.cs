using System.Collections.Generic;
using System.Linq;

namespace MassTransitCommVisualizer
{
    public static class ExtensionMethods
    {
        public static void AddIfNotExist<T>(this IList<T> list, T itemToAdd)
        {
            if (list.FirstOrDefault(item => item.Equals(itemToAdd)) != null)
            {
                return;
            }

            list.Add(itemToAdd);
        }

        public static void AddRangeIfNotExist<T>(this IList<T> list, IList<T> itemsToAdd)
        {
            foreach (var itemToAdd in itemsToAdd)
            {
                list.AddIfNotExist(itemToAdd);
            }
        }
    }
}
