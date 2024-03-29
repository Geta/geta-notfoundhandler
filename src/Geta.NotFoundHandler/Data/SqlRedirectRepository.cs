// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Geta.NotFoundHandler.Core.Redirects;

namespace Geta.NotFoundHandler.Data
{
    public class SqlRedirectRepository : IRepository<CustomRedirect>, IRedirectLoader
    {
        private readonly IDataExecutor _dataExecutor;

        private const string RedirectsTable = "[dbo].[NotFoundHandler.Redirects]";

        private const string AllFields = "Id, OldUrl, NewUrl, State, WildCardSkipAppend, RedirectType";

        public SqlRedirectRepository(IDataExecutor dataExecutor)
        {
            _dataExecutor = dataExecutor;
        }

        public void Save(CustomRedirect entity)
        {
            if (entity.Id == null)
            {
                Create(entity);
                return;
            }

            Update(entity);
        }

        private void Create(CustomRedirect entity)
        {
            var sqlCommand = $@"INSERT INTO {RedirectsTable}
                                    (Id, OldUrl, NewUrl, State, WildCardSkipAppend, RedirectType)
                                    VALUES
                                    (@id, @oldurl, @newurl, @state, @wildcardskipappend, @redirectType)";

            _dataExecutor.ExecuteNonQuery(
                sqlCommand,
                _dataExecutor.CreateGuidParameter("id", Guid.NewGuid()),
                _dataExecutor.CreateStringParameter("oldurl", entity.OldUrl),
                _dataExecutor.CreateStringParameter("newurl", entity.NewUrl),
                _dataExecutor.CreateIntParameter("state", entity.State),
                _dataExecutor.CreateBoolParameter("wildcardskipappend", entity.WildCardSkipAppend),
                _dataExecutor.CreateIntParameter("redirectType", (int)entity.RedirectType));
        }

        private void Update(CustomRedirect entity)
        {
            if (!entity.Id.HasValue)
            {
                throw new ArgumentException($"{nameof(entity.Id)} is null. Update requires a valid {nameof(entity.Id)} value.");
            }

            var sqlCommand = $@"UPDATE {RedirectsTable}
                                    SET OldUrl = @oldurl
                                        ,NewUrl = @newurl
                                        ,State = @state
                                        ,WildCardSkipAppend = @wildcardskipappend
                                        ,RedirectType = @redirectType
                                    WHERE Id = @id";

            _dataExecutor.ExecuteNonQuery(
                sqlCommand,
                _dataExecutor.CreateGuidParameter("id", entity.Id.Value),
                _dataExecutor.CreateStringParameter("oldurl", entity.OldUrl),
                _dataExecutor.CreateStringParameter("newurl", entity.NewUrl),
                _dataExecutor.CreateIntParameter("state", entity.State),
                _dataExecutor.CreateBoolParameter("wildcardskipappend", entity.WildCardSkipAppend),
                _dataExecutor.CreateIntParameter("redirectType", (int)entity.RedirectType));
        }

        public void Delete(CustomRedirect entity)
        {
            if (!entity.Id.HasValue)
            {
                throw new ArgumentException($"{nameof(entity.Id)} is null. Delete requires a valid {nameof(entity.Id)} value.");
            }

            var sqlCommand = $@"DELETE FROM {RedirectsTable}
                                    WHERE Id = @id";

            _dataExecutor.ExecuteNonQuery(
                sqlCommand,
                _dataExecutor.CreateGuidParameter("id", entity.Id.Value));
        }

        public CustomRedirect GetByOldUrl(string oldUrl)
        {
            var sqlCommand = $@"SELECT {AllFields} FROM {RedirectsTable}
                                    WHERE OldUrl = @oldurl";

            var dataTable = _dataExecutor.ExecuteQuery(
                sqlCommand,
                _dataExecutor.CreateStringParameter("oldurl", oldUrl));

            return ToCustomRedirects(dataTable).FirstOrDefault();
        }

        public IEnumerable<CustomRedirect> GetAll()
        {
            var sqlCommand = $@"SELECT {AllFields} FROM {RedirectsTable}";

            var dataTable = _dataExecutor.ExecuteQuery(sqlCommand);

            return ToCustomRedirects(dataTable);
        }

        public IEnumerable<CustomRedirect> GetByState(RedirectState state)
        {
            var sqlCommand = $@"SELECT {AllFields} FROM {RedirectsTable}
                                    WHERE State = @state";

            var dataTable = _dataExecutor.ExecuteQuery(
                sqlCommand,
                _dataExecutor.CreateIntParameter("state", (int)state));

            return ToCustomRedirects(dataTable);
        }

        public IEnumerable<CustomRedirect> Find(string searchText)
        {
            var sqlCommand = $@"SELECT {AllFields} FROM {RedirectsTable}
                                    WHERE OldUrl like '%' + @searchText + '%'
                                    OR NewUrl like '%' + @searchText + '%'";

            var dataTable = _dataExecutor.ExecuteQuery(
                sqlCommand,
                _dataExecutor.CreateStringParameter("searchText", searchText));

            return ToCustomRedirects(dataTable);
        }

        public CustomRedirect Get(Guid id)
        {
            var sqlCommand = $@"SELECT {AllFields} FROM {RedirectsTable}
                                    WHERE Id = @id";

            var dataTable = _dataExecutor.ExecuteQuery(
                sqlCommand,
                _dataExecutor.CreateGuidParameter("id", id));

            return ToCustomRedirects(dataTable).FirstOrDefault();
        }

        private static IEnumerable<CustomRedirect> ToCustomRedirects(DataTable table)
        {
            return table.AsEnumerable().Select(ToCustomRedirect);
        }

        private static CustomRedirect ToCustomRedirect(DataRow x)
        {
            return new CustomRedirect(
                x.Field<string>("OldUrl"),
                x.Field<string>("NewUrl"),
                x.Field<bool>("WildCardSkipAppend"),
                x.Field<RedirectType>("RedirectType")) { Id = x.Field<Guid>("Id"), State = x.Field<int>("State") };
        }
    }
}
