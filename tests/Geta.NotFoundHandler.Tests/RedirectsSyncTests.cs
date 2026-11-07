using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using Geta.NotFoundHandler.Core;
using Geta.NotFoundHandler.Core.Providers.RegexRedirects;
using Geta.NotFoundHandler.Core.Redirects;
using Geta.NotFoundHandler.Data;
using Geta.NotFoundHandler.Infrastructure.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Geta.NotFoundHandler.Tests
{
    public class RedirectsSyncTests
    {
        private static readonly Uri FirstUri = new("http://example.com/first");
        private static readonly Uri SecondUri = new("http://example.com/second");

        private readonly IRedirectsService _redirectsService = A.Fake<IRedirectsService>();
        private readonly IRedirectLoader _redirectLoader = A.Fake<IRedirectLoader>();
        private readonly IRegexRedirectCache _regexCache = A.Fake<IRegexRedirectCache>();
        private readonly CustomRedirectHandler _redirectHandler = new();
        private readonly RedirectsEvents _redirectsEvents = new();
        private readonly RedirectsInitializer _initializer;

        public RedirectsSyncTests()
        {
            _initializer = new RedirectsInitializer(
                _redirectsEvents,
                _redirectHandler,
                () => _redirectsService,
                Enumerable.Empty<INotFoundHandler>());
        }

        [Fact]
        public async Task Initialize_does_not_let_a_slow_load_overwrite_a_newer_one()
        {
            var inFirstLoad = new ManualResetEventSlim();
            var finishFirstLoad = new ManualResetEventSlim();
            var loads = 0;
            A.CallTo(() => _redirectsService.GetAll()).ReturnsLazily(() =>
            {
                if (Interlocked.Increment(ref loads) > 1)
                {
                    return new[] { Redirect("/first"), Redirect("/second") };
                }

                // The first load read the table before the second redirect was saved.
                inFirstLoad.Set();
                finishFirstLoad.Wait(TimeSpan.FromSeconds(10));
                return new[] { Redirect("/first") };
            });

            var slowLoad = Task.Run(_initializer.Initialize);
            Assert.True(inFirstLoad.Wait(TimeSpan.FromSeconds(10)));
            var newerLoad = Task.Run(_initializer.Initialize);
            await Task.WhenAny(newerLoad, Task.Delay(300));
            finishFirstLoad.Set();
            await Task.WhenAll(slowLoad, newerLoad);

            Assert.NotNull(_redirectHandler.Find(SecondUri));
        }

        [Fact]
        public async Task Initialize_shares_a_load_that_started_after_it_was_called()
        {
            var inFirstLoad = new ManualResetEventSlim();
            var finishFirstLoad = new ManualResetEventSlim();
            var loads = 0;
            A.CallTo(() => _redirectsService.GetAll()).ReturnsLazily(() =>
            {
                if (Interlocked.Increment(ref loads) == 1)
                {
                    inFirstLoad.Set();
                    finishFirstLoad.Wait(TimeSpan.FromSeconds(10));
                }

                return Array.Empty<CustomRedirect>();
            });

            var running = Task.Run(_initializer.Initialize);
            Assert.True(inFirstLoad.Wait(TimeSpan.FromSeconds(10)));
            // Own threads, as these block and the thread pool can take longer than the delay below to add more.
            var waiting = Enumerable.Range(0, 5)
                .Select(_ => Task.Factory.StartNew(_initializer.Initialize, TaskCreationOptions.LongRunning))
                .ToList();
            await Task.Delay(300);
            finishFirstLoad.Set();
            await Task.WhenAll(waiting.Append(running));

            // One load already running, and one for everything that arrived while it ran.
            Assert.Equal(2, loads);
        }

        [Fact]
        public void Initialize_keeps_working_after_a_failed_load()
        {
            A.CallTo(() => _redirectsService.GetAll())
                .Throws(new InvalidOperationException("database unavailable")).Once()
                .Then.Returns(new[] { Redirect("/first") });

            Assert.Throws<InvalidOperationException>(_initializer.Initialize);
            _initializer.Initialize();

            Assert.NotNull(_redirectHandler.Find(FirstUri));
        }

        [Fact]
        public void RedirectsUpdated_reaches_every_handler_when_one_fails()
        {
            var events = new RedirectsEvents();
            var reachedSecond = false;
            events.OnRedirectsUpdated += _ => throw new InvalidOperationException("reload failed");
            events.OnRedirectsUpdated += _ => reachedSecond = true;

            var exception = Assert.Throws<AggregateException>(events.RedirectsUpdated);

            Assert.True(reachedSecond);
            Assert.IsType<InvalidOperationException>(Assert.Single(exception.InnerExceptions));
        }

        [Fact]
        public void RegexRedirectsUpdated_reaches_every_handler_when_one_fails()
        {
            var events = new RedirectsEvents();
            var reachedSecond = false;
            events.OnRegexRedirectsUpdated += _ => throw new InvalidOperationException("failed");
            events.OnRegexRedirectsUpdated += _ => reachedSecond = true;

            Assert.Throws<AggregateException>(events.RegexRedirectsUpdated);

            Assert.True(reachedSecond);
        }

        [Fact]
        public void Refresh_reloads_on_the_first_check_as_an_update_could_have_been_missed_since_startup()
        {
            GivenStored("v1", "/first");

            CreateRefreshService().Refresh();

            Assert.NotNull(_redirectHandler.Find(FirstUri));
        }

        [Fact]
        public void Refresh_does_not_reload_when_nothing_changed()
        {
            GivenStored("v1", "/first");
            var sut = CreateRefreshService();
            sut.Refresh();
            Fake.ClearRecordedCalls(_redirectsService);

            sut.Refresh();

            A.CallTo(() => _redirectsService.GetAll()).MustNotHaveHappened();
        }

        [Fact]
        public void Refresh_picks_up_a_redirect_saved_on_another_instance()
        {
            GivenStored("v1", "/first");
            var sut = CreateRefreshService();
            sut.Refresh();
            GivenStored("v2", "/first", "/second");

            sut.Refresh();

            Assert.NotNull(_redirectHandler.Find(SecondUri));
        }

        [Fact]
        public void Refresh_tries_again_after_a_failed_reload()
        {
            GivenStored("v1", "/first");
            var sut = CreateRefreshService();
            sut.Refresh();
            A.CallTo(() => _redirectLoader.GetChecksum()).Returns("v2");
            A.CallTo(() => _redirectsService.GetAll())
                .Throws(new InvalidOperationException("database unavailable")).Once()
                .Then.Returns(new[] { Redirect("/first"), Redirect("/second") });

            sut.Refresh();
            Assert.Null(_redirectHandler.Find(SecondUri));
            sut.Refresh();

            Assert.NotNull(_redirectHandler.Find(SecondUri));
        }

        [Fact]
        public void Refresh_survives_a_failed_checksum()
        {
            A.CallTo(() => _redirectLoader.GetChecksum()).Throws(new InvalidOperationException("database unavailable"));

            var exception = Record.Exception(CreateRefreshService().Refresh);

            Assert.Null(exception);
        }

        [Fact]
        public void Refresh_leaves_redirects_alone_when_the_loader_has_no_checksum()
        {
            A.CallTo(() => _redirectLoader.GetChecksum()).Returns(null);

            CreateRefreshService().Refresh();

            A.CallTo(() => _redirectsService.GetAll()).MustNotHaveHappened();
        }

        [Fact]
        public void Refresh_clears_the_regex_cache_every_time()
        {
            GivenStored("v1");
            var sut = CreateRefreshService();

            sut.Refresh();
            sut.Refresh();

            A.CallTo(() => _regexCache.Remove()).MustHaveHappenedTwiceExactly();
        }

        [Fact]
        public void Refresh_keeps_the_cached_regex_redirects_when_the_database_is_unavailable()
        {
            A.CallTo(() => _redirectLoader.GetChecksum()).Throws(new InvalidOperationException("database unavailable"));

            CreateRefreshService().Refresh();

            A.CallTo(() => _regexCache.Remove()).MustNotHaveHappened();
        }

        [Fact]
        public void Refresh_drops_a_redirect_when_the_stored_ones_return_to_the_last_checksum()
        {
            GivenStored("v1", "/first");
            var sut = CreateRefreshService();
            sut.Refresh();

            // Saved and picked up through an event here, then removed with that event getting lost.
            GivenStored("v2", "/first", "/second");
            _initializer.Initialize();
            GivenStored("v1", "/first");

            sut.Refresh();

            Assert.Null(_redirectHandler.Find(SecondUri));
        }

        [Fact]
        public void Refresh_does_not_create_the_initializer_until_it_is_needed()
        {
            var created = false;
            A.CallTo(() => _redirectLoader.GetChecksum()).Returns(null);
            var sut = new RedirectsRefreshService(
                () =>
                {
                    created = true;
                    return _initializer;
                },
                _redirectLoader,
                _regexCache,
                Options.Create(new NotFoundHandlerOptions()),
                NullLogger<RedirectsRefreshService>.Instance);

            sut.Refresh();

            Assert.False(created);
        }

        [Fact]
        public void A_custom_loader_has_no_checksum_unless_it_adds_one()
        {
            IRedirectLoader loader = new LoaderWithoutChecksum();

            Assert.Null(loader.GetChecksum());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task The_service_does_not_run_when_the_interval_is_turned_off(int seconds)
        {
            var sut = CreateRefreshService(TimeSpan.FromSeconds(seconds));

            await sut.StartAsync(CancellationToken.None);
            await sut.ExecuteTask;

            A.CallTo(() => _redirectLoader.GetChecksum()).MustNotHaveHappened();
        }

        [Fact]
        public async Task An_interval_too_short_for_the_timer_does_not_stop_the_application()
        {
            var sut = CreateRefreshService(TimeSpan.FromTicks(1));

            await sut.StartAsync(CancellationToken.None);
            await sut.ExecuteTask;

            A.CallTo(() => _redirectLoader.GetChecksum()).MustNotHaveHappened();
        }

        [Fact]
        public async Task An_interval_too_long_for_the_timer_does_not_stop_the_application()
        {
            var sut = CreateRefreshService(TimeSpan.FromDays(365));

            await sut.StartAsync(CancellationToken.None);

            Assert.False(sut.ExecuteTask.IsFaulted);
            await sut.StopAsync(CancellationToken.None);
        }

        [Fact]
        public async Task The_service_checks_on_the_interval()
        {
            GivenStored("v1", "/first");
            var checked_ = new TaskCompletionSource();
            A.CallTo(() => _redirectLoader.GetChecksum()).Invokes(() => checked_.TrySetResult()).Returns("v1");
            var sut = CreateRefreshService(TimeSpan.FromMilliseconds(10));

            await sut.StartAsync(CancellationToken.None);
            await checked_.Task.WaitAsync(TimeSpan.FromSeconds(10));
            await sut.StopAsync(CancellationToken.None);
        }

        private void GivenStored(string checksum, params string[] oldUrls)
        {
            A.CallTo(() => _redirectLoader.GetChecksum()).Returns(checksum);
            A.CallTo(() => _redirectsService.GetAll()).Returns(oldUrls.Select(Redirect).ToList());
        }

        private static CustomRedirect Redirect(string oldUrl)
        {
            return new CustomRedirect(oldUrl, "/new");
        }

        private RedirectsRefreshService CreateRefreshService(TimeSpan? interval = null)
        {
            var options = new NotFoundHandlerOptions();
            if (interval.HasValue)
            {
                options.RedirectsRefreshInterval = interval.Value;
            }

            return new RedirectsRefreshService(
                () => _initializer,
                _redirectLoader,
                _regexCache,
                Options.Create(options),
                NullLogger<RedirectsRefreshService>.Instance);
        }

        private class LoaderWithoutChecksum : IRedirectLoader
        {
            public CustomRedirect GetByOldUrl(string oldUrl) => null;
            public IEnumerable<CustomRedirect> GetAll() => Enumerable.Empty<CustomRedirect>();
            public IEnumerable<CustomRedirect> GetByState(RedirectState state) => Enumerable.Empty<CustomRedirect>();
            public IEnumerable<CustomRedirect> Find(string searchText) => Enumerable.Empty<CustomRedirect>();
            public CustomRedirect Get(Guid id) => null;
        }
    }
}
