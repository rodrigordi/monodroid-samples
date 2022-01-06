using Android.App;
using Android.Content;
using Android.Gms.Location;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Android.Util;
using Java.Interop;
using Xamarin.Essentials;

namespace com.xamarin.samples.location.fusedlocationprovider
{
    [Service(Enabled = true, Exported = false)]
    public class GPSBackgroundService : Service
    {
        static readonly string TAG = typeof(GPSBackgroundService).FullName;

        static readonly int ServiceTimerWait = 5000; //milliseconds //roda a cada 5 minutos
        Timer serviceTimer;
        DateTime startTime;
        bool ServiceIsStarted = false;

        public override IBinder OnBind(Intent intent)
        {
            // Return null because this is a pure started service. A hybrid service would return a binder that would
            // allow access to the GetFormattedStamp() method.
            return null;
        }


        public override void OnCreate()
        {
            base.OnCreate();
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {

            Log.Debug(TAG, $"OnStartCommand called at {DateTime.UtcNow}, flags={flags}, startid={startId}");

            if (intent.Action.Equals(Constants.ACTION_START_SERVICE))
            {
                if (ServiceIsStarted)
                {
                    TimeSpan runtime = DateTime.UtcNow.Subtract(startTime);
                    Log.Debug(TAG, $"This service was already started, it's been running for {runtime:c}.");
                }
                else
                {
                    serviceTimer = new Timer(HandleTimerCallback, startTime, 0, ServiceTimerWait); //esse cara roda a cada 5 segundos.

                    //service
                    startTime = DateTime.UtcNow;
                    ServiceIsStarted = true;
                    Log.Debug(TAG, $"Starting the service, at {startTime}.");

                    if (Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
                        RegisterForegroundServiceO();
                    else
                        RegisterForegroundService();
                }
            }
            else if (intent.Action.Equals(Constants.ACTION_STOP_SERVICE))
            {
                if (ServiceIsStarted)
                {
                    ServiceIsStarted = false;
                    serviceTimer.Change(Timeout.Infinite, Timeout.Infinite);
                    startTime = DateTime.UtcNow;


                    TimeSpan runtime = DateTime.UtcNow.Subtract(startTime);
                    Log.Debug(TAG, $"This service was stopped, it's been running for {runtime:c}.");



                    StopForeground(true);
                    StopSelf();
                }
                else
                {
                    Log.Debug(TAG, $"Service is not running.");
                }
            }

            // This tells Android not to restart the service if it is killed to reclaim resources.
            return StartCommandResult.Sticky;
        }


        public override void OnDestroy()
        {
            try
            {
                base.OnDestroy();

                serviceTimer.Dispose();
                serviceTimer = null;

                if (TAG != null)
                {
                    Log.Debug(TAG, "This service destroyed at {0}.", DateTime.UtcNow);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                //Write Error on File as Log
            }
        }

        async void HandleTimerCallback(object state)
        {
            try
            {
                var request = new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(10));
                var cts = new CancellationTokenSource();
                var location = await Geolocation.GetLocationAsync(request, cts.Token);


                var x = Xamarin.Essentials.Preferences.Get("LOG", "");
                if (x.Length > 2000)
                    x = x.Substring(0, 1999);
                Xamarin.Essentials.Preferences.Set("LOG", $"{location.Latitude}|{location.Longitude}@{System.DateTime.Now.ToString("hh:mm:ss")};{x}");
                Log.Debug("FusedLocationProviderSample", $"{location.Latitude}|{location.Longitude}@{System.DateTime.Now.ToString("hh:mm:ss")}");

            }
            catch (Exception ex)
            {
                Log.Error(TAG, $"TimerSyncMES: {ex.Message}");
            }

            TimeSpan runTime = DateTime.UtcNow.Subtract(startTime);
            Log.Debug(TAG, $"This service has been running for {runTime:c} (since ${state}).");
        }


        void RegisterForegroundService()
        {
            var notification = new Notification.Builder(this)
               .SetContentTitle(Resources.GetString(Resource.String.app_name))
               .SetContentText(Resources.GetString(Resource.String.notification_text))
               .SetSmallIcon(Resource.Drawable.ic_stat_name)
               .SetContentIntent(BuildIntentToShowMainActivity())
               .SetOngoing(true)
               .AddAction(BuildRestartTimerAction())
               .AddAction(BuildStopServiceAction())
               .Build();

            StartForeground(Constants.SERVICE_RUNNING_NOTIFICATION_ID, notification);
        }


        //Verificar como criar a notificacao
        //https://youtu.be/4_RK_5bCoOY?t=489
        void RegisterForegroundServiceO()
        {
            String NOTIFICATION_CHANNEL_ID = "com.xamarin.samples.location.fusedlocationprovider.channelid";
            NotificationChannel chan = new NotificationChannel(NOTIFICATION_CHANNEL_ID, "GPSLogger", NotificationImportance.High);

            NotificationManager manager = (NotificationManager)GetSystemService(Context.NotificationService);

            manager.CreateNotificationChannel(chan);

            var notificationBuilder = new Notification.Builder(this, NOTIFICATION_CHANNEL_ID);

            Notification notification = notificationBuilder.SetOngoing(true)
                .SetContentTitle(Resources.GetString(Resource.String.app_name))
                .SetContentText(Resources.GetString(Resource.String.notification_text))
                .SetSmallIcon(Resource.Drawable.ic_stat_name)
                .SetContentIntent(BuildIntentToShowMainActivity())
                .SetOngoing(true)
                .Build();

            //const int Service_Running_Notification_ID = 936;
            //StartForeground(Service_Running_Notification_ID, notification);
            StartForeground(Constants.SERVICE_RUNNING_NOTIFICATION_ID, notification);
        }


        /// <summary>
		/// Builds a PendingIntent that will display the main activity of the app. This is used when the 
		/// user taps on the notification; it will take them to the main activity of the app.
		/// </summary>
		/// <returns>The content intent.</returns>
		PendingIntent BuildIntentToShowMainActivity()
        {
            var notificationIntent = new Intent(this, typeof(MainActivity));
            notificationIntent.SetAction(Constants.ACTION_MAIN_ACTIVITY);
            notificationIntent.SetFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTask);
            notificationIntent.PutExtra(Constants.SERVICE_STARTED_KEY, true);

            var pendingIntent = PendingIntent.GetActivity(this, 0, notificationIntent, PendingIntentFlags.UpdateCurrent);
            return pendingIntent;
        }

        /// <summary>
		/// Builds a Notification.Action that will instruct the service to restart the timer.
		/// </summary>
		/// <returns>The restart timer action.</returns>
		Notification.Action BuildRestartTimerAction()
        {
            var restartTimerIntent = new Intent(this, GetType());
            restartTimerIntent.SetAction(Constants.ACTION_RESTART_TIMER);
            var restartTimerPendingIntent = PendingIntent.GetService(this, 0, restartTimerIntent, 0);

            var builder = new Notification.Action.Builder(Resource.Drawable.ic_action_restart_timer,
                                              GetText(Resource.String.restart_timer),
                                              restartTimerPendingIntent);

            return builder.Build();
        }

        /// <summary>
        /// Builds the Notification.Action that will allow the user to stop the service via the
        /// notification in the status bar
        /// </summary>
        /// <returns>The stop service action.</returns>
        Notification.Action BuildStopServiceAction()
        {
            var stopServiceIntent = new Intent(this, GetType());
            stopServiceIntent.SetAction(Constants.ACTION_STOP_SERVICE);
            var stopServicePendingIntent = PendingIntent.GetService(this, 0, stopServiceIntent, 0);

            var builder = new Notification.Action.Builder(Android.Resource.Drawable.IcMediaPause,
                                                          GetText(Resource.String.stop_service),
                                                          stopServicePendingIntent);
            return builder.Build();

        }

    }
}