// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Antlr4.Misc
{
    public class LinkedHashMap<TKey, TValue> : IDictionary<TKey, TValue>, IDictionary
    {
        private readonly Dictionary<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>> _dictionary;
        private readonly LinkedList<KeyValuePair<TKey, TValue>> _list;

        public LinkedHashMap()
        {
            _dictionary = new Dictionary<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>>();
            _list = new LinkedList<KeyValuePair<TKey, TValue>>();
        }

        public LinkedHashMap(IEnumerable<KeyValuePair<TKey, TValue>> items)
            : this()
        {
            foreach (KeyValuePair<TKey, TValue> item in items)
            {
                Add(item.Key, item.Value);
            }
        }

        bool IDictionary.IsFixedSize => false;

        ICollection IDictionary.Keys => new KeyCollection(this);

        ICollection IDictionary.Values => new ValueCollection(this);

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => throw new NotSupportedException();

        public object this[object key]
        {
            get
            {
                if (!(key is TKey))
                {
                    if (!(key == (object) default(TKey)))
                    {
                        return null;
                    }
                }

                TValue result;
                if (!TryGetValue((TKey) key, out result))
                {
                    return null;
                }

                return result;
            }

            set => throw new NotImplementedException();
        }

        void IDictionary.Add(object key, object value)
        {
            throw new NotImplementedException();
        }

        bool IDictionary.Contains(object key)
        {
            if (!(key is TKey))
            {
                if (!(key == (object) default(TKey)))
                {
                    return false;
                }
            }

            TValue result;
            return TryGetValue((TKey) key, out result);
        }

        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return new DictionaryEnumerator(GetEnumerator());
        }

        void IDictionary.Remove(object key)
        {
            throw new NotImplementedException();
        }

        void ICollection.CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }

        public virtual TValue this[TKey key]
        {
            get => _dictionary[key].Value.Value;

            set
            {
                LinkedListNode<KeyValuePair<TKey, TValue>> node;
                if (_dictionary.TryGetValue(key, out node))
                {
                    node.Value = new KeyValuePair<TKey, TValue>(node.Value.Key, value);
                }
                else
                {
                    node = _list.AddLast(new KeyValuePair<TKey, TValue>(key, value));
                    _dictionary[key] = node;
                }
            }
        }

        public virtual int Count => _dictionary.Count;

        public virtual bool IsReadOnly => false;

        public virtual ICollection<TKey> Keys => new KeyCollection(this);

        public virtual ICollection<TValue> Values => new ValueCollection(this);

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        public virtual void Add(TKey key, TValue value)
        {
            var node = new LinkedListNode<KeyValuePair<TKey, TValue>>(new KeyValuePair<TKey, TValue>(key, value));
            _dictionary.Add(key, node);
            _list.AddLast(node);
        }

        public virtual void Clear()
        {
            _dictionary.Clear();
            _list.Clear();
        }

        public virtual bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return _list.Contains(item);
        }

        public virtual bool ContainsKey(TKey key)
        {
            return _dictionary.ContainsKey(key);
        }

        public virtual void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        public virtual IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            throw new NotImplementedException();
        }

        public virtual bool Remove(TKey key)
        {
            LinkedListNode<KeyValuePair<TKey, TValue>> node;
            if (!_dictionary.TryGetValue(key, out node))
            {
                return false;
            }

            _dictionary.Remove(key);
            _list.Remove(node);
            return true;
        }

        public virtual bool TryGetValue(TKey key, out TValue value)
        {
            LinkedListNode<KeyValuePair<TKey, TValue>> node;
            if (!_dictionary.TryGetValue(key, out node))
            {
                value = default;
                return false;
            }

            value = node.Value.Value;
            return true;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private class KeyCollection : ICollection<TKey>, ICollection
        {
            private readonly LinkedHashMap<TKey, TValue> _map;

            public KeyCollection(LinkedHashMap<TKey, TValue> map)
            {
                _map = map;
            }

            bool ICollection.IsSynchronized => false;

            object ICollection.SyncRoot => throw new NotSupportedException();

            void ICollection.CopyTo(Array array, int index)
            {
                TKey[] keys = this.ToArray();
                keys.CopyTo(array, index);
            }

            public int Count => _map.Count;

            bool ICollection<TKey>.IsReadOnly => false;

            void ICollection<TKey>.Add(TKey item)
            {
                throw new NotImplementedException();
            }

            public void Clear()
            {
                _map.Clear();
            }

            public bool Contains(TKey item)
            {
                return _map.ContainsKey(item);
            }

            public void CopyTo(TKey[] array, int arrayIndex)
            {
                TKey[] keys = this.ToArray();
                keys.CopyTo(array, arrayIndex);
            }

            public IEnumerator<TKey> GetEnumerator()
            {
                return _map.Select(pair => pair.Key).GetEnumerator();
            }

            public bool Remove(TKey item)
            {
                return _map.Remove(item);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public class ValueCollection : ICollection<TValue>, ICollection
        {
            private readonly LinkedHashMap<TKey, TValue> _map;

            public ValueCollection(LinkedHashMap<TKey, TValue> map)
            {
                _map = map;
            }

            bool ICollection.IsSynchronized => false;

            object ICollection.SyncRoot => throw new NotSupportedException();

            void ICollection.CopyTo(Array array, int index)
            {
                TValue[] values = this.ToArray();
                values.CopyTo(array, index);
            }

            public int Count => _map.Count;

            bool ICollection<TValue>.IsReadOnly => false;

            void ICollection<TValue>.Add(TValue item)
            {
                throw new NotSupportedException();
            }

            public void Clear()
            {
                _map.Clear();
            }

            bool ICollection<TValue>.Contains(TValue item)
            {
                throw new NotImplementedException();
            }

            public void CopyTo(TValue[] array, int arrayIndex)
            {
                if (arrayIndex < 0 || arrayIndex > array.Length - Count)
                {
                    throw new ArgumentException();
                }

                int currentIndex = arrayIndex;
                foreach (TValue value in this)
                {
                    array[currentIndex++] = value;
                }
            }

            public IEnumerator<TValue> GetEnumerator()
            {
                return _map.Select(pair => pair.Value).GetEnumerator();
            }

            bool ICollection<TValue>.Remove(TValue item)
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private class DictionaryEnumerator : IDictionaryEnumerator
        {
            private readonly IEnumerator<KeyValuePair<TKey, TValue>> _enumerator;

            public DictionaryEnumerator(IEnumerator<KeyValuePair<TKey, TValue>> enumerator)
            {
                _enumerator = enumerator;
            }

            public object Current => _enumerator.Current;

            public DictionaryEntry Entry => new DictionaryEntry(_enumerator.Current.Key, _enumerator.Current.Value);

            public object Key => _enumerator.Current.Key;

            public object Value => _enumerator.Current.Value;

            public bool MoveNext()
            {
                return _enumerator.MoveNext();
            }

            public void Reset()
            {
                _enumerator.Reset();
            }
        }
    }
}