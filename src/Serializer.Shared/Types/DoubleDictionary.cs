using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Das.Serializer.Types
{
    public class DoubleDictionary<TKeyOne, TKeyTwo, TValue>
    {
        public DoubleDictionary()
        {
            _backing = new Dictionary<TKeyOne, Dictionary<TKeyTwo, TValue>>();
        }

        public TValue this[TKeyOne k1,
                           TKeyTwo k2]
        {
            get => _backing[k1][k2];
            set
            {
                if (!_backing.TryGetValue(k1, out var d2))
                {
                    d2 = new Dictionary<TKeyTwo, TValue>();
                    _backing[k1] = d2;
                }

                d2[k2] = value;
            }
        }

        public IEnumerable<TValue> Values
        {
            get
            {
                foreach (var kvp in _backing)
                {
                    foreach (var kvp2 in kvp.Value)
                    {
                        yield return kvp2.Value;
                    }
                }
            }
        }

        public void Add(TKeyOne k1,
                        TKeyTwo k2,
                        TValue value)
        {
            if (!_backing.TryGetValue(k1, out var d))
            {
                d = new Dictionary<TKeyTwo, TValue>();
                _backing.Add(k1, d);
            }

            d.Add(k2, value);
        }

        public void Clear()
        {
            _backing.Clear();
        }

        public Boolean TryGetValue(TKeyOne k1,
                                   TKeyTwo k2,
                                   out TValue value)
        {
            if (_backing.TryGetValue(k1, out var d) && d.TryGetValue(k2, out value))
                return true;

            value = default!;
            return false;
        }

        private readonly Dictionary<TKeyOne, Dictionary<TKeyTwo, TValue>> _backing;
    }
}
