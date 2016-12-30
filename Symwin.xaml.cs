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
using System.Windows.Ink;
using Microsoft.Win32;
using System.IO;

namespace DollarFamily
{
	///DollarFamily by Joe Wileman
    ///10-21-16 CAP6105:Pen-based User Interfaces
    public partial class Symwin : Window
    {
        string folderpath;

        public Symwin()
        {
            InitializeComponent();

        }

        private void Createsym_Click(object sender, RoutedEventArgs e)
        {
            if (Symink.Strokes.Count > 0)
            {
                SaveFileDialog sav = new SaveFileDialog();
                sav.InitialDirectory = folderpath;
                sav.Title = "Save Symbol";
                sav.Filter = "Ink Serialized Format (*.isf)|*.isf";
                if (sav.ShowDialog(this) == true)
                {
                    FileStream sfil = new FileStream(sav.FileName, FileMode.Create, FileAccess.Write);
                    this.Symink.Strokes.Save(sfil);
                    sfil.Close();
                    Symlist.Items.Clear();
                    string[] files_path = Directory.GetFiles(folderpath, "*.isf");
                    
                    foreach (string filname in files_path)
                    {
                        Symlist.Items.Add(System.IO.Path.GetFileNameWithoutExtension(filname));
                    }
                }
            }
        }

        private void Symlist_Initialized(object sender, EventArgs e)
        {
            int index = MainWindow.mwin.Dataset.SelectedIndex;
            folderpath = MainWindow.mwin.dataset_folders[index];
            string[] files_path = Directory.GetFiles(folderpath, "*.isf");
            foreach (string filname in files_path)
            {
                Symlist.Items.Add(System.IO.Path.GetFileNameWithoutExtension(filname));
            }
        }

        private void Symlist_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string fname = String.Concat(Symlist.SelectedItem.ToString(), ".isf");
            string stroke_path = System.IO.Path.Combine(folderpath, fname);
            FileStream ofil = new FileStream(stroke_path, FileMode.Open, FileAccess.Read);
            this.Symink.Strokes.Clear();
            this.Symink.Strokes = new StrokeCollection(ofil);
            ofil.Close();
        }

        private void Clear_Canvas_Click(object sender, RoutedEventArgs e)
        {
            Symink.Strokes.Clear();
        }
    }
}
