using System;
using System.Runtime.Serialization;

namespace Shared
{
    [DataContract]
    public class StatusResponse
    {
        [DataMember]
        public string Status { get; set; }

        [DataMember]
        public StatusDetails Details { get; set; }

        [KnownType(typeof(RunningStatusDetails))]
        [KnownType(typeof(DirtyStatusDetails))]
        [KnownType(typeof(CleanStatusDetails))]
        [KnownType(typeof(UnknownStatusDetails))]
        public abstract class StatusDetails
        {
            
        }

        public class UnknownStatusDetails : StatusDetails
        {
            
        }
        public class RunningStatusDetails : StatusDetails
        {
            [DataMember]
            public DateTime StartTime { get; set; }
            [DataMember]
            public DateTime EstimatedEndTime { get; set; }
            [DataMember]
            public RunCycle RunCycle { get; set; }    
        }

        public class DirtyStatusDetails : StatusDetails
        {
            [DataMember]
            public DateTime DirtyTime { get; set; }
        }

        public class CleanStatusDetails : StatusDetails
        {
            [DataMember]
            public DishwasherRun DishwasherRun { get; set; }
        }
    }
}