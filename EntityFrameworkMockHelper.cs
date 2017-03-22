using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.AspNet.Identity.EntityFramework;
using Moq;

namespace Helpers
{
    public static class EntityFrameworkMockHelper
    {
        /// <summary>
        /// Returns a mock of a DbContext
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static MockedDbContext<T> GetMockContext<T>() where T : DbContext
        {
            var instance = new MockedDbContext<T>();
            instance.MockTables();
            return instance;
        }

        public static MockedIdentityDbContext<T, TU> GetIdentityMockContext<T, TU>() where T : IdentityDbContext<TU> where TU : IdentityUser
        {
            var instance = new MockedIdentityDbContext<T, TU>();
            instance.MockTables();
            return instance;
        }

        /// <summary>
        /// Use this method to mock a table, which is a DbSet{T} oject, in Entity Framework.
        /// Leave the second list null if no adds or deletes are used.
        /// </summary>
        /// <typeparam name="T">The table data type</typeparam>
        /// <param name="table">A List{T} that is being use to replace a database table.</param>
        /// <returns></returns>
        public static DbSet<T> MockDbSet<T>(List<T> table) where T : class
        {
            var dbSet = new Mock<DbSet<T>>();
            dbSet.As<IQueryable<T>>().Setup(q => q.Provider).Returns(new TestDbAsyncQueryProvider<T>(table.AsQueryable().Provider)); 
            dbSet.As<IQueryable<T>>().Setup(q => q.Expression).Returns(() => table.AsQueryable().Expression);
            dbSet.As<IQueryable<T>>().Setup(q => q.ElementType).Returns(() => table.AsQueryable().ElementType);
            dbSet.As<IQueryable<T>>().Setup(q => q.GetEnumerator()).Returns(() => table.AsQueryable().GetEnumerator());

            dbSet.As<IDbAsyncEnumerable<T>>().Setup(q => q.GetAsyncEnumerator()).Returns(new TestDbAsyncEnumerator<T>(table.GetEnumerator()));

            dbSet.Setup(q => q.AsNoTracking()).Returns(dbSet.Object);
            dbSet.Setup(set => set.Include(It.IsAny<string>())).Returns(dbSet.Object); // support .Include() extension
            dbSet.Setup(set => set.Add(It.IsAny<T>())).Callback<T>(table.Add);
            dbSet.Setup(set => set.AddRange(It.IsAny<IEnumerable<T>>())).Callback<IEnumerable<T>>(table.AddRange);
            dbSet.Setup(set => set.Remove(It.IsAny<T>())).Callback<T>(t => table.Remove(t));
            dbSet.Setup(set => set.RemoveRange(It.IsAny<IEnumerable<T>>())).Callback<IEnumerable<T>>(ts =>
            {
                foreach (var t in ts.ToList()) { table.Remove(t); }
            });
          
            dbSet.Setup(m => m.Find(It.IsAny<object[]>())).Returns<object[]>(ids => MockFind<T>((int)ids[0], table));

            return dbSet.Object;
        }

        /// <summary>
        /// This method assumes there is an Id property that acts as a key
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        /// <param name="table"></param>
        /// <returns></returns>
        private static T MockFind<T>(int id, IEnumerable<object> table)
        {
            T result = default(T);
            foreach (var e in table)
            {
                Type type = e.GetType();
                var idProperty = type.GetProperty("Id");
                var value = (int)idProperty.GetValue(e);
                if (value == id)
                {
                    result = (T) e;
                    break;
                }
            }
            return result;
        }

        /// <summary>
        /// Mocks all the DbSet{T} properties that represent tables in a DbContext.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="mockedContext"></param>
        public static void MockTables<T>(this MockedDbContext<T> mockedContext) where T : DbContext
        {
            Type contextType = typeof(T);
            var dbSetProperties = contextType.GetProperties().Where(prop => (prop.PropertyType.IsGenericType) && prop.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>));
            foreach (var prop in dbSetProperties)
            {
                var dbSetGenericType = prop.PropertyType.GetGenericArguments()[0];
                Type listType = typeof(List<>).MakeGenericType(dbSetGenericType);
                var listForFakeTable = Activator.CreateInstance(listType);
                var parameter = Expression.Parameter(contextType);
                var body = Expression.PropertyOrField(parameter, prop.Name);
                var lambdaExpression = Expression.Lambda<Func<T, object>>(body, parameter);
                var method = typeof(EntityFrameworkMockHelper).GetMethod("MockDbSet").MakeGenericMethod(dbSetGenericType);
                mockedContext.Setup(lambdaExpression).Returns(method.Invoke(null, new[] { listForFakeTable }));
                mockedContext.Tables.Add(prop.Name, listForFakeTable);
            }
        }

        public static void MockTables<T, TU>(this MockedIdentityDbContext<T, TU> mockedContext) where T : IdentityDbContext<TU> where TU : IdentityUser
        {
            Type contextType = typeof(T);
            var dbSetProperties = contextType.GetProperties().Where(prop => (prop.PropertyType.IsGenericType) && (prop.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>) || prop.PropertyType.GetGenericTypeDefinition() == typeof(IDbSet<>)));
            foreach (var prop in dbSetProperties)
            {
                var dbSetGenericType = prop.PropertyType.GetGenericArguments()[0];
                Type listType = typeof(List<>).MakeGenericType(dbSetGenericType);
                var listForFakeTable = Activator.CreateInstance(listType);
                var parameter = Expression.Parameter(contextType);
                var body = Expression.PropertyOrField(parameter, prop.Name);
                var lambdaExpression = Expression.Lambda<Func<T, object>>(body, parameter);
                var method = typeof(EntityFrameworkMockHelper).GetMethod("MockDbSet").MakeGenericMethod(dbSetGenericType);
                mockedContext.Setup(lambdaExpression).Returns(method.Invoke(null, new[] { listForFakeTable }));
                mockedContext.Tables.Add(prop.Name, listForFakeTable);
            }
        }
    }
}