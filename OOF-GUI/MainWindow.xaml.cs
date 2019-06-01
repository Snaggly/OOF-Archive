using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using OOF_Packer;
using System.IO;
using Microsoft.Win32;
using System.Threading;
using System.ComponentModel;
using RijndaelCryptography;
using System.Windows.Controls;

namespace OOF_GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private List<FileData> FileDatas = new List<FileData>();
        private string loadedOOF;
        private FileUnpacker unpacker;
        private Progress progressBar;
        
        private void ListInit(string filePath)
        {
            FileList.ItemsSource = unpacker.fileDatas;
            loadedOOF = filePath;
            MenuExtract.IsEnabled = true;
        }
        
        private OpenFileDialog SelectKeyDialog(string Text)
        {
            if (MessageBox.Show(Text, "OOF GUI", MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.OK)
            {
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Title = "OOF Browser",
                    DefaultExt = "hex",
                    Filter = "Keyfile (.hex)|*.hex"
                };

                return openFileDialog;
            }
            return null;
        }
        
        private async Task FillList(string filePath)
        {
            await FillList(filePath, null);
        }
        private async Task FillList(string filePath, CryptoClass crypto)
        {
            CancellationToken token;
            try
            {
                CancellationTokenSource tokenSource = new CancellationTokenSource();
                token = tokenSource.Token;
                progressBar = new Progress();
                progressBar.ProgressBar1.IsIndeterminate = true;
                progressBar.Show();
                progressBar.Closing += (object sender, CancelEventArgs e) => {
                    tokenSource.Cancel();
                };
                EventRaiser.OnFileNameChange += Unpacker_OnProgressFileNameEvent;
                EventRaiser.OnProgressChange += Unpacker_OnProgressPercentEvent;

                unpacker = await Task.Run(() => new FileUnpacker(filePath, crypto, token));

                Stream unpackerStream = unpacker.UnpackStream(unpacker.fileDatas[0]);
                ListInit(filePath);
            }
            catch (NotAnOOFPackException)
            {
                progressBar?.Close();
                MessageBox.Show("Invalid or corrupted OOF Pack!", "OOF GUI", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (InvalidHeaderException e)
            {
                progressBar?.Close();
                MessageBox.Show("Corrupted OOF Pack! " + e.Message, "OOF GUI", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (ThreadAbortException)
            {
                progressBar?.Close();
            }
            catch (EncryptedException)
            {
                progressBar?.Close();
                OpenFileDialog selectKeyDialog = SelectKeyDialog("Your Pack seems to be encrypted! Please select a Keyfile to decrypt it.");
                if (selectKeyDialog!=null && selectKeyDialog.ShowDialog().Value)
                {
                    await FillList(filePath, new CryptoClass(selectKeyDialog.FileName));
                }
            }
            catch (IncorrectKeyException)
            {
                progressBar?.Close();
                OpenFileDialog selectKeyDialog = SelectKeyDialog("The Key seems to be wrong or corrupted. Please select the correct decryption keyfile!");
                if (selectKeyDialog != null && selectKeyDialog.ShowDialog().Value)
                {
                    await FillList(filePath, new CryptoClass(selectKeyDialog.FileName));
                }
            }
            catch (Exception e)
            {
                progressBar?.Close();
                MessageBox.Show("Something went wrong! " + e.Message, "OOF GUI", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                progressBar?.Close();
            }
        }

        private void SelectKeyfile()
        {
            MessageBox.Show("This Pack seems to be Encrypted! ", "OOF GUI", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void Unpacker_OnProgressPercentEvent(double obj)
        {
            progressBar.Dispatcher.Invoke(() => progressBar.ProgressValue = obj);
        }

        private void Unpacker_OnProgressFileNameEvent(string obj)
        {
            progressBar.Dispatcher.Invoke(() => progressBar.ProgressLabel = obj);
        }

        private async void FileList_Drop(object sender, DragEventArgs e)
        {
            string droppedOOFFile = ((string[])e.Data.GetData(DataFormats.FileDrop))[0];
            await FillList(droppedOOFFile);
        }

        private void Create_Click(object sender, RoutedEventArgs e)
        {
            CreateWindow createWindow = new CreateWindow();
            createWindow.Show();
        }

        private async void Open_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "OOF Browser",
                DefaultExt = "oof",
                Filter = "OOF Archive (.oof)|*.oof"
            };
            if (openFileDialog.ShowDialog().Value)
                await FillList(openFileDialog.FileName);
        }

        private async void Extract_Click(object sender, RoutedEventArgs e)
        {
            await Extract(null);
        }

        private async Task Extract(List<FileData> FileDatas)
        {
            System.Windows.Forms.FolderBrowserDialog extractToFolderDialog = new System.Windows.Forms.FolderBrowserDialog();

            if (extractToFolderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                CancellationTokenSource tokenSource = new CancellationTokenSource();
                CancellationToken token = tokenSource.Token;
                unpacker.Token = token;
                progressBar = new Progress();
                progressBar.Closing += (object s, CancelEventArgs c) => {
                    tokenSource.Cancel();
                };
                progressBar.Show();
                var toExtractFileStream = new FileStream(loadedOOF, FileMode.Open, FileAccess.Read);
                try
                {
                    if (FileDatas == null)
                        await unpacker.UnpackAsync(extractToFolderDialog.SelectedPath);
                    else
                        await unpacker.UnpackAsync(extractToFolderDialog.SelectedPath, FileDatas);
                }
                catch (Exception exc)
                {
                    MessageBox.Show("An error has occurred! " + exc.Message, "OOF GUI", MessageBoxButton.OK, MessageBoxImage.Stop);
                }
                toExtractFileStream.Close();
                progressBar.Close();
            }
        }

        private async void Unpack_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await Extract(FileDatas);
            }
            catch (Exception exc)
            {
                MessageBox.Show("An error has occurred! " + exc.Message, "OOF GUI", MessageBoxButton.OK, MessageBoxImage.Stop);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        private void FileList_Selected(object sender, SelectionChangedEventArgs e)
        {
            foreach (FileData data in e.AddedItems)
                FileDatas.Add(data);
            foreach (FileData data in e.RemovedItems)
                FileDatas.Remove(data);

            UnpackButton.IsEnabled = FileDatas.Count > 0;
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("OOF-Packer v0.2b (05/2019) ©Snagglebee");
        }
    }
}
