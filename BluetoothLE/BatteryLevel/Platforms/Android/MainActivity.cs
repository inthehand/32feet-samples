﻿using Android;
using Android.App;
using Android.Content.PM;
using Android.OS;

namespace BatteryLevel;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        InTheHand.AndroidActivity.CurrentActivity = this;

        RequestPermissions(new string[] { Manifest.Permission.BluetoothConnect }, 1);
    }
}
