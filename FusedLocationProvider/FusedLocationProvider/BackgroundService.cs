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

namespace com.xamarin.samples.location.fusedlocationprovider
{
    [Service]
    public class BackgroundService : Service
    {
        const long ONE_MINUTE = 60 * 1000;
        const long FIVE_MINUTES = 5 * ONE_MINUTE;
        const long TWO_MINUTES = 2 * ONE_MINUTE;

        static readonly string TAG = typeof(BackgroundService).FullName;

        FusedLocationProviderClient fusedLocationProviderClient;
        LocationCallback locationCallback;
        LocationRequest locationRequest;


        public override IBinder OnBind(Intent intent)
        {
            // Return null because this is a pure started service. A hybrid service would return a binder that would
            // allow access to the GetFormattedStamp() method.
            return null;
        }


        public override void OnCreate()
        {
            base.OnCreate();


            locationRequest = new LocationRequest()
                                  .SetPriority(LocationRequest.PriorityHighAccuracy)
                                  .SetInterval(FIVE_MINUTES)
                                  .SetFastestInterval(TWO_MINUTES);
            locationCallback = new FusedLocationProviderCallback(this);


            fusedLocationProviderClient = LocationServices.GetFusedLocationProviderClient(this);
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {

            if (intent.Action.Equals(Constants.ACTION_START_SERVICE))
            {
                fusedLocationProviderClient.RequestLocationUpdates(locationRequest, locationCallback, null); //checar qual looper devemos passar aqui estando dentro de um service.

                if (Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
                    RegisterForegroundServiceO();
                else
                    RegisterForegroundService();
            }
            else if (intent.Action.Equals(Constants.ACTION_STOP_SERVICE))
            {
                fusedLocationProviderClient.RemoveLocationUpdates(locationCallback);

                StopForeground(true);
            }



            // This tells Android not to restart the service if it is killed to reclaim resources.
            return StartCommandResult.Sticky;
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