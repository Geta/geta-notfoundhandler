using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Geta.NotFoundHandler.Core.Suggestions;

namespace Geta.NotFoundHandler.Data
{
    public class SqlSuggestionRepository: IRepository<Suggestion>, ISuggestionLoader
    {
        public IEnumerable<SuggestionSummary> GetAllSummaries()
        {
            var summaries = new List<SuggestionSummary>();
            var dabe = DataAccessBaseEx.GetWorker();
            using (var allkeys = dabe.GetAllSuggestionCount())
            {
                foreach (DataTable table in allkeys.Tables)
                {
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
                }
            }

            return summaries;
        }

        public void Save(Suggestion entity)
        {
            throw new System.NotImplementedException();
        }

        public void Delete(Suggestion entity)
        {
            throw new System.NotImplementedException();
        }

        private static IEnumerable<RefererSummary> GetReferers(string url)
        {
            var dataAccess = DataAccessBaseEx.GetWorker();
            var referers = new List<RefererSummary>();

            using (var referersDs = dataAccess.GetSuggestionReferers(url))
            {
                var table = referersDs.Tables[0];
                if (table == null) return referers;

                var unknownReferers = 0;
                foreach (DataRow row in table.Rows)
                {
                    var referer = row[0].ToString();
                    var count = Convert.ToInt32(row[1].ToString());
                    if (referer.Trim() != string.Empty
                        && !referer.Contains("(null)"))
                    {
                        if (!referer.Contains("://")) referer = referer.Insert(0, "/");
                        referers.Add(new RefererSummary
                        {
                            Url = referer,
                            Count = count
                        });
                    }
                    else
                    {
                        unknownReferers += count;
                    }

                }
                if (unknownReferers > 0)
                {

                    referers.Add(new RefererSummary
                    {
                        Unknown = true,
                        Count = unknownReferers
                    });
                }
            }

            return referers;
        }
    }
}
