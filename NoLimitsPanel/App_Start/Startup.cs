using Owin;
using Microsoft.Owin;
using Microsoft.AspNet.SignalR;

[assembly: OwinStartup(typeof(NoLimitsPanel.Startup))]

namespace NoLimitsPanel
{
	public class Startup
	{
		public void Configuration(IAppBuilder app)
		{
			// SignalR config
			var hubConfig = new HubConfiguration();
			hubConfig.EnableDetailedErrors = true;
			app.MapSignalR(hubConfig);
		}
	}
}