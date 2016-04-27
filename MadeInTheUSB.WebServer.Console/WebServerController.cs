using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DynamicSugar;
using DS = DynamicSugar.DS;

namespace MadeInTheUSB.Web
{
    public partial class WebServerController //: IDisposable
    {
        WebServer _webServer;

        public void WebServer_TraceEvent(string message)
        {
            //if (this.UrlEvent != null)
            //    UrlEvent(message);
        }

        /// <summary>
        /// Start internal web server
        /// </summary>
        /// <param name="port"></param>
        public void StartWebServer(int port)
        {
            _webServer = new WebServer(port);
             WebServer.TraceEvent += WebServer_TraceEvent;
            _webServer.Start();
        }

        /// <summary>
        /// return the internal we server url
        /// </summary>
        /// <returns></returns>
        public string GetWebServerUrl()
        {
            return this._webServer.URL;
        }

        /// <summary>
        /// Stop internal web server
        /// </summary>
        public void StopWebServer()
        {
            if (_webServer != null)
            {
                _webServer.Stop();
                _webServer = null;
            }
        }
    }
}
