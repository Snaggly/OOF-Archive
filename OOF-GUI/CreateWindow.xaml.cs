using System.Collections.Generic;
using System.Linq;
using System.Windows;
using OOF_Packer;
using System.IO;
using Microsoft.Win32;
using RijndaelCryptography;
using System.Threading;
using System.ComponentModel;
using System;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace OOF_GUI
{
    /// <summary>
    /// Interaction logic for CreateWindow.xaml
    /// </summary>
    public partial class CreateWindow : Window
    {
        CryptoClass crypto;
        int inputSize;

        public CreateWindow()
        {
            FilesToAdd = new List<string>();
            InitializeComponent();
            bufferSlider.Maximum = 2000;
            bufferSlider.Minimum = 1;
            inputSize = 1;

            Style style = new Style(typeof(ListViewItem));
            style.Setters.Add(new Setter(AllowDropProperty, true));
            style.Setters.Add(new EventSetter(PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(ListViewItem_PreviewMouseLeftButtonDown)));
            style.Setters.Add(new EventSetter(DropEvent, new DragEventHandler(ListViewItem_Drop)));
        }

        private readonly List<string> FilesToAdd;

        private void ListViewItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
        }

        private void ListViewItem_Drop(object sender, DragEventArgs e)
        {
            if (sender is ListViewItem)
            {
                var source = e.Data.GetData(typeof(FileData)) as FileData;
                var target = ((ListBoxItem)(sender)).DataContext as FileData;

                int sourceIndex = FileList.Items.IndexOf(source);
                int targetIndex = FileList.Items.IndexOf(target);

                Move(source, sourceIndex, targetIndex);
            }
        }

        private void Move(FileData source, int sourceIndex, int targetIndex)
        {
            if (sourceIndex < targetIndex)
            {
                _items.Insert(targetIndex + 1, source);
                _items.RemoveAt(sourceIndex);
            }
            else
            {
                int removeIndex = sourceIndex + 1;
                if (_items.Count + 1 > removeIndex)
                {
                    _items.Insert(targetIndex, source);
                    _items.RemoveAt(removeIndex);
                }
            }
        }


        private void Window_Drop(object sender, DragEventArgs e)
        {
            string[] droppedFiles = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (droppedFiles != null)
            {
                FilesToAdd.AddRange(droppedFiles);
                FillList(droppedFiles);
            }
        }

        private void FillList(string[] droppedFiles)
        {
            try
            {
                foreach (string file in droppedFiles)
                {
                    FileList.Items.Add(new FileData(Path.GetFileName(file), new FileInfo(file).Length));
                }
            }
            catch (NotAnOOFPackException)
            {
                MessageBox.Show("Invalid or corrupted OOF Pack!", "OOF GUI", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            if (FilesToAdd.Count() < 1)
            {
                MessageBox.Show("No files selected!", "OOF-Packer", MessageBoxButton.OK, MessageBoxImage.Stop);
                return;
            }

            SaveFileDialog saveFile = new SaveFileDialog {
                Filter = "OOF-Pack (*.oof)|*.oof",
                Title = "OOF-Packer",
                DefaultExt = "oof"
            };

            if (saveFile.ShowDialog().Value)
            {
                FileStream fileToCreateStream = new FileStream(saveFile.FileName, FileMode.Create, FileAccess.ReadWrite);
                FileBuilder builder;
                if (crypto != null)
                    builder = new FileBuilder(FilesToAdd.ToArray(), (uint)inputSize * 0x100000u, crypto);
                else
                    builder = new FileBuilder(FilesToAdd.ToArray(), (uint)inputSize * 0x100000u);
                CancellationTokenSource tokenSource = new CancellationTokenSource();
                CancellationToken token = tokenSource.Token;
                progressWindow = new Progress();
                progressWindow.Closing += (object s, CancelEventArgs c) => {
                    tokenSource.Cancel();
                };
                EventRaiser.OnFileNameChange += Packer_OnProgressFileNameEvent;
                EventRaiser.OnProgressChange += Packer_OnProgressPercentEvent;

                Hide();
                progressWindow.Show();
                await builder.BuildFile(fileToCreateStream, token);
                fileToCreateStream.Close();
                progressWindow.Close();
                Close();
            }
        }

        Progress progressWindow;

        private void Packer_OnProgressPercentEvent(double obj)
        {
            progressWindow.Dispatcher.Invoke(() => progressWindow.ProgressValue = obj);
        }

        private void Packer_OnProgressFileNameEvent(string obj)
        {
            progressWindow.Dispatcher.Invoke(() => progressWindow.ProgressLabel = Path.GetFileName(obj));
        }

        private void Encryption_Click(object sender, RoutedEventArgs e)
        {
            EncryptionBox.IsEnabled = (bool)Encryption.IsChecked;
        }

        private void Create_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFile = new SaveFileDialog()
            {
                Title = "Key Generator",
                DefaultExt = "hex",
                Filter = "Keyfile (.hex)|*.hex"
            };

            if (saveFile.ShowDialog().Value)
            {
                BytesGenerator.KeyFileGenerator(saveFile.FileName, 56);
                FileName.Content = saveFile.FileName.Substring(saveFile.FileName.LastIndexOf('\\') + 1);
                crypto = new CryptoClass(saveFile.FileName);
            }

        }

        private void Select_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFile = new OpenFileDialog()
            {
                Title = "Key Selector",
                DefaultExt = "hex",
                Filter = "Keyfile (.hex)|*.hex"
            };

            if (openFile.ShowDialog().Value)
            {
                FileName.Content = openFile.FileName.Substring(openFile.FileName.LastIndexOf('\\') + 1);
                crypto = new CryptoClass(openFile.FileName);
            }
        }

        private void BufferTextbox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            int.TryParse(bufferTextbox.Text, out inputSize);
            bufferSlider.Value = inputSize;
        }

        private void BufferSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            bufferTextbox.Text = ((int)e.NewValue).ToString();
        }

        private Point _dragStartPoint;

        private readonly IList<FileData> _items = new ObservableCollection<FileData>();

        private T FindVisualParent<T>(DependencyObject child)
            where T : DependencyObject
        {
            var parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null)
                return null;
            if (parentObject is T parent)
                return parent;
            return FindVisualParent<T>(parentObject);
        }

        private void FileList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
        }

        private void MoveElement(int x)
        {
            int current = FileList.SelectedIndex;
            if (current >= 0)
            {
                string element = FilesToAdd[current];
                FilesToAdd.RemoveAt(current);
                if (x > 0 && current >0) { FilesToAdd.Insert(current - 1, element); }
                else { FilesToAdd.Insert(current + 1, element); }
                FileList.Items.Clear();
                FillList(FilesToAdd.ToArray());
                FileList.SelectedIndex = current;
            }
        }

        private void DeleteElement()
        {
            int current = FileList.SelectedIndex;
            if (current >= 0)
            {
                FilesToAdd.RemoveAt(current);
                FileList.Items.Clear();
                FillList(FilesToAdd.ToArray());
            }
        }

        private void FileList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up)
                MoveElement(1);
            else if (e.Key == Key.Down)
                MoveElement(0);
            else if (e.Key == Key.Delete)
                DeleteElement();
        }

        private void FileList_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            Point point = e.GetPosition(null);
            Vector diff = _dragStartPoint - point;
            if (e.LeftButton == MouseButtonState.Pressed &&
                (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance || Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
            {
                // lb = sender as ListView;
                var lbi = FindVisualParent<ListViewItem>(((DependencyObject)e.OriginalSource));
                if (lbi != null)
                {
                    DragDrop.DoDragDrop(lbi, lbi.DataContext, DragDropEffects.Move);
                }
            }
        }
    }
}
