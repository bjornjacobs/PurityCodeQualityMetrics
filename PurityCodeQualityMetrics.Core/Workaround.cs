using System;
using System.Collections.Generic;
using System.Text;

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}


namespace System.Linq
{
    public static class PolyFill
    {
        public static bool Contains(this string str, string str2, StringComparison opt)
        {
            if(opt == StringComparison.CurrentCultureIgnoreCase)
            {
                return str.ToLower().Contains(str2.ToLower());
            }

            throw new NotImplementedException();
        }


        public static T GetValueOrDefault<T>(this IDictionary<string, T> dic, string key)
        {
            if (dic.TryGetValue(key, out T val))
                return val;

            return default(T);
        }

        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            return source.GroupBy(keySelector).Select(x => x.First());
        }
    }
    
}