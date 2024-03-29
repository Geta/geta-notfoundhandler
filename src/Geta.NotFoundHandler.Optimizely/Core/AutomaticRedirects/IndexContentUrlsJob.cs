// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Linq;
using EPiServer.PlugIn;
using EPiServer.Scheduler;
using Geta.NotFoundHandler.Optimizely.Infrastructure;

namespace Geta.NotFoundHandler.Optimizely.Core.AutomaticRedirects
{
    [ScheduledPlugIn(DisplayName = "[Geta NotFoundHandler] Index content URLs",
                     GUID = "53C743AE-E152-497A-A7E5-7E30F4B5B321",
                     SortIndex = 5555)]
    public class IndexContentUrlsJob : ScheduledJobBase
    {
        private bool _stopped;

        private readonly ContentUrlIndexer _contentUrlIndexer;
        private readonly ContentLinkLoader _contentLinkLoader;
        private readonly JobStatusLogger _jobStatusLogger;

        public IndexContentUrlsJob(ContentUrlIndexer contentUrlIndexer, ContentLinkLoader contentLinkLoader)
        {
            _contentUrlIndexer = contentUrlIndexer;
            _contentLinkLoader = contentLinkLoader;
            _jobStatusLogger = new JobStatusLogger(OnStatusChanged);

            IsStoppable = true;
        }

        public override string Execute()
        {
            var contentLinks = _contentLinkLoader.GetAllLinks().ToList();

            var totalCount = contentLinks.Count;
            var successCount = 0;
            var failedCount = 0;
            var currentCount = 0;

            _jobStatusLogger.LogWithStatus($"In total will process unique references: {totalCount}");

            foreach (var contentLink in contentLinks)
            {
                if (_stopped)
                {
                    _jobStatusLogger.Log(
                        $"Job was stopped, successful references before stopped: {successCount} out of total {totalCount} references");
                    return _jobStatusLogger.ToString();
                }

                currentCount++;

                try
                {
                    _contentUrlIndexer.IndexContentUrls(contentLink);

                    successCount++;
                }
                catch (Exception ex)
                {
                    _jobStatusLogger.Log($"Processing [{contentLink}] failed, exception: {ex}");
                    failedCount++;
                }

                if (currentCount % 500 == 0)
                {
                    _jobStatusLogger.Status(
                        $"Processed {currentCount} of whom successful {successCount} out of total {totalCount} references; failed: {failedCount}");
                }
            }

            _jobStatusLogger.Log(
                $"Processed {currentCount} of whom successful {successCount} out of total {totalCount} references; failed: {failedCount}");

            return _jobStatusLogger.ToString();
        }

        public override void Stop()
        {
            _stopped = true;
        }
    }
}
