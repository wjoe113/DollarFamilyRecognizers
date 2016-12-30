using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DollarFamily
{
    ///DollarFamily by Joe Wileman
    ///10-21-16 CAP6105:Pen-based User Interfaces
    public partial class MainWindow : Window
    {
        public static MainWindow mwin;
        System.Windows.Threading.DispatcherTimer dtimer;
        bool next_stroke_wait;
        StrokeCollection strokes;
        List<StrokeCollection> data_samples = new List<StrokeCollection>();
        public string[] dataset_folders;
        List<string> names = new List<string>();
        List<string> symbol = new List<string>();
        List<int> symbol_count = new List<int>();
        List<int> symbol_references = new List<int>();

        List<List<StylusPointCollection>> DollarN_resamples = new List<List<StylusPointCollection>>();
        List<List<Vector>> DollarN_start_vecs = new List<List<Vector>>();

        List<List<StylusPointCollection>> Protractor_resamples = new List<List<StylusPointCollection>>();
        List<List<Vector>> Protractor_start_vecs = new List<List<Vector>>();
        List<List<List<Vector>>> Protractor_Vectors = new List<List<List<Vector>>>();

        List<List<StylusPointCollection>> Penny_resamples = new List<List<StylusPointCollection>>();

        //Identify which recognizer is checked in the window
        double Total_Recognition_Cycles = -1;
        double DollarN_Correct_identified = 0;
        double Protractor_Correct_identified = 0;
        double Penny_Correct_identified = 0;
        //See if a database is selected and what its name is
        Boolean datasetSelected = false;
        string datasetName = "";

        public MainWindow()
        {
            InitializeComponent();
            mwin = this;
            InkCanv.Strokes.StrokesChanged += strokecount_increment;
            string rootpath = System.IO.Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName, "symbolCollection");
            dataset_folders = Directory.GetDirectories(rootpath);
            foreach (string folderpath in dataset_folders)
                Dataset.Items.Add(folderpath.Remove(0, rootpath.Length));
            Dataset.SelectedIndex = 2;
        }

        private void strokecount_increment(object sender, System.Windows.Ink.StrokeCollectionChangedEventArgs e)
        {
            if (e.Added.Count > 0)
            {
                if (next_stroke_wait == true)
                {
                    strokes.Add(e.Added);
                    dtimer.Stop();
                }
                else
                {
                    strokes = new StrokeCollection();
                    strokes.Add(e.Added);
                    next_stroke_wait = true;
                }
                dtimer = new System.Windows.Threading.DispatcherTimer();
                dtimer.Tick += new EventHandler(dtimer_tick);
                dtimer.Interval = new TimeSpan(0, 0, 1);
                dtimer.Start();
            }
        }

        private void dtimer_tick(object sender, EventArgs e)
        {
            dtimer.Stop();
            next_stroke_wait = false;
            if (strokes.Count == 1)
            {

                if (ShortStraw_Ext.Scribbletest(strokes[0]) == true)
                {
                    Rect scribble_bounds = strokes[0].GetBounds();
                    for (int i = 0; i < InkCanv.Strokes.Count; i++)
                    {
                        Rect stroke_bounds = InkCanv.Strokes[i].GetBounds();
                        if (scribble_bounds.TopLeft.X + scribble_bounds.Width < stroke_bounds.TopLeft.X ||
                            stroke_bounds.TopLeft.X + stroke_bounds.Width < scribble_bounds.TopLeft.X ||
                            scribble_bounds.TopLeft.Y + scribble_bounds.Height < stroke_bounds.TopLeft.Y ||
                            stroke_bounds.TopLeft.Y + stroke_bounds.Height < scribble_bounds.TopLeft.Y)
                        {
                        }
                        else if (scribble_bounds.TopLeft.X + scribble_bounds.Width < stroke_bounds.TopLeft.X &&
                            stroke_bounds.TopLeft.X + stroke_bounds.Width < scribble_bounds.TopLeft.X &&
                            scribble_bounds.TopLeft.Y + scribble_bounds.Height < stroke_bounds.TopLeft.Y &&
                            stroke_bounds.TopLeft.Y + stroke_bounds.Height < scribble_bounds.TopLeft.Y)
                        {
                        }
                        else
                        { 
                            InkCanv.Strokes.RemoveAt(i);
                            if(InkCanv.Strokes.Count != 0)
                            InkCanv.Strokes.RemoveAt(InkCanv.Strokes.Count - 1);
                            strokes.Clear();
                        }
                    }
                }
                else
                    Recognize_Char();
            }
            else
                Recognize_Char();
        }

        private void Gen_Dataset(string folderpath)
        {
            int symbol_index = -1;
            string[] files_path;
            try
            {
                files_path = Directory.GetFiles(folderpath, "*.isf");
                int count = 0;
                if (data_samples.Count != 0)
                {
                    Initialize_Everything_After_DB_Change();
                }
                foreach (string filname in files_path)
                {
                    FileStream ofil = new FileStream(filname, FileMode.Open, FileAccess.Read);
                    data_samples.Add(new StrokeCollection(ofil));
                    string symbol_name = System.IO.Path.GetFileNameWithoutExtension(filname);
                    int seperator_pos = symbol_name.IndexOf("_");
                    symbol_name = symbol_name.Substring(0, seperator_pos);
                    names.Add(symbol_name);
                    if (!symbol.Contains(symbol_name))
                    {
                        symbol_count.Add(1);
                        symbol.Add(symbol_name);
                        symbol_index += 1;
                        symbol_references.Add(count);
                    }
                    else
                    {
                        symbol_count[symbol_index] += 1;
                    }
                    ofil.Close();
                    count += 1;
                }
                Result_Text.Clear();

                foreach (StrokeCollection data_sample_strcoll in data_samples)
                {
                    List<StylusPointCollection> resamp = new List<StylusPointCollection>();
                    List<Vector> start_vecs = new List<Vector>();
                    DollarN.Generate_Multistrokes(data_sample_strcoll, out resamp, out start_vecs);
                    DollarN_resamples.Add(resamp);
                    DollarN_start_vecs.Add(start_vecs);

                    resamp = new List<StylusPointCollection>();
                    start_vecs = new List<Vector>();
                    List<List<Vector>> vectors = new List<List<Vector>>();
                    Protractor.Generate_Multistrokes(data_sample_strcoll, out resamp, out start_vecs, out vectors);
                    Protractor_resamples.Add(resamp);
                    Protractor_start_vecs.Add(start_vecs);
                    Protractor_Vectors.Add(vectors);

                    resamp = new List<StylusPointCollection>();
                    PennyPincher.Generate_Multistrokes(data_sample_strcoll, out resamp);
                    Penny_resamples.Add(resamp);
                }
            }
            catch (DirectoryNotFoundException dir_ex)
            {
                Console.WriteLine("Directory not found: " + dir_ex.Message);
            }
        }

        private void Recognize_Char()
        {
            StrokeCollection test_stroke_coll = strokes;
            Result_Text.Clear();

            List<StylusPointCollection> test_resamp = new List<StylusPointCollection>();
            List<Vector> test_start_vec = new List<Vector>();
            DollarN.Generate_Multistrokes(test_stroke_coll, out test_resamp, out test_start_vec);
            double dollarn_score;
            int dollarn_idx;
            DollarN.Recognize(test_resamp, test_start_vec, DollarN_resamples, DollarN_start_vecs, out dollarn_score, out dollarn_idx);
            dollarn_score = Math.Round(dollarn_score, 4);
            //Display DollarN Score
            if (dollarn_score == double.NegativeInfinity)
            {
                MessageBox.Show("Select a database, silly!", "Select a Database", MessageBoxButton.OK, MessageBoxImage.None);
                InkCanv.Strokes.Clear();
                Result_Text.Clear();
                if (strokes != null)
                {
                    strokes.Clear();
                }
            }
            else
            {
                Result_Text.Text += ("DollarN: " + names[dollarn_idx] + "\nScore: " + dollarn_score + "\n");
            }

            test_resamp = new List<StylusPointCollection>();
            test_start_vec = new List<Vector>();
            List<List<Vector>> test_vectorized = new List<List<Vector>>();
            Protractor.Generate_Multistrokes(test_stroke_coll, out test_resamp, out test_start_vec, out test_vectorized);
            double protractor_score;
            int protractor_idx;
            Protractor.Recognize(test_resamp, test_start_vec, Protractor_resamples, Protractor_start_vecs, Protractor_Vectors, test_vectorized, out protractor_score, out protractor_idx);
            protractor_score = Math.Round(protractor_score, 4);
            //Display Protractor Score
            if (dollarn_score != double.NegativeInfinity)
            {
                Result_Text.Text += ("Protractor: " + names[protractor_idx] + "\nScore: " + protractor_score + "\n");
            }

            test_resamp = new List<StylusPointCollection>();
            PennyPincher.Generate_Multistrokes(test_stroke_coll, out test_resamp);
            double penny_score;
            int penny_idx;
            PennyPincher.Recognize(test_resamp, Penny_resamples, out penny_score, out penny_idx);
            penny_score = Math.Round(penny_score, 4);
            //Display PennyPincher Score
            if (dollarn_score != double.NegativeInfinity)
            {
                Result_Text.Text += ("PennyPincher: " + names[penny_idx] + "\nScore: " + penny_score);
            }
            Total_Recognition_Cycles += 1;
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            InkCanv.Strokes.Clear();
            Result_Text.Clear();
            if (strokes != null)
            {
                strokes.Clear();
            }
        }

        private void Symbols_Click(object sender, RoutedEventArgs e)
        {
            bool isWindowOpen = false;
            foreach (Window w in Application.Current.Windows)
            {
                if (w is Symwin)
                {
                    isWindowOpen = true;
                    w.Activate();
                }
            }

            if (!isWindowOpen)
            {
                if (datasetSelected == false)
                {
                    MessageBox.Show("Select a database, silly!", "Select a Database", MessageBoxButton.OK, MessageBoxImage.None);
                }
                else
                {
                    Symwin newwindow = new Symwin();
                    newwindow.Title = "Create Symbol for " + datasetName;
                    newwindow.Show();
                }
            }
        }

        private void Dataset_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            datasetSelected = true;
            int index = Dataset.SelectedIndex;
            Gen_Dataset(dataset_folders[index]);
            if(index == 0)
            {
                datasetName = "penDatabase";
            }
            else
            {
                datasetName = "touchDatabase";
            }
        }

        private void InkCanv_StrokeCollected(object sender, InkCanvasStrokeCollectedEventArgs e)
        {
        }

        private void Initialize_Everything_After_DB_Change()
        {
            data_samples.Clear();
            names = new List<string>();
            symbol = new List<string>();
            symbol_count = new List<int>();
            symbol_references = new List<int>();
            DollarN_resamples = new List<List<StylusPointCollection>>();
            Protractor_resamples = new List<List<StylusPointCollection>>();
            Penny_resamples = new List<List<StylusPointCollection>>();
            DollarN_start_vecs = new List<List<Vector>>();
            Protractor_start_vecs = new List<List<Vector>>();
            Protractor_Vectors = new List<List<List<Vector>>>();
            Total_Recognition_Cycles = 0;
            DollarN_Correct_identified = 0;
            Protractor_Correct_identified = 0;
            Penny_Correct_identified = 0;
            //Accuracy_box.Clear();
        }

        private void Acc_submit_Click(object sender, RoutedEventArgs e)
        {
            //Accuracy_box.Clear();
            DollarN_Correct_identified += (bool)DollarN_acc.IsChecked ? (1) : (0);
            Protractor_Correct_identified += (bool)Protractor_acc.IsChecked ? (1) : (0);
            Penny_Correct_identified += (bool)Penny_acc.IsChecked ? (1) : (0);
            //Show Accuracies
            MessageBox.Show(("DollarN Accuracy: " + (DollarN_Correct_identified / Total_Recognition_Cycles) * 100 + "%") + "\n" +
                ("\nProtractor Accuracy: " + (Protractor_Correct_identified / Total_Recognition_Cycles) * 100 + "%") + "\n" +
                ("\nPennyPincher Accuracy: " + (Penny_Correct_identified / Total_Recognition_Cycles) * 100 + "%") + "\n" +
                ("\nTotal Strokes Counted: " + Total_Recognition_Cycles), "Recognizer Accuracies", MessageBoxButton.OK, MessageBoxImage.None);
            //Accuracy Box Disabled
            //Accuracy_box.Text += ("$N Accuracy: " + (DollarN_Correct_identified / Total_Recognition_Cycles) * 100 + "%");
            //Accuracy_box.Text += ("\nProtractor Accuracy: " + (Protractor_Correct_identified / Total_Recognition_Cycles) * 100 + "%");
            //Accuracy_box.Text += ("\nPennyPincher Accuracy: " + (Penny_Correct_identified / Total_Recognition_Cycles) * 100 + "%");
            //Accuracy_box.Text += ("\nTotal Strokes Counted: " + Total_Recognition_Cycles);
            //Acc_submit.Background = new SolidColorBrush(Color.FromArgb(255, 221, 221, 221));
        }
    }
}
