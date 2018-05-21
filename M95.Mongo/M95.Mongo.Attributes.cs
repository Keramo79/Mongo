using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text;

namespace M95.Mongo
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
    public class AllowCopyAttribute : Attribute
    {
        public bool AllowCopy;
        public AllowCopyAttribute(bool allow = true)
        {
            this.AllowCopy = allow;
        }
    }
}
