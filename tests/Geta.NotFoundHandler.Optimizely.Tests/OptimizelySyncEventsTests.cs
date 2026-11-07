using System;
using System.Linq;
using EPiServer.Events.Clients;
using FakeItEasy;
using Geta.NotFoundHandler.Core;
using Geta.NotFoundHandler.Core.Providers.RegexRedirects;
using Geta.NotFoundHandler.Core.Redirects;
using Geta.NotFoundHandler.Optimizely.Core.Events;
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
                Enumerable.Empty<INotFoundHandler>());

            _sut = new OptimizelySyncEvents(
                _redirectsEvents,
                _eventRegistry,
                initializer,
                A.Fake<IRegexRedirectCache>(),
                NullLogger<OptimizelySyncEvents>.Instance);
        }

        [Fact]
        public void Is_not_initialized_until_asked()
        {
            Assert.False(_sut.IsInitialized);

            _sut.Initialize();

            Assert.True(_sut.IsInitialized);
        }

        [Fact]
        public void Initializing_twice_subscribes_once()
        {
            _sut.Initialize();
            Fake.ClearRecordedCalls(_eventRegistry);

            _sut.Initialize();

            A.CallTo(() => _eventRegistry.Get(A<Guid>._)).MustNotHaveHappened();
        }

        [Fact]
        public void A_failed_initialize_can_be_tried_again()
        {
            A.CallTo(() => _eventRegistry.Get(A<Guid>._))
                .Throws(new InvalidOperationException("events unavailable")).Once();

            Assert.Throws<InvalidOperationException>(_sut.Initialize);
            Assert.False(_sut.IsInitialized);

            _sut.Initialize();

            Assert.True(_sut.IsInitialized);
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
    }
}
