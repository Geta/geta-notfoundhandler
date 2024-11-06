// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using EPiServer.PlugIn;
using EPiServer.Scheduler;
using Geta.NotFoundHandler.Core.Suggestions;

namespace Geta.NotFoundHandler.Optimizely.Core.Suggestions.Jobs;

[ScheduledPlugIn(DisplayName = "[Geta NotFoundHandler] Suggestions cleanup job",
                 Description = "As suggestions table grow fast we should add a possibility to clean up old suggestions",
                 GUID = "6AE19CEC-1052-4482-97DF-981076DDD6F2",
                 SortIndex = 5555)]
public class SuggestionsCleanupJob : ScheduledJobBase
{
    private readonly ISuggestionsCleanupService _suggestionsCleanupService;

    public SuggestionsCleanupJob(ISuggestionsCleanupService suggestionsCleanupService)
    {
        IsStoppable = true;
        _suggestionsCleanupService = suggestionsCleanupService;
    }

    public override string Execute()
    {
        _suggestionsCleanupService.Cleanup();

        return string.Empty;
    }
}
