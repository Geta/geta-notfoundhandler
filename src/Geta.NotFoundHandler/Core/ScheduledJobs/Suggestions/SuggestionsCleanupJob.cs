// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System.Threading.Tasks;
using Coravel.Invocable;
using Geta.NotFoundHandler.Core.Suggestions;

namespace Geta.NotFoundHandler.Core.ScheduledJobs.Suggestions;

public class SuggestionsCleanupJob : IInvocable
{
    private readonly ISuggestionsCleanupService _suggestionsCleanupService;

    public SuggestionsCleanupJob(ISuggestionsCleanupService suggestionsCleanupService)
    {
        _suggestionsCleanupService = suggestionsCleanupService;
    }

    public async Task Invoke()
    {
        _suggestionsCleanupService.Cleanup();
    }
}
