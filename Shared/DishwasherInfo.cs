using System;
using SQLite.Net.Attributes;

namespace Shared
{
    public class DishwasherInfo
    {
        [PrimaryKey]
        public int? Id { get; set; }

        public DateTime CleanDateTime { get; set; }
        public DateTime DirtyDateTime { get; set; }
        public DateTime CurrentRunStart { get; set; }

        public DishwasherStatus CurrentStatus { get; set; }
        public RunCycle CurrentCycle { get; set; }
    }
}