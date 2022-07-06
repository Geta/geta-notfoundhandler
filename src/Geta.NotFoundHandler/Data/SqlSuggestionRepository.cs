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
            var summaries = new List<SuggestionSummary>();
            var table = GetAllSuggestions();

            var suggestionsCountTable = table.DefaultView.ToTable(true, "OldUrlCount", "OldUrl");

            foreach (DataRow row in suggestionsCountTable.Rows)
            {
                var oldUrl = row["OldUrl"].ToString();
                
                var summary = new SuggestionSummary
                {
                    OldUrl = oldUrl, Count = Convert.ToInt32(row["OldUrlCount"]), Referers = GetReferers(oldUrl, table).ToList()
                };
                summaries.Add(summary);
            }

            return summaries;
        }

        private static IEnumerable<RefererSummary> GetReferers(string url, DataTable table)
        {
            var referers = new List<RefererSummary>();

            var referrersTable = table.Select($"OldUrl = '{url.Replace("'", "''")}'" ,"RefererCount DESC");
            if (referrersTable.Length == 0) return referers;

            var unknownReferers = 0;
            foreach (var row in referrersTable)
            {
                var referer = row["Referer"].ToString() ?? string.Empty;
                var count = Convert.ToInt32(row["RefererCount"].ToString());
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

        private DataTable GetAllSuggestions()
        {
            var sqlCommand = $@"
            SELECT c.OldUrlCount, c.OldUrl, r.Referer, COUNT(r.Referer) as RefererCount
            FROM
                (SELECT 
                    COUNT([OldUrl]) as OldUrlCount
                    ,[OldUrl]
                  FROM {SuggestionsTable}
                GROUP BY [OldUrl]) c
            INNER JOIN {SuggestionsTable} r  ON c.OldUrl = r.OldUrl
            GROUP by c.OldUrlCount, c.OldUrl, r.Referer
            ORDER BY OldUrlCount desc";
            return _dataExecutor.ExecuteQuery(sqlCommand);
        }
    }
}
