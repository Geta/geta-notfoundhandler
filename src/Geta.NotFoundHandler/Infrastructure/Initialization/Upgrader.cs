// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using Geta.NotFoundHandler.Data;
using Geta.NotFoundHandler.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;

namespace Geta.NotFoundHandler.Infrastructure.Initialization;

public class Upgrader
{
    private const string SuggestionsTableName = "NotFoundHandler.Suggestions";
    private const string RedirectsTableName = "NotFoundHandler.Redirects";
    private const string RegexRedirectsTableName = "NotFoundHandler.RegexRedirects";
    private readonly IDataExecutor _dataExecutor;
    private readonly ILogger<Upgrader> _logger;

    public Upgrader(
        ILogger<Upgrader> logger,
        IDataExecutor dataExecutor)
    {
        _logger = logger;
        _dataExecutor = dataExecutor;
    }

    public void Start()
    {
        _logger.LogDebug("Initializing NotFoundHandler version check");
        var version = GetVersionNumber();
        if (version == NotFoundHandlerOptions.CurrentDbVersion)
        {
            return;
        }

        _logger.LogDebug("Older version found. Version nr. : {Version}", version);

        if (version == -1)
        {
            Create();
        }
        else
        {
            Upgrade();
        }
    }

    /// <summary>
    /// Create redirects and suggestions tables and SP for version number
    /// </summary>
    private void Create()
    {
        var created = CreateRedirectsTable();

        if (created)
        {
            created = CreateSuggestionsTable();
        }

        if (created)
        {
            created = CreateRegexRedirectsTable();
        }

        if (created)
        {
            CreateVersionNumberSp();
        }
    }

    private bool CreateRedirectsTable()
    {
        _logger.LogInformation("Create NotFoundHandler redirects table START");
        var createTableScript = @$"CREATE TABLE {GetFullTableName(RedirectsTableName)} (
                                        [Id] [uniqueidentifier] NOT NULL,
                                        [OldUrl] [nvarchar](2000) NOT NULL,
                                        [NewUrl] [nvarchar](2000) NOT NULL,
                                        [State] [int] NOT NULL,
                                        [WildCardSkipAppend] [bit] NOT NULL,
                                        [RedirectType] [int] NOT NULL,
                                        CONSTRAINT [PK_NotFoundHandlerRedirects] PRIMARY KEY CLUSTERED ([Id] ASC) ON [PRIMARY]
                                        ) ON [PRIMARY]";
        var created = _dataExecutor.ExecuteNonQuery(createTableScript);
        _logger.LogInformation("Create NotFoundHandler redirects table END");

        return created;
    }

    private bool CreateSuggestionsTable()
    {
        _logger.LogInformation("Create NotFoundHandler suggestions table START");
        var createTableScript = @$"CREATE TABLE {GetFullTableName(SuggestionsTableName)} (
                                        [ID] [int] IDENTITY(1,1) NOT NULL,
                                        [OldUrl] [nvarchar](2000) NOT NULL,
                                        [Requested] [datetime] NULL,
                                        [Referer] [nvarchar](2000) NULL
                                        ) ON [PRIMARY]";
        var created = _dataExecutor.ExecuteNonQuery(createTableScript);
        _logger.LogInformation("Create NotFoundHandler suggestions table END");

        if (created)
        {
            created = CreateSuggestionsTableIndex();
        }

        return created;
    }

    private bool CreateSuggestionsTableIndex()
    {
        _logger.LogInformation("Create suggestions table clustered index START");
        var clusteredIndex =
            $"CREATE CLUSTERED INDEX NotFoundHandlerSuggestions_ID ON {GetFullTableName(SuggestionsTableName)} (ID)";

        var created = _dataExecutor.ExecuteNonQuery(clusteredIndex);
        if (!created)
        {
            _logger.LogError(
                "An error occurred during the creation of the NotFoundHandler redirects clustered index. Canceling");
        }

        _logger.LogInformation("Create suggestions table clustered index END");
        return created;
    }

    private bool CreateRegexRedirectsTable()
    {
        _logger.LogInformation("Create NotFoundHandler regex redirects table START");
        var createTableScript = @$"CREATE TABLE {GetFullTableName(RegexRedirectsTableName)} (
                                        [Id] [uniqueidentifier] NOT NULL,
                                        [OldUrlRegex] [nvarchar](2000) NOT NULL,
                                        [NewUrlFormat] [nvarchar](2000) NOT NULL,
                                        [OrderNumber] [int] NOT NULL,
                                        [TimeoutCount] [int] NOT NULL,
                                        [CreatedAt] [datetime] NOT NULL,
                                        [ModifiedAt] [datetime] NOT NULL,
                                        CONSTRAINT [PK_NotFoundHandlerRegexRedirects] PRIMARY KEY CLUSTERED ([Id] ASC) ON [PRIMARY]
                                        ) ON [PRIMARY]";
        var created = _dataExecutor.ExecuteNonQuery(createTableScript);
        _logger.LogInformation("Create NotFoundHandler regex redirects table END");

        return created;
    }

    private void Upgrade()
    {
        var updated = UpdateRegexRedirectsTable();

        if (updated)
        {
            UpdateVersionNumber();
        }
    }

    private bool UpdateRegexRedirectsTable()
    {
        return TableExists(RegexRedirectsTableName) || CreateRegexRedirectsTable();
    }

    private void CreateVersionNumberSp()
    {
        _logger.LogInformation("Create NotFoundHandler version SP START");
        var versionSp =
            $@"CREATE PROCEDURE [dbo].[notfoundhandler_version] AS RETURN {NotFoundHandlerOptions.CurrentDbVersion}";

        var created = _dataExecutor.ExecuteNonQuery(versionSp);

        if (!created)
        {
            _logger.LogError(
                "An error occurred during the creation of the NotFoundHandler version stored procedure. Canceling");
        }

        _logger.LogInformation("Create NotFoundHandler version SP END");
    }

    private int GetVersionNumber()
    {
        var sqlCommand = "dbo.notfoundhandler_version";
        return _dataExecutor.ExecuteStoredProcedure(sqlCommand);
    }

    private void UpdateVersionNumber()
    {
        var versionSp =
            $@"ALTER PROCEDURE [dbo].[notfoundhandler_version] AS RETURN {NotFoundHandlerOptions.CurrentDbVersion}";
        _dataExecutor.ExecuteNonQuery(versionSp);
    }

    private bool TableExists(string tableName)
    {
        var cmd = $@"SELECT 1
                 FROM INFORMATION_SCHEMA.TABLES
                 WHERE TABLE_SCHEMA = 'dbo'
                 AND  TABLE_NAME = '{tableName}'";
        var num = _dataExecutor.ExecuteScalar(cmd);
        return num != 0;
    }

    private bool ColumnExists(string tableName, string columnName)
    {
        var cmd = $@"SELECT 1
                        FROM sys.columns
                        WHERE Name = '{columnName}'
                        AND  Object_ID = Object_ID(N'dbo.[{tableName}]')";
        var num = _dataExecutor.ExecuteScalar(cmd);
        return num != 0;
    }

    private static string GetFullTableName(string tableName)
    {
        return $"[dbo].[{tableName}]";
    }
}
