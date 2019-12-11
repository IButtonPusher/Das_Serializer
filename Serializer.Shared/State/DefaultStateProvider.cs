﻿using Serializer.Core;
using Serializer.Core.State;

namespace Das.Serializer
{
    public class DefaultStateProvider : StateProvider
    {
        public DefaultStateProvider(ISerializerSettings settings, BinaryLogger binaryLogger)
            : base(GetDynamicFacade(settings, binaryLogger),
                _xmlContext, _jsonContext, _binaryContext, settings) { }

        public DefaultStateProvider(ISerializerSettings settings) : this(settings, new BinaryLogger()) { }

        public DefaultStateProvider() : this(DasSettings.Default, new BinaryLogger()) { }

        public static ISerializationCore GetDynamicFacade(ISerializerSettings settings, BinaryLogger logger)
        {
            var dynamicFacade = new DynamicFacade(settings);

            _xmlContext = new XmlContext(dynamicFacade, settings);
            _jsonContext = new JsonContext(dynamicFacade, settings);
            _binaryContext = new BinaryContext(dynamicFacade, settings,logger);

            return dynamicFacade;
        }


        private static XmlContext _xmlContext;
        private static JsonContext _jsonContext;
        private static BinaryContext _binaryContext;
    }
}