using System;
using System.Collections.Generic;

namespace Das.Serializer.Types
{
    public class DoubleDictionary<TKeyOne, TKeyTwo, TValue>
    {
        private Dictionary<TKeyOne, Dictionary<TKeyTwo, TValue>> _backing;

        public DoubleDictionary()
        {
            _backing = new Dictionary<TKeyOne, Dictionary<TKeyTwo, TValue>>();
        }

        public void Add(TKeyOne k1, TKeyTwo k2, TValue value)
        {
            if (!_backing.TryGetValue(k1, out var d))
            {
                d = new Dictionary<TKeyTwo, TValue>();
                _backing.Add(k1, d);
            }

            d.Add(k2, value);
        }

        public Boolean TryGetValue(TKeyOne k1, TKeyTwo k2, out TValue value)
        {
            if (_backing.TryGetValue(k1, out var d) && d.TryGetValue(k2, out value))
                return true;

            value = default;
            return false;
        }
    }
}
