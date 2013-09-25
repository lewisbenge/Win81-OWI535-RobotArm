using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Usb;

namespace RoboticArm
{
    public class RobotArm
    {
        const UInt32 vid = 0x1267;
        const UInt32 pid = 0x0000;

        private const int GripperDelay = 1000;

        private UsbDevice _usbDevice;
        private bool _isLightOn;
        private RobotArm(UsbDevice usbDevice)
        {
            _usbDevice = usbDevice;
        }

        /// <summary>
        /// This method should be called on the UI thread so the permission pop-up can appear if required.
        /// </summary>
        /// <returns></returns>
        public static async Task<RobotArm> FindRoboticArm()
        {
            string aqs = UsbDevice.GetDeviceSelector(vid,pid);
            var finder = await DeviceInformation.FindAllAsync(aqs);
            if (finder != null && finder.Any())
            {
                var usb = await UsbDevice.FromIdAsync(finder.First().Id);
                return new RobotArm(usb);
            }

            return null;
        }

        /// <summary>
        /// Opens or closes the gripper
        /// </summary>
        /// <param name="openClose">true: open false: close</param>
        /// <returns></returns>
        public async Task OpenCloseGripper(bool openClose)
        {
            if (openClose)
            {
                Debug.WriteLine("Gripper: open");
                await SendCommand(0x02, 0x00, GripperDelay);
            }
            else
            {
                Debug.WriteLine("Gripper: close");
                await SendCommand(0x01, 0x00, GripperDelay);
            }
        } 

        /// <summary>
        /// Switches the LED on or off
        /// </summary>
        /// <param name="onOff">true: on false: off</param>
        /// <returns></returns>
        public Task SwitchLED(bool onOff)
        {
            _isLightOn = onOff;
            return SendControl(0x00, 0x00, LedCommand(_isLightOn));
        }

        /// <summary>
        /// Move an individual motor
        /// </summary>
        /// <param name="joint"></param>
        /// <param name="direction"></param>
        /// <param name="period">In milliseconds</param>
        /// <returns></returns>
        public async Task MoveJoint(Joint joint, Direction direction, int period)
        /* First byte:
           Wrist forwards == 0x04          Wrist backwards == 0x08
           Elbow forwards == 0x10          Elbow backwards == 0x20
           Shoulder forwards == 0x40       Shoulder backwards == 0x80
   
         Second byte:
           Base rotate right == 0x01  Base rotate left == 0x02
        */
        {
            if (period < 0)
            {
                Debug.WriteLine("Turn period cannot be negative");
                period = 0;
            }

            var cmd1 = 0x00;
            var cmd2 = 0x00;

            switch (joint)
            {
                case Joint.Base:
                    switch (direction)
                    {
                        case Direction.Right:
                            cmd2 = 0x01;
                            break;
                        case Direction.Left:
                            cmd2= 0x02;
                            break;
                        default:
                            throw new InvalidOperationException("Joint cannot be moved in that direction");
                    }
                    break;
                case Joint.Shoulder:
                    switch (direction)
                    {
                        case Direction.Backwards:
                            cmd1 = 0x80;
                            break;
                        case Direction.Forwards:
                            cmd1 = 0x40;
                            break;
                        default:
                            throw new InvalidOperationException("Joint cannot be moved in that direction");
                    }
                    break;
                case Joint.Elbow:
                    switch (direction)
                    {
                        case Direction.Backwards:
                            cmd1 = 0x20;
                            break;
                        case Direction.Forwards:
                            cmd1 = 0x10;
                            break;
                        default:
                            throw new InvalidOperationException("Joint cannot be moved in that direction");
                    }
                    break;
                case Joint.Wrist:
                    switch (direction)
                    {
                        case Direction.Backwards:
                            cmd1 = 0x08;
                            break;
                        case Direction.Forwards:
                            cmd1 = 0x04;
                            break;
                        default:
                            throw new InvalidOperationException("Joint cannot be moved in that direction");
                    }
                    break;
            }

            await SendCommand(cmd1, cmd2, period);
        }  


        private int LedCommand(bool isLightOn)
        {
            return (isLightOn) ? 0x01 : 0x00;
        }
        private async Task SendCommand(int cmd1, int cmd2, int period)
        {

            if (_usbDevice != null)
            {
                var cmd3=  LedCommand(_isLightOn);

                if (!await SendControl(cmd1, cmd2, cmd3))
                    throw new InvalidOperationException("An invalid command was sent to the device");
                
                await Task.Delay(period);

                if (!await SendControl(0, 0, cmd3))
                    throw new InvalidOperationException("An invalid command was sent to the device");
                
            }
        }  

        public async Task<bool> SendControl(int cmd1, int cmd2, int cmd3)
        {
            var bytes = new[] { Convert.ToByte(cmd1), Convert.ToByte(cmd2), Convert.ToByte(cmd3) };
            var buffer = bytes.AsBuffer();


            var setupPacket = new UsbSetupPacket();
            var requestType = new UsbControlRequestType();
            requestType.ControlTransferType = UsbControlTransferType.Vendor;
            requestType.Direction = UsbTransferDirection.Out;
            requestType.Recipient = UsbControlRecipient.Device;
            setupPacket.RequestType = requestType;
            setupPacket.Request = 6;
            setupPacket.Value = 0x0100;
            setupPacket.Length = buffer.Length;
            setupPacket.Index = 0;

            var transferred = await _usbDevice.SendControlOutTransferAsync(setupPacket, buffer);
            return (transferred == buffer.Length);
            

        }

        public void Dispose()
        {
            _usbDevice.Dispose();
            _usbDevice = null;
        }

        ~RobotArm()
        {
            _usbDevice.Dispose();
        }

    }

    public enum Direction
    {
        Left,
        Right,
        Forwards,
        Backwards
    }

    public enum Joint
    {
        Base,
        Shoulder,
        Elbow,
        Wrist
    }

    
}
