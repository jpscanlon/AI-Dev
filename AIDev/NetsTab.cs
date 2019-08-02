//-------1---------2---------3---------4---------5---------6---------7---------8---------9---------1
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using System.Windows.Media.Imaging;
using OxyPlot;
using System.Windows.Media;
using System.Windows.Threading;
using System.Threading;

namespace AIDev
{
    //class TabNets : MainWindow  // "TabNets : MainWindow" declares TabNets as a subclass of MainWindow.
    partial class MainWindow
    {
        List<Net> Nets = new List<Net>();
        int currentNet = -1;
        //string currentInputImage = @"C:\Users\Admin\Data\AI Dev Visual Inputs\Datasets\EMINST\mnist_png" +
        //    @"\training\8\146.png";
        string currentInputImage = @"C:\Users\Admin\Data\AI Dev Visual Inputs\Datasets" +
            @"\EMINST - Reduced Sets\mnist_png - (1 & 1)\training\8\41.png";  // All training sets have this image.
        //EMINST - Reduced Sets\mnist_png - (1 & 1)\training\8\41.png
        //C:\Users\Admin\Data\AI Dev Visual Inputs\Datasets\EMINST - Reduced Sets\mnist_png - (1 & 1)\training\8\41.png
        DispatcherTimer timerTrainingPlot;
        int getHistoryInterval = 4;

        private void ViewNets()
        {
            LoadNetsView();

            // Place in first tab position.
            tabWorkspace.Items.Remove(tabItemNets);
            tabWorkspace.Items.Insert(0, tabItemNets);

            tabItemNets.IsEnabled = true;
            tabItemNets.Visibility = Visibility.Visible;
            gridNets.Visibility = Visibility.Visible;
            gridNets.IsEnabled = true;
            tabItemNets.Focus();
        }

        private void LoadNetsView()
        {
            string response = "";
            response = TcpConnection.SendMessage("getnets");

            Nets = new List<Net>();  // Try removing creation of a new nets list here.
            // Binding the listview again here seems to be the only way to get the listview to populate.
            listViewNets.ItemsSource = Nets;

            if (!string.IsNullOrEmpty(response))
            {
                if (response.StartsWith("commanderror") ||
                    response.StartsWith("not connected"))
                {
                    MessageBox.Show("Server response: \r\n" + response);
                }
                else if (response.StartsWith("nodata"))  // No Nets in KB
                {
                    //MessageBox.Show("Server response: \r\n" + response);
                }
                else
                {
                    LoadNetsList(response);
                }
            }

            //MessageBox.Show("Server response: \r\n" + response);

            listViewNets.Items.Refresh();
        }

        private void LoadNetsList(string netsData)
        {
            Nets.Clear();

            try
            {
                string[] nets = netsData.Split('$');

                string[] netFields = new string[8];
                int i = 0;
                while ((i < nets.Length) && (!string.IsNullOrEmpty(nets[i])))
                {
                    // There is a new line of data. Process it.
                    netFields = nets[i].Split('|');
                    Net net = new Net()
                    {
                        NetID = Convert.ToInt32(netFields[0]),
                        Name = netFields[1],
                        Type = netFields[2],
                        ActivationFunction = netFields[3],
                        NumInputs = Convert.ToInt32(netFields[4]),
                        NumOutputs = Convert.ToInt32(netFields[5]),
                        NumFCLayers = Convert.ToInt32(netFields[6]),
                        NumConvLayers = Convert.ToInt32(netFields[7])
                    };
                    Nets.Add(net);
                    i++;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Exception: " + e);
            }

            if (Nets.Count > 0)
            {
                // Enable appropriate controls.
            }
        }

        private void ListViewNets_ItemSelectionChanged()
        {
            // Enable buttonOpenNet only if an item is selected.
            if (listViewNets.SelectedItems.Count == 1)
            {
                //buttonOpenNet.Enabled = true;
                //buttonDeleteNet.Enabled = true;
            }
            else
            {
                //buttonOpenNet.Enabled = false;
                //buttonDeleteNet.Enabled = false;
            }
        }

        private void ViewNet(string netName)
        {
            currentNet = OpenNet(netName);

            if (currentNet != -1)
            {
                BitmapImage imageSource = new BitmapImage(new Uri(currentInputImage, UriKind.RelativeOrAbsolute));
                imageNetInputImage1.Source = imageSource;

                // Place in first tab position.
                //tabWorkspace.Items.Remove(tabItemNet);
                //tabWorkspace.Items.Insert(0, tabItemNet);

                tabItemNet.IsEnabled = true;
                tabItemNet.Visibility = Visibility.Visible;
                gridNet.Visibility = Visibility.Visible;
                gridNet.IsEnabled = true;
                tabItemNet.Focus();

                labelNetTrainingProgress.Visibility = Visibility.Hidden;
                buttonNetSave.IsEnabled = true;
                buttonNetDelete.IsEnabled = true;

                if (comboBoxNetType.Text == "FullyConnected")
                {
                    textBoxNetConvLayers.IsEnabled = false;
                }
                else
                {
                    textBoxNetConvLayers.IsEnabled = true;
                }
            }
        }

        private int OpenNet(string netName)
        {
            bool nameFound = false;
            int netIdx = 0;
            bool isGrayscale = false;

            string command = "opennet \"" + netName + "\"";

            string response = "";
            
            response = TcpConnection.SendMessage(command);

            if (response != "error")
            {
                if (response == "True")
                {
                    isGrayscale = true;
                }
                else
                {
                    isGrayscale = false;
                }

                textBoxNetIterationSize.Text = "1";
                checkBoxNetIsGrayscale.IsChecked = isGrayscale;

                while (!nameFound && netIdx < Nets.Count)
                {
                    if (Nets[netIdx].Name == netName)
                    {
                        nameFound = true;
                        textBoxNetName.Text = Nets[netIdx].Name;
                        comboBoxNetType.Text = Nets[netIdx].Type.ToString();
                        comboBoxNetActivationFunction.Text = Nets[netIdx].ActivationFunction;
                        textBoxNetInputs.Text = Nets[netIdx].NumInputs.ToString();
                        textBoxNetOutputs.Text = Nets[netIdx].NumOutputs.ToString();
                        textBoxNetFCLayers.Text = Nets[netIdx].NumFCLayers.ToString();
                        textBoxNetConvLayers.Text = Nets[netIdx].NumConvLayers.ToString();
                    }
                    netIdx++;
                }

                if (!nameFound) netIdx = -1;
                else netIdx = netIdx - 1;

            }
            else
            {
                netIdx = -1;
            }

            currentNet = netIdx;

            if (netIdx == -1)
            {
                MessageBox.Show("Error: Could not open net.");
            }

            return netIdx;
        }

        private void CreateNet()
        {
            string response = "";
            response = TcpConnection.SendMessage("addnet");

            if (response != "")
            {
                if (IsCommandError(response))
                {
                    MessageBox.Show("Server response: \r\n" + response);
                }
                else
                {
                    LoadNetsView();
                    ViewNet(response);  // Response is new net name.
                }
            }
            else
            {
                MessageBox.Show("Error: could not create net.\r\n");
            }
        }

        private bool IsCommandError(string serverResponse)
        {
            bool isCommandError = false;

            if (serverResponse.Length >= 12)
            {
                if (serverResponse.Substring(0, 12) == "commanderror")
                {
                    isCommandError = true;
                }
            }

            return isCommandError;
        }

        private void DeleteNet(string name)
        {
            char quote = '"';

            TcpConnection.SendMessage("deletenet " + quote + name + quote);
        }

        private void StopTraining()
        {
            string response = "";
            string command = "";

            command = "stoptraining";

            response = TcpConnection.SendMessage(command);
            buttonNetStop.IsEnabled = false;
            buttonTrainStop.IsEnabled = false;
            buttonTrainBump.IsEnabled = false;
            labelNetTrainingProgress.Visibility = Visibility.Hidden;
            ProgressBarTraining.IsIndeterminate = false; // Stop animation.
            timerTrainingPlot.IsEnabled = false;
        }

        private void ButtonNewNet_Click(object sender, RoutedEventArgs e)
        {
            CreateNet();
        }

        private void ListViewNets_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // In example, this is called outside this method. Not sure what it does or if it's necessary:
            //InitializeComponent();
            //((INotifyCollectionChanged)listViewNets.Items).CollectionChanged += ListViewNets_CollectionChanged;

            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                // scroll the new item into view
                listViewNets.ScrollIntoView(e.NewItems[0]);
            }
        }

        private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Net netItem = (Net)listViewNets.Items[listViewNets.SelectedIndex];

            ViewNet(netItem.Name);
        }

        private void ListViewNets_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void TextBoxNetName_TextChanged(object sender, TextChangedEventArgs e)
        {
            // This would be used if doing automatic database updating for updating the nets collection when user makes
            // a change.
        }

        private void ButtonNetSave_Click(object sender, RoutedEventArgs e)
        {
            string response = "";
            string command = "";
            string descriptionArguments = "";
            string layerArguments = "";

            // Replace this with field validation on text change.
            if (comboBoxNetType.Text == "Convolutional" && Convert.ToInt32(textBoxNetConvLayers.Text) < 1)
            {
                textBoxNetConvLayers.Text = "1";
            }

            descriptionArguments = "\"" + Nets[currentNet].Name + "\" \"" +
                textBoxNetName.Text + "\" \"" + comboBoxNetType.Text + "\" \"" +
                comboBoxNetActivationFunction.Text + "\" \"" + 
                Convert.ToString(checkBoxNetIsGrayscale.IsChecked) + "\"";
            layerArguments = textBoxNetInputs.Text + " " + textBoxNetOutputs.Text + " " + 
                textBoxNetFCLayers.Text + " " + textBoxNetConvLayers.Text;

            if (comboBoxNetType.Text != Nets[currentNet].Type ||
                textBoxNetInputs.Text != Convert.ToString(Nets[currentNet].NumInputs) ||
                textBoxNetOutputs.Text != Convert.ToString(Nets[currentNet].NumOutputs) ||
                textBoxNetFCLayers.Text != Convert.ToString(Nets[currentNet].NumFCLayers) ||
                textBoxNetConvLayers.Text != Convert.ToString(Nets[currentNet].NumConvLayers))
            {
                // Net structure needs to be changed for the save.
                command = "updatewholenet " + descriptionArguments + " " + layerArguments;
            }
            else
            {
                command = "updatenet " + descriptionArguments;
            }

            response = TcpConnection.SendMessage(command);

            LoadNetsView();
            ViewNet(textBoxNetName.Text);

            MessageBox.Show("Server response: \r\n" + response);
        }

        private void ButtonNetDelete_Click(object sender, RoutedEventArgs e)
        {
            DeleteNet(Nets[currentNet].Name);

            SelectNextTab();

            tabItemNet.IsEnabled = false;
            tabItemNet.Visibility = Visibility.Collapsed;
            gridNet.Visibility = Visibility.Collapsed;
            gridNet.IsEnabled = false;

            ViewNets();
        }

        private void ButtonNetChangeImage_Click(object sender, RoutedEventArgs e)
        {
            string initialDirectory = @"C:\Users\Admin\Data\AI Dev Visual Inputs\Datasets\EMINST\mnist_png\";
            var fileContent = string.Empty;
            const bool ok = true;

            switch (comboBoxNetDataset.Text)
            {
                case "1 Item":
                    initialDirectory = @"C:\Users\Admin\Data\AI Dev Visual Inputs\Datasets\EMINST - Reduced Sets\mnist_png - (1 & 1)\";
                    break;
                case "2 Items":
                    initialDirectory = @"C:\Users\Admin\Data\AI Dev Visual Inputs\Datasets\EMINST - Reduced Sets\mnist_png - (1 & 2)\";
                    break;
                case "0.2 Percent":
                    initialDirectory = @"C:\Users\Admin\Data\AI Dev Visual Inputs\Datasets\EMINST - Reduced Sets\mnist_png - 0.1 & 0.2 Percent\";
                    break;
                case "2.0 Percent":
                    initialDirectory = @"C:\Users\Admin\Data\AI Dev Visual Inputs\Datasets\EMINST - Reduced Sets\mnist_png - 1 & 2 Percent\";
                    break;
                case "10.0 Percent":
                    initialDirectory = @"C:\Users\Admin\Data\AI Dev Visual Inputs\Datasets\EMINST - Reduced Sets\mnist_png - 10 Percent\";
                    break;
            }

            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                InitialDirectory = initialDirectory,
                FilterIndex = 3,
                RestoreDirectory = true
            };

            if (openFileDialog.ShowDialog() == ok)
            {
                //Get the path of specified file
                currentInputImage = openFileDialog.FileName;

                // Get target group number from file path.

                BitmapImage imageSource = new BitmapImage(new Uri(currentInputImage, UriKind.RelativeOrAbsolute));
                imageNetInputImage1.Source = imageSource;
            }
        }

        private void ButtonNetRun_Click(object sender, RoutedEventArgs e)
        {
            string response = "";
            string command = "";

            command = "runnet \"" + currentInputImage + "\"";

            response = TcpConnection.SendMessage(command);

            //MessageBox.Show("Server response: \r\n" + response);
            MessageBox.Show(response);
        }

        private void ButtonNetTrain_Click(object sender, RoutedEventArgs e)
        {
            string response = "";
            string command = "";

            switch (comboBoxNetDataset.Text)
            {
                case "1 Item":
                    getHistoryInterval = 3;
                    break;
                case "2 Items":
                    getHistoryInterval = 3;
                    break;
                case "0.2 Percent":
                    getHistoryInterval = 3;
                    break;
                case "2.0 Percent":
                    getHistoryInterval = 5;
                    break;
                case "10.0 Percent":
                    getHistoryInterval = 8;
                    break;
            }

            command = "trainnet";
            string args = '"' + comboBoxNetDataset.Text + "\" \"" + textBoxNetIterationSize.Text + '"';
            command = command + " " + args;

            response = TcpConnection.SendMessage(command);

            if (response == "training started")
            {
                labelNetTrainingProgress.Visibility = Visibility.Visible;
                ProgressBarTraining.IsIndeterminate = true;  // Start animation.
                buttonNetStop.IsEnabled = true;

                OpenTraining();
            }
        }

        private void OpenTraining()
        {
            // Using DispatcherTimer (best for WPF).
            timerTrainingPlot = new DispatcherTimer();
            timerTrainingPlot.Tick += new EventHandler(OnTimedEvent);
            timerTrainingPlot.Interval = new TimeSpan(0, 0, getHistoryInterval);
            timerTrainingPlot.Start();

            ViewModels.MainViewModel.Points.Clear();
            oxyPlotTrainingError.Axes[1].Maximum = 1;
            //ViewModels.MainViewModel.
            oxyPlotTrainingError.Title = "Training - " + Nets[currentNet].Name;
            //oxyPlotTrainingError.InvalidatePlot(true);

            ViewTraining();
            buttonTrainStop.IsEnabled = true;
            buttonTrainBump.IsEnabled = true;
        }

        private void OnTimedEvent(object sender, EventArgs e)
        {
            string response = "";
            string dataPointSize;
            response = TcpConnection.SendMessage("geterrorhistory");
            if (response == "training stopped" || response == "training finished")
            {
                timerTrainingPlot.Stop();
                buttonNetStop.IsEnabled = false;
                buttonTrainStop.IsEnabled = false;
                buttonTrainBump.IsEnabled = false;
                labelNetTrainingProgress.Visibility = Visibility.Hidden;
                ProgressBarTraining.IsIndeterminate = false; // Stop animation.
            }
            else
            {
                if (response == "empty")
                {
                    response = "";
                }

                if (response != "")
                {
                    dataPointSize = response.Substring(0, 2);
                    if (dataPointSize != "01" && dataPointSize != "10")
                    {
                        // Response is not in correct format.
                        timerTrainingPlot.Stop();
                        buttonNetStop.IsEnabled = false;
                        buttonTrainStop.IsEnabled = false;
                        buttonTrainBump.IsEnabled = false;
                        labelNetTrainingProgress.Visibility = Visibility.Hidden;
                        ProgressBarTraining.IsIndeterminate = false; // Stop animation.
                    }
                    else
                    {
                        if (dataPointSize == "10")
                        {
                            // Training history contains every 10th epoch.
                            oxyPlotTrainingError.Axes[0].Title = "Epoch x10";
                        }
                        response = response.Substring(3, response.Length - 3);
                        UpdateTrainingPlot(response);
                        //TestUpdateTrainingPlot(response);
                    }
                }
            }
        }

        private void TestUpdateTrainingPlot(string trainingErrorPoints)
        {
            ViewModels.MainViewModel.Points.Clear();
            oxyPlotTrainingError.InvalidatePlot(true);

            ViewModels.MainViewModel.Points.Add(new DataPoint(1, .9));
            ViewModels.MainViewModel.Points.Add(new DataPoint(2, .7));
            ViewModels.MainViewModel.Points.Add(new DataPoint(3, .6));
            ViewModels.MainViewModel.Points.Add(new DataPoint(4, .56));
            ViewModels.MainViewModel.Points.Add(new DataPoint(5, .52));
            ViewModels.MainViewModel.Points.Add(new DataPoint(6, .45));
            ViewModels.MainViewModel.Points.Add(new DataPoint(7, .42));
            ViewModels.MainViewModel.Points.Add(new DataPoint(8, .40));
            ViewModels.MainViewModel.Points.Add(new DataPoint(9, .39));
            ViewModels.MainViewModel.Points.Add(new DataPoint(10, .38));

            TogglePlotUpdateIndicator();
        }

        private void UpdateTrainingPlot(string trainingErrorPoints)
        {
            string[] errorPoints;
            float error;

            trainingErrorPoints = trainingErrorPoints.Trim();
            textBoxTrainUpdateErrorString.Text = trainingErrorPoints;
            textBoxTrainUpdateErrorString.ScrollToEnd();

            if (trainingErrorPoints != "" && trainingErrorPoints != "empty")
            {
                ViewModels.MainViewModel.Points.Clear();
                oxyPlotTrainingError.InvalidatePlot(true);

                errorPoints = trainingErrorPoints.Split('|');

                //oxyPlotTrainingError.ResetAllAxes();
                oxyPlotTrainingError.Axes[0].Maximum = errorPoints.Length;

                if (errorPoints.Length == 1)
                {
                    oxyPlotTrainingError.Axes[1].Maximum = Math.Ceiling(Convert.ToSingle(errorPoints[0]));
                    //oxyPlotTrainingError.InvalidatePlot(true);
                }

                int epoch = 1;
                while (epoch <= errorPoints.Length)
                {
                    error = Convert.ToSingle(errorPoints[epoch - 1]);

                    if (error > oxyPlotTrainingError.Axes[1].Maximum)
                    {
                        oxyPlotTrainingError.Axes[1].Maximum = Math.Ceiling(error) + 1;
                    }

                    ViewModels.MainViewModel.Points.Add(new DataPoint(epoch, error));
                    epoch++;
                }
            }

            TogglePlotUpdateIndicator();
        }

        private void TogglePlotUpdateIndicator()
        {
            const string blueBrush = "#FF0000FF";
            const string limeBrush = "#FF00FF00";
            var converter = new BrushConverter();

            if (Convert.ToString(labelPlotUpdate.Background) == blueBrush)
            {
                labelPlotUpdate.Background = (Brush)converter.ConvertFromString(limeBrush);
            }
            else
            {
                labelPlotUpdate.Background = (Brush)converter.ConvertFromString(blueBrush);
            }
        }

        private void ViewTraining()
        {
            tabItemTrain.IsEnabled = true;
            tabItemTrain.Visibility = Visibility.Visible;
            tabItemTrain.Focus();
            oxyPlotTrainingError.Focus();
        }

        private void ButtonNetReinitialize_Click(object sender, RoutedEventArgs e)
        {
            string response = "";
            string command = "";

            command = "reinitialize";

            response = TcpConnection.SendMessage(command);

            MessageBox.Show("Server response: \r\n" + response);
        }

        private void ButtonNetStop_Click(object sender, RoutedEventArgs e)
        {
            StopTraining();
            tabItemNet.Focus();
        }

        private void ButtonTrainBump_Click(object sender, RoutedEventArgs e)
        {
            string response = "";
            string command = "";

            command = "bump";

            response = TcpConnection.SendMessage(command);
        }

        private void ButtonTrainStop_Click(object sender, RoutedEventArgs e)
        {
            StopTraining();
            tabItemTrain.Focus();
        }
    }

    // Holds the basic property values of a net.
    public class Net
    {
        public int NetID { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string ActivationFunction { get; set; }
        public int NumInputs { get; set; }
        public int NumOutputs { get; set; }
        public int NumFCLayers { get; set; }
        public int NumConvLayers { get; set; }
        public bool IsGrayscale { get; set; }
    }
}
