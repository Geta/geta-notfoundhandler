// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Data;
using System.Data.Common;

namespace Geta.NotFoundHandler.Data
{
    public interface IDataExecutor
    {
        DataTable ExecuteQuery(string sqlCommand, params IDbDataParameter[] parameters);
        bool ExecuteNonQuery(string sqlCommand, params IDbDataParameter[] parameters);
        int ExecuteScalar(string sqlCommand);
        int ExecuteStoredProcedure(string sqlCommand, int defaultReturnValue = -1);
        DbParameter CreateParameter(string parameterName, DbType dbType);
        DbParameter CreateParameter(string parameterName, DbType dbType, int size);
        DbParameter CreateGuidParameter(string name, Guid value);
        DbParameter CreateStringParameter(string name, string value, int size = 2000);
        DbParameter CreateIntParameter(string name, int value);
        DbParameter CreateBoolParameter(string name, bool value);
        DbParameter CreateDateTimeParameter(string name, DateTime value);
        DbParameter CreateBinaryParameter(string name, byte[] value, int size = 8000);
    }
}
