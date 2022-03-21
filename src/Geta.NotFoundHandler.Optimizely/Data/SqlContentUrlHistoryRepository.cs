using System;
using System.Collections.Generic;
using Geta.NotFoundHandler.Data;
using Geta.NotFoundHandler.Optimizely.Core.AutomaticRedirects;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Geta.NotFoundHandler.Optimizely.Data
{
    public class SqlContentUrlHistoryRepository : IRepository<ContentUrlHistory>
    {
        private readonly IDataExecutor _dataExecutor;

        private const string ContentUrlHistoryTable = "[dbo].[NotFoundHandler.ContentUrlHistory]";

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

        public SqlContentUrlHistoryRepository(IDataExecutor dataExecutor)
        {
            _dataExecutor = dataExecutor;
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

        public void Delete(ContentUrlHistory entity)
        {
            var sqlCommand = $"DELETE FROM {ContentUrlHistoryTable} WHERE [Id] = @id";
            var idParameter = _dataExecutor.CreateGuidParameter("id", entity.Id);
            _dataExecutor.ExecuteNonQuery(sqlCommand, idParameter);
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
    }
}
