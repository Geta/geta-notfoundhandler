using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EPiServer.Events.Clients;
using FakeItEasy;
using Geta.NotFoundHandler.Core;
using Geta.NotFoundHandler.Core.Providers.RegexRedirects;
using Geta.NotFoundHandler.Core.Redirects;
using Geta.NotFoundHandler.Data;
using Geta.NotFoundHandler.Optimizely.Core.Events;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Geta.NotFoundHandler.Optimizely.Tests
{
    public class OptimizelySyncEventsTests
    {
        private readonly IEventRegistry _eventRegistry = A.Fake<IEventRegistry>();
        private readonly RedirectsEvents _redirectsEvents = new();
        private readonly OptimizelySyncEvents _sut;

        public OptimizelySyncEventsTests()
        {
            var initializer = new RedirectsInitializer(
                _redirectsEvents,
                new CustomRedirectHandler(),
                () => A.Fake<IRedirectsService>(),
                Enumerable.Empty<INotFoundHandler>(),
                A.Fake<IRedirectLoader>());

            _sut = new OptimizelySyncEvents(
                _redirectsEvents,
                _eventRegistry,
                initializer,
                A.Fake<IRegexRedirectCache>(),
                NullLogger<OptimizelySyncEvents>.Instance);
        }

        [Fact]
        public void Initializing_twice_subscribes_once()
        {
            Assert.True(_sut.Initialize());
            Fake.ClearRecordedCalls(_eventRegistry);

            Assert.False(_sut.Initialize());

            A.CallTo(() => _eventRegistry.Get(A<Guid>._)).MustNotHaveHappened();
        }

        [Fact]
        public void A_failed_initialize_can_be_tried_again()
        {
            A.CallTo(() => _eventRegistry.Get(A<Guid>._))
                .Throws(new InvalidOperationException("events unavailable")).Once();

            Assert.Throws<InvalidOperationException>(() => _sut.Initialize());

            Assert.True(_sut.Initialize());
        }

        [Fact]
        public void An_update_here_is_only_passed_on_once_initialized()
        {
            _redirectsEvents.RedirectsUpdated();
            A.CallTo(() => _eventRegistry.Get(A<Guid>._)).MustNotHaveHappened();

            _sut.Initialize();
            Fake.ClearRecordedCalls(_eventRegistry);
            _redirectsEvents.RedirectsUpdated();

            A.CallTo(() => _eventRegistry.Get(A<Guid>._)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task The_startup_check_starts_the_sync_and_warns_when_it_has_not_been_started()
        {
            var logger = A.Fake<ILogger<OptimizelySyncEventsStartupCheck>>();

            await RunStartupCheck(logger);

            Assert.False(_sut.Initialize());
            AssertWarnings(logger, 1);
        }

        [Fact]
        public async Task The_startup_check_stays_quiet_when_the_sync_has_been_started()
        {
            var logger = A.Fake<ILogger<OptimizelySyncEventsStartupCheck>>();
            _sut.Initialize();

            await RunStartupCheck(logger);

            AssertWarnings(logger, 0);
        }

        private async Task RunStartupCheck(ILogger<OptimizelySyncEventsStartupCheck> logger)
        {
            using var started = new CancellationTokenSource();
            var lifetime = A.Fake<IHostApplicationLifetime>();
            A.CallTo(() => lifetime.ApplicationStarted).Returns(started.Token);
            var check = new OptimizelySyncEventsStartupCheck(lifetime, () => _sut, logger);

            await check.StartAsync(CancellationToken.None);
            started.Cancel();
        }

        private static void AssertWarnings(ILogger logger, int expected)
        {
            var warnings = Fake.GetCalls(logger)
                .Count(call => call.Method.Name == nameof(ILogger.Log) && call.GetArgument<LogLevel>(0) == LogLevel.Warning);

            Assert.Equal(expected, warnings);
        }
    }
}
