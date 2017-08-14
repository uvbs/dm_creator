using Alpnames_bot.Helper.ThreadingHelper;
using Alpnames_bot.Helper.WebRequestHelper;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Schedulers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Alpnames_bot
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DataTable dtRecords;
        private List<string> lstEmails;
        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        internal static Mutex mut = new Mutex();

        public MainWindow()
        {

            

            InitializeComponent();
        }

        #region Title Bar Operations
        private void titleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.ChangedButton == MouseButton.Left)
                {
                    if (e.ClickCount == 2)
                    {
                        AdjustWindowSize();
                    }
                    else
                    {
                        Application.Current.MainWindow.DragMove();
                    }
                }
            }
            catch (Exception ex)
            {

                MessageBox.Show(string.Format("Exception occured: {0}", ex.Message));
            }
        }

        /// <summary>
        /// Adjusts the WindowSize to correct parameters when Maximize button is clicked
        /// </summary>
        private void AdjustWindowSize()
        {
            try
            {
                this.WindowState = WindowState.Normal;
                //if (this.WindowState == WindowState.Maximized)
                //{
                //    this.WindowState = WindowState.Normal;

                //}
                //else
                //{
                //    this.WindowState = WindowState.Maximized;

                //}
            }
            catch (Exception ex)
            {

                MessageBox.Show(string.Format("Exception occured: {0}", ex.Message));
            }

        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void btnMaximize_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AdjustWindowSize();
            }
            catch (Exception ex)
            {

                MessageBox.Show(string.Format("Exception occured: {0}", ex.Message));
            }
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
        #endregion

        #region Grid operations
        private void btnImport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                BindGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Exception occured: {0}", ex.Message));
            }
        }

        private void BindGrid()
        {
            dataGrid.HeadersVisibility = DataGridHeadersVisibility.Column;
            Microsoft.Win32.OpenFileDialog openFileDialog1 = new Microsoft.Win32.OpenFileDialog();
            openFileDialog1.Filter = "csv files (*.csv)|*.csv";
            Nullable<bool> dialogResult = openFileDialog1.ShowDialog();
            if (dialogResult == true)
            {
                if (!string.IsNullOrWhiteSpace(openFileDialog1.FileName))
                {
                    var reader = ReadAsLines(openFileDialog1.FileName);

                    var data = new DataTable();

                    //this assume the first record is filled with the column names
                    //var headers = reader.First().Split(',');
                    var headers = new string[] { "domain", "dns1", "dns2",  "proxy", "status", "sessionId" };
                    foreach (var header in headers)
                    {
                        data.Columns.Add(new DataColumn(header, typeof(string)));
                    }

                    var records = reader.Skip(0);
                    foreach (var record in records)
                    {
                        string[] columnVals = record.Split('|');
                        DataRow dr = data.NewRow();
                        int columnIndex = 0;

                        int index = -1;
                        foreach (string str in columnVals)
                        {

                            index++;
                            if (data.Columns.Count > columnIndex)
                            {
                                if (!string.IsNullOrWhiteSpace(str))
                                {
                                    dr[columnIndex] = str.Trim();
                                }
                                columnIndex++;
                            }
                        }
                        data.Rows.Add(dr);
                    }

                    dtRecords = data;
                    dataGrid.ItemsSource = data.DefaultView;
                    dataGrid.Columns[5].Visibility = Visibility.Collapsed;
                }
            }
        }

        static IEnumerable<string> ReadAsLines(string filename)
        {
            using (StreamReader reader = new StreamReader(filename))
                while (!reader.EndOfStream)
                    yield return reader.ReadLine();
        }

        private void MenuItem_SelectAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dtRecords == null || dtRecords.Rows.Count == 0)
                    return;
                foreach (DataRow dr in dtRecords.Rows)
                {
                    dr["select"] = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Exception occured: {0}\nPlease try again", ex.Message));

            }

        }
        private void MenuItem_SelectNone_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dtRecords == null || dtRecords.Rows.Count == 0)
                    return;
                foreach (DataRow dr in dtRecords.Rows)
                {
                    dr["select"] = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Exception occured: {0}\nPlease try again", ex.Message));

            }
        }
        private void MenuItem_SelectHighlighted_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dtRecords == null || dtRecords.Rows.Count == 0)
                    return;
                var items = dataGrid.SelectedItems;
                foreach (DataRowView item in items)
                {
                    item["select"] = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Exception occured: {0}\nPlease try again", ex.Message));

            }
        }
        #endregion

        private void PreviewTextInput(object sender, TextCompositionEventArgs e)
        {

            e.Handled = !IsTextAllowed(e.Text);

        }

        private bool IsTextAllowed(string text)
        {
            Regex regex = new Regex("^\\d+$"); //regex that matches disallowed text
            return regex.IsMatch(text);
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            CreateEmails();

            //CheckForAvailability(); // http://www.freenom.com/en/index.html


        }

        private void CheckForAvailability()
        {
            int threadCount = 0;
            bool isOperationCancelled = false;
            try
            {

                TaskScheduler taskScheduler = TaskScheduler.Current;
                List<Task> taskList = new List<Task>(dtRecords.Rows.Count);
                string threadcnt = txtThreads.Text;
                if (!int.TryParse(txtThreads.Text, out threadCount))
                {
                    MessageBox.Show("Please enter valid thread count.");
                }
                cancellationTokenSource = new CancellationTokenSource();

                QueuedTaskScheduler qts = new QueuedTaskScheduler(TaskScheduler.Default, threadCount);
                TaskScheduler pri0 = qts.ActivateNewQueue(priority: 0);

                int i = 0;
                List<Task<ThreadResult>> lst = new List<Task<ThreadResult>>();
                for (i = 0; i < dtRecords.Rows.Count; i++)
                {

                    string domain = !string.IsNullOrWhiteSpace(Convert.ToString(dtRecords.Rows[i]["domain"])) ? Convert.ToString(dtRecords.Rows[i]["domain"]) :
                        Convert.ToString(dtRecords.Rows[i][1]);
                    string dns1 = !string.IsNullOrWhiteSpace(Convert.ToString(dtRecords.Rows[i]["dns1"])) ? Convert.ToString(dtRecords.Rows[i]["dns1"]) :
                        Convert.ToString(dtRecords.Rows[i][2]);
                    string dns2 = !string.IsNullOrWhiteSpace(Convert.ToString(dtRecords.Rows[i]["dns2"])) ? Convert.ToString(dtRecords.Rows[i]["dns2"]) :
                        Convert.ToString(dtRecords.Rows[i][3]);
                    string proxy = !string.IsNullOrWhiteSpace(Convert.ToString(dtRecords.Rows[i]["proxy"])) ? Convert.ToString(dtRecords.Rows[i]["proxy"]) :
                        Convert.ToString(dtRecords.Rows[i][4]);

                    int index = i;
                    if (i % 9 == 0)
                    {
                        CreateWebRequest(dtRecords, lstEmails, index, domain, dns1, dns2, proxy, 
                         cancellationTokenSource.Token);
                    }
                    else
                    {
                        Task.Factory.StartNew<ThreadResult>(() => CreateWebRequest(dtRecords, lstEmails, index, domain, dns1, dns2, proxy, 
                             cancellationTokenSource.Token), CancellationToken.None, TaskCreationOptions.None, pri0);
                    }
                }


            }
            catch (OperationCanceledException)
            {
                isOperationCancelled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Exception occured: {0}\nPlease try again", ex.Message));
            }
            finally
            {
                if (isOperationCancelled)
                {
                    //MessageBox.Show("Operation Cancelled.");
                }
            }
        }

        private List<Task> taskList = new List<Task>();

        private void CreateEmails()
        {
            int threadCount = 0;
            bool isOperationCancelled = false;
            try
            {

                TaskScheduler taskScheduler = TaskScheduler.Current;
                List<Task> taskList = new List<Task>(dtRecords.Rows.Count);
                string threadcnt = txtThreads.Text;
                if (!int.TryParse(txtThreads.Text, out threadCount))
                {
                    MessageBox.Show("Please enter valid thread count.");
                }
                cancellationTokenSource = new CancellationTokenSource();

                QueuedTaskScheduler qts = new QueuedTaskScheduler(TaskScheduler.Default, threadCount);
                TaskScheduler pri0 = qts.ActivateNewQueue(priority: 0);

                int i = 0;
                List<Task<ThreadResult>> lst = new List<Task<ThreadResult>>();
                for (i = 0; i < dtRecords.Rows.Count; i=i+9)
                {
                    CreateParallelRequests(pri0, i);
                }

                
            }
            catch (OperationCanceledException)
            {
                isOperationCancelled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Exception occured: {0}\nPlease try again", ex.Message));
            }
            finally
            {
                if (isOperationCancelled)
                {
                    //MessageBox.Show("Operation Cancelled.");
                }
            }
        }

        private void CreateParallelRequests(TaskScheduler pri0, int i)
        {
            string domain = !string.IsNullOrWhiteSpace(Convert.ToString(dtRecords.Rows[i]["domain"])) ? Convert.ToString(dtRecords.Rows[i]["domain"]) :
                Convert.ToString(dtRecords.Rows[i][1]);
            string dns1 = !string.IsNullOrWhiteSpace(Convert.ToString(dtRecords.Rows[i]["dns1"])) ? Convert.ToString(dtRecords.Rows[i]["dns1"]) :
                Convert.ToString(dtRecords.Rows[i][2]);
            string dns2 = !string.IsNullOrWhiteSpace(Convert.ToString(dtRecords.Rows[i]["dns2"])) ? Convert.ToString(dtRecords.Rows[i]["dns2"]) :
                Convert.ToString(dtRecords.Rows[i][3]);
            string proxy = !string.IsNullOrWhiteSpace(Convert.ToString(dtRecords.Rows[i]["proxy"])) ? Convert.ToString(dtRecords.Rows[i]["proxy"]) :
                Convert.ToString(dtRecords.Rows[i][4]);

            int index = i;

            Task.Factory.StartNew<ThreadResult>(() => CreateWebRequest(dtRecords,lstEmails, index, domain, dns1, dns2, proxy, 
                 cancellationTokenSource.Token), CancellationToken.None, TaskCreationOptions.None, pri0).ContinueWith((t) => CreateParallelRequestsContinued(pri0, i));
            
        }

        private void CreateParallelRequests_(TaskScheduler pri0, int i)
        {
            string domain = !string.IsNullOrWhiteSpace(Convert.ToString(dtRecords.Rows[i]["domain"])) ? Convert.ToString(dtRecords.Rows[i]["domain"]) :
                Convert.ToString(dtRecords.Rows[i][1]);
            string dns1 = !string.IsNullOrWhiteSpace(Convert.ToString(dtRecords.Rows[i]["dns1"])) ? Convert.ToString(dtRecords.Rows[i]["dns1"]) :
                Convert.ToString(dtRecords.Rows[i][2]);
            string dns2 = !string.IsNullOrWhiteSpace(Convert.ToString(dtRecords.Rows[i]["dns2"])) ? Convert.ToString(dtRecords.Rows[i]["dns2"]) :
                Convert.ToString(dtRecords.Rows[i][3]);
            string proxy = !string.IsNullOrWhiteSpace(Convert.ToString(dtRecords.Rows[i]["proxy"])) ? Convert.ToString(dtRecords.Rows[i]["proxy"]) :
                Convert.ToString(dtRecords.Rows[i][4]);

            int index = i;

            Task.Factory.StartNew<ThreadResult>(() => CreateWebRequest(dtRecords,lstEmails, index, domain, dns1, dns2, proxy, 
                 cancellationTokenSource.Token), CancellationToken.None, TaskCreationOptions.None, pri0);
            
        }

        private void CreateParallelRequestsContinued(TaskScheduler pri0, int i)
        {
            for(int  j = i+1; j < i+9 && j < dtRecords.Rows.Count; j++)
            {
                CreateParallelRequests_(pri0, j);
            }
        }

        private ThreadResult CreateWebRequest(DataTable dtRecords,List<string> lstEmails, int index, string domain, string dns1, string dns2, string proxy, 
            CancellationToken cancellationToken)
        {
            string outSessionId = string.Empty;
            ThreadResult tr = new ThreadResult
            {
                index = index,
                sessionId = string.Empty
            };
            try
            {
                if (cancellationToken.IsCancellationRequested)
                    cancellationToken.ThrowIfCancellationRequested();

                //work starts here
                lock (dtRecords)
                {
                    dtRecords.Rows[index]["status"] = "working...";
                }
                DomainCreationRequest domainCreationRequest = new DomainCreationRequest(dtRecords,lstEmails, index, domain, dns1, dns2, proxy, cancellationToken);

                domainCreationRequest.MakeRequests();
            }
            catch (OperationCanceledException)
            {
                lock (dtRecords)
                {
                    dtRecords.Rows[index]["status"] = "Cancelled...";
                }
            }
            catch (Exception ex)
            {
                throw;
            }

            return tr;
        }

        //private ThreadResult CreateWebRequest(DataTable dtRecords, string username, string password, int index, string proxyIpParam, string domain, string ns1, string ns2, string ns3,
        //    string searchString, CancellationToken cancellationToken)
        //{
        //    ThreadResult tr = new ThreadResult
        //    {
        //        index = index,
        //    };
        //    try
        //    {
        //        if (cancellationToken.IsCancellationRequested)
        //            cancellationToken.ThrowIfCancellationRequested();

        //        //work start here
        //        dtRecords.Rows[index]["status"] = "working...";
        //        AlpWebRequest alpWebRequest = new AlpWebRequest(dtRecords, username, password, index, proxyIpParam, domain, ns1, ns2, ns3, searchString, cancellationToken);

        //        alpWebRequest.MakeRequests();
        //    }
        //    catch (OperationCanceledException)
        //    {
        //        dtRecords.Rows[index]["status"] = "Cancelled...";

        //    }
        //    catch (Exception ex)
        //    {
        //        throw;
        //    }

        //    return tr;

        //}

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            cancellationTokenSource.Cancel();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            
        }

        private void btnExportReport_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog saveFileDialog1 = new Microsoft.Win32.SaveFileDialog();
            saveFileDialog1.Filter = "csv files (*.csv)|*.csv";
            Nullable<bool> dialogResult = saveFileDialog1.ShowDialog();
            if (dialogResult == true)
            {
                if (!string.IsNullOrWhiteSpace(saveFileDialog1.FileName))
                {
                    string filePath = saveFileDialog1.FileName;
                    StringBuilder fileContent = new StringBuilder();

                    fileContent.Append("Email ID");

                    foreach (DataColumn col in dtRecords.Columns)
                    {
                        if (col.ColumnName.Equals("status", StringComparison.OrdinalIgnoreCase) &&
                            col.ColumnName.Equals("sessionId", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                        fileContent.Append(col.ToString() + ",");
                    }

                    fileContent.Replace(",", System.Environment.NewLine, fileContent.Length - 1, 1);


                    int rowIndex = 0;
                    foreach (DataRow dr in dtRecords.Rows)
                    {
                        fileContent.Append("\"" + lstEmails[rowIndex].ToString() + "\",");
                        foreach (var column in dr.ItemArray)
                        {
                            fileContent.Append("\"" + column.ToString() + "\",");
                        }

                        fileContent.Replace(",", System.Environment.NewLine, fileContent.Length - 1, 1);
                        rowIndex++;
                    }

                    System.IO.File.WriteAllText(filePath, fileContent.ToString());
                }
            }
        }
    }
}
