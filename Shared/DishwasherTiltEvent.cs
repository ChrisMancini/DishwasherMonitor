using System;
using SQLite.Net.Attributes;

namespace Shared
{
    public class DishwasherTiltEvent
    {
        [PrimaryKey]
        public int? Id { get; set; }

        public DateTime TiltTime { get; set; }
        public bool IsOpened { get; set; }

        public DishwasherTiltEvent()
        {
            TiltTime = DateTime.MinValue;
        }
    }
}