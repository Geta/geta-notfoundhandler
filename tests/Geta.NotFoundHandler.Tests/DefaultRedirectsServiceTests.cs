using System;
using System.Collections.Generic;
using System.Linq;
using FakeItEasy;
using Geta.NotFoundHandler.Core;
using Geta.NotFoundHandler.Core.Redirects;
using Geta.NotFoundHandler.Data;
using Xunit;

namespace Geta.NotFoundHandler.Tests
{
    public class DefaultRedirectsServiceTests
    {
        private static readonly Uri OldUri = new Uri("http://example.com/old");

        private readonly List<CustomRedirect> _storedRedirects;
        private readonly RedirectsEvents _redirectsEvents;
        private readonly CustomRedirectHandler _redirectHandler;
        private readonly RedirectsInitializer _redirectsInitializer;
        private readonly DefaultRedirectsService _sut;

        public DefaultRedirectsServiceTests()
        {
            _storedRedirects = new List<CustomRedirect>();
            var repository = A.Fake<IRepository<CustomRedirect>>();
            A.CallTo(() => repository.Delete(A<CustomRedirect>._))
                .Invokes((CustomRedirect redirect) => _storedRedirects.Remove(redirect));
            A.CallTo(() => repository.Save(A<CustomRedirect>._))
                .Invokes((CustomRedirect redirect) => _storedRedirects.Add(redirect));
            var redirectLoader = A.Fake<IRedirectLoader>();
            A.CallTo(() => redirectLoader.GetAll()).ReturnsLazily(() => _storedRedirects.ToList());
            A.CallTo(() => redirectLoader.Get(A<Guid>._))
                .ReturnsLazily((Guid id) => _storedRedirects.FirstOrDefault(x => x.Id == id));
            A.CallTo(() => redirectLoader.GetByOldUrl(A<string>._))
                .ReturnsLazily((string oldUrl) => _storedRedirects.FirstOrDefault(x => x.OldUrl == oldUrl));

            _redirectsEvents = new RedirectsEvents();
            _sut = new DefaultRedirectsService(repository, redirectLoader, _redirectsEvents);

            _redirectHandler = new CustomRedirectHandler();
            _redirectsInitializer = new RedirectsInitializer(
                _redirectsEvents,
                _redirectHandler,
                () => _sut,
                Enumerable.Empty<INotFoundHandler>());
        }

        [Fact]
        public void DeleteById_raises_redirects_updated()
        {
            var redirect = GivenStoredRedirect();
            var raised = false;
            _redirectsEvents.OnRedirectsUpdated += _ => raised = true;

            _sut.DeleteById(redirect.Id.Value);

            Assert.True(raised);
        }

        [Fact]
        public void DeleteById_removes_redirect_from_redirect_handler()
        {
            var redirect = GivenStoredRedirect();
            _redirectsInitializer.Initialize();
            Assert.NotNull(_redirectHandler.Find(OldUri));

            _sut.DeleteById(redirect.Id.Value);

            Assert.Null(_redirectHandler.Find(OldUri));
        }

        [Theory]
        [InlineData("/old")]
        [InlineData(" /old")]
        [InlineData("/old\t")]
        public void DeleteByOldUrl_ignores_whitespace_around_the_url(string oldUrl)
        {
            GivenStoredRedirect();

            _sut.DeleteByOldUrl(oldUrl);

            Assert.Empty(_storedRedirects);
        }

        [Fact]
        public void DeleteByOldUrl_finds_a_row_saved_earlier_with_whitespace()
        {
            _storedRedirects.Add(new CustomRedirect("/old\t", "/new") { Id = Guid.NewGuid() });

            _sut.DeleteByOldUrl("/old\t");

            Assert.Empty(_storedRedirects);
        }

        /// <summary>
        /// https://github.com/Geta/geta-notfoundhandler/issues/123
        /// </summary>
        [Theory]
        [InlineData("/old ", "/new ")]
        [InlineData(" /old", " /new")]
        [InlineData("/old\t", "\t/new")]
        [InlineData("/old\r\n", "/new\n")]
        public void AddOrUpdate_removes_whitespace_around_urls(string oldUrl, string newUrl)
        {
            _sut.AddOrUpdate(new CustomRedirect(oldUrl, newUrl));

            var saved = Assert.Single(_storedRedirects);
            Assert.Equal("/old", saved.OldUrl);
            Assert.Equal("/new", saved.NewUrl);
        }

        [Fact]
        public void AddOrUpdate_leaves_a_row_saved_earlier_with_whitespace_unused()
        {
            _storedRedirects.Add(new CustomRedirect("/old\t", "/first") { Id = Guid.NewGuid() });

            _sut.AddOrUpdate(new CustomRedirect("/old", "/second"));
            _redirectsInitializer.Initialize();

            Assert.Equal("/second", _redirectHandler.Find(OldUri).NewUrl);
        }

        private CustomRedirect GivenStoredRedirect()
        {
            var redirect = new CustomRedirect(OldUri.PathAndQuery, "/new") { Id = Guid.NewGuid() };
            _storedRedirects.Add(redirect);
            return redirect;
        }
    }
}
