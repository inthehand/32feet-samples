using InTheHand.Bluetooth;
using System.Diagnostics;

namespace BatteryLevel;

public partial class MainPage : ContentPage
{
	int count = 0;
	GattCharacteristic characteristic = null;

	public MainPage()
	{
		InitializeComponent();
	}

	private async void OnBatteryClicked(object sender, EventArgs e)
	{
		Debug.WriteLine("Requesting Bluetooth Device...");
		var device = await Bluetooth.RequestDeviceAsync(new RequestDeviceOptions { AcceptAllDevices = true });
		if (device != null)
		{
			var gatt = device.Gatt;
			Debug.WriteLine("Connecting to GATT Server...");
			await gatt.ConnectAsync();

			Debug.WriteLine("Getting Battery Service...");
			var service = await gatt.GetPrimaryServiceAsync(GattServiceUuids.Battery);

			if (service != null)
			{
				Debug.WriteLine("Getting Battery Level Characteristic...");
				characteristic = await service.GetCharacteristicAsync(BluetoothUuid.GetCharacteristic("battery_level"));

				if (characteristic != null)
				{
					Debug.WriteLine("Reading Battery Level...");
					var value = await characteristic.ReadValueAsync();

					Debug.WriteLine($"Battery Level is {value[0]} %");

					characteristic.CharacteristicValueChanged += Characteristic_CharacteristicValueChanged;
					await characteristic.StartNotificationsAsync();
				}
			}
		}
	}

    private void Characteristic_CharacteristicValueChanged(object sender, GattCharacteristicValueChangedEventArgs e)
    {
        Debug.WriteLine($"Battery Level has changed to {e.Value[0]} %");
    }
}
