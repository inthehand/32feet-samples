using InTheHand.Bluetooth;
using System.Diagnostics;

namespace LegoController;

public partial class MainPage : ContentPage
{
	GattCharacteristic characteristic = null;
	int count = 0;

	public MainPage()
	{
		InitializeComponent();
	}

	private async void OnConnectClicked(object sender, EventArgs e)
	{
		var isavailable = await Bluetooth.GetAvailabilityAsync();
		var device = await Bluetooth.RequestDeviceAsync(new RequestDeviceOptions { AcceptAllDevices = true });
		if(device != null)
		{
			await device.Gatt.ConnectAsync();
			var service = await device.Gatt.GetPrimaryServiceAsync(LegoWirelessProtocol.LegoHubService);
			if(service != null)
			{
				characteristic = await service.GetCharacteristicAsync(LegoWirelessProtocol.LegoHubCharacteristic);
				if(characteristic != null)
				{
                    characteristic.CharacteristicValueChanged += Characteristic_CharacteristicValueChanged;
					await characteristic.StartNotificationsAsync();
				}
			}
		}
	}

    private async void Characteristic_CharacteristicValueChanged(object sender, GattCharacteristicValueChangedEventArgs e)
    {
		var msg = LegoMessageHeader.Parse(e.Value);
        System.Diagnostics.Debug.WriteLine($"{msg.Length} {msg.HubID} {msg.MessageType}");

		switch(msg.MessageType)
		{
			case LegoMessageType.HubAttachedIO:
                var iomsg = HubAttachedIOMessage.Parse(e.Value);

				// for user ports request the name
                if (iomsg.PortID < 50)
                {
                    System.Diagnostics.Debug.WriteLine($"{iomsg.IOEvent} Port:{iomsg.PortID} {iomsg.IOType}");
                
                    byte[] message = new byte[6];
                    message[0] = 6;
                    message[2] = (byte)LegoMessageType.PortModeInformationRequest;
                    message[3] = iomsg.PortID;
                    message[4] = 0;
                    message[5] = (byte)InformationType.Name;
                    await characteristic.WriteValueWithoutResponseAsync(message);
                }
				break;

			case LegoMessageType.PortModeInformation:

				InformationType informationType = (InformationType)e.Value[5];
				switch (informationType)
				{
					case InformationType.Name:
						string name = System.Text.Encoding.ASCII.GetString(e.Value, 6, e.Value.Length - 6).TrimEnd();
						Debug.WriteLine($"Port:{e.Value[3]} Mode:{e.Value[4]} Name:{name}");
                        
						if (e.Value[3] == 0)
                        {
                            Dispatcher.Dispatch(() =>
                            {
                                PortName.Text= name;
                            });
                        }

                        byte[] getRangeMessage = new byte[6];
                        getRangeMessage[0] = 6;
                        getRangeMessage[2] = (byte)LegoMessageType.PortModeInformationRequest;
                        getRangeMessage[3] = e.Value[3]; //PortID
						getRangeMessage[5] = (byte)InformationType.RawRange;
                        await characteristic.WriteValueWithoutResponseAsync(getRangeMessage);
                        break;

					case InformationType.RawRange:
						float rawMin = BitConverter.ToSingle(e.Value, 6);
						float rawMax = BitConverter.ToSingle(e.Value, 10);

						// for the 1st (motor) port
						if (e.Value[3] == 0)
						{
							Dispatcher.Dispatch(() =>
							{
								MinLabel.Text = rawMin.ToString();
								MidLabel.Text = ((rawMax + rawMin)/2f).ToString();
								MaxLabel.Text = rawMax.ToString();
								Motor.Maximum = rawMax;
								Motor.Minimum = rawMin;
							});
						}

						Debug.WriteLine($"{msg.Length} Port:{e.Value[3]} Mode:{e.Value[4]} Min:{rawMin} Max:{rawMax}");
                        
                        break;
				}
                break;

			default:
				break;
        }

    }

    private async void Motor_ValueChanged(object sender, ValueChangedEventArgs e)
    {
		if(characteristic != null)
		{
            byte[] messageValue = new byte[6];
            messageValue[0] = 6;
            messageValue[2] = (byte)LegoMessageType.PortValueSingle;
            messageValue[3] = 0; //PortID
            BitConverter.GetBytes((short)e.NewValue).CopyTo(messageValue, 4);
            await characteristic.WriteValueWithoutResponseAsync(messageValue);
        }
    }
}

