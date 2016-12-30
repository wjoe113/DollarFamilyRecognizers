using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;

namespace DollarFamily
{
	///DollarFamily by Joe Wileman
    ///10-21-16 CAP6105:Pen-based User Interfaces
    class PennyPincher
    {
        public static int N = 16;

        public static void Generate_Multistrokes(StrokeCollection data_sample, out List<StylusPointCollection> points_data)
        {
            if (data_sample.Count == 1)
            {
                StylusPointCollection points = Resample(data_sample[0], N);
                points_data = new List<StylusPointCollection>();
                points_data.Add(points);
            }
            else
            {
                List<int> order = new List<int>();
                List<List<int>> orders = new List<List<int>>();
                points_data = new List<StylusPointCollection>();

                for (int i = 0; i < data_sample.Count; i++)
                {
                    order.Add(i);
                }
                HeapPermute(data_sample.Count, order, orders);
                StrokeCollection multistrokes_data_sample = Gen_Unistrokes(data_sample, orders);
                foreach (Stroke sample in multistrokes_data_sample)
                {
                    StylusPointCollection points = Resample(sample, N);
                    points_data.Add(points);
                }
            }
        }

        public static void HeapPermute(int no_of_strokes, List<int> order, List<List<int>> orders)
        {
            if (no_of_strokes == 1)
            {
                orders.Add(new List<int>(order));
            }
            else
            {
                for (int i = 0; i < no_of_strokes; i++)
                {
                    HeapPermute(no_of_strokes - 1, order, orders);
                    if ((no_of_strokes % 2) == 0)
                    {
                        int temp = order[0];
                        order[0] = order[no_of_strokes - 1];
                        order[no_of_strokes - 1] = temp;
                    }
                    else
                    {
                        int temp = order[i];
                        order[i] = order[no_of_strokes - 1];
                        order[no_of_strokes - 1] = temp;
                    }
                }
            }
        }

        public static StrokeCollection Gen_Unistrokes(StrokeCollection data_sample, List<List<int>> orders)
        {
            StrokeCollection multistroke_data_sample = new StrokeCollection();
            foreach (List<int> order in orders)
            {
                for (int b = 0; b < Math.Pow(2d, order.Count); b++)
                {
                    StylusPointCollection unisty_points = new StylusPointCollection();
                    for (int i = 0; i < order.Count; i++)
                    {
                        Stroke stroke = data_sample[(int)order[i]];
                        StylusPointCollection strokepoints = stroke.StylusPoints;
                        if (((b >> i) & 1) == 1)
                        {
                            strokepoints.Reverse();
                        }
                        unisty_points.Add(strokepoints);
                    }
                    multistroke_data_sample.Add(new Stroke(unisty_points));
                }
            }
            return multistroke_data_sample;
        }

        public static StylusPointCollection Resample(Stroke stroke, int N)
        {
            StylusPointCollection points = stroke.StylusPoints;
            double I = GetPathLen(points) / (N - 1);
            double D = 0;
            StylusPointCollection vector = new StylusPointCollection();

            StylusPoint origin = new StylusPoint(0, 0);
            StylusPoint pr = new StylusPoint();
            pr = stroke.StylusPoints[0];
            for (int i = 1; i < points.Count; i++)
            {
                double d = GetEuDist(points[i - 1], points[i]);
                if (D + d >= I)
                {
                    StylusPoint q = new StylusPoint();
                    q.X = points[i - 1].X + ((I - D) / d) * (points[i].X - points[i - 1].X);
                    q.Y = points[i - 1].Y + ((I - D) / d) * (points[i].Y - points[i - 1].Y);
                    StylusPoint r = new StylusPoint(q.X - pr.X, q.Y - pr.Y);
                    double rdist = GetEuDist(origin, r);
                    StylusPoint r_norm = new StylusPoint(r.X / rdist, r.Y / rdist);
                    vector.Add(r_norm);
                    points.Insert(i, q);
                    pr = q;
                    D = 0;
                }
                else
                    D = D + d;
            }

            return vector;
        }

        public static double GetPathLen(StylusPointCollection points)
        {
            double dist = 0;
            for (int i = 1; i < points.Count; i++)
            {
                dist = dist + GetEuDist(points[i - 1], points[i]);
            }
            return dist;
        }

        public static double GetEuDist(StylusPoint a, StylusPoint b)
        {
            double delX = Math.Abs(a.X - b.X);
            double delY = Math.Abs(a.Y - b.Y);
            double eudist = Math.Abs(Math.Sqrt(Math.Pow(delX, 2) + Math.Pow(delY, 2)));
            return eudist;
        }
        
        public static void Recognize(List<StylusPointCollection> test_points, List<List<StylusPointCollection>> database_points, out double score, out int idx)
        {
            double similarity = Double.NegativeInfinity;
            idx = 0;
            for (int i = 0; i < database_points.Count; i++)
            {
                if (test_points.Count == database_points[i].Count)
                {
                    for (int j = 0; j < database_points[i].Count; j++)
                    {
                        double d = 0;
                        for (int k = 0; k < database_points[i][j].Count - 2; k++)
                        {
                            StylusPoint tp = test_points[j][k];
                            StylusPoint db = database_points[i][j][k];
                            d = d + db.X * tp.X + db.Y * tp.Y;
                        }
                        if (d > similarity)
                        {
                            similarity = d;
                            idx = i;
                        }
                    }
                }
            }
            score =  similarity;
        }

    }
}
