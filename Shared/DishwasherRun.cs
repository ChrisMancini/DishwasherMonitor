using System;
using SQLite.Net.Attributes;

namespace Shared
{
    public class DishwasherRun
    {
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        [PrimaryKey]
        public int? Id { get; set; }

        public DishwasherRun()
        {
            StartDateTime = DateTime.MinValue;
            EndDateTime = DateTime.MinValue;
        }
    }

    public class DishwasherInfo
    {
        
    }
}