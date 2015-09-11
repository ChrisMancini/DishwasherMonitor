using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Devices.Gpio;
using Windows.Foundation;
using Windows.System.Threading;
using Shared;

namespace Server
{
    public sealed class GpioMonitor
    {
        private const int CleanLightPin = 4;
        private const int TiltSensorPin = 18;
        private const int NormalCycleLight = 22;
        private const int HeavyCycleLight = 23;
        private const int SanitizeCycleLight = 27;

        private IDictionary<int, GpioSensor> _gpioSensors;
        private readonly IDictionary<int, RunCycle> _pinToCycleTypeMap = new Dictionary<int, RunCycle>
        {
            { NormalCycleLight, RunCycle.Normal },
            { HeavyCycleLight, RunCycle.Heavy},
            {SanitizeCycleLight, RunCycle.Sanitize }
        };

        private SqlHelper _halper;

        private class GpioSensor
        {
            public GpioPin Pin { get; }
            public int PinNumber { get; set; }

            public GpioSensor(int pinNumber, TypedEventHandler<GpioPin, GpioPinValueChangedEventArgs> valueChangedHandler)
            {
                PinNumber = pinNumber;
                try
                {
                    Pin = GpioController.GetDefault().OpenPin(pinNumber);
                    Pin.SetDriveMode(GpioPinDriveMode.Input);
                    Pin.DebounceTimeout = TimeSpan.FromMilliseconds(400);
                    Pin.ValueChanged += valueChangedHandler;
                }
                catch (Exception)
                {
                    Debug.WriteLine("Couldn't open pin {0}", pinNumber);
                }
            }

            public GpioPinValue Read()
            {
                return Pin.Read();
            }
        }

        public void Start()
        {
            _halper = new SqlHelper();
            _gpioSensors = new Dictionary<int, GpioSensor>
            {
                { CleanLightPin, new GpioSensor(CleanLightPin, ValueChangedHandler) },
                { TiltSensorPin, new GpioSensor(TiltSensorPin, ValueChangedHandler) },
                { NormalCycleLight, new GpioSensor(NormalCycleLight, ValueChangedHandler) },
                { HeavyCycleLight, new GpioSensor(HeavyCycleLight, ValueChangedHandler) },
                { SanitizeCycleLight, new GpioSensor(SanitizeCycleLight, ValueChangedHandler) }
            };

            //_halper.StartDishwasherRun(RunCycle.Normal);

            //ValueChangedHandler(_gpioSensors[NormalCycleLight]);

            //var deferral = taskInstance.GetDeferral();
        }

        private void ValueChangedHandler(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            var pinNumber = sender.PinNumber;
            var gpioPinValue = sender.Read();
            Debug.WriteLine("Pin {0} changed to {1}", pinNumber, gpioPinValue);

            if (pinNumber == TiltSensorPin)
            {
                _halper.DishwasherTilt(gpioPinValue == GpioPinValue.High);
                var currentStatus = _halper.Get().CurrentStatus;
                if (currentStatus == DishwasherStatus.Clean && gpioPinValue == GpioPinValue.High)
                {
                    ThreadPoolTimer.CreatePeriodicTimer(Timer_Tick, TimeSpan.FromMilliseconds(10000));
                }
                return;
            }

            var tiltSensorValue = _gpioSensors[TiltSensorPin].Read();
            if (gpioPinValue == GpioPinValue.High)
            {
                if (pinNumber == CleanLightPin)
                {
                    _halper.EndDishwasherRun();
                }
                else if (tiltSensorValue == GpioPinValue.Low && _pinToCycleTypeMap.ContainsKey(pinNumber))
                {
                    _halper.StartDishwasherRun(_pinToCycleTypeMap[pinNumber]);
                }
            }
        }

        private void Timer_Tick(ThreadPoolTimer threadPoolTimer)
        {
            var tiltSensorValue = _gpioSensors[TiltSensorPin].Read();
            if (tiltSensorValue == GpioPinValue.High)
            {
                _halper.DishwasherEmptied();
            }
            threadPoolTimer.Cancel();
        }
    }
}
