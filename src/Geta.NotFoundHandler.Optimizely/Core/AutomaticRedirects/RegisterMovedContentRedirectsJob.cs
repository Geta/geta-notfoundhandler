// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Linq;
using EPiServer.PlugIn;
using EPiServer.Scheduler;
using Geta.NotFoundHandler.Optimizely.Infrastructure;
using Geta.NotFoundHandler.Optimizely.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

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
        private readonly int _batchSize;
        private bool _stopped;

        public RegisterMovedContentRedirectsJob(
            IAutomaticRedirectsService automaticRedirectsService,
            IContentUrlHistoryLoader contentUrlHistoryLoader,
            IOptions<OptimizelyNotFoundHandlerOptions> options)
        {
            _automaticRedirectsService = automaticRedirectsService;
            _contentUrlHistoryLoader = contentUrlHistoryLoader;
            _batchSize = options.Value.MovedContentBatchSize;
            _jobStatusLogger = new JobStatusLogger(OnStatusChanged);

            IsStoppable = true;
        }

        public override string Execute()
        {
            var successCount = 0;
            var failedCount = 0;
            var currentCount = 0;
            var skip = 0;

            _jobStatusLogger.LogWithStatus($"Processing moved content in batches of {_batchSize}");

            // Page through the moved content rather than materialising the whole table up front.
            // CreateRedirects only touches the redirects store, not ContentUrlHistory, so the moved
            // set is stable for the duration of the run and skip-based paging never skips a key.
            while (true)
            {
                var batch = _contentUrlHistoryLoader.GetAllMoved(skip, _batchSize).ToList();

                foreach (var content in batch)
                {
                    if (_stopped)
                    {
                        _jobStatusLogger.Log(
                            $"Job was stopped, successful content handled before stopped: {successCount} out of {currentCount} processed");
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
                            $"Processed {currentCount}, of whom successful {successCount}; failed: {failedCount}");
                    }
                }

                if (batch.Count < _batchSize)
                {
                    break;
                }

                skip += _batchSize;
            }

            _jobStatusLogger.Log(
                $"Processed {currentCount}, of whom successful {successCount}; failed: {failedCount}");

            return _jobStatusLogger.ToString();
        }

        public override void Stop()
        {
            _stopped = true;
        }
    }
}
