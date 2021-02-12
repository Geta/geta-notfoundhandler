// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.Logging;
using EPiServer.ServiceLocation;
using Geta.NotFound.Core;
using Geta.NotFoundHandler.Core.CustomRedirects;
using Geta.NotFoundHandler.Core.Data;
using Geta.NotFoundHandler.Core.Upgrade;

namespace Geta.NotFoundHandler.Core.Initialization
{
    /// <inheritdoc />
    /// <summary>
    /// Global File Not Found Handler, for handling Asp.net exceptions
    /// </summary>
    [InitializableModule]
    [ModuleDependency(typeof(EPiServer.Web.InitializationModule))]
    public class Custom404HandlerInitialization : IInitializableModule
    {
        private static readonly ILogger Logger = LogManager.GetLogger();

        // ReSharper disable UnusedAutoPropertyAccessor.Local
        private static Injected<RequestHandler> RequestHandler { get; set; }
        private static Injected<ErrorHandler> ErrorHandler { get; set; }
        // ReSharper restore UnusedAutoPropertyAccessor.Local

        public void Initialize(InitializationEngine context)
        {

            Logger.Debug("Initializing NotFoundHandler version check");
            var dba = DataAccessBaseEx.GetWorker();
            var version = dba.CheckNotFoundHandlerVersion();
            if (version != Geta.NotFound.Core.Configuration.Configuration.CurrentVersion)
            {
                Logger.Debug("Older version found. Version nr. :" + version);
                Upgrader.Start(version);
            }
            else
            {
                Upgrader.Valid = true;
            }

            // Load all custom redirects into memory
            // TODO: create better load of the cache
            var handler = CustomRedirectHandler.Current;
        }

        public void Uninitialize(InitializationEngine context)
        {
        }

        public void Preload(string[] parameters)
        {
        }

        public void InitializeHttpEvents(HttpApplication application)
        {
            application.Error += OnError;
            application.EndRequest += OnEndRequest;
        }

        private void OnEndRequest(object sender, EventArgs eventArgs)
        {
            try
            {
                RequestHandler.Service.Handle(GetContext());
            }
            catch (HttpException ex)
            {
                Logger.Warning("Http error (headers already written or similar) on 404 handling.", ex);
            }
            catch (Exception ex)
            {
                Logger.Error("Error on 404 handling.", ex);
                throw;
            }
        }

        private void OnError(object sender, EventArgs eventArgs)
        {
            try
            {
                ErrorHandler.Service.Handle(GetContext());
            }
            catch (HttpException ex)
            {
                Logger.Warning("Http error (headers already written or similar) on 404 handling.", ex);
            }
            catch (Exception ex)
            {
                Logger.Error("Error on 404 handling.", ex);
                throw;
            }
        }

        private static HttpContextBase GetContext()
        {
            var context = HttpContext.Current;
            if (context != null) return new HttpContextWrapper(context);

            Logger.Debug("No HTTPContext, returning");
            return null;
        }
    }
}