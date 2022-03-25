using System;
using System.Linq;
using EPiServer.PlugIn;
using EPiServer.Scheduler;
using Geta.NotFoundHandler.Core.Redirects;
using Geta.NotFoundHandler.Optimizely.Infrastructure;

namespace Geta.NotFoundHandler.Optimizely.Core.AutomaticRedirects
{
    [ScheduledPlugIn(DisplayName = "[Geta NotFoundHandler] Register content move redirects",
                     GUID = "EC96ABEE-5DA4-404F-A0C8-451C77CA4983",
                     SortIndex = 5555)]
    public class RegisterMovedContentRedirectsJob : ScheduledJobBase
    {
        private readonly IContentUrlHistoryLoader _contentUrlHistoryLoader;
        private readonly RedirectBuilder _redirectBuilder;
        private readonly JobStatusLogger _jobStatusLogger;
        private readonly IRedirectsService _redirectsService;
        private bool _stopped;

        public RegisterMovedContentRedirectsJob(
            IRedirectsService redirectsService,
            IContentUrlHistoryLoader contentUrlHistoryLoader, 
            RedirectBuilder redirectBuilder)
        {
            _redirectsService = redirectsService;
            _contentUrlHistoryLoader = contentUrlHistoryLoader;
            _redirectBuilder = redirectBuilder;
            _jobStatusLogger = new JobStatusLogger(OnStatusChanged);

            IsStoppable = true;
        }

        public override string Execute()
        {
            var movedContent = _contentUrlHistoryLoader.GetAllMoved().ToList();
            var totalCount = movedContent.Count;
            var successCount = 0;
            var failedCount = 0;
            var currentCount = 0;

            _jobStatusLogger.LogWithStatus($"In total will process moved content: {totalCount}");

            foreach (var content in movedContent)
            {
                if (_stopped)
                {
                    _jobStatusLogger.Log(
                        $"Job was stopped, successful content handled before stopped: {successCount} out of total {totalCount} content");
                    return _jobStatusLogger.ToString();
                }

                currentCount++;

                try
                {
                    var redirects = _redirectBuilder.CreateRedirects(content.histories).ToList();
                    _redirectsService.AddOrUpdate(redirects);
                    var urlsToRemove = redirects.Where(x => x.NewUrl == x.OldUrl).Select(x => x.OldUrl);
                    _redirectsService.DeleteByOldUrl(urlsToRemove);

                    successCount++;
                }
                catch (Exception ex)
                {
                    _jobStatusLogger.Log($"Processing [{content.contentKey}] failed, exception: {ex}");
                    failedCount++;
                }

                if (currentCount % 500 == 0)
                {
                    _jobStatusLogger.Status(
                        $"Processed {currentCount} of whom successful {successCount} out of total {totalCount} content; failed: {failedCount}");
                }
            }

            _jobStatusLogger.Log(
                $"Processed {currentCount} of whom successful {successCount} out of total {totalCount} content; failed: {failedCount}");

            return _jobStatusLogger.ToString();
        }

        public override void Stop()
        {
            _stopped = true;
        }
    }
}
