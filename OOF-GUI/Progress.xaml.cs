using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace OOF_GUI
{
    /// <summary>
    /// Interaktionslogik für ProgressBar.xaml
    /// </summary>
    public partial class Progress : Window
    {
        public Progress()
        {
            InitializeComponent();
            ProgressBar1.Value = 0;
        }
        
        public double ProgressValue
        {
            get
            {
                return ProgressBar1.Value;
            }
            set
            {
                LabelPercent.Content = (value.ToString() + " %");
                ProgressBar1.Value = value;
            }
        }
        public string ProgressLabel
        {
            get
            {
                return (string)Label1.Content;
            }
            set
            {
                Label1.Content = value;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
