using MongoDB.Driver;
using System;
using System.Linq;
using M95.Mongo.ObservableExtensions;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace M95.Mongo
{
    public class Context
    {
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
        public class ConnectionData
        {
            public IMongoDatabase Database { get; set; }
            public string Collection { get; set; }
        }
        public IMongoDatabase database { get; set; }
        public Context(string connectionString, string database)
        {
            var client = new MongoClient(connectionString);
            this.database = client.GetDatabase(database);
        }

        public Observables.ObservableList<T> GetObservableList<T>(string collection)
        {
            if (!Properties.ContainsKey(collection))
            {
                var t = GetObservableListTask<T>(collection);
                t.Start();
                Properties.Add(collection, t.Result);
            }
            return (Observables.ObservableList<T>)Properties[collection];
        }

        public Task<Observables.ObservableList<T>> GetObservableListTask<T>(string collection)
        {
            var t = new Task<Observables.ObservableList<T>>(() =>
            {
                var a = new M95.Observables.ObservableList<T>();
                a.Load(this.database, collection);
                a.OnItemsLoaded();
                return a;
            });
            return t;
        }
    }
}
