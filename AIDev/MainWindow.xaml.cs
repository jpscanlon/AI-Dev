using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AIDev
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool debugServerMode = true;
        Process serverProcess;

        public MainWindow()
        {
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;

            InitializeComponent();

            menuItemTestTcpConnect.IsEnabled = true;
            menuItemTestTcpDisconnect.IsEnabled = false;

            tabWorkspace.AllowDrop = true;

            //tabItemCommands.IsEnabled = false;
            //tabItemCommands.Visibility = Visibility.Collapsed;
            //gridCommands.Visibility = Visibility.Collapsed;
            //gridCommands.IsEnabled = false;
            tabItemVocab.IsEnabled = false;
            tabItemVocab.Visibility = Visibility.Collapsed;
            //gridVocab.Visibility = Visibility.Collapsed;
            //gridVocab.IsEnabled = false;
            tabItemNets.IsEnabled = false;
            tabItemNets.Visibility = Visibility.Collapsed;
            gridNets.Visibility = Visibility.Collapsed;
            gridNets.IsEnabled = false;
            tabItemNet.IsEnabled = false;
            tabItemNet.Visibility = Visibility.Collapsed;
            gridNet.Visibility = Visibility.Collapsed;
            gridNet.IsEnabled = false;
            tabItemTrain.IsEnabled = false;
            tabItemTrain.Visibility = Visibility.Collapsed;
            //gridTrain.Visibility = Visibility.Collapsed;
            //gridTrain.IsEnabled = false;

            //buttonTabCommandsClose.Visibility = Visibility.Hidden;
            buttonTabVocabClose.Visibility = Visibility.Hidden;
            buttonTabNetsClose.Visibility = Visibility.Hidden;
            buttonTabNetClose.Visibility = Visibility.Hidden;
            buttonTabTrainClose.Visibility = Visibility.Hidden;

            if (!debugServerMode)
            {
                StartServer();
                //Thread.Sleep(1000);  // Wait for server to start listening for new connection
                // requests. Useful?
            }

            //TcpConnection.SendMessage("serverconnect");  // Reset server connection.
           TcpConnect();

            menuItemTestStartServer.IsEnabled = false;
            menuItemTestTcpConnect.IsEnabled = false;
            menuItemTestTcpDisconnect.IsEnabled = true;

            //InitializeComponent();
            // Handle executed events for commands textbox.
            textBoxCommands.AddHandler(CommandManager.ExecutedEvent,
                new RoutedEventHandler(TextBoxCommands_CommandExecuted), true);

            //ViewNets();
            ViewCommands();

            //this.Top = 400;
            //this.Left = 400;
        }

        private void WindowMain_Closed(object sender, EventArgs e)
        {
            try  // Try-catch in case the server isn't running
            {
                string response = "";

                // Close server by sending command.
                //response = TcpConnection.SendMessage("closeserver");

                if (debugServerMode)
                {
                    // Close server by sending command.
                    response = TcpConnection.SendMessage("closeserver");
                    //TcpConnection.Disconnect();
                }
                else
                {
                    // Close server by closing process.
                    serverProcess.CloseMainWindow();
                    serverProcess.Close();  // Free resources associated with the server.
                }
            }
            catch
            {
            }
        }

        // If server isn't running, start the server.
        private void StartServer()
        {
            //if (TcpConnection.TestConnection() == "server connected")
            //{
            //    // Server is already running.
            //    menuItemTestTcpDisconnect.IsEnabled = true;
            //}
            //else
            {
                // Start server and connect.
                debugServerMode = false;
                try
                {
                    //serverProcess = Process.Start("C:\\Users\\User2\\Dev\\AI Dev\\AIDev Solution\\AIDevServer\\bin" + 
                    //    "\\Debug\\AIDevServer.exe");  // Start the AI Dev Server.
                    serverProcess = Process.Start(AppProperties.ServerPath);  // Start the AI Dev Server.
                    TcpConnect();
                }
                catch (Exception e)
                {
                    string result = "Exception: " + e;
                    MessageBox.Show("Start Server result: \r\n" + result);
                }

                menuItemTestStartServer.IsEnabled = false;
                menuItemTestCloseServer.IsEnabled = true;
                menuItemTestTcpConnect.IsEnabled = false;
                menuItemTestTcpDisconnect.IsEnabled = true;
                menuItemTestTcpMessage.IsEnabled = true;
            }
        }

        private string TcpConnect()
        {
            string response = TcpConnection.Connect();

            menuItemTestTcpConnect.IsEnabled = false;
            menuItemTestTcpDisconnect.IsEnabled = true;
            menuItemTestTcpMessage.IsEnabled = true;

            //MessageBox.Show("TCP Connection result: \r\n" + response);

            return response;
        }

        private void LoadLineChartData()
        {
            ////(LineSeries)chartTrainStatus.

            //((LineSeries)chartTrainStatus.Series[0]).ItemsSource =
            //    new KeyValuePair<int, float>[]{
            //    new KeyValuePair<int, float>(1, 1.0F),
            //    new KeyValuePair<int, float>(2, 0.6F),
            //    new KeyValuePair<int, float>(3, 0.45F),
            //    new KeyValuePair<int, float>(4, 0.3F),
            //    new KeyValuePair<int, float>(5, 0.28F) };
        }

        private void MenuItemFileOpenKB_Click(object sender, RoutedEventArgs e)
        {
            _ = TcpConnection.SendMessage("openkb");
            //MessageBox.Show("Server response: " + response);
        }

        private void MenuItemViewCommands_Click(object sender, RoutedEventArgs e)
        {
            ViewCommands();
        }

        private void MenuItemViewVocab_Click(object sender, RoutedEventArgs e)
        {
            // Place in first tab position.
            tabWorkspace.Items.Remove(tabItemVocab);
            tabWorkspace.Items.Insert(0, tabItemVocab);

            tabItemVocab.IsEnabled = true;
            tabItemVocab.Visibility = Visibility.Visible;
            gridVocab.Visibility = Visibility.Visible;
            gridVocab.IsEnabled = true;
            tabItemVocab.Focus();
        }

        private void MenuItemViewNets_Click(object sender, RoutedEventArgs e)
        {
            ViewNets();
        }

        private void MenuItemTestClearStream_Click(object sender, RoutedEventArgs e)
        {

            // Try to clear client stream.
            //response = TcpConnection.SendMessage("clearstream");
            string response = TcpConnection.ClearClientStream();

            MessageBox.Show("Server response: " + response);
        }

        private void MenuItemTestStartServer_Click(object sender, RoutedEventArgs e)
        {
            StartServer();
        }

        private void MenuItemTestCloseServer_Click(object sender, RoutedEventArgs e)
        {
            _ = TcpConnection.SendMessage("closeserver");

            menuItemTestStartServer.IsEnabled = true;
            menuItemTestCloseServer.IsEnabled = false;
            menuItemTestTcpConnect.IsEnabled = false;
            menuItemTestTcpDisconnect.IsEnabled = false;
            menuItemTestTcpMessage.IsEnabled = false;

            //MessageBox.Show("Server response: " + response);
        }

        private void MenuItemTestTcpConnect_Click(object sender, RoutedEventArgs e)
        {
            string response = TcpConnect();
            MessageBox.Show("TCP Connection result: \r\n" + response);
        }

        private void MenuItemTestTcpDisconnect_Click(object sender, RoutedEventArgs e)
        {
            string response = TcpConnection.Disconnect();
            menuItemTestTcpConnect.IsEnabled = true;
            menuItemTestTcpDisconnect.IsEnabled = false;
            menuItemTestTcpMessage.IsEnabled = false;
            MessageBox.Show("Server response: " + response);
        }

        private void MenuItemTestTcpMessage_Click(object sender, RoutedEventArgs e)
        {
            string response = TcpConnection.SendMessage("test message");
            MessageBox.Show("Server response: " + response);
        }

        private void MenuItemWriteToLog_Click(object sender, RoutedEventArgs e)
        {
            _ = TcpConnection.SendMessage("writetolog");
        }

        private void MenuClearLog_Click(object sender, RoutedEventArgs e)
        {
            _ = TcpConnection.SendMessage("clearlog");
        }

        private void MenuItemTestCode_Click(object sender, RoutedEventArgs e)
        {
            //ViewNet("UCAlpha");
            _ = TcpConnection.SendMessage("runcode");
            //response = TcpConnection.SendMessage("geterrorhistory");
            //UpdateTrainingPlot(response);

            //MessageBox.Show("Server response: " + response);

            //TestRecognize("nibbler");
        }

        private void MenuItemTestSQLStatement_Click(object sender, RoutedEventArgs e)
        {
            _ = TcpConnection.SendMessage("runsql");
            //MessageBox.Show("Server response: " + response);
        }

        private void MenuItemSettingsVoices_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;

            SelectVoice(menuItem.Tag.ToString());
        }

        // Selects another tab item in the Workspace. Called when a tab item is closed.
        // Needs to be fixed.
        private void SelectNextTab()
        {
            if (tabWorkspace.SelectedIndex != -1)
            {
                //tabWorkspace.SelectedIndex = 0;
                //tabWorkspace.Items.MoveCurrentToFirst();
                //tabWorkspace.Items.MoveCurrentToLast();
                tabWorkspace.Items.MoveCurrentToPrevious();
                tabWorkspace.SelectedIndex = tabWorkspace.Items.CurrentPosition;

                //tabWorkspace.SelectedIndex = tabWorkspace.Items.CurrentPosition - 1;
                //tabWorkspace.SelectedItem = tabWorkspace.FindName("tabItemNet");
                //tabWorkspace.Items.CurrentItem;
                //tabWorkspace.Items[1].Equals;

                tabWorkspace.Items.Refresh();
            }
        }

        private void ButtonTabCommandsClose_Click(object sender, RoutedEventArgs e)
        {
            //tabWorkspace.Items.RemoveAt(tabWorkspace.SelectedIndex);  // Can't be reopened after this.
            SelectNextTab();

            tabItemCommands.IsEnabled = false;
            tabItemCommands.Visibility = Visibility.Collapsed;
            gridCommands.Visibility = Visibility.Collapsed;
            gridCommands.IsEnabled = false;
        }

        private void ButtonTabVocabClose_Click(object sender, RoutedEventArgs e)
        {
            SelectNextTab();

            tabItemVocab.IsEnabled = false;
            tabItemVocab.Visibility = Visibility.Collapsed;
            gridVocab.Visibility = Visibility.Collapsed;
            gridVocab.IsEnabled = false;
        }

        private void ButtonTabNetsClose_Click(object sender, RoutedEventArgs e)
        {
            SelectNextTab();

            tabItemNets.IsEnabled = false;
            tabItemNets.Visibility = Visibility.Collapsed;
            gridNets.Visibility = Visibility.Collapsed;
            gridNets.IsEnabled = false;
        }

        private void ButtonTabNetClose_Click(object sender, RoutedEventArgs e)
        {
            tabItemTrain.IsEnabled = false;
            tabItemTrain.Visibility = Visibility.Collapsed;

            tabItemNet.IsEnabled = false;
            tabItemNet.Visibility = Visibility.Collapsed;
            gridNet.Visibility = Visibility.Collapsed;
            gridNet.IsEnabled = false;

            //SelectNextTab();

            tabWorkspace.SelectedItem = tabWorkspace.FindName("tabItemNets");
            tabWorkspace.Items.Refresh();
        }

        private void ButtonTabTrainClose_Click(object sender, RoutedEventArgs e)
        {
            tabItemTrain.IsEnabled = false;
            tabItemTrain.Visibility = Visibility.Collapsed;

            //SelectNextTab();

            tabWorkspace.SelectedItem = tabWorkspace.FindName("tabItemNet");
            tabWorkspace.Items.Refresh();
        }

        private void TabItem_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!(e.Source is TabItem tabItem))
                return;

            if (Mouse.PrimaryDevice.LeftButton == MouseButtonState.Pressed)
            {
                DragDrop.DoDragDrop(tabItem, tabItem, DragDropEffects.All);
            }
        }

        private void TabItem_Drop(object sender, DragEventArgs e)
        {
            var tabItemSource = e.Data.GetData(typeof(TabItem)) as TabItem;

            if (e.Source is TabItem tabItemTarget)  // Why does dropping make tabItemTarget often null?
            {
                if (!tabItemTarget.Equals(tabItemSource))
                {
                    var tabControl = tabItemTarget.Parent as TabControl;
                    int sourceIndex = tabControl.Items.IndexOf(tabItemSource);
                    int targetIndex = tabControl.Items.IndexOf(tabItemTarget);

                    tabControl.Items.Remove(tabItemSource);
                    tabControl.Items.Insert(targetIndex, tabItemSource);

                    tabControl.Items.Remove(tabItemTarget);
                    tabControl.Items.Insert(sourceIndex, tabItemTarget);

                    tabItemSource.Focus();
                }
            }
        }

        private void TabItemCommands_GotFocus(object sender, RoutedEventArgs e)
        {
            buttonTabCommandsClose.IsEnabled = true;
            buttonTabCommandsClose.Visibility = Visibility.Visible;
        }

        private void TabItemCommands_LostFocus(object sender, RoutedEventArgs e)
        {
            buttonTabCommandsClose.IsEnabled = false;
            buttonTabCommandsClose.Visibility = Visibility.Hidden;
        }

        private void TabItemVocab_GotFocus(object sender, RoutedEventArgs e)
        {
            buttonTabVocabClose.IsEnabled = true;
            buttonTabVocabClose.Visibility = Visibility.Visible;
        }

        private void TabItemVocab_LostFocus(object sender, RoutedEventArgs e)
        {
            buttonTabVocabClose.IsEnabled = false;
            buttonTabVocabClose.Visibility = Visibility.Hidden;
        }

        private void TabItemNets_GotFocus(object sender, RoutedEventArgs e)
        {
            buttonTabNetsClose.IsEnabled = true;
            buttonTabNetsClose.Visibility = Visibility.Visible;
        }

        private void TabItemNets_LostFocus(object sender, RoutedEventArgs e)
        {
            buttonTabNetsClose.IsEnabled = false;
            buttonTabNetsClose.Visibility = Visibility.Hidden;
        }

        private void TabItemNet_GotFocus(object sender, RoutedEventArgs e)
        {
            buttonTabNetClose.IsEnabled = true;
            buttonTabNetClose.Visibility = Visibility.Visible;
        }

        private void TabItemNet_LostFocus(object sender, RoutedEventArgs e)
        {
            buttonTabNetClose.IsEnabled = false;
            buttonTabNetClose.Visibility = Visibility.Hidden;
        }

        private void ButtonTabNetsClose_GotFocus(object sender, RoutedEventArgs e)
        {
            //listViewNets.ItemsSource = NetsList;
            //listViewNets.Items.Refresh();

            //tabItemNets.IsEnabled = true;
            //tabItemNets.Visibility = Visibility.Visible;
            //gridNets.Visibility = Visibility.Visible;
            //gridNets.IsEnabled = true;
        }

        private void TabItemTrain_GotFocus(object sender, RoutedEventArgs e)
        {
            buttonTabTrainClose.Visibility = Visibility.Visible;

            LoadLineChartData();
        }

        private void TabItemTrain_LostFocus(object sender, RoutedEventArgs e)
        {
            buttonTabTrainClose.Visibility = Visibility.Hidden;
        }

        private void ComboBoxNetType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (tabItemNet.IsInitialized == true)
            {
                if (comboBoxNetType.SelectedIndex == 1)
                {
                    labelNetConvLayers.IsEnabled = false;
                    textBoxNetConvLayers.IsEnabled = false;
                    textBoxNetConvLayers.Text = "0";
                }
                else
                {
                    if (textBoxNetConvLayers.Text == "0") textBoxNetConvLayers.Text = "1";
                    labelNetConvLayers.IsEnabled = true;
                    textBoxNetConvLayers.IsEnabled = true;
                }
            }
        }

        private void MemuItemSettingsVoicesGet_Click(object sender, RoutedEventArgs e)
        {
            GetInstalledVoices();
        }

        private void ButtonGetOutput_Click(object sender, RoutedEventArgs e)
        {
            GetOutput();
        }

        private void ButtonStopOutput_Click(object sender, RoutedEventArgs e)
        {
            StopOutput();
        }
    }

    static class AppProperties
    {
        // "\.." doesn't work with SQL Server connection string.
        //static readonly string solutionPath = System.AppDomain.CurrentDomain.BaseDirectory + @"..\..\..";

        public static string AIDevDataFolderPath = Properties.Settings.Default.AIDevFolderPath + @"\AIDev Data";
        public static string IOFolderPath = AIDevDataFolderPath + @"\IO";
        public static string MotorOutPath = IOFolderPath + @"\motor out.txt";  // for motor outputs

        public static string SolutionPath = Properties.Settings.Default.AIDevFolderPath + @"\AIDev Solution";
        //public static string SolutionPath = System.AppDomain.CurrentDomain.BaseDirectory + @"..\..\..\";
        public static string ServerPath = SolutionPath + @"\AIDevServer\bin\x64\Debug Multiple\netcoreapp3.1" +
            @"\AIDevServer.exe";
        //public static string ServerPath = Properties.Settings.Default.AIDevFolderPath +
        //    @"\AIDev Solution\AIDevServer\bin\Debug Multiple\AIDevServer.exe";

        // Installed locations:
        //public static string AIDevInstalledFolderPath = @"C:\Users\User2\Dev\AI Dev Installed";
        //public static string ServerPath = AIDevInstalledFolderPath + @"\AIDevServer.exe";
    }
}
