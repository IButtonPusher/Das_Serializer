using System;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public abstract class BaseState : SerializerCore,
                                      ISerializationState
    {
        protected BaseState(ISerializationCore stateProvider,
                            ISerializerSettings settings)
            : base(stateProvider, settings)
        {
        }

        public abstract void Dispose();

        public void UpdateSettings(ISerializerSettings settings)
        {
            Settings = settings;
        }
    }
}
