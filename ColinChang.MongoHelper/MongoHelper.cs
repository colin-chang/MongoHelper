using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ColinChang.MongoHelper
{
    public class MongoHelper
    {
        private readonly MongoClient _client;
        private IMongoDatabase _database;

        public string Database
        {
            set => _database = _client.GetDatabase(value);
        }

        public MongoHelper(string connectionString)
        {
            _client = new MongoClient(connectionString);
        }

        public MongoHelper(string connectionString, string database) : this(connectionString)
        {
            _database = _client.GetDatabase(database);
        }

        #region CRUD

        public async Task InsertAsync<T>(string collection, params T[] documents)
        {
            if (string.IsNullOrWhiteSpace(collection) || documents == null || !documents.Any())
                return;

            await _database.GetCollection<T>(collection).InsertManyAsync(documents);
        }

        public async Task InsertAsync(string collection, params string[] jsons)
        {
            if (string.IsNullOrWhiteSpace(collection) || jsons == null || !jsons.Any())
                return;

            await _database
                .GetCollection<BsonDocument>(collection)
                .InsertManyAsync(jsons.Select(BsonDocument.Parse));
        }

        public async Task UpdateAsync<TDocument, TField>(string collection,
            IEnumerable<UpdateCondition<TDocument, TField>> updateConditions,
            Expression<Func<TDocument, bool>> where = null) where TDocument : class
        {
            if (string.IsNullOrWhiteSpace(collection) || updateConditions == null || !updateConditions.Any())
                return;

            var filter = where == null
                ? new BsonDocument()
                : Builders<TDocument>.Filter.Where(where);

            var update = Builders<TDocument>.Update.Combine();
            foreach (var condition in updateConditions)
                update = update.Set(condition.Property, condition.NewValue);

            await _database.GetCollection<TDocument>(collection).UpdateManyAsync(filter, update);
        }

        public async Task DeleteAsync<T>(string collection, Expression<Func<T, bool>> where = null)
        {
            if (string.IsNullOrWhiteSpace(collection))
                return;

            var filter = where == null
                ? new BsonDocument()
                : Builders<T>.Filter.Where(where);

            await _database.GetCollection<T>(collection).DeleteManyAsync(filter);
        }

        public async Task<long> QueryCountAsync<T>(string collection, Expression<Func<T, bool>> where = null)
        {
            if (string.IsNullOrWhiteSpace(collection))
                return -1;

            var filter = where == null
                ? new BsonDocument()
                : Builders<T>.Filter.Where(where);

            return await _database.GetCollection<T>(collection).CountDocumentsAsync(filter);
        }

        public async Task<IEnumerable<T>> QueryAsync<T>(string collection, Expression<Func<T, bool>> where = null,
            int skip = -1, int limit = -1, IEnumerable<SortCondition<T>> sortConditions = null) where T : class
        {
            if (string.IsNullOrWhiteSpace(collection))
                return null;

            var (filter, options) = BuildCondition(where, skip, limit, sortConditions);
            using (var cursor = await _database.GetCollection<T>(collection).FindAsync(filter, options))
                return await cursor.ToListAsync();
        }

        public async Task<IAsyncCursor<T>> QueryBigDataAsync<T>(string collection,
            Expression<Func<T, bool>> where = null,
            int skip = -1, int limit = -1, IEnumerable<SortCondition<T>> sortConditions = null) where T : class
        {
            if (string.IsNullOrWhiteSpace(collection))
                return null;

            var (filter, options) = BuildCondition(where, skip, limit, sortConditions);
            return await _database.GetCollection<T>(collection).FindAsync(filter, options);
        }

        private static (FilterDefinition<T> filter, FindOptions<T, T> options) BuildCondition<T>(
            Expression<Func<T, bool>> where,
            int skip, int limit, IEnumerable<SortCondition<T>> sortConditions) where T : class
        {
            //where筛选
            var filter = where == null
                ? new BsonDocument()
                : Builders<T>.Filter.Where(where);

            //分页
            var opt = new FindOptions<T, T>();
            if (skip > 0) opt.Skip = skip;
            if (limit > 0) opt.Limit = limit;

            //排序
            if (sortConditions != null && sortConditions.Any())
            {
                var sort = Builders<T>.Sort.Combine();
                foreach (var sc in sortConditions)
                {
                    sort = sc.SortDirection == SortDirection.Ascending
                        ? sort.Ascending(sc.Property)
                        : sort.Descending(sc.Property);
                }

                opt.Sort = sort;
            }

            return (filter, opt);
        }

        #endregion

        #region DML

        public async Task<IEnumerable<string>> GetIndexKeysAsync(string collection)
        {
            using (var cursor = await _database.GetCollection<BsonDocument>(collection).Indexes.ListAsync())
            {
                var indexes = await cursor.ToListAsync();

                var keys = indexes.Select(index =>
                {
                    var iks = (index
                                .Elements
                                .FirstOrDefault(e => e.Name == "key")
                                .Value
                            as BsonDocument)
                        ?.Names
                        .ToList();
                    iks?.Sort();
                    return iks == null ? null : string.Join(",", iks);
                });

                return keys;
            }
        }

        public async Task DropCollectionAsync(string collection)
        {
            if (string.IsNullOrWhiteSpace(collection))
                return;

            await _database.DropCollectionAsync(collection);
        }

        public async Task DropDatabaseAsync(string database)
        {
            await _client.DropDatabaseAsync(database);
        }

        /// <summary>
        /// 创建单个索引，如果多个条件则创建一个复合索引
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="indexs"></param>
        /// <returns></returns>
        public async Task CreateOneIndexAsync(string collection, Dictionary<string, SortDirection> indexs)
        {
            var keys = Builders<BsonDocument>.IndexKeys.Combine();

            foreach (var col in indexs.Keys)
            {
                keys = indexs[col] == SortDirection.Ascending
                    ? keys.Ascending(col)
                    : keys.Descending(col);
            }

            var indexModel = new CreateIndexModel<BsonDocument>(keys);
            await _database.GetCollection<BsonDocument>(collection).Indexes.CreateOneAsync(indexModel);
        }

        /// <summary>
        /// 创建多个索引，如果多个条件则创建多个普通索引
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="indexs"></param>
        /// <returns></returns>
        public async Task CreateManyIndexAsync(string collection, Dictionary<string, SortDirection> indexs)
        {
            var indexModels = new List<CreateIndexModel<BsonDocument>>();

            foreach (var col in indexs.Keys)
            {
                var keys = Builders<BsonDocument>.IndexKeys.Combine();
                keys = indexs[col] == SortDirection.Ascending
                    ? keys.Ascending(col)
                    : keys.Descending(col);

                indexModels.Add(new CreateIndexModel<BsonDocument>(keys));
            }

            await _database.GetCollection<BsonDocument>(collection).Indexes.CreateManyAsync(indexModels);
        }

        #endregion
    }


    public class UpdateCondition<TDocument, TField> where TDocument : class
    {
        public Expression<Func<TDocument, TField>> Property { get; set; }
        public TField NewValue { get; set; }

        public UpdateCondition(Expression<Func<TDocument, TField>> property, TField newValue)
        {
            Property = property;
            NewValue = newValue;
        }
    }

    public class SortCondition<T> where T : class
    {
        public Expression<Func<T, object>> Property { get; set; }
        public SortDirection SortDirection { get; set; }

        public SortCondition(Expression<Func<T, object>> property, SortDirection sortType = SortDirection.Ascending)
        {
            Property = property;
            SortDirection = sortType;
        }
    }

    public enum SortDirection
    {
        Ascending = 1,
        Descending = -1
    }
}
