// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Data;
using FakeItEasy;
using Geta.NotFoundHandler.Data;
using Geta.NotFoundHandler.Infrastructure.Configuration;
using Geta.NotFoundHandler.Infrastructure.Initialization;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Geta.NotFoundHandler.Tests
{
    public class UpgraderTests
    {
        private readonly IDataExecutor _dataExecutor = A.Fake<IDataExecutor>();
        private readonly Upgrader _sut;

        public UpgraderTests()
        {
            _sut = new Upgrader(NullLogger<Upgrader>.Instance, _dataExecutor);

            A.CallTo(() => _dataExecutor.ExecuteStoredProcedure(A<string>._)).Returns(2);
            A.CallTo(() => _dataExecutor.ExecuteNonQuery(A<string>._, A<IDbDataParameter[]>._)).Returns(true);
            A.CallTo(() => _dataExecutor.ExecuteScalar(A<string>.That.Contains("INFORMATION_SCHEMA.TABLES"))).Returns(1);
        }

        [Fact]
        public void Start_adds_the_new_columns_to_an_older_database()
        {
            _sut.Start();

            AssertExecuted("ALTER TABLE [dbo].[NotFoundHandler.Redirects] ADD [ExactMatch] [bit] NOT NULL");
            AssertExecuted("ALTER TABLE [dbo].[NotFoundHandler.RegexRedirects] ADD [RedirectType] [int] NULL");
            AssertExecuted($"RETURN {NotFoundHandlerOptions.CurrentDbVersion}");
        }

        [Fact]
        public void Start_leaves_columns_already_there_alone()
        {
            A.CallTo(() => _dataExecutor.ExecuteScalar(A<string>.That.Contains("sys.columns"))).Returns(1);

            _sut.Start();

            AssertExecuted("ALTER TABLE", times: 0);
            AssertExecuted($"RETURN {NotFoundHandlerOptions.CurrentDbVersion}");
        }

        [Fact]
        public void Start_keeps_the_version_when_a_column_could_not_be_added()
        {
            A.CallTo(() => _dataExecutor.ExecuteNonQuery(A<string>.That.Contains("ALTER TABLE"), A<IDbDataParameter[]>._))
                .Returns(false);

            _sut.Start();

            AssertExecuted("ALTER PROCEDURE", times: 0);
        }

        [Fact]
        public void Start_does_nothing_on_a_current_database()
        {
            A.CallTo(() => _dataExecutor.ExecuteStoredProcedure(A<string>._)).Returns(NotFoundHandlerOptions.CurrentDbVersion);

            _sut.Start();

            A.CallTo(() => _dataExecutor.ExecuteNonQuery(A<string>._, A<IDbDataParameter[]>._)).MustNotHaveHappened();
        }

        [Fact]
        public void Start_creates_the_new_columns_on_a_new_database()
        {
            A.CallTo(() => _dataExecutor.ExecuteStoredProcedure(A<string>._)).Returns(-1);

            _sut.Start();

            AssertExecuted("[ExactMatch] [bit] NOT NULL");
            AssertExecuted("[RedirectType] [int] NULL");
            AssertExecuted("ALTER TABLE", times: 0);
        }

        private void AssertExecuted(string sql, int times = 1)
        {
            A.CallTo(() => _dataExecutor.ExecuteNonQuery(A<string>.That.Contains(sql), A<IDbDataParameter[]>._))
                .MustHaveHappenedANumberOfTimesMatching(n => n == times);
        }
    }
}
