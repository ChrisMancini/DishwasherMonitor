// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Net.Http;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.Devices.Gpio;
using Windows.Foundation;
using Windows.System.Threading;
using Shared;

namespace Monitor
{
    public sealed class GpioMonitorTask : IBackgroundTask
    {
        BackgroundTaskDeferral deferral;

        private const int CleanLightPin = 18;
        private const int TiltSensorPin = 17;
        private const int Sensor1 = 15;
        private const int Sensor2 = 14;

        private GpioSensor cleanLightGpio;
        private GpioSensor tiltSensorGpio;
        private GpioSensor sensor1;
        private GpioSensor sensor2;

        SqlHelper halper = new SqlHelper();

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

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            deferral = taskInstance.GetDeferral();
            cleanLightGpio = new GpioSensor(CleanLightPin, ValueChangedHandler);
            tiltSensorGpio = new GpioSensor(TiltSensorPin, ValueChangedHandler);
            sensor1 = new GpioSensor(Sensor1, ValueChangedHandler);
            sensor2 = new GpioSensor(Sensor2, ValueChangedHandler);
        }

        private void ValueChangedHandler(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            Debug.WriteLine("Pin {0} changed to {1}", sender.PinNumber, sender.Read());
        }
    }
}
