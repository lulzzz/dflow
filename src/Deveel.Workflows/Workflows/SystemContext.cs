﻿using System;

namespace Deveel.Workflows
{
    public sealed class SystemContext : IContext
    {
        private IServiceProvider provider;

        public SystemContext(IServiceProvider provider)
        {
            this.provider = provider;
        }

        IContext IContext.Parent => null;

        public void Dispose()
        {
            provider = null;
        }

        object IServiceProvider.GetService(Type serviceType)
        {
            return provider.GetService(serviceType);
        }
    }
}
