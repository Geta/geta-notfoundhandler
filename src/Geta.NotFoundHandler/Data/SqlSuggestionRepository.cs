// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
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

        public IEnumerable<SuggestionSummary> GetAllSummaries()
        {
            var table = GetSuggestionsPaged(null, null);

            return CreateSuggestionSummaries(table);            
        }

        public IEnumerable<SuggestionSummary> GetSummariesPaged(int page, int pageSize)
        {
            var table = GetSuggestionsPaged(page, pageSize);

            return CreateSuggestionSummaries(table);
        }

        public int GetSummaryCount()
        {
            return CountSummaries();
        }

        private IEnumerable<SuggestionSummary> CreateSuggestionSummaries(DataTable table)
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
            var sqlCommand = $@"delete from {SuggestionsTable}";
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

        private DataTable GetSuggestionsPaged(int? page, int? pageSize)
        {
            var sqlCommand =
                $"SELECT [OldUrl], COUNT(*) as Requests FROM {SuggestionsTable} GROUP BY [OldUrl] order by Requests desc";

            if (page.HasValue && pageSize.HasValue)
            {
                page = Math.Min(1, page.Value);
                var skip = (page.Value - 1) * pageSize.Value;

                sqlCommand += $" OFFSET {skip} ROWS FETCH NEXT {pageSize.Value} ROWS ONLY";
            }

            return _dataExecutor.ExecuteQuery(sqlCommand);
        }

        private int CountSummaries()
        {
            var sqlCommand = $"SELECT COUNT([ID]) FROM {SuggestionsTable}";

            return _dataExecutor.ExecuteScalar(sqlCommand);
        }

        public DataTable GetSuggestionReferers(string url)
        {
            var sqlCommand =
                $"SELECT [Referer], COUNT(*) as Requests FROM {SuggestionsTable} where [OldUrl] = @oldUrl GROUP BY [Referer] order by Requests desc";

            var oldUrlParam = _dataExecutor.CreateParameter("oldUrl", DbType.String, 2000);
            oldUrlParam.Value = url;

            return _dataExecutor.ExecuteQuery(sqlCommand, oldUrlParam);
        }
    }
}
