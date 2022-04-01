// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Linq;
using EPiServer.PlugIn;
using EPiServer.Scheduler;
using Geta.NotFoundHandler.Optimizely.Infrastructure;

namespace Geta.NotFoundHandler.Optimizely.Core.AutomaticRedirects
{
    [ScheduledPlugIn(DisplayName = "[Geta NotFoundHandler] Register content move redirects",
                     GUID = "EC96ABEE-5DA4-404F-A0C8-451C77CA4983",
                     SortIndex = 5555)]
    public class RegisterMovedContentRedirectsJob : ScheduledJobBase
    {
        private readonly IContentUrlHistoryLoader _contentUrlHistoryLoader;
        private readonly JobStatusLogger _jobStatusLogger;
        private readonly IAutomaticRedirectsService _automaticRedirectsService;
        private bool _stopped;

        public RegisterMovedContentRedirectsJob(
            IAutomaticRedirectsService automaticRedirectsService,
            IContentUrlHistoryLoader contentUrlHistoryLoader)
        {
            _automaticRedirectsService = automaticRedirectsService;
            _contentUrlHistoryLoader = contentUrlHistoryLoader;
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
                    _automaticRedirectsService.CreateRedirects(content.histories);
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
