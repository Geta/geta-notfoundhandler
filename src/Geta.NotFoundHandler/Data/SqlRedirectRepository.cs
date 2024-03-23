// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using Geta.NotFoundHandler.Core;
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
            var sqlCommand = $"SELECT {AllFields} FROM {RedirectsTable}";

            var dataTable = _dataExecutor.ExecuteQuery(sqlCommand);

            return ToCustomRedirects(dataTable);
        }

        public IEnumerable<CustomRedirect> GetByState(RedirectState state)
        {
            return GetRedirects(new QueryParams() { QueryState = state }).Redirects;
        }

        public IEnumerable<CustomRedirect> Find(string searchText)
        {
            return GetRedirects(new QueryParams() { QueryText = searchText }).Redirects;
        }

        public CustomRedirectsResult GetRedirects(QueryParams query)
        {
            var parameters = new List<IDbDataParameter>();

            var whereString = GetWhereString(query, parameters);

            var suffixString = GetSuffixString(query, parameters, out var isPaginated);

            var dataTable = _dataExecutor.ExecuteQuery(
                $"SELECT {AllFields} FROM {RedirectsTable}{whereString}{suffixString}",
                parameters.ToArray());

            var items = ToCustomRedirects(dataTable);
            var totalCount = items.Count;
            if (isPaginated)
            {
                var whereState = query.QueryState != null ? @"
                    WHERE state = @state" : "";
                totalCount = _dataExecutor.ExecuteScalar($"SELECT COUNT(*) FROM {RedirectsTable}{whereState}",
                    _dataExecutor.CreateIntParameter("state", (int)query.QueryState));
            }
            var filteredCount = totalCount;
            if (!string.IsNullOrWhiteSpace(whereString))
            {
                filteredCount = _dataExecutor.ExecuteScalar($"SELECT COUNT(*) FROM {RedirectsTable}{whereString}", parameters.ToArray());
            }

            return new CustomRedirectsResult(items, totalCount, filteredCount);
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

        private static IList<CustomRedirect> ToCustomRedirects(DataTable table)
        {
            return table.AsEnumerable().Select(ToCustomRedirect).ToList();
        }

        private static CustomRedirect ToCustomRedirect(DataRow x)
        {
            return new CustomRedirect(
                x.Field<string>("OldUrl"),
                x.Field<string>("NewUrl"),
                x.Field<bool>("WildCardSkipAppend"),
                x.Field<RedirectType>("RedirectType"))
            { Id = x.Field<Guid>("Id"), State = x.Field<int>("State") };
        }

        private string GetWhereString(QueryParams query, IList<IDbDataParameter> parameters)
        {
            var conditions = new List<string>();
            if (!string.IsNullOrEmpty(query?.QueryText))
            {
                parameters.Add(_dataExecutor.CreateStringParameter("searchText", query.QueryText));
                conditions.Add(@"(OldUrl like '%' + @searchText + '%'
                            OR NewUrl like '%' + @searchText + '%')");
            }

            if (query.QueryState != null)
            {
                parameters.Add(_dataExecutor.CreateIntParameter("state", (int)query.QueryState));
                conditions.Add("State = @state");
            }

            var hasFilter = conditions.Any();
            if (hasFilter)
            {
                return $@"
                    WHERE {string.Join(" AND ", conditions)}";
            }

            return "";
        }

        private string GetSuffixString(QueryParams query, IList<IDbDataParameter> parameters, out bool isPaginated)
        {
            var suffixString = "";
            var safeSortBy = Regex.Replace(query.SortBy ?? string.Empty, "[^A-Za-z]", "", RegexOptions.IgnoreCase);
            var hasSortBy = !string.IsNullOrWhiteSpace(safeSortBy);
            if (hasSortBy)
            {
                suffixString += $@"
                    ORDER BY [{safeSortBy}] {(query.SortDirection == SortOrder.Ascending ? "ASC" : "DESC")}";
            }

            isPaginated = false;
            if (query.PageSize is int ps && ps > 0)
            {
                isPaginated = true;
                if (!hasSortBy)
                {
                    // Adds dummy ORDER BY for pagination
                    suffixString += @"
                        ORDER BY(SELECT NULL)";
                }
                parameters.Add(_dataExecutor.CreateIntParameter("pageSize", ps));
                parameters.Add(_dataExecutor.CreateIntParameter("skip", (query.Page - 1) * ps));
                suffixString += $@"
                    OFFSET {(query.Page - 1) * ps} ROWS
                    FETCH NEXT @pageSize ROWS ONLY";
            }

            return suffixString;
        }
    }
}
