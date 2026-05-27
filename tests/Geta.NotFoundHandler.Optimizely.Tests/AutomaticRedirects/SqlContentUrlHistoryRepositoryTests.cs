using System;
using System.Data;
using System.Linq;
using FakeItEasy;
using Geta.NotFoundHandler.Data;
using Geta.NotFoundHandler.Optimizely.Data;
using Xunit;

namespace Geta.NotFoundHandler.Optimizely.Tests.AutomaticRedirects;

public class SqlContentUrlHistoryRepositoryTests
{
    private readonly IDataExecutor _dataExecutor = A.Fake<IDataExecutor>();
    private readonly SqlContentUrlHistoryRepository _repository;

    public SqlContentUrlHistoryRepositoryTests()
    {
        _repository = new SqlContentUrlHistoryRepository(_dataExecutor);
    }

    [Fact]
    public void GetAllMoved_paged_passes_skip_and_take()
    {
        A.CallTo(() => _dataExecutor.ExecuteQuery(A<string>._, A<IDbDataParameter[]>._)).Returns(EmptyTable());

        _ = _repository.GetAllMoved(40, 20).ToList();

        A.CallTo(() => _dataExecutor.CreateIntParameter("skip", 40)).MustHaveHappened();
        A.CallTo(() => _dataExecutor.CreateIntParameter("take", 20)).MustHaveHappened();
    }

    [Fact]
    public void GetAllMoved_paged_groups_histories_by_content_key()
    {
        var table = EmptyTable();
        AddRow(table, "key-a");
        AddRow(table, "key-a");
        AddRow(table, "key-b");
        A.CallTo(() => _dataExecutor.ExecuteQuery(A<string>._, A<IDbDataParameter[]>._)).Returns(table);

        var result = _repository.GetAllMoved(0, 1000).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(2, result.Single(x => x.contentKey == "key-a").histories.Count);
        Assert.Equal(1, result.Single(x => x.contentKey == "key-b").histories.Count);
    }

    [Fact]
    public void GetAllMoved_stops_paging_when_page_is_not_full()
    {
        var shortPage = EmptyTable();
        AddRow(shortPage, "only-key");
        A.CallTo(() => _dataExecutor.ExecuteQuery(A<string>._, A<IDbDataParameter[]>._)).Returns(shortPage);

        _ = _repository.GetAllMoved().ToList();

        // The first page returns fewer rows than the page size, so no further query is issued.
        A.CallTo(() => _dataExecutor.ExecuteQuery(A<string>._, A<IDbDataParameter[]>._)).MustHaveHappenedOnceExactly();
    }

    private static DataTable EmptyTable()
    {
        var table = new DataTable();
        table.Columns.Add("Id", typeof(Guid));
        table.Columns.Add("ContentKey", typeof(string));
        table.Columns.Add("Urls", typeof(string));
        table.Columns.Add("CreatedUtc", typeof(DateTime));
        return table;
    }

    private static void AddRow(DataTable table, string contentKey)
    {
        table.Rows.Add(Guid.NewGuid(), contentKey, string.Empty, DateTime.UtcNow);
    }
}
