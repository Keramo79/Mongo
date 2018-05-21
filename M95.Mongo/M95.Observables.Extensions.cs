using M95.Observables;
using MongoDB.Driver;
using System;
using System.Linq;
using M95.Mongo;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using System.Reflection;

namespace M95.Mongo.ObservableExtensions
{
    public static class MongoExtensions
    {
        public static void SetContext<T>(this ObservableList<T> ts, IMongoDatabase database, string collection)
        {
            var mc = new Context.ConnectionData { Database = database, Collection = collection };
            if (ts.Properties.ContainsKey("MongoContext")) { ts.Properties.Remove("MongoContext"); }
            ts.Properties.Add("MongoContext", mc);
        }
        public static Context.ConnectionData GetContext<T>(this ObservableList<T> ts)
        {
            return (Context.ConnectionData)ts.Properties["MongoContext"];
        }
        public static void Load<T>(this ObservableList<T> ts)
        {
            LoadTask(ts, GetContext(ts).Database, GetContext(ts).Collection).RunSynchronously();
        }
        public static void Load<T>(this ObservableList<T> ts, IMongoDatabase database, string collection, Context context = null)
        {
            LoadTask(ts, database, collection, context).RunSynchronously();
        }
        private static void Ts_ItemAdded<T>(ObservableList<T> ol, ObservableListEventArgs<T> e)
        {
          //  ol.Save(e.Item);
        }
        private static Task LoadTask<T>(this ObservableList<T> ts, IMongoDatabase database, string collection, M95.Mongo.Context context = null)
        {
            if (ts == null) { ts = new ObservableList<T>(); }
            SetContext(ts, database, collection);
            return new Task(() =>
            {
                ts.ItemChanged += ((s, e) =>
                {
                    if (!s.Loading)
                    {
                        var pi = e.Item.GetType().GetProperty("updatedAt");
                        if (pi != null) { pi.SetValue(e.Item, DateTime.UtcNow); }
                        if (!ts.Changing)
                        {
                            ts.Changing = true;
                            Task.Delay(1000).ContinueWith((t) =>
                            {
                             //   s.Save(e.Item);
                                ts.Changing = false;
                            });
                        }
                    }
                });
                ts.ItemAdded += ((s, e) =>
                {
                    if (!s.Loading)
                    {
                        var pi = e.Item.GetType().GetProperty("createdAt");
                        if (pi != null) { pi.SetValue(e.Item, DateTime.UtcNow); }
                        pi = e.Item.GetType().GetProperty("updatedAt");
                        if (pi != null) { pi.SetValue(e.Item, DateTime.UtcNow); }
                       // s.Save(e.Item);
                    }
                });
                ts.Loading = true;
                ts.AddRange(database.GetCollection<T>(collection).AsQueryable().ToList());
                ts.Loading = false;
                if (context != null)
                {
                    if (!context.Properties.ContainsKey(collection)) { context.Properties.Add(collection, ts); }
                }
                ts.OnItemsLoaded();
            });
        }
        public static void LoadAsync<T>(this ObservableList<T> ts, IMongoDatabase database, string collection, Context context = null)
        {
            LoadTask(ts, database, collection, context).Start();
        }
        public static void Save<T>(this ObservableList<T> ts, T item)
        {
            var id = GetId(ts, item);
            var tl = GetById(ts, id);
            if (tl == null)
            {
                GetContext(ts).Database.GetCollection<T>(GetContext(ts).Collection).InsertOne(item);
            }
            else
            {
                GetContext(ts).Database.GetCollection<T>(GetContext(ts).Collection).ReplaceOne(Builders<T>.Filter.Eq("_id", GetId(ts, item)), item);
            }
        }
        public static object GetId<T>(this ObservableList<T> ts, T item)
        {
            object oid = item.GetType().GetProperty("_id").GetValue(item);
            if (oid == null) { throw new Exception("Item does not contain _id property."); }
            return oid;
        }
        public static object DefaultIdValue<T>(this ObservableList<T> ts, T item)
        {
            string defaultvalue;
            if (item.GetType() == typeof(ObjectId))
            {
                defaultvalue = new ObjectId().ToString();
            }
            else
            {
                defaultvalue = item.GetType().GetProperty("_id").GetValue(item).ToString();
            }
            return defaultvalue;
        }
        public static T GetById<T>(this ObservableList<T> ts, object id)
        {
            return ts.Where(c => GetId(ts, c).ToString() == id.ToString()).FirstOrDefault();
        }

        public static void CopyTo<T>(this ObservableObject o, T dest)
        {
            foreach (PropertyInfo pi in o.GetType().GetProperties())
            {
                if(pi.GetCustomAttribute(typeof(AllowCopyAttribute))!=null)
                {
                    var a = (AllowCopyAttribute)pi.GetCustomAttribute(typeof(AllowCopyAttribute));
                    if (a.AllowCopy)
                    {
                        if (pi.GetValue(o) != dest.GetType().GetProperty(pi.Name).GetValue(dest))
                        {
                            dest.GetType().GetProperty(pi.Name).SetValue(dest, pi.GetValue(o));
                        }
                    }
                }
            }
        }
    }
}
