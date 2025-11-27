using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace myFlatLightLogin.DalSQLite
{
    public class DbCore
    {
        private static string dbFile = Path.Combine(Environment.CurrentDirectory, "security.db3");

        public static bool Insert<T>(T item)
        {
            bool result = false;

            using (SQLiteConnection conn = new SQLiteConnection(dbFile))
            {
                conn.CreateTable<T>();

                int rows = conn.Insert(item);
                if (rows > 0)
                {
                    result = true;
                }
            }

            return result;
        }

        public static bool Update<T>(T item)
        {
            bool result = false;

            using (SQLiteConnection conn = new SQLiteConnection(dbFile))
            {
                conn.CreateTable<T>();

                int rows = conn.Update(item);
                if (rows > 0)
                {
                    result = true;
                }
            }

            return result;
        }

        public static bool Delete<T>(T item)
        {
            bool result = false;

            using (SQLiteConnection conn = new SQLiteConnection(dbFile))
            {
                conn.CreateTable<T>();

                int rows = conn.Delete(item);
                if (rows > 0)
                {
                    result = true;
                }
            }

            return result;
        }

        public static List<T> Fetch<T>() where T : new()
        {
            List<T> items;

            using (SQLiteConnection conn = new SQLiteConnection(dbFile))
            {
                conn.CreateTable<T>();
                items = conn.Table<T>().ToList();
            }

            return items;
        }

        //public static T Fetch<T>(T data) where T : new()
        //{
        //    T item;

        //    using (SQLiteConnection conn = new SQLiteConnection(dbFile))
        //    {
        //        conn.CreateTable<T>();
        //        item = conn.Table<T>().Where(i => i == data).FirstOrDefault();
        //    }

        //    return item;
        //}
    }
}
