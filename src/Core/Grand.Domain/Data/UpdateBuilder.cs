using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Grand.Domain.Data
{
    public class UpdateBuilder<T>
    {
        private readonly List<UpdateDefinition<T>> _list = new();

        protected UpdateBuilder() { }

        public static UpdateBuilder<T> Create()
        {
            return new();
        }

        public UpdateBuilder<T> Set<TProperty>(Expression<Func<T, TProperty>> selector, TProperty value)
        {
            _list.Add(Builders<T>.Update.Set(selector, value));
            return this;
        }

        public IEnumerable<UpdateDefinition<T>> Fields {
            get { return _list; }
        }
    }

}
