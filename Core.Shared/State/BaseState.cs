﻿using System;
using Das.Serializer;

namespace Serializer.Core
{
    public abstract class BaseState : CoreContext, ISerializationState
    {
        protected BaseState(ISerializationCore stateProvider, ISerializerSettings settings)
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