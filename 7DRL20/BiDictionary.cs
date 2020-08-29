using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine
{
    class BiDictionary<TKey, TValue>
    {
        Dictionary<TKey, TValue> _forward = new Dictionary<TKey, TValue>();
        Dictionary<TValue, TKey> _backward = new Dictionary<TValue, TKey>();

        public Indexer<TKey, TValue> Forward { get; private set; }
        public Indexer<TValue, TKey> Reverse { get; private set; }

        public BiDictionary()
        {
            Forward = new Indexer<TKey, TValue>(_forward);
            Reverse = new Indexer<TValue, TKey>(_backward);
        }

        public void Add(TKey key, TValue value)
        {
            _forward.Add(key, value);
            _backward.Add(value, key);
        }

        public void Remove(TKey key)
        {
            if (_forward.ContainsKey(key))
            {
                TValue value = _forward[key];
                _forward.Remove(key);
                _backward.Remove(value);
            }
        }

        public void Remove(TValue value)
        {
            if (_backward.ContainsKey(value))
            {
                TKey key = _backward[value];
                _forward.Remove(key);
                _backward.Remove(value);
            }
        }

        public void Clear()
        {
            _forward.Clear();
            _backward.Clear();
        }

        public class Indexer<T3, T4>
        {
            private readonly Dictionary<T3, T4> _dictionary;

            public Indexer(Dictionary<T3, T4> dictionary)
            {
                _dictionary = dictionary;
            }

            public T4 this[T3 index]
            {
                get { return _dictionary[index]; }
                set { _dictionary[index] = value; }
            }

            public bool Contains(T3 key)
            {
                return _dictionary.ContainsKey(key);
            }
        }
    }
}
