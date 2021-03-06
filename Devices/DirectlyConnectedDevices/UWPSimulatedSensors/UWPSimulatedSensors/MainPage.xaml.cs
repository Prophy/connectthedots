﻿using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;
using ConnectTheDotsHelper;
using ZXing.Mobile;
using Windows.UI.Popups;

namespace UWPSimulatedSensors
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private GeolocationAccessStatus LocationAccess = GeolocationAccessStatus.Unspecified;
        private Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
        private ConnectTheDots CTD;
        private MobileBarcodeScanner Scanner;

        public MainPage()
        {
            this.InitializeComponent();

            // Initialize QRCode Scanner
            Scanner = new MobileBarcodeScanner(Dispatcher);
            Scanner.Dispatcher = Dispatcher;

            // Initialize ConnectTheDots Helper
            CTD = new ConnectTheDots();

            // Hook up a callback to display message received from Azure
            CTD.ReceivedMessage += CTD_ReceivedMessage;

            // Restore local settings
            if (localSettings.Values.ContainsKey("ConnectionString"))
            {
                CTD.ConnectionString = (string)localSettings.Values["ConnectionString"];
                this.TBConnectionString.Text = CTD.ConnectionString;
            }

            if (localSettings.Values.ContainsKey("DisplayName"))
            {
                CTD.DisplayName = (string)localSettings.Values["DisplayName"];
                this.TBDeviceName.Text = CTD.DisplayName;
            }

            // Check configuration settings
            ConnectToggle.IsEnabled = checkConfig();
            CTD.ConnectionString = this.TBConnectionString.Text;
            CTD.DisplayName = this.TBDeviceName.Text;
            CTD.Organization = "My Company";
            CTD.Location = "Unknown";

            // Get user consent for accessing location
            Task.Run(async () =>
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                async () =>
                {
                    this.LocationAccess = await Geolocator.RequestAccessAsync();
                    // Get device location
                    await updateLocation();

                });
            });

            // Add sensors to the ConnectTheDots object
            CTD.AddSensor("Temperature", "C");
            CTD.AddSensor("Humidity", "%");

        }

        /// <summary>
        /// CTD_ReceivedMessage
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void CTD_ReceivedMessage(object sender, EventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                C2DMessage message = ((ConnectTheDots.ReceivedMessageEventArgs)e).Message;
                var textToDisplay = message.timecreated + " - Alert received:" + message.message + ": " + message.value + " " + message.unitofmeasure + "\r\n";
                TBAlerts.Text += textToDisplay;
            });

        }

        /// <summary>
        /// checkConfig
        /// Check stored configuration
        /// </summary>
        /// <returns></returns>
        private bool checkConfig()
        {
            return ((this.TBDeviceName.Text != null) && (this.TBConnectionString.Text != null) &&
                    (this.TBDeviceName.Text != "") && (this.TBConnectionString.Text != ""));
        }

        /// <summary>
        /// updateLocation
        /// Updates current location of the device
        /// </summary>
        /// <returns></returns>
        private async Task updateLocation()
        {
            // Update current device location
            try
            {
                if (LocationAccess == GeolocationAccessStatus.Allowed)
                {

                    Geolocator geolocator = new Geolocator();
                    Geoposition pos = await geolocator.GetGeopositionAsync();
                    CTD.Location = pos.Coordinate.Point.Position.Longitude.ToString() + "," + pos.Coordinate.Point.Position.Latitude.ToString();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error while trying to retreive device's location: " + ex.Message);
                CTD.Location = "unknown";
            }
        }

        /// <summary>
        /// toggleButton_Checked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toggleButton_Checked(object sender, RoutedEventArgs e)
        {
            SendDataToggle.Content = "Sending telemetry data";
            CTD.SendTelemetryData = true;
        }
        /// <summary>
        /// toggleButton_Unchecked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            SendDataToggle.Content = "Press to send telemetry data";
            CTD.SendTelemetryData = false;
        }
        /// <summary>
        /// TempSlider_ValueChanged
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TempSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if ((CTD !=null) && (CTD.Sensors["Temperature"] != null))
                CTD.Sensors["Temperature"].message.value = TempSlider.Value;
        }
        /// <summary>
        /// HmdtSlider_ValueChanged
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HmdtSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if ((CTD != null) && (CTD.Sensors["Humidity"] != null))
                CTD.Sensors["Humidity"].message.value = HmdtSlider.Value;
        }
        /// <summary>
        /// TBDeviceName_TextChanged
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TBDeviceName_TextChanged(object sender, TextChangedEventArgs e)
        {
            CTD.DisplayName = TBDeviceName.Text;
            localSettings.Values["DisplayName"] = CTD.DisplayName;
            ConnectToggle.IsEnabled = checkConfig();

        }
        /// <summary>
        /// TBConnectionString_TextChanged
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TBConnectionString_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (CTD.ConnectionString != TBConnectionString.Text)
            {
                CTD.ConnectionString = TBConnectionString.Text;
            }
            localSettings.Values["ConnectionString"] = CTD.ConnectionString;
            ConnectToggle.IsEnabled = checkConfig();
        }

        /// <summary>
        /// ConnectToggle_Checked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ConnectToggle_Checked(object sender, RoutedEventArgs e)
        {
            if (CTD.Connect())
            {
                SendDataToggle.IsEnabled = true;
                TBDeviceName.IsEnabled = false;
                TBConnectionString.IsEnabled = false;
                ConnectToggle.Content = "Dots connected";
                buttonScanCode.IsEnabled = false;
            }
        }

        /// <summary>
        /// ConnectToggle_Unchecked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ConnectToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            if (CTD.Disconnect())
            {
                SendDataToggle.IsChecked = false;
                SendDataToggle.IsEnabled = false;
                TBDeviceName.IsEnabled = true;
                TBConnectionString.IsEnabled = true;
                ConnectToggle.Content = "Press to connect the dots";
                buttonScanCode.IsEnabled = true;
            }
        }


        /// <summary>
        /// ScanCode
        /// Scan a QR Code using the ZXing library
        /// </summary>
        /// <returns></returns>
        private async Task ScanCode()
        {
            Scanner.UseCustomOverlay = false;
            Scanner.TopText = "Hold camera up to QR code";
            Scanner.BottomText = "Camera will automatically scan QR code\r\n\rPress the 'Back' button to cancel";

            ZXing.Result result = await Scanner.Scan().ConfigureAwait(true);

            if (result == null || (string.IsNullOrEmpty(result.Text)))
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                    {
                        MessageDialog dialog = new MessageDialog("An error occured while scanning the QRCode. Try again");
                        await dialog.ShowAsync();
                    });
            }
            else
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    TBConnectionString.Text = result.Text;
                    TBDeviceName.Text = CTD.ExtractDeviceIdFromConnectionString(result.Text);
                });
            }
        }

        /// <summary>
        /// buttonScanCode_Tapped
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private async void buttonScanCode_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            if (SendDataToggle.IsEnabled == false)
            {
                await ScanCode();
            }
        }
    }
}
