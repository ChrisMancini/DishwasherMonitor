using System;
using System.Dynamic;
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
                CreateTableIfNeeded<DishwasherInfo>(conn);
                conn.Commit();
            }
        }

        readonly string _path;

        private void EditDishwasherInfo(DishwasherStatus setStatus)
        {
            using (
                var conn =
                    new SQLiteConnection(new SQLitePlatformWinRT(), _path))
            {
                var result =
                    conn.Get<DishwasherInfo>(x => x.Id != null);

                if (result != null && result.CurrentStatus != setStatus)
                {
                    switch (setStatus)
                    {
                        case DishwasherStatus.Clean:
                            result.CleanDateTime = DateTime.Now;
                            break;
                        case DishwasherStatus.Dirty:
                            result.DirtyDateTime = DateTime.Now;
                            break;
                        case DishwasherStatus.Running:
                            result.CurrentRunStart = DateTime.Now;
                            break;
                    }
                  
                    result.CurrentStatus = setStatus;
                    conn.Update(result);
                    conn.Commit();
                }
                else
                {
                    conn.Insert(new DishwasherInfo
                    {
                        CurrentRunStart = DateTime.MinValue,
                        CleanDateTime = DateTime.MinValue,
                        DirtyDateTime = DateTime.Now,
                        CurrentStatus = DishwasherStatus.Dirty
                    });
                    conn.Commit();
                }
            }
        }

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

        public void StartDishwasherRun()
        {
            EditDishwasherInfo(DishwasherStatus.Running);
        }

        public void EndDishwasherRun()
        {
            using (
                var conn =
                    new SQLiteConnection(new SQLitePlatformWinRT(), _path))
            {
                var now = DateTime.Now;
                var info = conn.Get<DishwasherInfo>(x => x.Id != null);
                if (info == null) return;

                conn.Insert(new DishwasherRun
                {
                    StartDateTime = info.CurrentRunStart,
                    EndDateTime = now
                });

                conn.Commit();
            }

            EditDishwasherInfo(DishwasherStatus.Clean);
        }

        public void DishwasherEmptied()
        {
            EditDishwasherInfo(DishwasherStatus.Dirty);
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
