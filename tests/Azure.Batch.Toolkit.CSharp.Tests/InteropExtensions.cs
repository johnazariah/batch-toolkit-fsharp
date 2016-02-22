using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.FSharp.Collections;

namespace Batch.Toolkit.CSharp.Tests
{
    public static class InteropExtensions
    {
        public static FSharpList<T> ToFSharpList<T>(this IEnumerable<T> _this)
        {
            return _this.Reverse().Aggregate(FSharpList<T>.Empty, (result, curr) => new FSharpList<T>(curr, result));
        }

        public static FSharpMap<TK, TV> ToFSharpMap<TK, TV>(this IDictionary<TK, TV> _this)
        {
            return new FSharpMap<TK, TV>(
                from kvp in _this
                select new Tuple<TK, TV>(kvp.Key, kvp.Value));
        }
    }
}