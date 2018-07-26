using System.Linq;

namespace System
{
    public static class Extension
    {
        public static void ThrowIfNull(this object obj, string nameof)
        {
            if (obj == null)
                throw new ArgumentException(nameof);
        }
    }
}

namespace System.Collections.Generic
{
    public static class Extension
    {
        public static void ThrowIfNullOrEmpty<T>(this IEnumerable<T> enumerable, string nameof)
        {
            if (!enumerable.IsNullOrEmpty())
                throw new ArgumentException(nameof);
        }

        public static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable)
        {
            return enumerable == null || enumerable.Count() == 0;
        }

        public static List<T> ToSingleList<T>(this T value)
        {
            return new List<T>() { value };
        }
    }
}
