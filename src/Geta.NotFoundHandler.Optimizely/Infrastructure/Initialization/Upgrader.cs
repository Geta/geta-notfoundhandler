// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using Geta.NotFoundHandler.Data;
using Geta.NotFoundHandler.Optimizely.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;

namespace Geta.NotFoundHandler.Optimizely.Infrastructure.Initialization
{
    public class Upgrader
    {
        private readonly ILogger<Upgrader> _logger;
        private readonly IDataExecutor _dataExecutor;

        private const string ContentUrlHistoryTable = "[dbo].[NotFoundHandler.ContentUrlHistory]";
        private const string VersionProcedure = "[dbo].[optimizely_notfoundhandler_version]";

        public Upgrader(
            ILogger<Upgrader> logger,
            IDataExecutor dataExecutor)
        {
            _logger = logger;
            _dataExecutor = dataExecutor;
        }

        public void Start()
        {
            _logger.LogDebug("Initializing Optimizely NotFoundHandler version check");
            var version = GetVersionNumber();
            if (version == OptimizelyNotFoundHandlerOptions.CurrentDbVersion)
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
            var result = CreateContentUrlHistoryTable();

            if (result)
            {
                result = CreateContentKeyMd5Column();
            }

            if (result)
            {
                CreateVersionNumberSp();
            }
        }

        private bool CreateContentUrlHistoryTable()
        {
            _logger.LogInformation("Create Optimizely NotFoundHandler ContentUrlHistoryTable START");
            var createTableScript = @$"CREATE TABLE {ContentUrlHistoryTable} (
                                        [Id] [uniqueidentifier] NOT NULL,
                                        [ContentKey] [nvarchar](2000) NOT NULL,
                                        [Urls] [nvarchar](MAX) NOT NULL,
                                        [CreatedUtc] [datetime] NOT NULL,
                                        CONSTRAINT [PK_ContentUrlHistory] PRIMARY KEY CLUSTERED ([Id] ASC) ON [PRIMARY]
                                        ) ON [PRIMARY]";
            var created = _dataExecutor.ExecuteNonQuery(createTableScript);
            _logger.LogInformation("Create Optimizely NotFoundHandler ContentUrlHistoryTable END");

            return created;
        }

        private void Upgrade()
        {
            var result = CreateContentKeyMd5Column();

            if (result)
            {
                UpdateVersionNumber();
            }
        }

        private void CreateVersionNumberSp()
        {
            _logger.LogInformation("Create Optimizely NotFoundHandler version SP START");
            var versionSp =
                $@"CREATE PROCEDURE {VersionProcedure} AS RETURN {OptimizelyNotFoundHandlerOptions.CurrentDbVersion}";

            var created = _dataExecutor.ExecuteNonQuery(versionSp);

            if (!created)
            {
                _logger.LogError(
                    "An error occurred during the creation of the NotFoundHandler version stored procedure. Canceling");
            }

            _logger.LogInformation("Create Optimizely NotFoundHandler version SP END");
        }

        private int GetVersionNumber()
        {
            var sqlCommand = VersionProcedure;
            return _dataExecutor.ExecuteStoredProcedure(sqlCommand);
        }

        private void UpdateVersionNumber()
        {
            var versionSp =
                $@"ALTER PROCEDURE {VersionProcedure} AS RETURN {OptimizelyNotFoundHandlerOptions.CurrentDbVersion}";
            _dataExecutor.ExecuteNonQuery(versionSp);
        }

        private bool CreateContentKeyMd5Column()
        {
            var command = $@"
            BEGIN TRY
                BEGIN TRANSACTION;
                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.columns
                    WHERE object_id = OBJECT_ID('{ContentUrlHistoryTable}')
                    AND name = 'md5_ContentKey'
                )
                BEGIN
                    ALTER TABLE {ContentUrlHistoryTable} ADD md5_ContentKey AS HASHBYTES('MD5', [ContentKey]);
                    CREATE INDEX PContentKey_index ON {ContentUrlHistoryTable} (md5_ContentKey);
                END

                COMMIT TRANSACTION;
            END TRY
            BEGIN CATCH
                ROLLBACK TRANSACTION;
                THROW;
            END CATCH;
            ";

            return _dataExecutor.ExecuteNonQuery(command);
        }
    }
}
