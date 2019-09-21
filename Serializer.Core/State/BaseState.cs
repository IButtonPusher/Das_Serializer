using System;
using Das;
using Das.Serializer;

namespace Serializer.Core
{
    public abstract class BaseState : CoreContext, ISerializationState
    {
        private readonly IStateProvider _stateProvider;

        protected BaseState(IStateProvider stateProvider, ISerializerSettings settings)
            : base(stateProvider, settings)
        {
            _stateProvider = stateProvider;
        }

        public T Copy<T>(T @from) where T : class =>
            _stateProvider.ObjectConverter.Copy(from, Settings);

        public T Copy<T>(T from, ISerializerSettings settings) where T : class
            => _stateProvider.ObjectConverter.Copy(from, settings);


        public void Copy<T>(T from, ref T to, ISerializerSettings settings) where T : class
            => _stateProvider.ObjectConverter.Copy(from, ref to, settings);

        public T ConvertEx<T>(Object obj, ISerializerSettings settings) =>
            _stateProvider.ObjectConverter.ConvertEx<T>(obj, settings);

        public T ConvertEx<T>(Object obj) =>
            _stateProvider.ObjectConverter.ConvertEx<T>(obj, Settings);

        public Object ConvertEx(Object obj, Type newObjectType, ISerializerSettings settings)
            => _stateProvider.ObjectConverter.ConvertEx(obj, newObjectType, settings);

        public Object SpawnCollection(Object[] objects, Type collectionType,
            ISerializerSettings settings, Type collectionGenericArgs = null)
            => _stateProvider.ObjectConverter.SpawnCollection(objects,
                collectionType, settings);


        public IObjectConverter ObjectConverter => _stateProvider.ObjectConverter;

        public abstract void Dispose();
    }
}