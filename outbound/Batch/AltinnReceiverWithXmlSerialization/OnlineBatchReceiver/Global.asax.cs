using System;
using System.IO;
using System.Reflection;
using log4net;

namespace OnlineBatchReceiver
{
    public class Global : System.Web.HttpApplication
    {

        protected void Application_Start(object sender, EventArgs e)
        {
            var uri = new UriBuilder(Assembly.GetExecutingAssembly().Location).Path;
            GlobalContext.Properties["LogFileName"] = Path.GetDirectoryName(Uri.UnescapeDataString(uri)) + "\\Debug.log"; //log file path
            log4net.Config.XmlConfigurator.Configure();
        }

        protected void Session_Start(object sender, EventArgs e)
        {

        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {

        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {

        }

        protected void Application_Error(object sender, EventArgs e)
        {

        }

        protected void Session_End(object sender, EventArgs e)
        {

        }

        protected void Application_End(object sender, EventArgs e)
        {

        }
    }
}