using System;
using System.Threading.Tasks;
using Das.Serializer.NodeBuilders;

namespace Das.Serializer
{
    public class DefaultStateProvider : StateProvider
    {
        public DefaultStateProvider(ISerializerSettings settings)
            : this(settings,
                GetDynamicFacade(settings,
                    //out var xmlContext,
                    //out var jsonContext,
                    out var binaryContext),
                //xmlContext, jsonContext, 
                binaryContext)
        {
        }

        private DefaultStateProvider(ISerializerSettings settings,
                                     ISerializationCore dynamicFacade,
                                     //XmlContext xmlContext,
                                     //JsonContext jsonContext,
                                     BinaryContext binaryContext)
            : base(dynamicFacade, //xmlContext, jsonContext, 
                binaryContext, settings)
        {
        }

        public DefaultStateProvider() : this(DasSettings.CloneDefault())
        {
        }

        public static ISerializationCore GetDynamicFacade(
            ISerializerSettings settings,
            out BinaryContext binaryContext)
        {
            var dynamicFacade = new DynamicFacade(settings);
            var binaryNodeProvider = new BinaryNodeProvider(dynamicFacade, settings);

            //xmlContext = new XmlContext(dynamicFacade, settings);
            //jsonContext = new JsonContext(dynamicFacade, settings);
            binaryContext = new BinaryContext(dynamicFacade, settings, binaryNodeProvider);

            return dynamicFacade;
        }
    }
}
