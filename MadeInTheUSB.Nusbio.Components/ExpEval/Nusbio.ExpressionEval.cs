/*using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ExpressionEvaluator;
using MadeInTheUSB.GPIO;

namespace MadeInTheUSB
{
    public class ExpressionEvalCompilationException : System.Exception
    {
        public ExpressionEvalCompilationException(string message): base(message)
        {

        }
    }

    public class ExpressionEvalExecutionException : System.Exception
    {
        public ExpressionEvalExecutionException(string message): base(message)
        {

        }
    }
 
    public class NusbioExpressionEval 
    {
        public class NusbioExpressionEvalObject : IDigitalWriteRead
        {
            private Nusbio _nusbio;

            public NusbioExpressionEvalObject(Nusbio nusbio)
            {
                this._nusbio = nusbio;
            }
            public int Tickcount
            {
                get { return Environment.TickCount; }
            }
            public string NewGuid
            {
                get { return Guid.NewGuid().ToString(); }
            }
            public bool IsNumeric(string s)
            {
                return IsInteger(s) || IsDouble(s) || IsDecimal(s);
            }
            public bool IsInteger(string s)
            {
                int v;
                return int.TryParse(s, out v);
            }
            public bool IsDouble(string s)
            {
                double v;
                return double.TryParse(s, out v);
            }
            public bool IsDecimal(string s)
            {
                Decimal v;
                return Decimal.TryParse(s, out v);
            }
            public void SetPinMode(int pin, string mode)
            {
                switch (mode.ToLowerInvariant())
                {
                    case "input"       : _nusbio.SetPinMode(pin, PinMode.Input );                                  break;
                    case "output"      : _nusbio.SetPinMode(pin, PinMode.Output );                                 break;
                    case "inputpullup" : _nusbio.SetPinMode(pin, PinMode.InputPullUp );                            break;
                    default            : throw new ArgumentException(string.Format("Invalid input mode:{0}", mode)); break;
                }
            }
            public void SetPinMode(int pin, PinMode mode)
            {
                _nusbio.SetPinMode(pin, mode);
            }
            public void DigitalWrite(int pin, bool state)
            {
                this.DigitalWrite(pin, state ? PinState.High : PinState.Low);
            }
            public void DigitalWrite(int pin, PinState state)
            {
                _nusbio.DigitalWrite(pin, state);
            }
            public PinState DigitalRead(int pin)
            {
                return _nusbio.DigitalRead(pin);
            }
            public void SetGpioMask(byte mask)
            {
                _nusbio.SetGpioMask(mask);
            }
            public byte GetGpioMask()
            {
                return _nusbio.GetGpioMask();
            }
            public byte GpioStartIndex
            {
                get { return _nusbio.GpioStartIndex; }
            }
            public void SetPullUp(int p, PinState d)
            {
                _nusbio.SetPullUp(p, d);
            }
        }

        public object Eval(Nusbio nusbio, string expression, Guid? subscriberGuid = null)
        {
            CompiledExpression expCompiled = null;
            try
            {
                expCompiled = new CompiledExpression(expression);
                var tr = new TypeRegistry();
                tr.RegisterSymbol("nusbio", new NusbioExpressionEvalObject(nusbio));
                expCompiled.TypeRegistry = tr;
                expCompiled.Compile();
            }
            catch (System.Exception ex)
            {
                throw new ExpressionEvalCompilationException(string.Format("Compilation error expression:'{0}', failed:{1}",expression, ex.Message));
            }

            try
            {
                var result = expCompiled.Eval();

                if (result != null && result.GetType().Name == "Boolean")
                {
                    result = result.ToString().ToLowerInvariant();
                }
                return result;
            }
            catch (System.Exception ex)
            {
                throw new ExpressionEvalExecutionException(
                    string.Format("NusbioExpressionEval eval error expression:'{0}', failed:{1}",expression, ex.Message)
                    );
            }
        }
    }

}
*/