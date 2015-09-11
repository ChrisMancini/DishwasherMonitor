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
            _path = Path.Combine(Path.GetTempPath(), "dishwasherdb.db");
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

        private void EditDishwasherInfo(DishwasherStatus setStatus, RunCycle? runCycle)
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
                            result.CurrentCycle = runCycle ?? RunCycle.Normal;
                            break;
                    }

                    result.CurrentStatus = setStatus;
                    conn.Update(result);
                    conn.Commit();
                }
                else if(result == null)
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

                conn.Table<DishwasherTiltEvent>().Delete(x => x.TiltTime < DateTime.Now.Subtract(new TimeSpan(7, 0, 0, 0)));

                conn.Commit();
            }
        }

        public void StartDishwasherRun(RunCycle? cycleType)
        {
            EditDishwasherInfo(DishwasherStatus.Running, cycleType);
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
                    EndDateTime = now,
                    CycleType = info.CurrentCycle
                });

                conn.Table<DishwasherRun>().Delete(x => x.StartDateTime < DateTime.Now.Subtract(new TimeSpan(14, 0, 0, 0)));

                conn.Commit();
            }

            EditDishwasherInfo(DishwasherStatus.Clean, null);
        }

        public void DishwasherEmptied()
        {
            EditDishwasherInfo(DishwasherStatus.Dirty, null);
        }

        public DishwasherInfo Get()
        {
            using (
                var conn =
                    new SQLiteConnection(new SQLitePlatformWinRT(), _path))
            {
                return conn.Get<DishwasherInfo>(x => x.Id != null);
            }
        }

        public DishwasherRun GetDishwasherRun()
        {
            using (
                var conn =
                    new SQLiteConnection(new SQLitePlatformWinRT(), _path))
            {
                return conn.Table<DishwasherRun>().OrderByDescending(r => r.EndDateTime).FirstOrDefault();
            }
        }

        private static void CreateTableIfNeeded<T>(SQLiteConnection conn) where T :class
        {
            if (!conn.TableMappings.Any(table => typeof (T).IsAssignableFrom(table.MappedType)))
            {
                conn.CreateTable<T>(CreateFlags.AutoIncPK);
            }
        }

        public void SeedDatabase()
        {
            using (
                var conn =
                    new SQLiteConnection(new SQLitePlatformWinRT(), _path))
            {
                conn.Insert(new DishwasherInfo
                {
                    CurrentRunStart = DateTime.MinValue,
                    CleanDateTime = DateTime.MinValue,
                    DirtyDateTime = DateTime.Now,
                    CurrentStatus = DishwasherStatus.Dirty
                });

                conn.Insert(new DishwasherRun
                {
                    CycleType = RunCycle.Heavy,
                    StartDateTime = DateTime.Now - TimeSpan.FromHours(1),
                    EndDateTime = DateTime.Now
                });

                conn.Commit();
            }
        }
        }
}
