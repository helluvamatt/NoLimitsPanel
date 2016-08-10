using System.Web;
using System.Web.Optimization;

namespace NoLimitsPanel
{
    public class BundleConfig
    {
        // For more information on bundling, visit http://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
			bundles.Add(
				new ScriptBundle("~/assets/js")
					.Include("~/Scripts/jquery-{version}.js")
					.Include("~/Scripts/jquery.signalR-{version}.js")
					.Include("~/Scripts/angular.js")
					.Include("~/Scripts/angular-touch.js")
					.Include("~/Scripts/app/enums.js")
					.Include("~/Scripts/app/directives.js")
					.Include("~/Scripts/app/controllers.js")
				);

            bundles.Add(
				new StyleBundle("~/assets/css")
					.Include("~/Content/reset.css")
					.Include("~/Content/app.css")
				);
        }
    }
}
