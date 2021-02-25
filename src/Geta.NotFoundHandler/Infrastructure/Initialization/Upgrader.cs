// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using EPiServer.Logging;
using Geta.NotFoundHandler.Data;
using Geta.NotFoundHandler.Infrastructure.Configuration;

namespace Geta.NotFoundHandler.Infrastructure.Initialization
{
    public static class Upgrader
    {
        private static readonly ILogger Logger = LogManager.GetLogger();
        private const string SuggestionsTable = "[dbo].[NotFoundHandler.Suggestions]";
        private const string RedirectsTable = "[dbo].[NotFoundHandler.Redirects]";

        public static void Start(int version)
        {
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
        private static void Create()
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

        private static bool CreateRedirectsTable(DataAccessBaseEx dba)
        {
            Logger.Information("Create NotFoundHandler redirects table START");
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
            Logger.Information("Create NotFoundHandler redirects table END");

            return created;
        }

        private static bool CreateSuggestionsTable(DataAccessBaseEx dba)
        {
            Logger.Information("Create NotFoundHandler suggestions table START");
            var createTableScript = @$"CREATE TABLE {SuggestionsTable} (
                                        [ID] [int] IDENTITY(1,1) NOT NULL,
                                        [OldUrl] [nvarchar](2000) NOT NULL,
                                        [Requested] [datetime] NULL,
                                        [Referer] [nvarchar](2000) NULL
                                        ) ON [PRIMARY]";
            var created = dba.ExecuteNonQuery(createTableScript);
            Logger.Information("Create NotFoundHandler suggestions table END");

            if (created)
            {
                created = CreateSuggestionsTableIndex(dba);
            }

            return created;
        }

        private static bool CreateSuggestionsTableIndex(DataAccessBaseEx dba)
        {
            Logger.Information("Create suggestions table clustered index START");
            var clusteredIndex =
                $"CREATE CLUSTERED INDEX NotFoundHandlerSuggestions_ID ON {SuggestionsTable} (ID)";

            var created = dba.ExecuteNonQuery(clusteredIndex);
            if (!created)
            {
                Logger.Error("An error occurred during the creation of the NotFoundHandler redirects clustered index. Canceling.");
            }

            Logger.Information("Create suggestions table clustered index END");
            return created;
        }

        private static void Upgrade()
        {
            var dba = DataAccessBaseEx.GetWorker();
            UpdateVersionNumber(dba);
        }

        private static bool CreateVersionNumberSp(DataAccessBaseEx dba)
        {
            Logger.Information("Create NotFoundHandler version SP START");
            var versionSp =
                $@"CREATE PROCEDURE [dbo].[notfoundhandler_version] AS RETURN {NotFoundHandlerOptions.CurrentDbVersion}";

            var created = dba.ExecuteNonQuery(versionSp);

            if (!created)
            {
                Logger.Error("An error occurred during the creation of the NotFoundHandler version stored procedure. Canceling.");
            }

            Logger.Information("Create NotFoundHandler version SP END");
            return created;
        }

        private static void UpdateVersionNumber(DataAccessBaseEx dba)
        {
            var versionSp =
                $@"ALTER PROCEDURE [dbo].[notfoundhandler_version] AS RETURN {NotFoundHandlerOptions.CurrentDbVersion}";
            dba.ExecuteNonQuery(versionSp);
        }

        private static bool TableExists(string tableName, DataAccessBaseEx dba)
        {
            var cmd = $@"SELECT *
                 FROM INFORMATION_SCHEMA.TABLES
                 WHERE TABLE_SCHEMA = 'dbo'
                 AND  TABLE_NAME = '{tableName}'";
            var num = dba.ExecuteScalar(cmd);
            return num != 0;
        }

        private static bool ColumnExists(string tableName, string columnName, DataAccessBaseEx dba)
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
