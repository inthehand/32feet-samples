using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace LegoController
{
    public sealed class LegoWirelessProtocol
    {
        public static readonly Guid LegoHubService = new("00001623-1212-EFDE-1623-785FEABCD123");
        public static readonly Guid LegoHubCharacteristic = new("00001624-1212-EFDE-1623-785FEABCD123");

    }

    public class LegoMessageHeader
    {
        public ushort Length { get; set; }
        public byte HubID { get; set; }
        public LegoMessageType MessageType { get; set; }
        internal byte Offset { get;set; }

        public static LegoMessageHeader Parse(byte[] data)
        {
            LegoMessageHeader header = new LegoMessageHeader();
            header.Offset = 0;
            if (data[1] > 127)
            {
                header.Length = (ushort)(0x8FFF & BitConverter.ToUInt16(data, 0));
                header.Offset++;
            }
            else
            {
                header.Length = data[0];
            }

            header.HubID = data[1 + header.Offset];
            header.MessageType = (LegoMessageType)data[2 + header.Offset];
            return header;
        }
    }

    public class HubAttachedIOMessage : LegoMessageHeader
    {
        public byte PortID { get; set; }
        public IOEvent IOEvent { get; set; }
        public IOType IOType { get; set; }
        public uint HardwareRevision { get; set; }
        public uint SoftwareRevision { get; set; }
        public byte PortIDA { get; set; }
        public byte PortIDB { get; set; }   

        public static HubAttachedIOMessage Parse(byte[] data)
        {
            HubAttachedIOMessage message = new HubAttachedIOMessage();

            var header = LegoMessageHeader.Parse(data);
     
            message.Length= header.Length;
            message.HubID = header.HubID;
            message.MessageType= header.MessageType;
            message.Offset = header.Offset;
            if (header.MessageType != LegoMessageType.HubAttachedIO)
                throw new InvalidDataException();

            message.PortID = data[3];
            message.IOEvent = (IOEvent)data[4];
            if (message.IOEvent != IOEvent.Detatched)
                message.IOType = (IOType)BitConverter.ToUInt16(data, 5);

            if(message.IOEvent == IOEvent.Attached)
            {
                message.HardwareRevision = BitConverter.ToUInt32(data, 6);
                message.SoftwareRevision = BitConverter.ToUInt32(data, 10);
            }
            else if(message.IOEvent == IOEvent.AttachedVirtual)
            {
                message.PortIDA = data[6];
                message.PortIDB = data[7];
            }

            return message;
        }
    }

    public enum LegoMessageType : byte
    {
        HubProperties = 0x1,
        HubActions = 0x2,
        HubAlerts = 0x3,
        HubAttachedIO = 0x4,
        GenericErrorMessages = 0x5,
        HWNetworkCommands = 0x8,
        FWUpdateGoIntoBootMode = 0x10,
        FWUpdateLockMemory = 0x11,
        FWUpdateLockStatusReport = 0x12,
        FWLockStatus = 0x13,

        PortInformationRequest = 0x21,
        PortModeInformationRequest = 0x22,
        PortInputFormatSetupSingle = 0x41,
        PortInputFormatSetupCombined =0x42,
        PortInformation = 0x43,
        PortModeInformation = 0x44,
        PortValueSingle = 0x45,
        PortValueCombined = 0x46,
        PortInputFormatSingle = 0x47,
        PortInputFormatCombined = 0x48,
        VirtualPortSetup = 0x61,
        PortOutputCommand = 0x81,
        PortOutputCommandFeedback = 0x82
    }

    public enum InformationType : byte
    {
        Name = 0,
        RawRange = 1,
        PercentageRange = 2,
        SiRange = 3,
        Symbol = 4,
        Mapping = 5,
        MotorBias = 7,
        CapabilityBits  = 8,
        ValueFormat = 128,
    }

    public enum IOEvent : byte
    {
        Detatched = 0,
        Attached = 1,
        AttachedVirtual =2,
    }

    public enum IOType : ushort
    {
        Motor = 0x1,
        SystemTrainMotor = 0x2,
        Button = 0x5,
        LEDLight = 0x8,
        Voltage = 0x14,
        Current = 0x15,
        PiezoTone = 0x16,
        RGBLight = 0x17,
        ExternalTiltSensor = 0x22,
        MotionSensor = 0x23,
        VisionSensor = 0x25,
        ExternalMotorWithTacho = 0x26,
        InternalMotorWithTacho = 0x27,
        InternalTilt = 0x28,
    }
}
