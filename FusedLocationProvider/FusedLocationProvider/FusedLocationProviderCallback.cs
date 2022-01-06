using System.Linq;

using Android.Gms.Location;
using Android.Util;
using Xamarin.Essentials;

namespace com.xamarin.samples.location.fusedlocationprovider
{
    public class FusedLocationProviderCallback : LocationCallback
    {
        FusedBackgroundService service;

        public FusedLocationProviderCallback(FusedBackgroundService service)
        {
            this.service = service;
        }

        public override void OnLocationAvailability(LocationAvailability locationAvailability)
        {
            Log.Debug("FusedLocationProviderSample", "IsLocationAvailable: {0}", locationAvailability.IsLocationAvailable);
            var x = Preferences.Get("LOG", "");
            Preferences.Set("LOG", $"IsLocationAvailable: {locationAvailability.IsLocationAvailable};{x}");
        }


        public override void OnLocationResult(LocationResult result)
        {
            if (result.Locations.Any())
            {
                var location = result.Locations.First();
                //var l = service.GetSharedPreferences("lllocations", Android.Content.FileCreationMode.)

                var x = Preferences.Get("LOG", "");
                if (x.Length > 2000)
                    x = x.Substring(0, 1999);
                Preferences.Set("LOG", $"{location.Latitude}|{location.Longitude}@{System.DateTime.Now.ToString("hh:mm:ss")};{x}");


                Log.Debug("FusedLocationProviderSample", $"{location.Latitude}|{location.Longitude}@{System.DateTime.Now.ToString("hh:mm:ss")}");

                //activity.latitude2.Text = activity.Resources.GetString(Resource.String.latitude_string, location.Latitude);
                //activity.longitude2.Text = activity.Resources.GetString(Resource.String.longitude_string, location.Longitude);
                //activity.provider2.Text = activity.Resources.GetString(Resource.String.requesting_updates_provider_string, location.Provider);
            }
            else
            {
                Log.Debug("FusedLocationProviderSample", $"No locations available.");

                //activity.latitude2.SetText(Resource.String.location_unavailable);
                //activity.longitude2.SetText(Resource.String.location_unavailable);
                //activity.provider2.SetText(Resource.String.could_not_get_last_location);
                //activity.requestLocationUpdatesButton.SetText(Resource.String.request_location_button_text);
            }
        }
    }
}
