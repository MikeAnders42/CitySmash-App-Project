using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Hardware;

namespace CitySmash
{
    public class AccelerometerListener: Java.Lang.Object, ISensorEventListener
    {
        // Sensor reading support
        private float[] accelReadings = new float[3];
        private SensorManager sensorManager;
        private Sensor accelerometer;

        #region Constructor

        /// <summary>
        /// The event listener that will listen and record accelerometer readings
        /// </summary>
        /// <param name="sensorManager">A sensor manager from the Android.Hardware namespace</param>
        /// <param name="accelerometer">An accelerometer type sensor instantiated using the above manager</param>
        public AccelerometerListener(SensorManager sensorManager, Sensor accelerometer)
        {
            this.sensorManager = sensorManager;
            this.accelerometer = accelerometer;

            sensorManager.RegisterListener(this, accelerometer, SensorDelay.Ui);
        }

        #endregion

        #region Properties

        public float[] AccelReadings
        {
            get { return accelReadings; }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Begin listening to the accelerometer
        /// </summary>
        public void Register()
        {
            sensorManager.RegisterListener(this, accelerometer, SensorDelay.Ui);
        }

        /// <summary>
        /// Stop listening to the accelerometer
        /// </summary>
        public void UnRegister()
        {
            sensorManager.UnregisterListener(this);
        }

        public void OnAccuracyChanged(Sensor sensor, [GeneratedEnum] SensorStatus accuracy)
        {
            // We don't want to do anything here.
        }

        // Listen for new sensor values and save them to a local variable
        public void OnSensorChanged(SensorEvent e)
        {
            if (e != null)
            {
                if (e.Sensor.Type == SensorType.Accelerometer)
                {
                    accelReadings[0] = e.Values[0];
                    accelReadings[1] = e.Values[1];
                    accelReadings[2] = e.Values[2];
                }
            }
        }

        #endregion
    }
}