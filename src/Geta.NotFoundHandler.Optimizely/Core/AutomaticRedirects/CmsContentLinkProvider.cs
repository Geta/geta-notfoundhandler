// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Linq;
using EPiServer;
using EPiServer.Applications;
using EPiServer.Core;

namespace Geta.NotFoundHandler.Optimizely.Core.AutomaticRedirects
{
    public class CmsContentLinkProvider : IContentLinkProvider
    {
        private readonly IApplicationRepository _applicationRepository;
        private readonly IContentLoader _contentLoader;

        public CmsContentLinkProvider(IApplicationRepository applicationRepository, IContentLoader contentLoader)
        {
            _applicationRepository = applicationRepository;
            _contentLoader = contentLoader;
        }

        public IEnumerable<ContentReference> GetAllLinks()
        {
            return _applicationRepository.List()
                .OfType<IRoutableApplication>()
                .SelectMany(app => _contentLoader.GetDescendents(app.EntryPoint));
        }
    }
}
