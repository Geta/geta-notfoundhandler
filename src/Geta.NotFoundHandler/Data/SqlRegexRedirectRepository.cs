using System;
using System.Collections.Generic;
using System.Data;
using Geta.NotFoundHandler.Core.Providers.RegexRedirects;

namespace Geta.NotFoundHandler.Data;

public class SqlRegexRedirectRepository : IRegexRedirectLoader
{
    private const string RegexRedirectsTable = "[dbo].[NotFoundHandler.RegexRedirects]";

    private const string AllFields = "Id, OldUrlRegex, NewUrlFormat, OrderNumber, TimeoutCount";
    
    private readonly IDataExecutor _dataExecutor;
    private readonly RegexRedirectFactory _regexRedirectFactory;

    public SqlRegexRedirectRepository(IDataExecutor dataExecutor, RegexRedirectFactory regexRedirectFactory)
    {
        _dataExecutor = dataExecutor;
        _regexRedirectFactory = regexRedirectFactory;
    }

    public IEnumerable<RegexRedirect> GetAll()
    {
        var sqlCommand = $@"SELECT {AllFields} FROM {RegexRedirectsTable}";

        var dataTable = _dataExecutor.ExecuteQuery(sqlCommand);

        return ToRedirects(dataTable);
    }

    private IEnumerable<RegexRedirect> ToRedirects(DataTable table)
    {
        return table.AsEnumerable().Select(ToRedirect);
    }

    private RegexRedirect ToRedirect(DataRow x)
    {
        return _regexRedirectFactory.Create(
            x.Field<Guid>("Id"),
            x.Field<string>("OldUrlRegex"),
            x.Field<string>("NewUrlFormat"),
            x.Field<int>("OrderNumber"),
            x.Field<int>("TimeoutCount"));

    }
}
