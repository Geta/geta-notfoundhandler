using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Geta.NotFoundHandler.Core.Providers.RegexRedirects;

namespace Geta.NotFoundHandler.Data;

public class SqlRegexRedirectRepository : IRepository<RegexRedirect>, IRegexRedirectLoader
{
    private const string RegexRedirectsTable = "[dbo].[NotFoundHandler.RegexRedirects]";

    private const string AllFields = "Id, OldUrlRegex, NewUrlFormat, OrderNumber, TimeoutCount, CreatedAt, ModifiedAt";

    private readonly IDataExecutor _dataExecutor;
    private readonly RegexRedirectFactory _regexRedirectFactory;

    public SqlRegexRedirectRepository(IDataExecutor dataExecutor, RegexRedirectFactory regexRedirectFactory)
    {
        _dataExecutor = dataExecutor;
        _regexRedirectFactory = regexRedirectFactory;
    }

    public IEnumerable<RegexRedirect> GetAll()
    {
        var sqlCommand = $@"SELECT {AllFields} FROM {RegexRedirectsTable} ORDER BY OrderNumber";

        var dataTable = _dataExecutor.ExecuteQuery(sqlCommand);

        return ToRedirects(dataTable);
    }

    public RegexRedirect Get(Guid id)
    {
        var sqlCommand = $@"SELECT {AllFields} FROM {RegexRedirectsTable}
                                    WHERE Id = @id";

        var dataTable = _dataExecutor.ExecuteQuery(
            sqlCommand,
            _dataExecutor.CreateGuidParameter("id", id));

        return ToRedirects(dataTable).FirstOrDefault();
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
            x.Field<int>("TimeoutCount"),
            x.Field<DateTime>("CreatedAt"),
            x.Field<DateTime>("ModifiedAt"));
    }

    public void Save(RegexRedirect entity)
    {
        if (entity.Id == null)
        {
            Create(entity);
            return;
        }

        Update(entity);
    }

    private void Create(RegexRedirect entity)
    {
        var sqlCommand = $@"INSERT INTO {RegexRedirectsTable}
                                    (Id, OldUrlRegex, NewUrlFormat, OrderNumber, TimeoutCount, CreatedAt, ModifiedAt)
                                    VALUES
                                    (@id, @oldurlregex, @newurlformat, @ordernumber, @timeoutcount, @createdat, @modifiedat)";

        var now = DateTime.UtcNow;

        _dataExecutor.ExecuteNonQuery(
            sqlCommand,
            _dataExecutor.CreateGuidParameter("id", Guid.NewGuid()),
            _dataExecutor.CreateStringParameter("oldurlregex", entity.OldUrlRegex.ToString()),
            _dataExecutor.CreateStringParameter("newurlformat", entity.NewUrlFormat),
            _dataExecutor.CreateIntParameter("ordernumber", entity.OrderNumber),
            _dataExecutor.CreateIntParameter("timeoutcount", 0),
            _dataExecutor.CreateDateTimeParameter("createdat", now),
            _dataExecutor.CreateDateTimeParameter("modifiedat", now));
    }

    private void Update(RegexRedirect entity)
    {
        if (!entity.Id.HasValue)
        {
            throw new ArgumentException($"{nameof(entity.Id)} is null. Update requires a valid {nameof(entity.Id)} value.");
        }

        var sqlCommand = $@"UPDATE {RegexRedirectsTable}
                                    SET OldUrlRegex = @oldurlregex
                                        ,NewUrlFormat = @newurlformat
                                        ,OrderNumber = @ordernumber,
                                        ,ModifiedAt = @modifiedat
                                    WHERE Id = @id";

        _dataExecutor.ExecuteNonQuery(
            sqlCommand,
            _dataExecutor.CreateGuidParameter("id", entity.Id.Value),
            _dataExecutor.CreateStringParameter("oldurlregex", entity.OldUrlRegex.ToString()),
            _dataExecutor.CreateStringParameter("newurlformat", entity.NewUrlFormat),
            _dataExecutor.CreateIntParameter("ordernumber", entity.OrderNumber),
            _dataExecutor.CreateDateTimeParameter("modifiedat", DateTime.UtcNow));
    }

    public void Delete(RegexRedirect entity)
    {
        if (!entity.Id.HasValue)
        {
            throw new ArgumentException($"{nameof(entity.Id)} is null. Delete requires a valid {nameof(entity.Id)} value.");
        }

        var sqlCommand = $@"DELETE FROM {RegexRedirectsTable}
                                    WHERE Id = @id";

        _dataExecutor.ExecuteNonQuery(
            sqlCommand,
            _dataExecutor.CreateGuidParameter("id", entity.Id.Value));
    }
}
