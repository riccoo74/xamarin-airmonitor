using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Input;
using AirMonitor.Models;
using AirMonitor.Views;
using Newtonsoft.Json;
using Xamarin.Forms;
using Xamarin.Essentials;
using System.Globalization;
using System.Web;

namespace AirMonitor.ViewModels
{
    public class HomeViewModel : BaseViewModel
    {
        private readonly INavigation _navigation;
        public HttpClient httpClient { get; private set; }
        public double Latitude { get; private set; }
        public double Longitude { get; private set; }

        public HomeViewModel(INavigation navigation)
        {
            _navigation = navigation;

            Initialize();
        }

        private async Task Initialize()
        {
            IsBusy = true;

            IEnumerable<Measurement> measurements = await GetMeasurements();
            Items = new List<Measurement>(measurements);

            IsBusy = false;
        }

        private ICommand _goToDetailsCommand;
        public ICommand GoToDetailsCommand => _goToDetailsCommand ?? (_goToDetailsCommand = new Command<Measurement>(OnGoToDetails));

        private void OnGoToDetails(Measurement item)
        {
            _navigation.PushAsync(new DetailsPage(item));
        }

        private List<Measurement> _items;

        public List<Measurement> Items
        {
            get => _items;
            set => SetProperty(ref _items, value);
        }

        private bool _isBusy;

        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public async Task<IEnumerable<Installation>> GetCloseInstallations()
        {
            await UpdateLocalization();

            Uri uri = new Uri($"{App.AirlyApiUrl}/v2/installations/nearest?lat={Latitude}&lng={Longitude}&maxDistanceKM=10&maxResults=3");
            HttpResponseMessage response = await httpClient.GetAsync(uri);

            List<Installation> installations = new List<Installation>();

            if (response.IsSuccessStatusCode)
            {
                string body = await response.Content.ReadAsStringAsync();

                installations = JsonConvert.DeserializeObject<List<Installation>>(body);
            }

            return installations;
        }

        public async Task<IEnumerable<Measurement>> GetMeasurements()
        {
            List<Measurement> measurements = new List<Measurement>();
            IEnumerable<Installation> installations = await GetCloseInstallations();

            foreach (Installation installation in installations)
            {
                Uri uri = new Uri($"{App.AirlyApiUrl}/v2/measurements/installation?installationId={installation.Id}");
                HttpResponseMessage response = await httpClient.GetAsync(uri);

                if (response.IsSuccessStatusCode)
                {
                    string body = await response.Content.ReadAsStringAsync();
                    Measurement measurement = JsonConvert.DeserializeObject<Measurement>(body);
                    measurement.Installation = installation;

                    measurements.Add(measurement);
                }
            }

            return measurements;
        }

        private void InitializeHttpClient()
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(App.AirlyApiUrl);
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip");
            client.DefaultRequestHeaders.Add("apikey", App.AirlyApiKey);
            httpClient = client;
        }

        public async Task UpdateLocalization()
        {
            var location = await Geolocation.GetLastKnownLocationAsync();
            Latitude = location.Latitude;
            Longitude = location.Longitude;
        }
    }
}