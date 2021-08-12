// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;

using Antlr4.Runtime.Utility;

namespace Antlr4.Runtime.Dfa
{
    /// <author>Sam Harwell</author>
    public sealed class ArrayEdgeMap<T> : AbstractEdgeMap<T>
        where T : class
    {
        private readonly T[] arrayData;

        private int size;

        public ArrayEdgeMap(int minIndex, int maxIndex)
            : base(minIndex, maxIndex)
        {
            arrayData = new T[maxIndex - minIndex + 1];
        }

        public override int Count => Volatile.Read(ref size);

        public override bool IsEmpty => Count == 0;

        public override T this[int key]
        {
            get
            {
                if (key < minIndex || key > maxIndex)
                {
                    return null;
                }

                return Volatile.Read(ref arrayData[key - minIndex]);
            }
        }

        public override bool ContainsKey(int key)
        {
            return this[key] != null;
        }

        public override AbstractEdgeMap<T> Put(int key, T value)
        {
            if (key >= minIndex && key <= maxIndex)
            {
                T existing = Interlocked.Exchange(ref arrayData[key - minIndex], value);
                if (existing == null && value != null)
                {
                    Interlocked.Increment(ref size);
                }
                else
                {
                    if (existing != null && value == null)
                    {
                        Interlocked.Decrement(ref size);
                    }
                }
            }

            return this;
        }

        public override AbstractEdgeMap<T> Remove(int key)
        {
            return Put(key, null);
        }

        public override AbstractEdgeMap<T> PutAll(IEdgeMap<T> m)
        {
            if (m.IsEmpty)
            {
                return this;
            }

            if (m is ArrayEdgeMap<T>)
            {
                var other = (ArrayEdgeMap<T>) m;
                int minOverlap = Math.Max(minIndex, other.minIndex);
                int maxOverlap = Math.Min(maxIndex, other.maxIndex);
                ArrayEdgeMap<T> result = this;
                for (int i = minOverlap;
                    i <= maxOverlap;
                    i++)
                {
                    result = (ArrayEdgeMap<T>) result.Put(i, m[i]);
                }

                return result;
            }

            if (m is SingletonEdgeMap<T>)
            {
                var other = (SingletonEdgeMap<T>) m;
                Debug.Assert(!other.IsEmpty);
                return Put(other.Key, other.Value);
            }

            if (m is SparseEdgeMap<T>)
            {
                var other = (SparseEdgeMap<T>) m;
                lock (other)
                {
                    int[] keys = other.Keys;
                    IList<T> values = other.Values;
                    ArrayEdgeMap<T> result = this;
                    for (int i = 0;
                        i < values.Count;
                        i++)
                    {
                        result = (ArrayEdgeMap<T>) result.Put(keys[i], values[i]);
                    }

                    return result;
                }
            }

            throw new NotSupportedException($"EdgeMap of type {m.GetType().FullName} is supported yet.");
        }

        public override AbstractEdgeMap<T> Clear()
        {
            return new EmptyEdgeMap<T>(minIndex, maxIndex);
        }

        public override ReadOnlyDictionary<int, T> ToMap()
        {
            if (IsEmpty)
            {
                return Collections.EmptyMap<int, T>();
            }

#if COMPACT
            IDictionary<int, T> result = new SortedList<int, T>();
#elif PORTABLE && !true
            IDictionary<int, T> result = new Dictionary<int, T>();
#else
            IDictionary<int, T> result = new SortedDictionary<int, T>();
#endif
            for (int i = 0;
                i < arrayData.Length;
                i++)
            {
                T element = arrayData[i];
                if (element == null)
                {
                    continue;
                }

                result[i + minIndex] = element;
            }

            return new ReadOnlyDictionary<int, T>(result);
        }
    }
}