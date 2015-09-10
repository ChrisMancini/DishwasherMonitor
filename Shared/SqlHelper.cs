using System;
using System.IO;
using System.Linq;
using System.Reflection;
using SQLite.Net;
using SQLite.Net.Interop;
using SQLite.Net.Platform.WinRT;

namespace Shared
{
    public class SqlHelper
    {
        public SqlHelper()
        {
            _path = Path.Combine(Path.GetTempPath(), "dishwasher.db");
            using (
                var conn =
                    new SQLiteConnection(new SQLitePlatformWinRT(), _path, SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite))
            {
                CreateTableIfNeeded<DishwasherRun>(conn);
                CreateTableIfNeeded<DishwasherTiltEvent>(conn);
                conn.Commit();
            }
        }

        readonly string _path;

        public void DishwasherTilt(bool isOpened)
        {
            using (
                var conn =
                    new SQLiteConnection(new SQLitePlatformWinRT(), _path))
            {
                conn.Insert(new DishwasherTiltEvent
                {
                    TiltTime = DateTime.Now,
                    IsOpened = isOpened
                });

                conn.Commit();
            }
        }

        public void AddDishwasherRunStart()
        {
            using (
                var conn =
                    new SQLiteConnection(new SQLitePlatformWinRT(), _path))
            {
                conn.Insert(new DishwasherRun
                {
                    StartDateTime = DateTime.Now
                });

                conn.Commit();
            }
        }

        public void EndDishwasherRun()
        {
            using (
                var conn =
                    new SQLiteConnection(new SQLitePlatformWinRT(), _path))
            {
                var result =
                    conn.Table<DishwasherRun>().Where(x => x.EndDateTime == DateTime.MinValue).OrderByDescending(x => x.StartDateTime).First();

                if (result != null)
                {
                    result.EndDateTime = DateTime.Now;
                    conn.Update(result);
                    conn.Commit();
                }
            }
        }

        private static void CreateTableIfNeeded<T>(SQLiteConnection conn) where T :class
        {
            if (!conn.TableMappings.Any(table => typeof (T).IsAssignableFrom(table.MappedType)))
            {
                conn.CreateTable<T>(CreateFlags.AutoIncPK);
            }
        }
    }
}
