// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Data;
using System.Data.Common;
using EPiServer.Data;
using EPiServer.Logging;
using EPiServer.ServiceLocation;
using Microsoft.Data.SqlClient;

namespace Geta.NotFoundHandler.Core.Data
{
    public class DataAccessBaseEx  : EPiServer.DataAccess.DataAccessBase
    {
        public DataAccessBaseEx(IDatabaseExecutor handler)
            : base(handler)
        {
            Executor = handler;
        }

        public static DataAccessBaseEx GetWorker()
        {
            return ServiceLocator.Current.GetInstance<DataAccessBaseEx>();
        }

        private const string SuggestionsTable = "[dbo].[NotFoundHandler.Suggestions]";

        private static readonly ILogger Logger = LogManager.GetLogger();

        public DataSet ExecuteSql(string sqlCommand, params IDbDataParameter[] parameters)
        {
            return Executor.Execute(delegate
            {
                var ds = new DataSet();
                try
                {
                    using (var command = CreateCommand(sqlCommand, parameters))
                    {
                        using (var da = CreateDataAdapter(command))
                        {
                            da.Fill(ds);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(
                        $"An error occurred in the ExecuteSQL method with the following sql: {sqlCommand}", ex);
                }

                return ds;
            });

        }

        private DbCommand CreateCommand(string sqlCommand, params IDbDataParameter[] parameters)
        {
            var command = base.CreateCommand(sqlCommand);

            if (parameters != null)
            {
                foreach (var dbDataParameter in parameters)
                {
                    var parameter = (SqlParameter)dbDataParameter;
                    command.Parameters.Add(parameter);
                }
            }

            command.CommandType = CommandType.Text;

            return command;
        }

        public bool ExecuteNonQuery(string sqlCommand, params IDbDataParameter[] parameters)
        {
            return Executor.Execute(delegate
            {
                var success = true;

                try
                {
                    using (var command = CreateCommand(sqlCommand, parameters))
                    {
                        command.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    success = false;
                    Logger.Error(
                        $"An error occurred in the ExecuteSQL method with the following sql: {sqlCommand}", ex);
                }
                return success;
            });
        }

        public int ExecuteScalar(string sqlCommand)
        {
            return Executor.Execute(delegate
            {
                int result;
                try
                {
                    using (var command = CreateCommand(sqlCommand))
                    {
                        result = (int)command.ExecuteScalar();
                    }
                }
                catch (Exception ex)
                {
                    result = 0;
                    Logger.Error(
                        $"An error occurred in the ExecuteScalar method with the following sql: {sqlCommand}", ex);
                }
                return result;
            });
        }

        public DataSet GetAllSuggestionCount()
        {
            var sqlCommand =
                $"SELECT [OldUrl], COUNT(*) as Requests FROM {SuggestionsTable} GROUP BY [OldUrl] order by Requests desc";
            return ExecuteSql(sqlCommand);
        }

        public void DeleteSuggestionsForRequest(string oldUrl)
        {
            var sqlCommand = $"DELETE FROM {SuggestionsTable} WHERE [OldUrl] = @oldurl";
            var oldUrlParam = CreateParameter("oldurl", DbType.String, 2000);
            oldUrlParam.Value = oldUrl;

            ExecuteNonQuery(sqlCommand, oldUrlParam);
        }

        public void DeleteSuggestions(int maxErrors, int minimumDaysOld)
        {
            var sqlCommand = $@"delete from {SuggestionsTable}
                                                where [OldUrl] in (
                                                select [OldUrl]
                                                  from (
                                                      select [OldUrl]
                                                      from {SuggestionsTable}
                                                      Where DATEDIFF(day, [Requested], getdate()) >= {minimumDaysOld}
                                                      group by [OldUrl]
                                                      having count(*) <= {maxErrors}
                                                      ) t
                                                )";
            ExecuteNonQuery(sqlCommand);
        }
        public void DeleteAllSuggestions()
        {
            var sqlCommand = $@"delete from {SuggestionsTable}";
            ExecuteNonQuery(sqlCommand);
        }

        public DataSet GetSuggestionReferrers(string url)
        {
            var sqlCommand =
                $"SELECT [Referer], COUNT(*) as Requests FROM {SuggestionsTable} where [OldUrl] = @oldurl  GROUP BY [Referer] order by Requests desc";
            var oldUrlParam = CreateParameter("oldurl", DbType.String, 2000);
            oldUrlParam.Value = url;

            return ExecuteSql(sqlCommand, oldUrlParam);
        }

        public int GetTotalNumberOfSuggestions()
        {
            var sqlCommand = $"SELECT COUNT(DISTINCT [OldUrl]) FROM {SuggestionsTable}";
            return ExecuteScalar(sqlCommand);
        }

        public int CheckNotFoundHandlerVersion()
        {
            return Executor.Execute(() =>
            {
                var sqlCommand = "dbo.notfoundhandler_version";
                var version = -1;
                try
                {
                    using (var command = CreateCommand())
                    {
                        command.Parameters.Add(CreateReturnParameter());
                        command.CommandText = sqlCommand;
                        command.CommandType = CommandType.StoredProcedure;
                        command.ExecuteNonQuery();
                        version = Convert.ToInt32(GetReturnValue(command).ToString());
                    }
                }
                catch (SqlException)
                {
                    Logger.Information("Stored procedure not found. Creating it.");
                    return version;
                }
                catch (Exception ex)
                {
                    Logger.Error("Error during NotFoundHandler version check", ex);
                }
                return version;
            });
        }

        public void LogSuggestionToDb(string oldUrl, string referrer, DateTime now)
        {
            Executor.Execute(() =>
               {
                   var sqlCommand = @$"INSERT INTO {SuggestionsTable}
                                    (Requested, OldUrl, Referer)
                                    VALUES
                                    (@requested, @oldurl, @referer)";
                   try
                   {
                       var requestedParam = CreateParameter("requested", DbType.DateTime, 0);
                       requestedParam.Value = now;

                       var referrerParam = CreateParameter("referer", DbType.String, 2000);
                       referrerParam.Value = referrer ?? string.Empty;

                       var oldUrlParam = CreateParameter("oldurl", DbType.String, 2000);
                       oldUrlParam.Value = oldUrl;

                       using (var command = CreateCommand(sqlCommand, requestedParam, referrerParam, oldUrlParam))
                       {
                           command.ExecuteNonQuery();
                       }
                   }
                   catch (Exception ex)
                   {

                       Logger.Error("An error occurred while logging a NotFoundHandler error.", ex);
                   }
                   return true;
               });
        }
    }
}
