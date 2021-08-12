// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
#if true
using Antlr4.Runtime.Misc;
#else
using System.Diagnostics.CodeAnalysis;
#endif

using Antlr4.Runtime.Utility;

namespace Antlr4.Runtime.Dfa
{
    /// <author>Sam Harwell</author>
    public sealed class SparseEdgeMap<T> : AbstractEdgeMap<T>
        where T : class
    {
        private const int DefaultMaxSize = 5;

        private readonly List<T> values;

        public SparseEdgeMap(int minIndex, int maxIndex)
            : this(minIndex, maxIndex, DefaultMaxSize)
        {
        }

        public SparseEdgeMap(int minIndex, int maxIndex, int maxSparseSize)
            : base(minIndex, maxIndex)
        {
            Keys = new int[maxSparseSize];
            values = new List<T>(maxSparseSize);
        }

        private SparseEdgeMap([NotNull] SparseEdgeMap<T> map, int maxSparseSize)
            : base(map.minIndex, map.maxIndex)
        {
            lock (map)
            {
                if (maxSparseSize < map.values.Count)
                {
                    throw new ArgumentException();
                }

                Keys = Arrays.CopyOf(map.Keys, maxSparseSize);
                values = new List<T>(maxSparseSize);
                values.AddRange(map.Values);
            }
        }

        public int[] Keys { get; }

        public IList<T> Values => values;

        public int MaxSparseSize => Keys.Length;

        public override int Count => values.Count;

        public override bool IsEmpty => values.Count == 0;

        public override T this[int key]
        {
            get
            {
                // Special property of this collection: values are only even added to
                // the end, else a new object is returned from put(). Therefore no lock
                // is required in this method.
                int index = Array.BinarySearch(Keys, 0, Count, key);
                if (index < 0)
                {
                    return null;
                }

                return values[index];
            }
        }

        public override bool ContainsKey(int key)
        {
            return this[key] != null;
        }

        public override AbstractEdgeMap<T> Put(int key, T value)
        {
            if (key < minIndex || key > maxIndex)
            {
                return this;
            }

            if (value == null)
            {
                return Remove(key);
            }

            lock (this)
            {
                int index = Array.BinarySearch(Keys, 0, Count, key);
                if (index >= 0)
                {
                    // replace existing entry
                    values[index] = value;
                    return this;
                }

                Debug.Assert(index < 0 && value != null);
                int insertIndex = -index - 1;
                if (Count < MaxSparseSize && insertIndex == Count)
                {
                    // stay sparse and add new entry
                    Keys[insertIndex] = key;
                    values.Add(value);
                    return this;
                }

                int desiredSize = Count >= MaxSparseSize ? MaxSparseSize * 2 : MaxSparseSize;
                int space = maxIndex - minIndex + 1;
                // SparseEdgeMap only uses less memory than ArrayEdgeMap up to half the size of the symbol space
                if (desiredSize >= space / 2)
                {
                    var arrayMap = new ArrayEdgeMap<T>(minIndex, maxIndex);
                    arrayMap = (ArrayEdgeMap<T>) arrayMap.PutAll(this);
                    arrayMap.Put(key, value);
                    return arrayMap;
                }

                var resized = new SparseEdgeMap<T>(this, desiredSize);
                Array.Copy(resized.Keys, insertIndex, resized.Keys, insertIndex + 1, Count - insertIndex);
                resized.Keys[insertIndex] = key;
                resized.values.Insert(insertIndex, value);
                return resized;
            }
        }

        public override AbstractEdgeMap<T> Remove(int key)
        {
            lock (this)
            {
                int index = Array.BinarySearch(Keys, 0, Count, key);
                if (index < 0)
                {
                    return this;
                }

                var result = new SparseEdgeMap<T>(this, MaxSparseSize);
                Array.Copy(result.Keys, index + 1, result.Keys, index, Count - index - 1);
                result.values.RemoveAt(index);
                return result;
            }
        }

        public override AbstractEdgeMap<T> Clear()
        {
            if (IsEmpty)
            {
                return this;
            }

            return new EmptyEdgeMap<T>(minIndex, maxIndex);
        }

        public override ReadOnlyDictionary<int, T> ToMap()
        {
            if (IsEmpty)
            {
                return Collections.EmptyMap<int, T>();
            }

            lock (this)
            {
#if COMPACT
                IDictionary<int, T> result = new SortedList<int, T>();
#elif PORTABLE && !true
                IDictionary<int, T> result = new Dictionary<int, T>();
#else
                IDictionary<int, T> result = new SortedDictionary<int, T>();
#endif
                for (int i = 0;
                    i < Count;
                    i++)
                {
                    result[Keys[i]] = values[i];
                }

                return new ReadOnlyDictionary<int, T>(result);
            }
        }
    }
}