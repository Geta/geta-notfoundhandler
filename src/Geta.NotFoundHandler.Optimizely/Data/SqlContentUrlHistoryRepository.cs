using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Geta.NotFoundHandler.Data;
using Geta.NotFoundHandler.Optimizely.Core.AutomaticRedirects;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Geta.NotFoundHandler.Optimizely.Data
{
    public class SqlContentUrlHistoryRepository : IRepository<ContentUrlHistory>, IContentUrlHistoryLoader
    {
        private const string ContentUrlHistoryTable = "[dbo].[NotFoundHandler.ContentUrlHistory]";
        private const string AllFields = "Id, ContentKey, Urls, CreatedUtc";
        private readonly IDataExecutor _dataExecutor;

        public SqlContentUrlHistoryRepository(IDataExecutor dataExecutor)
        {
            _dataExecutor = dataExecutor;
        }

        private static JsonSerializerSettings JsonSettings
        {
            get
            {
                var settings = new JsonSerializerSettings
                {
                    DateTimeZoneHandling = DateTimeZoneHandling.Utc, Formatting = Formatting.None
                };
                settings.Converters.Add(new StringEnumConverter());
                return settings;
            }
        }

        public bool IsRegistered(ContentUrlHistory entity)
        {
            var sqlCommand = $@"SELECT TOP 1 {AllFields} 
                                FROM {ContentUrlHistoryTable}
                                WHERE ContentKey = @contentKey
                                ORDER BY CreatedUtc DESC";

            var dataTable = _dataExecutor.ExecuteQuery(
                sqlCommand,
                _dataExecutor.CreateStringParameter("contentKey", entity.ContentKey));

            var last = ToContentUrlHistory(dataTable).FirstOrDefault();

            return last != null && last.Urls.Count == entity.Urls.Count && last.Urls.All(entity.Urls.Contains);
        }

        public IEnumerable<(string contentKey, IReadOnlyCollection<ContentUrlHistory> histories)> GetAllMoved()
        {
            var sqlCommand = $@"SELECT h.Id, h.ContentKey, h.Urls, h.CreatedUtc 
                                FROM {ContentUrlHistoryTable} h
                                INNER JOIN 
                                    (SELECT ContentKey
                                    FROM {ContentUrlHistoryTable}
                                    GROUP BY ContentKey
                                    HAVING COUNT(*) > 1) k
                                ON h.ContentKey = k.ContentKey
                                ORDER BY h.ContentKey, h.CreatedUtc DESC";

            var dataTable = _dataExecutor.ExecuteQuery(sqlCommand);

            var histories = ToContentUrlHistory(dataTable);

            return histories.GroupBy(x => x.ContentKey).Select(x => (x.Key, (IReadOnlyCollection<ContentUrlHistory>)x.ToList()));
        }

        public void Save(ContentUrlHistory entity)
        {
            if (entity.Id == Guid.Empty)
            {
                Create(entity);
                return;
            }

            Update(entity);
        }

        public void Delete(ContentUrlHistory entity)
        {
            var sqlCommand = $"DELETE FROM {ContentUrlHistoryTable} WHERE [Id] = @id";
            var idParameter = _dataExecutor.CreateGuidParameter("id", entity.Id);
            _dataExecutor.ExecuteNonQuery(sqlCommand, idParameter);
        }

        private void Create(ContentUrlHistory entity)
        {
            entity.Id = Guid.NewGuid();
            entity.CreatedUtc = DateTime.UtcNow;

            var sqlCommand = $@"INSERT INTO {ContentUrlHistoryTable}
                                    (Id, ContentKey, Urls, CreatedUtc)
                                    VALUES
                                    (@id, @contentKey, @urls, @createdUtc)";

            _dataExecutor.ExecuteNonQuery(
                sqlCommand,
                _dataExecutor.CreateGuidParameter("id", entity.Id),
                _dataExecutor.CreateStringParameter("contentKey", entity.ContentKey),
                _dataExecutor.CreateStringParameter("urls", ToJson(entity.Urls)),
                _dataExecutor.CreateDateTimeParameter("createdUtc", entity.CreatedUtc));
        }

        private void Update(ContentUrlHistory entity)
        {
            if (entity.Id == Guid.Empty)
            {
                throw new ArgumentException($"{nameof(entity.Id)} is empty. Update requires a valid {nameof(entity.Id)} value.");
            }

            var sqlCommand = $@"UPDATE {ContentUrlHistoryTable}
                                    SET ContentKey = @contentKey
                                        ,Urls = @urls
                                        ,CreatedUtc = @createdUtc
                                    WHERE Id = @id";

            _dataExecutor.ExecuteNonQuery(
                sqlCommand,
                _dataExecutor.CreateGuidParameter("id", entity.Id),
                _dataExecutor.CreateStringParameter("contentKey", entity.ContentKey),
                _dataExecutor.CreateStringParameter("urls", ToJson(entity.Urls)),
                _dataExecutor.CreateDateTimeParameter("createdUtc", entity.CreatedUtc));
        }

        private static string ToJson(ICollection<TypedUrl> urls)
        {
            return JsonConvert.SerializeObject(urls, JsonSettings);
        }

        private static ICollection<TypedUrl> FromJson(string value)
        {
            return string.IsNullOrEmpty(value)
                ? new List<TypedUrl>()
                : JsonConvert.DeserializeObject<List<TypedUrl>>(value, JsonSettings);
        }

        private static IEnumerable<ContentUrlHistory> ToContentUrlHistory(DataTable table)
        {
            return table.AsEnumerable().Select(ToContentUrlHistory);
        }

        private static ContentUrlHistory ToContentUrlHistory(DataRow x)
        {
            return new ContentUrlHistory
            {
                Id = x.Field<Guid>("Id"),
                ContentKey = x.Field<string>("ContentKey"),
                Urls = FromJson(x.Field<string>("Urls")),
                CreatedUtc = x.Field<DateTime>("CreatedUtc")
            };
        }
    }
}
