using System;
using Android.App;
using Android.Content.PM;
using Android.Hardware;
using Android.OS;
using Android.Runtime;
using Android.Views;
using CitySmash;

namespace MonsterSmashAndroid
{
    [Activity(Label = "MonsterSmashAndroid"
        , MainLauncher = true
        , Icon = "@drawable/icon"
        , Theme = "@style/Theme.Splash"
        , AlwaysRetainTaskState = true
        , LaunchMode = Android.Content.PM.LaunchMode.SingleInstance
        , ScreenOrientation = ScreenOrientation.Landscape
        , ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.ScreenSize)]
    public class Activity1 : Microsoft.Xna.Framework.AndroidGameActivity
    {
        // initialize sensors, storage fields, and game
        SensorManager sensorManager;
        Sensor accelerometer;
        AccelerometerListener accelListener;
        float[] accelReadings = new float[3];

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // sensor reading support
            sensorManager = (SensorManager)GetSystemService("sensor");
            accelerometer = sensorManager.GetDefaultSensor(SensorType.Accelerometer);
            accelListener = new AccelerometerListener(sensorManager, accelerometer);

            // start a new game instance and set basic values
            var gameInstance = new Game1(ref accelListener);
            gameInstance.actualWidth = Resources.DisplayMetrics.WidthPixels;
            gameInstance.actualHeight = Resources.DisplayMetrics.HeightPixels;

            // switch the screen to the game and let it run
            SetContentView((View)gameInstance.Services.GetService(typeof(View)));
            gameInstance.Run();
        }

        // Stop listening to the sensor if the app is not active
        protected override void OnPause()
        {
            base.OnPause();
            accelListener.UnRegister();
        }

        // Start listening to the sensor if the app is active
        protected override void OnResume()
        {
            base.OnResume();
            accelListener.Register();
        }

    }
}

