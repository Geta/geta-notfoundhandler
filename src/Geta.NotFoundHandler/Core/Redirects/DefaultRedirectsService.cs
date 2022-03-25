// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using Geta.NotFoundHandler.Data;

namespace Geta.NotFoundHandler.Core.Redirects
{
    public class DefaultRedirectsService : IRedirectsService
    {
        private readonly IRepository<CustomRedirect> _repository;
        private readonly IRedirectLoader _redirectLoader;
        private readonly RedirectsEvents _redirectsEvents;

        public DefaultRedirectsService(
            IRepository<CustomRedirect> repository,
            IRedirectLoader redirectLoader,
            RedirectsEvents redirectsEvents)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _redirectLoader = redirectLoader ?? throw new ArgumentNullException(nameof(redirectLoader));
            _redirectsEvents = redirectsEvents;
        }

        public IEnumerable<CustomRedirect> GetAll()
        {
            return _redirectLoader.GetAll();
        }

        public IEnumerable<CustomRedirect> GetSaved()
        {
            return _redirectLoader.GetByState(RedirectState.Saved);
        }

        public IEnumerable<CustomRedirect> GetIgnored()
        {
            return _redirectLoader.GetByState(RedirectState.Ignored);
        }

        public IEnumerable<CustomRedirect> GetDeleted()
        {
            return _redirectLoader.GetByState(RedirectState.Deleted);
        }

        public IEnumerable<CustomRedirect> Search(string searchText)
        {
            return _redirectLoader.Find(searchText);
        }

        public void AddOrUpdate(CustomRedirect redirect)
        {
            AddOrUpdate(redirect, notifyUpdated: true);
        }

        public void AddOrUpdate(IEnumerable<CustomRedirect> redirects)
        {
            foreach (var redirect in redirects)
            {
                AddOrUpdate(redirect, notifyUpdated: false);
            }

            _redirectsEvents.RedirectsUpdated();
        }

        public void AddDeletedRedirect(string oldUrl)
        {
            var redirect = new CustomRedirect
            {
                OldUrl = oldUrl, NewUrl = string.Empty, State = Convert.ToInt32(RedirectState.Deleted)
            };
            AddOrUpdate(redirect, notifyUpdated: true);
        }

        public void DeleteByOldUrl(string oldUrl)
        {
            var match = _redirectLoader.GetByOldUrl(oldUrl);
            if (match != null)
            {
                _repository.Delete(match);
                _redirectsEvents.RedirectsUpdated();
            }
        }

        public int DeleteAll()
        {
            // In order to avoid a database timeout, we delete the items one by one.
            var redirects = GetAll().ToList();
            foreach (var redirect in redirects)
            {
                _repository.Delete(redirect);
            }

            _redirectsEvents.RedirectsUpdated();
            return redirects.Count;
        }

        public int DeleteAllIgnored()
        {
            // In order to avoid a database timeout, we delete the items one by one.
            var ignoredRedirects = GetIgnored().ToList();
            foreach (var redirect in ignoredRedirects)
            {
                _repository.Delete(redirect);
            }

            _redirectsEvents.RedirectsUpdated();
            return ignoredRedirects.Count;
        }

        public void AddOrUpdate(CustomRedirect redirect, bool notifyUpdated)
        {
            var match = _redirectLoader.GetByOldUrl(redirect.OldUrl);

            // if there is a match, replace the value.
            if (match != null)
            {
                redirect.Id = match.Id;
            }

            _repository.Save(redirect);

            if (notifyUpdated)
            {
                _redirectsEvents.RedirectsUpdated();
            }
        }
    }
}
