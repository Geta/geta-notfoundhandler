// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

namespace Geta.NotFoundHandler.Core.Suggestions;

public class SuggestionsCleanupOptions
{
    public int DaysToKeep { get; set; } = 14;
    public int Timeout { get; set; } = 30 * 60;
    public string CronInterval { get; set; } = "0 0 * * *";
}
