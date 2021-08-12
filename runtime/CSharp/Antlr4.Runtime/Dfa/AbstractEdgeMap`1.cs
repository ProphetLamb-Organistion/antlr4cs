// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Antlr4.Runtime.Dfa
{
    /// <author>Sam Harwell</author>
    public abstract class AbstractEdgeMap<T> : IEdgeMap<T>
        where T : class
    {
        protected internal readonly int maxIndex;
        protected internal readonly int minIndex;

        protected AbstractEdgeMap(int minIndex, int maxIndex)
        {
            // the allowed range (with minIndex and maxIndex inclusive) should be less than 2^32
            Debug.Assert(maxIndex - minIndex + 1 >= 0);
            this.minIndex = minIndex;
            this.maxIndex = maxIndex;
        }

        IEdgeMap<T> IEdgeMap<T>.Put(int key, T value)
        {
            return Put(key, value);
        }

        IEdgeMap<T> IEdgeMap<T>.PutAll(IEdgeMap<T> m)
        {
            return PutAll(m);
        }

        IEdgeMap<T> IEdgeMap<T>.Clear()
        {
            return Clear();
        }

        IEdgeMap<T> IEdgeMap<T>.Remove(int key)
        {
            return Remove(key);
        }

        public abstract bool ContainsKey(int arg1);

        public abstract T this[int arg1] { get; }

        public abstract bool IsEmpty { get; }

        public abstract int Count { get; }

        public abstract ReadOnlyDictionary<int, T> ToMap();

        public virtual IEnumerator<KeyValuePair<int, T>> GetEnumerator()
        {
            return ToMap().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public abstract AbstractEdgeMap<T> Put(int key, T value);

        public virtual AbstractEdgeMap<T> PutAll(IEdgeMap<T> m)
        {
            AbstractEdgeMap<T> result = this;
            foreach (KeyValuePair<int, T> entry in m)
            {
                result = result.Put(entry.Key, entry.Value);
            }

            return result;
        }

        public abstract AbstractEdgeMap<T> Clear();

        public abstract AbstractEdgeMap<T> Remove(int key);
    }
}