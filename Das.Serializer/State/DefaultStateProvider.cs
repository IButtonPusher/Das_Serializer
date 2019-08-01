using Serializer.Core;
using System;
using Serializer.Core.State;

namespace Das.Serializer
{
    public class DefaultStateProvider : StateProvider
    {
        public DefaultStateProvider(ISerializerSettings settings)
            : base(GetDynamicFacade(settings),
                _xmlContext, _jsonContext, _binaryContext, settings)
        {
        }

        public DefaultStateProvider()
            : base(GetDynamicFacade(DasSettings.Default),
                _xmlContext, _jsonContext, _binaryContext, DasSettings.Default)
        {
        }


        public static IDynamicFacade GetDynamicFacade()
            => GetDynamicFacade(DasSettings.Default);

        public static IDynamicFacade GetDynamicFacade(ISerializerSettings settings)
        {
            var dynamicFacade = new DynamicFacade(settings);

            _xmlContext = new XmlContext(dynamicFacade, settings);
            _jsonContext = new JsonContext(dynamicFacade, settings);
            _binaryContext = new BinaryContext(dynamicFacade, settings);

            return dynamicFacade;
        }


        private static XmlContext _xmlContext;
        private static JsonContext _jsonContext;
        private static BinaryContext _binaryContext;
    }
}