// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Geta.NotFoundHandler.Core;
using Geta.NotFoundHandler.Core.Suggestions;

namespace Geta.NotFoundHandler.Data
{
    public class SqlSuggestionRepository : ISuggestionLoader, ISuggestionRepository
    {
        private const string SuggestionsTable = "[dbo].[NotFoundHandler.Suggestions]";

        private readonly IDataExecutor _dataExecutor;

        public SqlSuggestionRepository(IDataExecutor dataExecutor)
        {
            _dataExecutor = dataExecutor;
        }

        public SuggestionRedirectsResult GetSummaries(QueryParams query)
        {
            var parameters = new List<IDbDataParameter>();

            var whereString = GetWhereString(query, parameters);

            var suffixString = GetSuffixString(query, parameters, out var isPaginated);

            var dataTable = _dataExecutor.ExecuteQuery(
                $@"SELECT [OldUrl], COUNT(*) as Requests FROM {SuggestionsTable}{whereString}
                    GROUP BY [OldUrl]{suffixString}",
                parameters.ToArray());

            var items = CreateSuggestionSummaries(dataTable);
            var totalCount = items.Count;
            if (isPaginated)
            {
                totalCount = _dataExecutor.ExecuteScalar($"SELECT COUNT(*) FROM {SuggestionsTable}");
            }
            var filteredCount = totalCount;
            if (!string.IsNullOrWhiteSpace(whereString))
            {
                filteredCount = _dataExecutor.ExecuteScalar($"SELECT COUNT(*) FROM {SuggestionsTable}{whereString}", parameters.ToArray());
            }

            return new SuggestionRedirectsResult(items, totalCount, filteredCount);
        }

        private IList<SuggestionSummary> CreateSuggestionSummaries(DataTable table)
        {
            var summaries = new List<SuggestionSummary>();

            foreach (DataRow row in table.Rows)
            {
                var oldUrl = row[0].ToString();
                var summary = new SuggestionSummary
                {
                    OldUrl = oldUrl,
                    Count = Convert.ToInt32(row[1]),
                    Referers = GetReferers(oldUrl).ToList()
                };
                summaries.Add(summary);
            }

            return summaries;
        }

        private IEnumerable<RefererSummary> GetReferers(string url)
        {
            var referers = new List<RefererSummary>();

            var table = GetSuggestionReferers(url);
            if (table == null) return referers;

            var unknownReferers = 0;
            foreach (DataRow row in table.Rows)
            {
                var referer = row[0].ToString() ?? string.Empty;
                var count = Convert.ToInt32(row[1].ToString());
                if (referer.Trim() != string.Empty
                    && !referer.Contains("(null)"))
                {
                    if (!referer.Contains("://")) referer = referer.Insert(0, "/");
                    referers.Add(new RefererSummary { Url = referer, Count = count });
                }
                else
                {
                    unknownReferers += count;
                }
            }

            if (unknownReferers > 0)
            {
                referers.Add(new RefererSummary { Unknown = true, Count = unknownReferers });
            }

            return referers;
        }

        public void DeleteAll()
        {
            var sqlCommand = $"delete from {SuggestionsTable}";
            _dataExecutor.ExecuteNonQuery(sqlCommand);
        }

        public void Delete(int maxErrors, int minimumDaysOld)
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
            _dataExecutor.ExecuteNonQuery(sqlCommand);
        }

        public void DeleteForRequest(string oldUrl)
        {
            var sqlCommand = $"DELETE FROM {SuggestionsTable} WHERE [OldUrl] = @oldurl";
            var oldUrlParam = _dataExecutor.CreateParameter("oldurl", DbType.String, 2000);
            oldUrlParam.Value = oldUrl;

            _dataExecutor.ExecuteNonQuery(sqlCommand, oldUrlParam);
        }

        public void Save(string oldUrl, string referer, DateTime requestedOn)
        {
            var sqlCommand = @$"INSERT INTO {SuggestionsTable}
                                    (Requested, OldUrl, Referer)
                                    VALUES
                                    (@requested, @oldurl, @referer)";

            var requestedParam = _dataExecutor.CreateParameter("requested", DbType.DateTime, 0);
            requestedParam.Value = requestedOn;

            var refererParam = _dataExecutor.CreateParameter("referer", DbType.String, 2000);
            refererParam.Value = referer ?? string.Empty;

            var oldUrlParam = _dataExecutor.CreateParameter("oldurl", DbType.String, 2000);
            oldUrlParam.Value = oldUrl;

            _dataExecutor.ExecuteNonQuery(sqlCommand, requestedParam, refererParam, oldUrlParam);
        }

        public DataTable GetSuggestionReferers(string url)
        {
            var sqlCommand =
                $"SELECT [Referer], COUNT(*) as Requests FROM {SuggestionsTable} where [OldUrl] = @oldUrl GROUP BY [Referer] order by Requests desc";

            var oldUrlParam = _dataExecutor.CreateParameter("oldUrl", DbType.String, 2000);
            oldUrlParam.Value = url;

            return _dataExecutor.ExecuteQuery(sqlCommand, oldUrlParam);
        }

        private string GetWhereString(QueryParams query, IList<IDbDataParameter> parameters)
        {
            if (!string.IsNullOrEmpty(query?.QueryText))
            {
                parameters.Add(_dataExecutor.CreateStringParameter("searchText", query.QueryText));
                return @"
                    WHERE OldUrl like '%' + @searchText + '%'";
            }

            return "";
        }

        private string GetSuffixString(QueryParams query, IList<IDbDataParameter> parameters, out bool isPaginated)
        {
            var suffixString = "";
            var hasSortBy = !string.IsNullOrWhiteSpace(query.SortBy);
            if (hasSortBy)
            {
                parameters.Add(_dataExecutor.CreateStringParameter("sortBy", query.SortBy));
                suffixString += $@"
                    ORDER BY @sortBy {(query.SortDirection == SortOrder.Ascending ? "ASC" : "DESC")}";
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
