// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
#if true
using Antlr4.Runtime.Misc;
#else
using System.Diagnostics.CodeAnalysis;
#endif


namespace Antlr4.Runtime.Dfa
{
    /// <author>Sam Harwell</author>
    public interface IEdgeMap<T> : IEnumerable<KeyValuePair<int, T>>
    {
        int Count { get; }

        bool IsEmpty { get; }

        T this[int key] { get; }

        bool ContainsKey(int key);

        [return: NotNull]
        IEdgeMap<T> Put(int key, [AllowNull] T value);

        [return: NotNull]
        IEdgeMap<T> Remove(int key);

        [return: NotNull]
        IEdgeMap<T> PutAll(IEdgeMap<T> m);

        [return: NotNull]
        IEdgeMap<T> Clear();

        [return: NotNull]
        ReadOnlyDictionary<int, T> ToMap();
    }
}