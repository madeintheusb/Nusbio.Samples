using System.Diagnostics;
using System.IO;
using System.Reflection;
using MadeInTheUSB;
using Nancy;
using Nancy.Hosting.Self;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DynamicSugar;

namespace MadeInTheUSB.Web
{
    public class WebApiModule : NancyModule
    {
        /// <summary>
        /// http://localhost:1964/
        /// </summary>
        public class MainModule : NancyModule
        {
            private const string ERR_001_MESSAGE = "WRC-ERR001:Internal error grabbing a screenshot";

            private static Response CreateNewNoContentReponse()
            {
                var response = new Response
                {
                    ContentType = "text/html",
                    StatusCode  =  HttpStatusCode.NoContent,
                    Contents    = (stream) =>
                    {
                        
                    }
                };
                return response;
            }

            private static Response CreateNewHttpErrorReponse(Nancy.HttpStatusCode statusCode, string errorMessage)
            {
                var response = new Response
                {
                    ContentType = "text/html",
                    StatusCode  = statusCode,
                    Contents    = (stream) =>
                    {
                        var buffer = Encoding.UTF8.GetBytes(errorMessage);
                        stream.Write(buffer, 0, buffer.Length);
                    }
                };
                return response;
            }

            private static Response CreateNewHttpImageReponse(byte[] buffer, string contentType = "image/jpeg")
            {
                var response = new Response()
                {
                    ContentType = contentType,
                    StatusCode  = Nancy.HttpStatusCode.OK,
                    Contents    = stream =>
                    {
                        using (var writer = new BinaryWriter(stream))
                        {
                            writer.Write(buffer);
                        }
                    }
                };
                return response;
            }

            public static string Base64Encode(string plainText) 
            {
                var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
                return System.Convert.ToBase64String(plainTextBytes);
            }

            public static string GetTempFolder()
            {
                var f = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), "WinRemoteControl.Server.Console");
                if (!Directory.Exists(f))
                    Directory.CreateDirectory(f);
                return f;
            }
            
            public void Initialize()
            {
            }

            private void Trace(string m)
            {
                Console.WriteLine(m);
            }

            static string _WebSiteHtml = null;

            private string GetHtmlTemplate()
            {
                //var fileName = @"D:\DVT\MadeInTheUSB\MadeInTheUSB.Nusbio.Lib\Nusbio\WebSite\lDeviceHttpHelp.html";
                //return System.IO.File.ReadAllText(fileName);
                var html = DS.Resources.GetTextResource("Nusbio.WebSite.html", typeof (MadeInTheUSB.Nusbio).Assembly);   
                return html;
            }
            /// <summary>
            /// http://localhost:1964
            /// </summary>
            /// <returns></returns>
            private string GetWebSiteHtml()
            {
                return "<html><h1>Hello MadeInTheUSB</h1></html>";
                //if (_WebSiteHtml != null) return _WebSiteHtml;
                
                var html = GetHtmlTemplate();

                var files  = DS.List(
                                        "bootswatch.min.css", 
                                        "bootstrap.slate.css", 
                                        "jquery-1.10.2.min.js", 
                                        "bootstrap.min.js", 
                                        "bootswatch.js", 
                                        "String.js", 
                                        "Sys.js",
                                        "Nusbio.js"
                                );

                foreach (var file in files)
                {
                    var src = DS.Resources.GetTextResource(file, typeof (MadeInTheUSB.Nusbio).Assembly);
                    html    = html.Replace("[{0}]".FormatString(file), src);
                }
                _WebSiteHtml = html;
                return _WebSiteHtml;
            }

            private void TraceUrl(Uri uri)
            {
                WebServer.Trace(uri.PathAndQuery);
            }

            /*
                http://localhost:1964/nusbio/state

                http://localhost:1964/gpio/0/high
                http://localhost:1964/gpio/0/low
                http://localhost:1964/gpio/0/state

                http://localhost:1964/gpio/0,2,4,6/high
                http://localhost:1964/gpio/1,3,5,7/high
                http://localhost:1964/gpio/0,1,2,3,4,5,6,7/low
                http://localhost:1964/gpio/0/state

                http://localhost:1964/gpio/all/state
                http://localhost:1964/gpio/all/low
                http://localhost:1964/gpio/all/high

                http://localhost:1964/gpio/0/blink/10/0
                http://localhost:1964/gpio/1/blink/100/0
                http://localhost:1964/gpio/2/blink/200/0
                http://localhost:1964/gpio/3/blink/300/0
                http://localhost:1964/gpio/4/blink/400/0
                http://localhost:1964/gpio/5/blink/500/0
                http://localhost:1964/gpio/6/blink/600/0
                http://localhost:1964/gpio/7/blink/700/0

            */

            public MainModule()
            {
                this.Initialize();

                Get["/"] = parameters =>
                {
                    this.TraceUrl(Request.Url);
                    return GetWebSiteHtml();
                };

              
                Get["/nusbio/{action}"] = parameters =>
                {
                    return PrepareResponse(Request, null);
                };

                Get["/gpio/{gpioName}/blink/{rate}/{doubleRate}"] = parameters =>
                {
                    this.TraceUrl(Request.Url);

                    return PrepareResponse(Request, null);
                };

                
                Get["/gpio/{gpioName}/{action}"] = parameters =>
                {
                    this.TraceUrl(Request.Url);

                    return PrepareResponse(Request, null);
                };
            }

            private string PrepareResponse(Request request, object rep)
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(rep, Newtonsoft.Json.Formatting.Indented);
                return json;
            }
        }
    }
}
