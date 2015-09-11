using System.Net.Http;
using System.Web.Http;

namespace EchoService.Controllers
{
    public class AlexaController : ApiController
    {
        [Route("alexa/dishwasher")]
        [HttpPost]
        public HttpResponseMessage SampleSession()
        {
            var speechlet = new DishwasherSpeechlet();
            return speechlet.GetResponse(Request);
        }

    }
}
