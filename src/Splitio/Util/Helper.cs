using System.Collections.Generic;

namespace Splitio.Util
{
    public class Helper
    {
        public static List<T> TakeFromList<T>(List<T> items, int size)
        {
            if (items == null) return new List<T>();

            var count = size;

            if (items.Count < size)
            {
                count = items.Count;
            }

            var bulk = items.GetRange(0, count);
            items.RemoveRange(0, count);

            return bulk;
        }
    }
}
