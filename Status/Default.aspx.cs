using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Humanizer;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Deserializers;
using Shared;

namespace Status
{
    public partial class _Default : Page
    {
        public StatusResponse StatusResponse { get; set; }

        public string DirtyDuration { get; set; }

        public string CleanRunDuration { get; set; }
        public string CleanRunCycleType { get; set; }

        public string RunningCycleType { get; set; }
        public string RunningStartTime { get; set; }
        public string RunningDurationLeft { get; set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            StatusResponse = GetStatusResponse();
            var dets = StatusResponse.Details;

            if (dets is StatusResponse.DirtyStatusDetails)
            {
                DirtyDuration = "I have been dirty for " + (DateTime.Now - ((StatusResponse.DirtyStatusDetails) dets).DirtyTime).Humanize();
            }
            else if(dets is StatusResponse.CleanStatusDetails)
            {
                var cleanStatusDetails = ((StatusResponse.CleanStatusDetails) dets);
                CleanRunDuration = "The last run took " +
                                   (cleanStatusDetails.DishwasherRun.EndDateTime - cleanStatusDetails.DishwasherRun.StartDateTime).Humanize();
                CleanRunCycleType = "The last run was a " + cleanStatusDetails.DishwasherRun.CycleType + " cycle";
            }
            else if (dets is StatusResponse.RunningStatusDetails)
            {
                var runningStatusDetails = ((StatusResponse.RunningStatusDetails)dets);
                RunningStartTime = "The dishwasher was started " +
                                   (DateTime.Now - runningStatusDetails.StartTime).Humanize();
                RunningDurationLeft = "Estimated time remaining " + (runningStatusDetails.EstimatedEndTime - DateTime.Now).Humanize();
                RunningCycleType = "The current run is a " + runningStatusDetails.RunCycle + " cycle";
            }
        }

        private static StatusResponse GetStatusResponse()
        {
            var restClient = new RestClient("http://10.170.142.9/api/status");
            restClient.AddHandler("application/json", new RestSharpJsonNetDeserializer());
            var restResponse = restClient.Get<StatusResponse>(new RestRequest());


            var statusResponse = restResponse.Data;
            return statusResponse;
        }


    }

    public class RestSharpJsonNetDeserializer : IDeserializer
    {
        public T Deserialize<T>(IRestResponse response)
        {
            return JsonConvert.DeserializeObject<T>(
                response.Content,
                new StatusDetailsConverter());
        }

        public string RootElement { get; set; }
        public string Namespace { get; set; }
        public string DateFormat { get; set; }
    }


    public abstract class JsonCreationConverter<T> : JsonConverter
    {
        protected abstract T Create(Type objectType, JObject jsonObject);

        public override bool CanConvert(Type objectType)
        {
            return typeof(T).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType,
          object existingValue, JsonSerializer serializer)
        {
            var jsonObject = JObject.Load(reader);
            var target = Create(objectType, jsonObject);
            serializer.Populate(jsonObject.CreateReader(), target);
            return target;
        }
    }

    public class StatusDetailsConverter : JsonCreationConverter<StatusResponse.StatusDetails>
    {
        protected override StatusResponse.StatusDetails Create(Type objectType, JObject jsonObject)
        {
            var typeName = jsonObject["__type"].ToString();

            switch (typeName)
            {
                case "StatusResponse.CleanStatusDetails:#Server":
                    return new StatusResponse.CleanStatusDetails();
                case "StatusResponse.DirtyStatusDetails:#Server":
                    return new StatusResponse.DirtyStatusDetails();
                case "StatusResponse.RunningStatusDetails:#Server":
                    return new StatusResponse.RunningStatusDetails();
            }

            return new StatusResponse.StatusDetails();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}