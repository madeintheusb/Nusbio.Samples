/*
    Copyright (C) 2015 MadeInTheUSB.net

    Permission is hereby granted, free of charge, to any person obtaining a copy of this software and 
    associated documentation files (the "Software"), to deal in the Software without restriction, 
    including without limitation the rights to use, copy, modify, merge, publish, distribute, 
    sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is 
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all copies or substantial 
    portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT 
    LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. 
    IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
    WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE 
    OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

var
    Gpio0 = 0,
    Gpio1 = 1, 
    Gpio2 = 2, 
    Gpio3 = 3, 
    Gpio4 = 4, 
    Gpio5 = 5, 
    Gpio6 = 6, 
    Gpio7 = 7;

function NusbioClass(host, hostPort, getCallBack) {

    this._traceOn  = false;
    this.GpioNames = ["Gpio0", "Gpio1", "Gpio2", "Gpio3", "Gpio4", "Gpio5", "Gpio6", "Gpio7"];
    var $this      = this;

    this.__init__ = function() {

        if (!getCallBack)
            throw "parameter getCallBack is required";
        this._baseUrl       =  this.__stringFormat("http://{0}:{1}", host, hostPort);
        this.__userCallBack = getCallBack;
    }
    this.__get = function(url, data, successFunction) {
        try {
            this.__trace(this.__stringFormat("GET url:{0}, data:{1}", url, data));
            jQuery.get(url, data, successFunction);
        } catch (ex) {
            this.__trace(this.__stringFormat("Cannot contat Nusbio web server at {0}, error:{1}", url, ex));
            throw ex;
        }
    }
    this.__callNusbioRestApi = function(url, callBack) {
                
        if (typeof(callBack) === 'undefined')
            callBack = this.__getCallBack;

        var url = this._baseUrl + url;
        this.__get(url, "", callBack);
    }
    this.__getCallBack = function(result) {
        try {
            //alert(result);
            var r = JSON.parse(result)[0]; // Result is an array of ExecuteScriptItemResult only get the firstone
            $this.__userCallBack(r);
        } 
        catch (ex) {
            $this.__trace($this.__stringFormat("Error calling callback:{0}", ex));
            throw ex;
        }
    }
    this.__trace = function(m) {

        if(this._traceOn)
            console.log("NusbioWebSite:" + m.toString());
        return m;
    }
    this.digitalRead = function(gpio) {

        this.__callNusbioRestApi(this.__stringFormat("/gpio/{0}/state", gpio));
    }
    this.allGpioOff = function() {

        this.__callNusbioRestApi("/gpio/all/low");
    }
    this.reverseGpio = function(gpio) {

        this.__callNusbioRestApi(this.__stringFormat("/gpio/{0}/reverse", gpio));
    }
    this.digitalWrite = function(gpio, state /*bool*/) {

        this.__callNusbioRestApi(this.__stringFormat("/gpio/{0}/{1}", gpio, state ? "high":"low"));
    }
    this.setBlinkMode = function(gpio, rate, doubleRate) {

        if (typeof(doubleRate) === 'undefined')
            doubleRate = 0;
        this.__callNusbioRestApi(this.__stringFormat("/gpio/{0}/blink/{1}/{2}", gpio, rate, doubleRate));
    }
    this.getDeviceState = function(callBack, hideRestCall) {

        var url = "/nusbio/state";
        if (hideRestCall)
            url = "/nusbio/stateh";
        this.__callNusbioRestApi(url, callBack);
    }
    this.__stringFormat = function () {
        ///	<summary>
        ///Format the string passed as first argument based on the list of following arguments referenced in the format template&#10;
        ///with the synatx {index} 
        ///Sample:&#10;
        ///     var r = "LastName:{0}, Age:{1}".format("TORRES", 45);&#10;
        ///	</summary>    
        ///	<param name="tpl" type="string">value</param>
        ///	<param name="p1" type="object">value</param>
        for (var i = 1; i < arguments.length; i++)
            arguments[0] = arguments[0].replace(new RegExp('\\{' + (i - 1) + '\\}', 'gm'), arguments[i]);
        return arguments[0];
    }
    this.__init__();
}