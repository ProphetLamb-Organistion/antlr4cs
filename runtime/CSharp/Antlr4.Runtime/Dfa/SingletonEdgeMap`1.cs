// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using System.Collections.ObjectModel;

using Antlr4.Runtime.Utility;

namespace Antlr4.Runtime.Dfa
{
    /// <author>Sam Harwell</author>
    public sealed class SingletonEdgeMap<T> : AbstractEdgeMap<T>
        where T : class
    {
        public SingletonEdgeMap(int minIndex, int maxIndex, int key, T value)
            : base(minIndex, maxIndex)
        {
            if (key >= minIndex && key <= maxIndex)
            {
                Key = key;
                Value = value;
            }
            else
            {
                Key = 0;
                Value = null;
            }
        }

        public int Key { get; }

        public T Value { get; }

        public override int Count => Value != null ? 1 : 0;

        public override bool IsEmpty => Value == null;

        public override T this[int key]
        {
            get
            {
                if (key == Key)
                {
                    return Value;
                }

                return null;
            }
        }

        public override bool ContainsKey(int key)
        {
            return key == Key && Value != null;
        }

        public override AbstractEdgeMap<T> Put(int key, T value)
        {
            if (key < minIndex || key > maxIndex)
            {
                return this;
            }

            if (key == Key || Value == null)
            {
                return new SingletonEdgeMap<T>(minIndex, maxIndex, key, value);
            }

            if (value != null)
            {
                AbstractEdgeMap<T> result = new SparseEdgeMap<T>(minIndex, maxIndex);
                result = result.Put(Key, Value);
                result = result.Put(key, value);
                return result;
            }

            return this;
        }

        public override AbstractEdgeMap<T> Remove(int key)
        {
            if (key == Key && Value != null)
            {
                return new EmptyEdgeMap<T>(minIndex, maxIndex);
            }

            return this;
        }

        public override AbstractEdgeMap<T> Clear()
        {
            if (Value != null)
            {
                return new EmptyEdgeMap<T>(minIndex, maxIndex);
            }

            return this;
        }

        public override ReadOnlyDictionary<int, T> ToMap()
        {
            if (IsEmpty)
            {
                return Collections.EmptyMap<int, T>();
            }

            return Collections.SingletonMap(Key, Value);
        }
    }
}