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

        public void AddOrUpdate(CustomRedirect redirect, bool notifyUpdated)
        {
            // Note: Forms and imported files can carry whitespace that would silently stop the redirect
            // matching. Rows saved earlier stay as they are, so an upgrade doesn't change what they do.
            redirect.OldUrl = redirect.OldUrl?.Trim();
            redirect.NewUrl = redirect.NewUrl?.Trim();

            var match = _redirectLoader.GetByOldUrl(redirect.OldUrl);

            if (!HasChanged(match, redirect))
            {
                return;
            }

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
            DeleteByOldUrl(new[] { oldUrl });
        }

        public void DeleteByOldUrl(IEnumerable<string> oldUrls)
        {
            foreach (var oldUrl in oldUrls)
            {
                DeleteByOldUrl(oldUrl, notifyUpdated: false);
            }

            _redirectsEvents.RedirectsUpdated();
        }

        public void DeleteByOldUrl(string oldUrl, bool notifyUpdated)
        {
            // Note: Try it as given first, as rows saved before urls were trimmed can still have whitespace.
            var match = _redirectLoader.GetByOldUrl(oldUrl) ?? _redirectLoader.GetByOldUrl(oldUrl?.Trim());
            if (match != null)
            {
                _repository.Delete(match);
            }

            if (notifyUpdated)
            {
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

        public void DeleteById(Guid id)
        {
            var match = _redirectLoader.Get(id);

            if (match != null)
            {
                _repository.Delete(match);
            }

            _redirectsEvents.RedirectsUpdated();
        }

        public CustomRedirect Get(Guid id)
        {
            return _redirectLoader.Get(id);
        }

        private static bool HasChanged(CustomRedirect oldRedirect, CustomRedirect newRedirect)
        {
            if (oldRedirect == null) return true;

            var comparer = new CustomRedirectEqualityComparer();

            return !comparer.Equals(oldRedirect, newRedirect);
        }
    }
}
