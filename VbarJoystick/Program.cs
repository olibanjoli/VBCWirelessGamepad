﻿// See https://aka.ms/new-console-template for more information

using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using vJoyInterfaceWrap;

Console.WriteLine("Hello, World!");

const int VBAR_STICK_MIN = 2048 - 1600;
const int VBAR_STICK_MAX = 2048 + 1600;
const int VBAR_RANGE = VBAR_STICK_MAX - VBAR_STICK_MIN;

// [Flags]
// enum Days
// {
//     None = 0,
//     Sunday    = 0b0000001,
//     Monday    = 0b0000010,   // 2
//     Tuesday   = 0b0000100,   // 4
//     Wednesday = 0b0001000,   // 8
//     Thursday  = 0b0010000,   // 16
//     Friday    = 0b0100000,   // etc.
//     Saturday  = 0b1000000,
//     Weekend = Saturday | Sunday,
//     Weekdays = Monday | Tuesday | Wednesday | Thursday | Friday
// }

// [Flags]
// enum Days
// {
//     None        = 0,
//     Sunday      = 1,
//     Monday      = 1 << 1,   // 2
//     Tuesday     = 1 << 2,   // 4
//     Wednesday   = 1 << 3,   // 8
//     Thursday    = 1 << 4,   // 16
//     Friday      = 1 << 5,   // etc.
//     Saturday    = 1 << 6,
//     Weekend     = Saturday | Sunday,
//     Weekdays    = Monday | Tuesday | Wednesday | Thursday | Friday
// }

var PacketId = 0x8C51;
var udpClient = new UdpClient(1025);

var joystick = new vJoy();
var iReport = new vJoy.JoystickState();

if (!joystick.vJoyEnabled())
{
    Console.WriteLine("vJoy driver not enabled: Failed Getting vJoy attributes.\n");
    return;
}

Console.WriteLine("Vendor: {0}\nProduct :{1}\nVersion Number:{2}\n",
    joystick.GetvJoyManufacturerString(), joystick.GetvJoyProductString(),
    joystick.GetvJoySerialNumberString());

const int joystickId = 1;

// Get the state of the requested device
var joystickStatus = joystick.GetVJDStatus(joystickId);

switch (joystickStatus)
{
    case VjdStat.VJD_STAT_OWN:
        Console.WriteLine("vJoy Device {0} is already owned by this feeder\n", joystickId);
        break;
    case VjdStat.VJD_STAT_FREE:
        Console.WriteLine("vJoy Device {0} is free\n", joystickId);
        break;
    case VjdStat.VJD_STAT_BUSY:
        Console.WriteLine("vJoy Device {0} is already owned by another feeder\nCannot continue\n", joystickId);
        return;
    case VjdStat.VJD_STAT_MISS:
        Console.WriteLine("vJoy Device {0} is not installed or disabled\nCannot continue\n", joystickId);
        return;
    default:
        Console.WriteLine("vJoy Device {0} general error\nCannot continue\n", joystickId);
        return;
}

// Check which axes are supported
bool AxisX = joystick.GetVJDAxisExist(joystickId, HID_USAGES.HID_USAGE_X);
bool AxisY = joystick.GetVJDAxisExist(joystickId, HID_USAGES.HID_USAGE_Y);
bool AxisZ = joystick.GetVJDAxisExist(joystickId, HID_USAGES.HID_USAGE_Z);
bool AxisRX = joystick.GetVJDAxisExist(joystickId, HID_USAGES.HID_USAGE_RX);
bool AxisRZ = joystick.GetVJDAxisExist(joystickId, HID_USAGES.HID_USAGE_RZ);
// Get the number of buttons and POV Hat switchessupported by this vJoy device
int nButtons = joystick.GetVJDButtonNumber(joystickId);
int ContPovNumber = joystick.GetVJDContPovNumber(joystickId);
int DiscPovNumber = joystick.GetVJDDiscPovNumber(joystickId);

// Print results
Console.WriteLine("\nvJoy Device {0} capabilities:\n", joystickId);
Console.WriteLine("Numner of buttons\t\t{0}\n", nButtons);
Console.WriteLine("Numner of Continuous POVs\t{0}\n", ContPovNumber);
Console.WriteLine("Numner of Descrete POVs\t\t{0}\n", DiscPovNumber);
Console.WriteLine("Axis X\t\t{0}\n", AxisX ? "Yes" : "No");
Console.WriteLine("Axis Y\t\t{0}\n", AxisX ? "Yes" : "No");
Console.WriteLine("Axis Z\t\t{0}\n", AxisX ? "Yes" : "No");
Console.WriteLine("Axis Rx\t\t{0}\n", AxisRX ? "Yes" : "No");
Console.WriteLine("Axis Rz\t\t{0}\n", AxisRZ ? "Yes" : "No");

// Test if DLL matches the driver
UInt32 DllVer = 0, DrvVer = 0;
bool match = joystick.DriverMatch(ref DllVer, ref DrvVer);
if (match)
    Console.WriteLine("Version of Driver Matches DLL Version ({0:X})\n", DllVer);
else
    Console.WriteLine("Version of Driver ({0:X}) does NOT match DLL Version ({1:X})\n", DrvVer, DllVer);


// Acquire the target
if ((joystickStatus == VjdStat.VJD_STAT_OWN) ||
    ((joystickStatus == VjdStat.VJD_STAT_FREE) && (!joystick.AcquireVJD(joystickId))))
{
    Console.WriteLine("Failed to acquire vJoy device number {0}.\n", joystickId);
    return;
}
else
{
    Console.WriteLine("Acquired: vJoy device number {0}.\n", joystickId);
}

long maxval = 0;
long min = 0;
joystick.GetVJDAxisMax(joystickId, HID_USAGES.HID_USAGE_RX, ref maxval); // 32767
joystick.GetVJDAxisMin(joystickId, HID_USAGES.HID_USAGE_X, ref min);

long maxX = 0;
long maxY = 0;
long maxZ = 0;
long maxRX = 0;
long maxRY = 0;
long maxRZ = 0;
long maxSL0 = 0;
long maxSL1 = 0;
// long maxPOV = 0;
// long maxWHL = 0;

joystick.GetVJDAxisMax(joystickId, HID_USAGES.HID_USAGE_X, ref maxX);
joystick.GetVJDAxisMax(joystickId, HID_USAGES.HID_USAGE_Y, ref maxY);
joystick.GetVJDAxisMax(joystickId, HID_USAGES.HID_USAGE_Z, ref maxZ);
joystick.GetVJDAxisMax(joystickId, HID_USAGES.HID_USAGE_RX, ref maxRX);
joystick.GetVJDAxisMax(joystickId, HID_USAGES.HID_USAGE_RY, ref maxRY);
joystick.GetVJDAxisMax(joystickId, HID_USAGES.HID_USAGE_RZ, ref maxRZ);
joystick.GetVJDAxisMax(joystickId, HID_USAGES.HID_USAGE_SL0, ref maxSL0);
joystick.GetVJDAxisMax(joystickId, HID_USAGES.HID_USAGE_SL1, ref maxSL1);
// joystick.GetVJDAxisMax(joystickId, HID_USAGES.HID_USAGE_POV, ref maxPOV);
// joystick.GetVJDAxisMax(joystickId, HID_USAGES.HID_USAGE_WHL, ref maxWHL);


Console.WriteLine($"maxval {maxval}");
// uint IOC_IN = 0x80000000;
// uint IOC_VENDOR = 0x18000000;
// uint SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12;
//udpClient.Client.IOControl((int)SIO_UDP_CONNRESET, new byte[] { Convert.ToByte(false) }, null);

//var sender = new UdpClient("10.0.0.201", 1025) { DontFragment = true };


//Creates an IPEndPoint to record the IP Address and port number of the sender.
// The IPEndPoint will allow you to read datagrams sent from any source.
var remoteIpEndPoint = new IPEndPoint(IPAddress.Any, 1025);

// packet data is 28 Bytes
byte[] receiveData = new byte[100];
byte[] sendData = new byte[10];

[MethodImpl(MethodImplOptions.AggressiveInlining)]
int MapRange(int vbarValue, long maxRange)
{ 
    var value = (((vbarValue - VBAR_STICK_MIN) * maxRange) / VBAR_RANGE) + 0;

    return (int)value;
}

var testMid = MapRange(2048, 32767);

while (true)
{
    try
    {
        // Blocks until a message returns on this socket from a remote host.
        var receiveBytes = udpClient.Receive(ref remoteIpEndPoint);
        //var target = new IPEndPoint(remoteIpEndPoint.Address.Address, 1025);

        int id = receiveBytes[0] & 0xFF | (receiveBytes[1] & 0xFF) << 8;
        int version = receiveBytes[2] & 0xFF;
        int command = receiveBytes[3] & 0xFF;

        //Console.WriteLine($"received {receiveBytes.Length} id: {id} command: {command} version: {version}");

        // Check if the Header fields are valid
        if ((id != 0x8c51) || (version != 1))
        {
            //Console.WriteLine($"id / version mismatch");
            continue;
        }
        else
        {
        }


        switch (command)
        {
            case 1: // plain control data
                //Console.WriteLine("cmd 1");
                var switches = receiveBytes[4] & 0xFF | (receiveBytes[5] & 0xFF) << 8 | (receiveBytes[6] & 0xFF) << 16
                               | (receiveBytes[7] & 0xFF) << 24;
                int foo;
                //Console.WriteLine(Convert.ToString(switches, 2));

                var motorOff = (switches & 0b_0000_0000_0000_0000_0000_0000_0000_0001) != 0;
                var motorOn = (switches & 0b_0000_0000_0000_0000_0000_0000_0000_0010) != 0;
                var motorIdle = !motorOff && !motorOn;
                
                var bank1 = (switches & 0b_0000_0000_0000_0000_0000_0000_0000_0100) != 0;
                var bank2 = (switches & 0b_0000_0000_0000_0000_0000_0000_0000_1000) != 0;
                var bank3 = !bank1 && !bank2;
                
                var buddy = (switches & 0b_0000_0000_0000_0000_0000_0000_0001_0000) != 0;
                var master = !buddy;
                
                
                //Console.WriteLine($"motorOff {motorOff}   motorOn {motorOn}");
                joystick.SetBtn(motorOff, joystickId, 1);
                joystick.SetBtn(motorIdle, joystickId, 2);
                joystick.SetBtn(motorOn, joystickId, 3);
                
                var middle = (switches & 0b_0000_0000_0000_0000_0000_0000_0000_0010) == 0 &&
                    (switches & 0b_0000_0000_0000_0000_0000_0000_0000_0001) == 0;
                
              

                //joystick.SetBtn((switches & 0b_0100_0000_0000_0000_0000_0000_0000_0000) != 0, joystickId, 1);

// Print the header and UDP Data
                //Console.WriteLine($"from: {remoteIpEndPoint.Address} len: {receiveBytes.Length}");
                // System.out.print("from:"+receivePacket.getAddress()+" len:"+ receivePacket.getLength() +" ");
                // System.out.print( "id=" + id +" version=" + version +" command=" + command +" switches=" + switches);

                // main channels center value is 2048
                var ail = receiveBytes[8] & 0xFF | (receiveBytes[9] & 0xFF) << 8;
                var elev = receiveBytes[10] & 0xFF | (receiveBytes[11] & 0xFF) << 8;
                var tail = receiveBytes[12] & 0xFF | (receiveBytes[13] & 0xFF) << 8;
                var pitch = receiveBytes[14] & 0xFF | (receiveBytes[15] & 0xFF) << 8;

                joystick.SetAxis(MapRange(ail, maxX), 1, HID_USAGES.HID_USAGE_X);
                joystick.SetAxis(MapRange(elev, maxY), 1, HID_USAGES.HID_USAGE_Y);
                joystick.SetAxis(MapRange(tail, maxZ), 1, HID_USAGES.HID_USAGE_Z);
                joystick.SetAxis(MapRange(pitch, maxRX), 1, HID_USAGES.HID_USAGE_RX);
                
                //System.out.print( " ail=" + ail +" elev=" + elev +" tail=" + tail +" pitch=" + pitch);

                // aux channels center value is 2048
                var pot1 = receiveBytes[16] & 0xFF | (receiveBytes[17] & 0xFF) << 8;
                var pot2 = receiveBytes[18] & 0xFF | (receiveBytes[19] & 0xFF) << 8;
                var trim1 = receiveBytes[20] & 0xFF | (receiveBytes[21] & 0xFF) << 8;
                var trim2 = receiveBytes[22] & 0xFF | (receiveBytes[23] & 0xFF) << 8;
                var trim3 = receiveBytes[24] & 0xFF | (receiveBytes[25] & 0xFF) << 8;
                var trim4 = receiveBytes[26] & 0xFF | (receiveBytes[27] & 0xFF) << 8;
                
                joystick.SetAxis(MapRange(pot1, maxRY), 1, HID_USAGES.HID_USAGE_RY);
                joystick.SetAxis(MapRange(pot2, maxRZ), 1, HID_USAGES.HID_USAGE_RZ);
                joystick.SetAxis(MapRange(trim1, maxSL0), 1, HID_USAGES.HID_USAGE_SL0);
                joystick.SetAxis(MapRange(trim2, maxSL1), 1, HID_USAGES.HID_USAGE_SL1);
                
                break;

            case 2: // transmitter name packet
                //Console.WriteLine("cmd 2");
                var serial = receiveBytes[4] & 0xFF | (receiveBytes[5] & 0xFF) << 8 | (receiveBytes[6] & 0xFF) << 16 |
                             (receiveBytes[7] & 0xFF) << 24;

                //var name = new String(receiveBytes,9,receiveBytes[8]);
                var name = Encoding.UTF8.GetString(
                    receiveBytes[new Range(9, new Index(receiveBytes[8] + 9, fromEnd: false))]);
                //Console.WriteLine($"name: {name} serial: {serial:x8}");
                //System.out.println("Serial:"+Integer.toHexString(serial)+" Name:"+name);
// answer the packet to show that the connection is active

                sendData[0] = 0x51;
                sendData[1] = 0x8c;
                sendData[2] = 1; // Version 1
                sendData[3] = 3; // command for answer My ID
                sendData[4] = 4; // ID of Simulator detected 1=Helix
// send the data back to where it came from

                //Console.WriteLine("answering");
                //sender.Send(sendData, 10);
                var status = udpClient.Send(sendData, remoteIpEndPoint.Address.ToString(), 1026);

                Console.WriteLine($"send: {status}");
                // sendPacket.setAddress( receivePacket.getAddress() );
                // sendPacket.setPort( 1025 );
                // clientSocket.send(sendPacket);

                break;

            default:
                Console.WriteLine("unknown command");
                break;
        }
    }
    catch (Exception e)
    {
        Console.WriteLine(e.ToString());
    }
}

struct ControlData
{
    ushort id;
    char version;
    char command;
    uint switches; // 32 Switch bits
    ushort[] analog; // maximal 10 Analog Channels
}

struct Identification
{
    public ushort id;
    public char version;
    public char command;
    public UInt32 serial;
    public char len;
    public char[] name; // [41]
}

struct SimIdentification
{
    public ushort id;
    public char version;
    public char command;
    public char sim_ident;
}

//Creates a UdpClient for reading incoming data.