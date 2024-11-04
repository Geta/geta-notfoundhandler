// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using Geta.NotFoundHandler.Infrastructure.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Geta.NotFoundHandler.Core.Suggestions;

public class SuggestionsCleanupService : ISuggestionsCleanupService
{
    private readonly IOptions<NotFoundHandlerOptions> _options;
    private readonly ILogger<SuggestionsCleanupService> _logger;

    public SuggestionsCleanupService(
        IOptions<NotFoundHandlerOptions> options,
        ILogger<SuggestionsCleanupService> logger
    )
    {
        _options = options;
        _logger = logger;
    }

    private string CleanupCommandText(int daysToKeep) => $@"
        -- [NotFoundHandler.Suggestions]
        IF OBJECT_ID('[NotFoundHandler.Suggestions]', 'U') IS NOT NULL
        BEGIN
            DELETE
            FROM
                [NotFoundHandler.Suggestions]
            WHERE
                [Requested] < DATEADD(day, -{daysToKeep}, GETDATE())
                                
            PRINT '* Deleted ' + CAST(@@ROWCOUNT AS nvarchar) + ' outdated records from table [NotFoundHandler.Suggestions].'
        END
        ";

    
    public bool Cleanup()
    {
        try
        {
            using var connection = new SqlConnection(_options.Value.ConnectionString);
            connection.InfoMessage += (_, e) => _logger.LogInformation("{Message}", e.Message);

            var command = new SqlCommand(CleanupCommandText(_options.Value.SuggestionsCleanupOptions.DaysToKeep), connection);
            command.CommandTimeout = _options.Value.SuggestionsCleanupOptions.Timeout;
            command.Connection.Open();
            command.ExecuteNonQuery();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "There was a problem while performing cleanup on connection");

            return false;
        }
    }
}
