using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Text;

namespace M95.Mongo
{
    public interface IBaseObject<T>
    {
        T _id { get; set; }
    }
}
