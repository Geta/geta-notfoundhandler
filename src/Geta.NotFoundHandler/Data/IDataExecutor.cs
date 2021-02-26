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
    }
}
