// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;

namespace Geta.NotFoundHandler.Tests.Hosting
{
    public class TestServerBuilder
    {
        private readonly IList<Action<IServiceCollection>> _serviceActions;
        private readonly IList<Action<IApplicationBuilder>> _applicationActions;

        public TestServerBuilder()
        {
            _applicationActions = new List<Action<IApplicationBuilder>>();
            _serviceActions = new List<Action<IServiceCollection>>();
        }

        public virtual void ConfigureServices(Action<IServiceCollection> action)
        {
            _serviceActions.Add(action);
        }

        public virtual void Configure(Action<IApplicationBuilder> action)
        {
            _applicationActions.Add(action);
        }

        public virtual TestServer Build()
        {
            var builder = new WebHostBuilder();

            foreach (var action in _serviceActions)
            {
                builder.ConfigureServices(action);
            }

            foreach (var action in _applicationActions)
            {
                builder.Configure(action);
            }

            return new TestServer(builder);
        }
    }
}
