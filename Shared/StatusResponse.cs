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
        public class StatusDetails
        {
            
        }

        [DataContract]
        public class RunningStatusDetails : StatusDetails
        {
            [DataMember]
            public DateTime StartTime { get; set; }
            [DataMember]
            public DateTime EstimatedEndTime { get; set; }
            [DataMember]
            public RunCycle RunCycle { get; set; }    
        }

        [DataContract]
        public class DirtyStatusDetails : StatusDetails
        {
            [DataMember]
            public DateTime DirtyTime { get; set; }
        }

        [DataContract]
        public class CleanStatusDetails : StatusDetails
        {
            [DataMember]
            public DishwasherRun DishwasherRun { get; set; }
        }
    }
}