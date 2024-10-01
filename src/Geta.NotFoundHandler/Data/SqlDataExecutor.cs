// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Data;
using System.Data.Common;
using System.Globalization;
using Geta.NotFoundHandler.Infrastructure.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Geta.NotFoundHandler.Data
{
    public class SqlDataExecutor : IDataExecutor
    {
        private readonly ILogger<SqlDataExecutor> _logger;
        private readonly string _connectionString;

        public SqlDataExecutor(
            IOptions<NotFoundHandlerOptions> options,
            ILogger<SqlDataExecutor> logger)
        {
            _connectionString = options.Value.ConnectionString;
            _logger = logger;
        }

        public DataTable ExecuteQuery(string sqlCommand, params IDbDataParameter[] parameters)
        {
            var ds = new DataSet();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                using var command = CreateCommand(connection, sqlCommand, parameters);
                using var da = new SqlDataAdapter(command);
                da.Fill(ds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                                 "An error occurred in the ExecuteSQL method with the following sql: {SqlCommand}",
                                 sqlCommand);
            }

            return ds.Tables[0];
        }

        public bool ExecuteNonQuery(string sqlCommand, params IDbDataParameter[] parameters)
        {
            var success = true;

            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                using var command = CreateCommand(connection, sqlCommand, parameters);
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                success = false;
                _logger.LogError(ex,
                                 "An error occurred in the ExecuteSQL method with the following sql: {SqlCommand}",
                                 sqlCommand);
            }

            return success;
        }

        public int ExecuteScalar(string sqlCommand)
        {
            int result;
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                using var command = CreateCommand(connection, sqlCommand);
                var queryResult = command.ExecuteScalar();
                if (queryResult == null) return 0;
                result = (int)queryResult;
            }
            catch (Exception ex)
            {
                result = 0;
                _logger.LogError(ex,
                                 "An error occurred in the ExecuteScalar method with the following sql: {SqlCommand}",
                                 sqlCommand);
            }

            return result;
        }

        public int ExecuteStoredProcedure(string sqlCommand, int defaultReturnValue = -1)
        {
            var value = defaultReturnValue;
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                using var command = CreateCommand(connection, sqlCommand);
                command.Parameters.Add(CreateReturnParameter());
                command.CommandText = sqlCommand;
                command.CommandType = CommandType.StoredProcedure;
                command.ExecuteNonQuery();
                value = Convert.ToInt32(GetReturnValue(command).ToString());
            }
            catch (SqlException)
            {
                _logger.LogInformation("Stored procedure not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while running stored procedure");
            }

            return value;
        }

        public DbParameter CreateParameter(string parameterName, DbType dbType)
        {
            var parameter = new SqlParameter
            {
                ParameterName = parameterName, DbType = dbType, Direction = ParameterDirection.Input
            };
            return parameter;
        }

        public DbParameter CreateParameter(string parameterName, DbType dbType, int size)
        {
            var parameter = new SqlParameter
            {
                ParameterName = parameterName,
                DbType = dbType,
                Direction = ParameterDirection.Input,
                Size = size == 0 ? 1 : size
            };
            return parameter;
        }

        public static int GetReturnValue(DbCommand cmd)
        {
            var parameter = cmd.Parameters["@ReturnValue"];
            return Convert.ToInt32(parameter.Value, CultureInfo.InvariantCulture);
        }

        public DbParameter CreateGuidParameter(string name, Guid value)
        {
            var parameter = CreateParameter(name, DbType.Guid);
            parameter.Value = value;
            return parameter;
        }

        public DbParameter CreateStringParameter(string name, string value, int size = 2000)
        {
            var parameter = CreateParameter(name, DbType.String, size);
            parameter.Value = value;
            return parameter;
        }

        public DbParameter CreateIntParameter(string name, int value)
        {
            var parameter = CreateParameter(name, DbType.Int32);
            parameter.Value = value;
            return parameter;
        }

        public DbParameter CreateBoolParameter(string name, bool value)
        {
            var parameter = CreateParameter(name, DbType.Boolean);
            parameter.Value = value;
            return parameter;
        }

        public DbParameter CreateDateTimeParameter(string name, DateTime value)
        {
            var parameter = CreateParameter(name, DbType.DateTime, 0);
            parameter.Value = value;
            return parameter;
        }

        public DbParameter CreateBinaryParameter(string name, byte[] value, int size = 8000)
        {
            var parameter = CreateParameter(name, DbType.Binary, size);
            parameter.Value = value;
            return parameter;
        }

        private static SqlParameter CreateReturnParameter()
        {
            var parameter = new SqlParameter
            {
                ParameterName = "@ReturnValue", DbType = DbType.Int32, Direction = ParameterDirection.ReturnValue,
            };
            return parameter;
        }

        private static SqlCommand CreateCommand(SqlConnection connection, string sqlCommand, params IDbDataParameter[] parameters)
        {
            var command = connection.CreateCommand();
            command.CommandText = sqlCommand;

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
    }
}
