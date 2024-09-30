// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Geta.NotFoundHandler.Data;
using Geta.NotFoundHandler.Optimizely.Core.AutomaticRedirects;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Security.Cryptography;
using System.Text;

namespace Geta.NotFoundHandler.Optimizely.Data
{
    public class SqlContentUrlHistoryRepository : IRepository<ContentUrlHistory>, IContentUrlHistoryLoader
    {
        private const string ContentUrlHistoryTable = "[dbo].[NotFoundHandler.ContentUrlHistory]";
        private const string AllFields = "Id, ContentKey, Urls, CreatedUtc, md5_ContentKey";
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
        
        private static byte[] CalculateMd5Hash(string input)
        {
            using var md5 = MD5.Create();
            var inputBytes = Encoding.Unicode.GetBytes(input);
            var hashBytes = md5.ComputeHash(inputBytes);

            return hashBytes;
        }

        public bool IsRegistered(ContentUrlHistory entity)
        {
            var sqlCommand = $@"SELECT TOP 1 {AllFields} 
                                FROM {ContentUrlHistoryTable}
                                WHERE ContentKey = @contentKey AND md5_ContentKey = @contentKeyHash
                                ORDER BY CreatedUtc DESC";

            var dataTable = _dataExecutor.ExecuteQuery(
                sqlCommand,
                _dataExecutor.CreateStringParameter("contentKey", entity.ContentKey),
                _dataExecutor.CreateBinaryParameter("contentKeyHash",  CalculateMd5Hash(entity.ContentKey))
                );

            var last = ToContentUrlHistory(dataTable).FirstOrDefault();

            var result = last != null && last.Urls.Count == entity.Urls.Count && last.Urls.All(entity.Urls.Contains);

            return result;
        }

        public IEnumerable<(string contentKey, IReadOnlyCollection<ContentUrlHistory> histories)> GetAllMoved()
        {
            var sqlCommand = $@"SELECT h.Id, h.ContentKey, h.Urls, h.CreatedUtc, h.md5_ContentKey
                                FROM {ContentUrlHistoryTable} h
                                INNER JOIN 
                                    (SELECT ContentKey, md5_ContentKey
                                    FROM {ContentUrlHistoryTable}
                                    GROUP BY ContentKey, md5_ContentKey
                                    HAVING COUNT(*) > 1) k
                                ON h.ContentKey = k.ContentKey AND h.md5_ContentKey = k.md5_ContentKey
                                ORDER BY h.ContentKey, h.CreatedUtc DESC";

            var dataTable = _dataExecutor.ExecuteQuery(sqlCommand);

            var histories = ToContentUrlHistory(dataTable);

            return histories.GroupBy(x => x.ContentKey).Select(x => (x.Key, (IReadOnlyCollection<ContentUrlHistory>)x.ToList()));
        }

        public IReadOnlyCollection<ContentUrlHistory> GetMoved(string contentKey)
        {
            var contentKeyHash = CalculateMd5Hash(contentKey);
            
            var sqlCommand = $@"SELECT h.Id, h.ContentKey, h.Urls, h.CreatedUtc, h.md5_ContentKey
                                FROM {ContentUrlHistoryTable} h
                                INNER JOIN 
                                    (SELECT ContentKey
                                    FROM {ContentUrlHistoryTable}
                                    WHERE md5_ContentKey = @contentKeyHash
                                    GROUP BY ContentKey
                                    HAVING COUNT(*) > 1) k
                                ON h.ContentKey = k.ContentKey
                                WHERE h.ContentKey = @contentKey AND h.md5_ContentKey = @contentKeyHash
                                ORDER BY h.ContentKey, h.CreatedUtc DESC";

            var dataTable = _dataExecutor.ExecuteQuery(sqlCommand, 
                                                       _dataExecutor.CreateStringParameter("contentKey", contentKey),
                                                       _dataExecutor.CreateBinaryParameter("contentKeyHash", contentKeyHash)
                                                       );

            var histories = ToContentUrlHistory(dataTable);

            return histories.ToList();
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
                _dataExecutor.CreateStringParameter("urls", ToJson(entity.Urls), -1),
                _dataExecutor.CreateDateTimeParameter("createdUtc", entity.CreatedUtc)
                );
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
                _dataExecutor.CreateStringParameter("urls", ToJson(entity.Urls), -1),
                _dataExecutor.CreateDateTimeParameter("createdUtc", entity.CreatedUtc)
                );
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
