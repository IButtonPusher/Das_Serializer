using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public class PropertyValueIterator<TProperty> : IPropertyValueIterator<TProperty>
        where TProperty : class, INamedValue
    {
#pragma warning disable 8618
        public PropertyValueIterator()
#pragma warning restore 8618
        {
            _propertyValues = new List<TProperty>();
        }

        public Boolean MoveNext()
        {
            if (_current >= _propertyValues.Count)
                return false;
            _currentValue = _propertyValues[_current];
            _current++;
            return true;
        }

        public TProperty this[Int32 index] => _propertyValues[index];

        public void Clear()
        {
            _propertyValues.Clear();
            _current = 0;
            _currentValue = null!;
        }

        public Int32 Count => _propertyValues.Count;


        public Type? Type
        {
            get => _currentValue.Type;
            set => throw new NotSupportedException();
        }

        public String Name => _currentValue.Name;

        public Object? Value => _currentValue.Value;

        public void Dispose()
        {
            _currentValue.Dispose();
        }

        public Boolean IsEmptyInitialized => _currentValue.IsEmptyInitialized;

        public IEnumerator<TProperty> GetEnumerator()
        {
            return _propertyValues.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(TProperty property)
        {
            _propertyValues.Add(property);
        }

        protected readonly List<TProperty> _propertyValues;

        protected Int32 _current;


        protected TProperty _currentValue;
    }
}
