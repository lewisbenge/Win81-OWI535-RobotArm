using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Usb;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace RoboticArm
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            this.Loaded += MainPage_Loaded;
        }
     

        async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            var robotArm = await RobotArm.FindRoboticArm();
            if (robotArm != null)
            {
                await robotArm.MoveJoint(Joint.Base, Direction.Left, 500);
                await robotArm.SwitchLED(true);
                await robotArm.MoveJoint(Joint.Shoulder, Direction.Forwards, 1500);
                await robotArm.OpenCloseGripper(true);
            }

        }

     

    }

}
