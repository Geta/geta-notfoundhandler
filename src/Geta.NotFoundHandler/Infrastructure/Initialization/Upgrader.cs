// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using Geta.NotFoundHandler.Data;
using Geta.NotFoundHandler.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;

namespace Geta.NotFoundHandler.Infrastructure.Initialization
{
    public class Upgrader
    {
        private readonly ILogger<Upgrader> _logger;

        private const string SuggestionsTable = "[dbo].[NotFoundHandler.Suggestions]";
        private const string RedirectsTable = "[dbo].[NotFoundHandler.Redirects]";

        public Upgrader(ILogger<Upgrader> logger)
        {
            _logger = logger;
        }

        public void Start()
        {
            _logger.LogDebug("Initializing NotFoundHandler version check");
            var dba = DataAccessBaseEx.GetWorker();
            var version = dba.CheckNotFoundHandlerVersion();
            if (version == NotFoundHandlerOptions.CurrentDbVersion)
            {
                return;
            }

            _logger.LogDebug("Older version found. Version nr. :" + version);

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
            var dba = DataAccessBaseEx.GetWorker();

            var created = CreateRedirectsTable(dba);

            if (created)
            {
                created = CreateSuggestionsTable(dba);
            }

            if (created)
            {
                CreateVersionNumberSp(dba);
            }
        }

        private bool CreateRedirectsTable(DataAccessBaseEx dba)
        {
            _logger.LogInformation("Create NotFoundHandler redirects table START");
            var createTableScript = @$"CREATE TABLE {RedirectsTable} (
                                        [Id] [uniqueidentifier] NOT NULL,
                                        [OldUrl] [nvarchar](2000) NOT NULL,
                                        [NewUrl] [nvarchar](2000) NOT NULL,
                                        [State] [int] NOT NULL,
                                        [WildCardSkipAppend] [bit] NOT NULL,
                                        [RedirectType] [int] NOT NULL,
                                        CONSTRAINT [PK_NotFoundHandlerRedirects] PRIMARY KEY CLUSTERED ([Id] ASC) ON [PRIMARY]
                                        ) ON [PRIMARY]";
            var created = dba.ExecuteNonQuery(createTableScript);
            _logger.LogInformation("Create NotFoundHandler redirects table END");

            return created;
        }

        private bool CreateSuggestionsTable(DataAccessBaseEx dba)
        {
            _logger.LogInformation("Create NotFoundHandler suggestions table START");
            var createTableScript = @$"CREATE TABLE {SuggestionsTable} (
                                        [ID] [int] IDENTITY(1,1) NOT NULL,
                                        [OldUrl] [nvarchar](2000) NOT NULL,
                                        [Requested] [datetime] NULL,
                                        [Referer] [nvarchar](2000) NULL
                                        ) ON [PRIMARY]";
            var created = dba.ExecuteNonQuery(createTableScript);
            _logger.LogInformation("Create NotFoundHandler suggestions table END");

            if (created)
            {
                created = CreateSuggestionsTableIndex(dba);
            }

            return created;
        }

        private bool CreateSuggestionsTableIndex(DataAccessBaseEx dba)
        {
            _logger.LogInformation("Create suggestions table clustered index START");
            var clusteredIndex =
                $"CREATE CLUSTERED INDEX NotFoundHandlerSuggestions_ID ON {SuggestionsTable} (ID)";

            var created = dba.ExecuteNonQuery(clusteredIndex);
            if (!created)
            {
                _logger.LogError("An error occurred during the creation of the NotFoundHandler redirects clustered index. Canceling.");
            }

            _logger.LogInformation("Create suggestions table clustered index END");
            return created;
        }

        private void Upgrade()
        {
            var dba = DataAccessBaseEx.GetWorker();
            UpdateVersionNumber(dba);
        }

        private void CreateVersionNumberSp(DataAccessBaseEx dba)
        {
            _logger.LogInformation("Create NotFoundHandler version SP START");
            var versionSp =
                $@"CREATE PROCEDURE [dbo].[notfoundhandler_version] AS RETURN {NotFoundHandlerOptions.CurrentDbVersion}";

            var created = dba.ExecuteNonQuery(versionSp);

            if (!created)
            {
                _logger.LogError("An error occurred during the creation of the NotFoundHandler version stored procedure. Canceling.");
            }

            _logger.LogInformation("Create NotFoundHandler version SP END");
        }

        private void UpdateVersionNumber(DataAccessBaseEx dba)
        {
            var versionSp =
                $@"ALTER PROCEDURE [dbo].[notfoundhandler_version] AS RETURN {NotFoundHandlerOptions.CurrentDbVersion}";
            dba.ExecuteNonQuery(versionSp);
        }

        private bool TableExists(string tableName, DataAccessBaseEx dba)
        {
            var cmd = $@"SELECT *
                 FROM INFORMATION_SCHEMA.TABLES
                 WHERE TABLE_SCHEMA = 'dbo'
                 AND  TABLE_NAME = '{tableName}'";
            var num = dba.ExecuteScalar(cmd);
            return num != 0;
        }

        private bool ColumnExists(string tableName, string columnName, DataAccessBaseEx dba)
        {
            var cmd = $@"SELECT 1
                        FROM sys.columns
                        WHERE Name = '{columnName}'
                        AND  Object_ID = Object_ID(N'dbo.[{tableName}]')";
            var num = dba.ExecuteScalar(cmd);
            return num != 0;
        }
    }
}
