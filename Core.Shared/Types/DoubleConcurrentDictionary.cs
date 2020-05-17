using System;
using System.Collections.Concurrent;
using System.Collections.Generic;


namespace Das.Serializer
{
    public class DoubleConcurrentDictionary<TKey1, TKey2, TValue>
    {
        public DoubleConcurrentDictionary()
        {
            _backingDictionary = new ConcurrentDictionary<TKey1, ConcurrentDictionary<TKey2, TValue>>();
        }

        public void Clear() => _backingDictionary.Clear();

        private ConcurrentDictionary<TKey1, ConcurrentDictionary<TKey2, TValue>> _backingDictionary;

        public TValue this[TKey1 k1, TKey2 k2]
        {
            get => _backingDictionary[k1][k2];
            set
            {
                if (!_backingDictionary.TryGetValue(k1, out var d2))
                {
                    d2 = new ConcurrentDictionary<TKey2, TValue>();
                    _backingDictionary[k1] = d2;
                }

                d2[k2] = value;
            }
        }

        public IEnumerable<TValue> GetValues(TKey1 k1, Func<TKey2, Boolean> predicate)
        {
            if (!_backingDictionary.TryGetValue(k1, out var d2))
                yield break;

            foreach (var kvp in d2)
            {
                if (predicate(kvp.Key))
                    yield return kvp.Value;
            }
        }

        public IEnumerable<TValue> GetOrAddValues<TKeyValue>(TKey1 k1, Func<TKey2, Boolean> predicate,
            Func<TKey1, IEnumerable<TKeyValue>> keyVals)
            where TKeyValue : TKey2, TValue
        {
            if (!_backingDictionary.TryGetValue(k1, out var d2))
            {
                var allMyVals = keyVals(k1);
                AddRange(k1, allMyVals);

                if (!_backingDictionary.TryGetValue(k1, out d2))
                    yield break;
            }

            foreach (var kvp in d2)
            {
                if (predicate(kvp.Key))
                    yield return kvp.Value;
            }
        }

        public void Remove(TKey1 k1)
        {
            _backingDictionary.TryRemove(k1, out _);
        }

        public void AddRange<TKeyValue>(TKey1 k1, IEnumerable<TKeyValue> keyVals)
        where TKeyValue : TKey2, TValue
        {
            var updating = _backingDictionary.GetOrAdd(k1, new ConcurrentDictionary<TKey2, TValue>());
            foreach (var kvp in keyVals)
                updating[kvp] = kvp;
        }

        public Boolean ContainsKey(TKey1 k1) => _backingDictionary.TryGetValue(k1, out _);
    }
}
