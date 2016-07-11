/*
   Copyright (C) 2016 MadeInTheUSB LLC

   The MIT License (MIT)

        Permission is hereby granted, free of charge, to any person obtaining a copy
        of this software and associated documentation files (the "Software"), to deal
        in the Software without restriction, including without limitation the rights
        to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
        copies of the Software, and to permit persons to whom the Software is
        furnished to do so, subject to the following conditions:

        The above copyright notice and this permission notice shall be included in
        all copies or substantial portions of the Software.

        THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
        IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
        FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
        AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
        LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
        OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
        THE SOFTWARE.
 
    Written by FT for MadeInTheUSB
    MIT license, all text above must be included in any redistribution
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Newtonsoft.Json.Linq;
using MadeInTheUSB;
using MadeInTheUSB.GPIO;

namespace MadeInTheUSB.NusbioDevice.WebClient.Controllers
{
    public class GpioController : NusbioController
    {
        
    }

    public class NusbioController : ApiController
    {
        private static MadeInTheUSB.Nusbio _nusbio;
        public static bool InitNusbioDevice()
        {
            MadeInTheUSB.Devices.Initialize();
            var serialNumber = Nusbio.Detect();
            if(serialNumber == null)
                return false;

            _nusbio = new Nusbio(serialNumber);
            return true;
        }

        private const string NUSBIO_DEVICE_NOT_DETECTED = "Nusbio device not detected";

        private static bool NusbioDeviceAvailable()
        {
            return _nusbio != null;
        }

        [HttpGet]
        /// <summary>
        /// http://localhost:55977/api/Nusbio
        /// http://localhost:55977/api/nusbio/state
        /// http://localhost:55977/api/nusbio/gpio0/high
        /// http://localhost:55977/api/gpio/gpio0/high
        /// </summary>
        /// <returns></returns>
        public string Get(string p1)
        {
            return Get(p1, null);
        }
        public string Get(string p1, string p2)
        {
            if(p1 != null) p1 = p1.ToLowerInvariant();
            if(p2 != null) p2 = p2.ToLowerInvariant();

            if (!NusbioDeviceAvailable())
                return NUSBIO_DEVICE_NOT_DETECTED; 

            var uri = base.Request.RequestUri.AbsoluteUri;

            if(p1 == "state" || p1 == "stateh")
                return nusbioState(uri, p1);
            if(p1.StartsWith("gpio") || p1.StartsWith("all") || (p1.Length>0 && char.IsDigit(p1[0])))
                return gpioAction(uri, p1, p2);

            return "undefined;";
        }

        //private string GetUrlLastToken2(string u)
        //{
        //    return GetUrlLastToken(GetUrlLastToken(u));
        //}

        //private string GetUrlLastToken(string u)
        //{
        //    var tokens = u.Split('/').ToList();
        //    if (tokens.Count > 0)
        //    {
        //        if (string.IsNullOrEmpty(tokens[tokens.Count - 1]))
        //        {
        //            return tokens[tokens.Count - 2].ToLowerInvariant();
        //        }
        //        else
        //        {
        //            return tokens[tokens.Count - 1].ToLowerInvariant();
        //        }
        //    }
        //    return u;
        //}

        void TraceUrl(string url)
        {
            Debug.WriteLine(url);
        }


        private bool? GpioState(string action)
        {
            if ((action == "high")||(action == "true")||(action == "1"))
                return true;
            if ((action == "low")||(action == "false")||(action == "0"))
                return false;
            return null;
        }

        string gpioAction(string url, string gpioName, string action)
        {
            var script      = new NusbioScript();
            var itemType    = NusbioScriptItemType.GetGpioState;

            if (GpioState(action).HasValue && GpioState(action).Value == true)
                itemType = NusbioScriptItemType.SetGpioOn;
            else if (GpioState(action).HasValue && GpioState(action).Value == false)
                itemType = NusbioScriptItemType.SetGpioOff;
            else if (action == "reverse")
                itemType = NusbioScriptItemType.ReverseGpio;
            else 
                itemType = NusbioScriptItemType.GetGpioState;

            if (gpioName == "all")
            {                        
                if ((action == "low")||(action == "false"))
                    itemType = NusbioScriptItemType.AllGpioOff;
                if ((action == "high")||(action == "true"))
                    itemType = NusbioScriptItemType.AllGpioOn;
            }

            script.Add(itemType, gpioName);
            var result = _nusbio.NusbioScript.ExecuteScript(script);
            return PrepareResponse(url, result);
        }
        string nusbioState(string url, string action) 
        {
            var script = new NusbioScript();
            var traceRequest = true;

            action = action.ToLowerInvariant();

            var itemType = NusbioScriptItemType.Invalid;

            if (action == "state")
                itemType = NusbioScriptItemType.GetNusbioState;
            else if (action == "stateh")
            {
                itemType = NusbioScriptItemType.GetNusbioState;
                traceRequest = false;
            }
            if(traceRequest)
                this.TraceUrl(url);
            script.Add(itemType, null);
            var r = _nusbio.NusbioScript.ExecuteScript(script);
            return PrepareResponse(url, r);
        }

        private string PrepareResponse(string url, List<ExecuteScriptItemResult> executionScriptItemResult)
            {
                foreach (var e in executionScriptItemResult)
                {
                    e.Url = url;
                }
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(executionScriptItemResult, Newtonsoft.Json.Formatting.Indented);
                return json;
            }

    }
}


