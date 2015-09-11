using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Status.Startup))]
namespace Status
{
    public partial class Startup {
        public void Configuration(IAppBuilder app) {
            ConfigureAuth(app);
        }
    }
}
