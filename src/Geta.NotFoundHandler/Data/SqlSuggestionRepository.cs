using System;
using System.Collections.Generic;
using System.Data;
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
                        var summary = new SuggestionSummary
                        {
                            OldUrl = row[0].ToString(),
                            Count = Convert.ToInt32(row[1])
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
    }
}
