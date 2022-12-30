using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;
using System.Diagnostics;

namespace HandsFree;

public partial class MainPage : ContentPage
{
    BluetoothClient client = new BluetoothClient();
    BluetoothDeviceInfo device = null;
    Stream stream = null;

    public MainPage()
	{
		InitializeComponent();
	}

    protected override void OnDisappearing()
    {
        if (stream is not null)
        {
            stream.Dispose();
            stream = null;
        }

        base.OnDisappearing();
    }

    private async void OnConnectClicked(object sender, EventArgs e)
    {
        var picker = new BluetoothDevicePicker();
        picker.ClassOfDevices.Add(new ClassOfDevice(DeviceClass.AudioVideoUnclassified, ServiceClass.Audio));
        device = await picker.PickSingleDeviceAsync();

        if (device != null)
        {
            if (!device.Authenticated)
            {
                bool paired = BluetoothSecurity.PairRequest(device.DeviceAddress, null);
                await Task.Delay(1000);
            }

            client.Connect(device.DeviceAddress, BluetoothService.Handsfree);
            if (client.Connected)
            {
                stream = client.GetStream();
                StreamReader reader = new StreamReader(stream, System.Text.Encoding.ASCII);
                await Task.Run(StreamLoop);
            }
        }
    }

    private async Task StreamLoop()
    {
        byte[] buffer = new byte[1024];

        while (client.Connected)
        {
            int readBytes = await stream.ReadAsync(buffer, 0, 80);
            var text = System.Text.Encoding.ASCII.GetString(buffer, 0, readBytes);
            var split = text.Split('\r');
            foreach (string line in split)
            {
                Debug.WriteLine(line);

                if (!string.IsNullOrWhiteSpace(line))
                {
                    if (line.StartsWith("AT+BRSF"))
                    {
                        // report no optional features
                        stream.WriteString("BRSF:0");
                        stream.WriteString("OK");
                    }
                    else if (line.StartsWith("AT+CIND=?"))
                    {
                        // report default indicators
                        stream.WriteString("+CIND: (\"service\",(0,1)),(\"call\",(0,1))");
                        stream.WriteString("OK");
                    }
                    else if (line.StartsWith("AT+CIND?"))
                    {
                        // report service but no active call
                        stream.WriteString("+CIND: 1,0");
                        stream.WriteString("OK");
                    }
                    else if (line.StartsWith("AT+CHLD=?"))
                    {
                        // report no hold support
                        stream.WriteString("+CHLD: 0");
                        stream.WriteString("OK");
                    }
                    else if (line.StartsWith("AT+XAPL"))
                    {
                        // respond to apple specific command to indicate support for battery reporting
                        stream.WriteString("+XAPL=iPhone,2");
                        stream.WriteString("OK");
                    }
                    else if (line.StartsWith("AT+IPHONEACCEV"))
                    {
                        // apple specific (but very common) message to report battery status
                        AtStreamHelper.ParseAppleBatteryStatus(line);

                        stream.WriteString("OK");
                    }
                    else if (line.StartsWith("ATD"))
                    {
                        // handsfree initiated a dial, parse the number
                        var number = AtStreamHelper.ParseAtdString(line);
                        Debug.WriteLine($"Dial {number}");
                        Dispatcher.Dispatch(async () =>
                        {
                            await DisplayAlert("Dialed", $"{number} was dialed", "Ok");
                        });

                        stream.WriteString("OK");
                    }
                    else
                    {
                        // for any other command return an OK
                        stream.WriteString("OK");
                    }
                }
            }
        }
    }
}