using Das.Serializer.State;

namespace Das.Serializer
{
    public class DefaultStateProvider : StateProvider
    {
        public DefaultStateProvider(ISerializerSettings settings)
            : this(settings, 
                
                GetDynamicFacade(settings, 
                out var xmlContext,
                out var jsonContext,
                out var binaryContext), 

                xmlContext, jsonContext, binaryContext)
        {
            
        }

        private DefaultStateProvider(ISerializerSettings settings,
            ISerializationCore dynamicFacade,
            XmlContext xmlContext,
            JsonContext jsonContext,
            BinaryContext binaryContext) 
            : base(dynamicFacade,xmlContext, 
                jsonContext, binaryContext, settings)
        {

        }

        public DefaultStateProvider() : this(DasSettings.Default) { }

        public static ISerializationCore GetDynamicFacade(
            ISerializerSettings settings,
            out XmlContext xmlContext,
            out JsonContext jsonContext,
            out BinaryContext binaryContext)
        {
            var dynamicFacade = new DynamicFacade(settings);

            xmlContext = new XmlContext(dynamicFacade, settings);
            jsonContext = new JsonContext(dynamicFacade, settings);
            binaryContext = new BinaryContext(dynamicFacade, settings);

            return dynamicFacade;
        }

    }
}