using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Input;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;


namespace RemoteWhiteboard
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private Point _previousContactPt;
        private Point currentContactPt;
        private double x1;
        private double y1;
        private double x2;
        private double y2;

        Socket socket;
        private Brush mCurrentColor;
        private bool mPressed;

        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse("192.168.1.118"), 51111);

        public MainPage()
        {
            this.InitializeComponent();

            mCurrentColor = new SolidColorBrush( Colors.Green );

            MyCanvas.PointerPressed += new PointerEventHandler(MyCanvas_PointerPressed);
            MyCanvas.PointerMoved += new PointerEventHandler(MyCanvas_PointerMoved);
            MyCanvas.PointerReleased += new PointerEventHandler(MyCanvas_PointerReleased);
            MyCanvas.PointerExited += new PointerEventHandler(MyCanvas_PointerReleased);
            
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                socket.Connect(remoteEP);
                if (socket.Connected)
                {
                    socket.Send(new byte[] { 0x04, Colors.Green.B, Colors.Green.G, Colors.Green.R, Colors.Green.A });
                    socket.Send(new byte[] { 0x05 });

                    ConnectionTextBlock.Text = "Connected!";
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        private void CPtest_ColorChanged(object sender, Color color)
        {
            mCurrentColor = new SolidColorBrush(color);
            socket.Send( new byte[] { 0x04, color.B, color.G, color.R, color.A } );
        }

        private void MyCanvas_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            socket.Send( new byte[] { 0x03 });

            PointerPoint pt = e.GetCurrentPoint(MyCanvas);
            mPressed = false;
            e.Handled = true;
        }

        private void MyCanvas_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (mPressed)
            {
                PointerPoint pt = e.GetCurrentPoint(MyCanvas);
                byte[] x = BitConverter.GetBytes((float)(2 * pt.Position.X));
                byte[] y = BitConverter.GetBytes((float)(2 * pt.Position.Y));
                var sendBuffer = new byte[1 + x.Length + y.Length];
                sendBuffer[0] = 0x02;
                x.CopyTo(sendBuffer, 1);
                y.CopyTo(sendBuffer, 1 + x.Length);
                socket.Send(sendBuffer);

                // Render a red line on the canvas as the pointer moves. 
                // Distance() is an application-defined function that tests
                // whether the pointer has moved far enough to justify 
                // drawing a new line.
                currentContactPt = pt.Position;
                x1 = _previousContactPt.X;
                y1 = _previousContactPt.Y;
                x2 = currentContactPt.X;
                y2 = currentContactPt.Y;

                if (Distance(x1, y1, x2, y2) > 2.0) // We need to developp this method now 
                {
                    Line line = new Line()
                    {
                        X1 = x1,
                        Y1 = y1,
                        X2 = x2,
                        Y2 = y2,
                        StrokeThickness = 4.0,
                        Stroke = mCurrentColor
                    };

                    _previousContactPt = currentContactPt;

                    // Draw the line on the canvas by adding the Line object as
                    // a child of the Canvas object.
                    MyCanvas.Children.Add(line);

                    // Pass the pointer information to the InkManager.
                    //_inkKhaled.ProcessPointerUpdate(pt);
                }
            }
        }

        private double Distance(double x1, double y1, double x2, double y2)
        {
            double d = 0;
            d = Math.Sqrt(Math.Pow((x2 - x1), 2) + Math.Pow((y2 - y1), 2));
            return d;
        }

        private void MyCanvas_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            // Get information about the pointer location.
            PointerPoint pt = e.GetCurrentPoint(MyCanvas);
            _previousContactPt = pt.Position;
            byte[] x = BitConverter.GetBytes((float)(2 * pt.Position.X));
            byte[] y = BitConverter.GetBytes((float)(2 * pt.Position.Y));
            var sendBuffer = new byte[1 + x.Length + y.Length];
            sendBuffer[0] = 0x01;
            x.CopyTo(sendBuffer, 1);
            y.CopyTo(sendBuffer, 1 + x.Length);
            socket.Send(sendBuffer);
            mPressed = true;

            e.Handled = true;
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            socket.Send(new byte[] { 0x05 });
            MyCanvas.Children.Clear();
        }

        private void ReconnectButton_Click(object sender, RoutedEventArgs e)
        {
            ConnectionTextBlock.Text = "Disconnected";
            socket.Shutdown(SocketShutdown.Both);

            socket.Connect(remoteEP);
            if (socket.Connected)
            {
                socket.Send(new byte[] { 0x04, Colors.Green.B, Colors.Green.G, Colors.Green.R, Colors.Green.A });
                ConnectionTextBlock.Text = "Connected!";
            }
        }

    }
}
