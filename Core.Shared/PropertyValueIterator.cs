using System;
using System.Collections;
using System.Collections.Generic;

namespace Das.Serializer
{
    public class PropertyValueIterator<TProperty> : IPropertyValueIterator<TProperty>
        where TProperty : class, INamedValue
    {
        protected List<TProperty> _propertyValues;
        protected TProperty _currentValue;
        protected Int32 _current;

        public PropertyValueIterator()
        {
            _propertyValues = new List<TProperty>();
        }

        public virtual void Add(TProperty property)
        {
            _propertyValues.Add(property);
        }

        public virtual Boolean MoveNext()
        {
            if (_current >= _propertyValues.Count )
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
            _currentValue = null;
        }

        public Int32 Count => _propertyValues.Count;

        public Boolean Equals(INamedField other)
        {
            throw new NotImplementedException();
        }

        public Type Type
        {
            get => _currentValue.Type;
            set => throw new NotSupportedException();
        }

        public String Name => _currentValue.Name;

        public Object Value => _currentValue.Value;

        public void Dispose()
        {
            _currentValue.Dispose();
        }

        public Boolean IsEmptyInitialized => _currentValue.IsEmptyInitialized;

        public Type DeclaringType { get; set; }
        public IEnumerator<TProperty> GetEnumerator()
        {
            return _propertyValues.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
