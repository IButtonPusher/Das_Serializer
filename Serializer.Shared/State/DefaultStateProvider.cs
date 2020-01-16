using Das.Serializer.ProtoBuf;
using Das.Serializer.State;

namespace Das.Serializer
{
    public class DefaultStateProvider : StateProvider
    {
        public DefaultStateProvider(ISerializerSettings settings)
            : base(GetDynamicFacade(settings),
                _xmlContext, _jsonContext, _binaryContext, settings) { }

        public DefaultStateProvider() : this(DasSettings.Default) { }

        public static ISerializationCore GetDynamicFacade(ISerializerSettings settings)
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