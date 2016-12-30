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
    class Protractor
    {
        public static int N = 16;
        public static int I = 12;
        public static double size_s = 250;
        public static double delta = 0.30;
        public static StylusPoint origin = new StylusPoint(0, 0);


        public static void Generate_Multistrokes(StrokeCollection data_sample, out List<StylusPointCollection> points_data, out List<Vector> start_unit_vector, out List<List<Vector>> vectors)
        {
            if (data_sample.Count == 1)
            {
                StylusPointCollection points = Resample(data_sample[0], N);
                double ind_angle = Indicative_Angle(points);
                points = RotateBy(points, ind_angle);
                bool isonedim = is_one_dim_strokes(points);
                points = Scale_Dim_To(points, size_s, delta);
                points = Translate_to(points, origin);
                points_data = new List<StylusPointCollection>();
                points_data.Add(points);
                start_unit_vector = new List<Vector>();
                start_unit_vector.Add(Calc_Start_Unit_Vector(points, I));
                vectors = new List<List<Vector>>();
                vectors.Add(Vectorize(points));
            }
            else
            {
                List<int> order = new List<int>();
                List<List<int>> orders = new List<List<int>>();
                points_data = new List<StylusPointCollection>();
                start_unit_vector = new List<Vector>();
                vectors = new List<List<Vector>>();

                for (int i = 0; i < data_sample.Count; i++)
                {
                    order.Add(i);
                }
                HeapPermute(data_sample.Count, order, orders);
                StrokeCollection multistrokes_data_sample = Gen_Unistrokes(data_sample, orders);
                foreach (Stroke sample in multistrokes_data_sample)
                {
                    StylusPointCollection points = Resample(sample, N);
                    double ind_angle = Indicative_Angle(points);
                    points = RotateBy(points, ind_angle);
                    bool isonedim = is_one_dim_strokes(points);
                    points = Scale_Dim_To(points, size_s, delta);
                    points = Translate_to(points, origin);
                    points_data.Add(points);
                    start_unit_vector.Add(Calc_Start_Unit_Vector(points, I));
                    vectors.Add(Vectorize(points));
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
            StylusPointCollection newPoints = new StylusPointCollection();
            newPoints.Add(points[0]);

            for (int i = 1; i < points.Count; i++)
            {
                double d = GetEuDist(points[i - 1], points[i]);
                if (D + d >= I)
                {
                    StylusPoint q = new StylusPoint();
                    q.X = points[i - 1].X + ((I - D) / d) * (points[i].X - points[i - 1].X);
                    q.Y = points[i - 1].Y + ((I - D) / d) * (points[i].Y - points[i - 1].Y);
                    newPoints.Add(q);
                    points.Insert(i, q);
                    D = 0;
                }
                else
                    D = D + d;
            }

            if (newPoints.Count == N - 1)
            {
                newPoints.Add(points[points.Count - 1]);
            }

            return newPoints;
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

        public static double Indicative_Angle(StylusPointCollection points)
        {
            StylusPoint c = Centroid(points);
            double ind_angle = 0.0;
            if (c.X != points[0].X)
            {
                ind_angle = Math.Atan2(points[0].Y - c.Y, points[0].X - c.X);
            }
            else
            {
                if (points[0].Y < c.Y)
                    ind_angle = -Math.PI / 2.0;
                else if (points[0].Y > c.Y)
                    ind_angle = Math.PI / 2.0;
            }
            return ind_angle;
        }

        public static StylusPoint Centroid(StylusPointCollection points)
        {
            double x = 0, y = 0, cen_x, cen_y;
            foreach (StylusPoint point in points)
            {
                x = x + point.X;
                y = y + point.Y;
            }
            cen_x = x / points.Count;
            cen_y = y / points.Count;
            StylusPoint centroid = new StylusPoint(cen_x, cen_y);
            return centroid;
        }

        public static StylusPointCollection RotateBy(StylusPointCollection points, double ind_angle)
        {
            StylusPointCollection newPoints = new StylusPointCollection(points.Count);
            StylusPoint c = Centroid(points);

            for (int i = 0; i < points.Count; i++)
            {
                StylusPoint p = points[i];
                StylusPoint q = new StylusPoint();
                q.X = (p.X - c.X) * Math.Cos(ind_angle) - (p.Y - c.Y) * Math.Sin(ind_angle) + c.X;
                q.Y = (p.X - c.X) * Math.Sin(ind_angle) + (p.Y - c.Y) * Math.Cos(ind_angle) + c.Y;

                newPoints.Add(q);
            }
            return newPoints;
        }

        public static bool is_one_dim_strokes(StylusPointCollection points)
        {
            StylusPointCollection temp_points = new StylusPointCollection(points);

            double angle = Indicative_Angle(points);
            temp_points = RotateBy(temp_points, -angle);
            Stroke tempstroke = new Stroke(temp_points);
            Rect r = tempstroke.GetBounds();
            if ((r.Width == 0) || (r.Height == 0))
                return true;
            else if ((r.Width / r.Height) < (r.Height / r.Width))
            {
                if ((r.Width / r.Height) < delta)
                    return true;
                else
                    return false;
            }
            else
            {
                if ((r.Height / r.Width) < delta)
                    return true;
                else
                    return false;
            }
        }

        public static StylusPointCollection Scale_Dim_To(StylusPointCollection points, double size, double delta)
        {
            Stroke tempstroke = new Stroke(points);
            Rect B = tempstroke.GetBounds();
            StylusPointCollection newPoints = new StylusPointCollection();
            foreach (StylusPoint p in points)
            {
                if (Math.Min(B.Width / B.Height, B.Height / B.Width) <= delta)
                {
                    StylusPoint q = new StylusPoint();
                    q.X = p.X * (size / Math.Max(B.Width, B.Height));
                    q.Y = p.Y * (size / Math.Max(B.Width, B.Height));
                    newPoints.Add(q);

                }
                else
                {
                    StylusPoint q = new StylusPoint();
                    q.X = p.X * (size / B.Width);
                    q.Y = p.Y * (size / B.Height);
                    newPoints.Add(q);
                }

            }

            return newPoints;
        }

        public static StylusPointCollection Translate_to(StylusPointCollection points, StylusPoint originpt)
        {
            StylusPoint c = Centroid(points);
            StylusPointCollection newPoints = new StylusPointCollection();
            foreach (StylusPoint p in points)
            {
                StylusPoint q = new StylusPoint();
                q.X = p.X + (originpt.X - c.X);
                q.Y = p.Y + (originpt.Y - c.Y);
                newPoints.Add(q);
            }
            return newPoints;
        }

        public static Vector Calc_Start_Unit_Vector(StylusPointCollection points, int I)
        {
            StylusPoint q = new StylusPoint();
            q.X = points[I].X - points[0].X;
            q.Y = points[I].Y - points[0].Y;
            Vector v = new Vector();
            v.X = (q.X / Math.Sqrt(Math.Pow(q.X, 2) + Math.Pow(q.Y, 2)));
            v.Y = (q.Y / Math.Sqrt(Math.Pow(q.X, 2) + Math.Pow(q.Y, 2)));
            return v;
        }

        public static double Path_Distance(StylusPointCollection A, StylusPointCollection B)
        {
            double dist = 0;
            for (int i = 1; i < A.Count; i++)
            {
                dist = dist + GetEuDist(A[i], B[i - 1]);
            }
            return dist / A.Count;
        }

        public static List<Vector> Vectorize(StylusPointCollection points)
        {
            double c = 1, s = 0;
            double theta_first = Math.Atan2(points[0].Y, points[0].X);
            double theta_base = (Math.PI / 4) * Math.Floor((theta_first + Math.PI / 8) / (Math.PI / 4));
            c = Math.Cos(theta_base - theta_first);
            s = Math.Sin(theta_base - theta_first);

            double sum = 0;
            List<Vector> vectors = new List<Vector>();
            foreach (Point p in points)
            {
                double x_new = p.X * c - p.Y * s;
                double y_new = p.Y * c + p.X * s;
                vectors.Add(new Vector(x_new, y_new));
                sum += Math.Pow(x_new, 2) + Math.Pow(y_new, 2);
            }
            double mag = Math.Sqrt(sum);
            for (int i = 0; i < vectors.Count; i++)
            {
                vectors[i] /= mag;
            }
            return vectors;
        }

        public static double Opt_Cosine_Dist(List<Vector> v1, List<Vector> v2)
        {
            double a = 0;
            double b = 0;

            for (int i = 0; i < v1.Count; i++)
            {
                a = a + v1[i].X * v2[i].X + v1[i].Y * v2[i].Y;
                b = b + v1[i].X * v2[i].Y + v1[i].Y * v2[i].X;
            }

            double angle = Math.Atan(b / a);
            return Math.Acos(a * Math.Cos(angle) + b * Math.Sin(angle));
        }


        public static void Recognize(List<StylusPointCollection> test_points, List<Vector> test_vectors,
            List<List<StylusPointCollection>> database_points, List<List<Vector>> database_vectors,
            List<List<List<Vector>>> database_vectorized, List<List<Vector>> test_vectorized, out double score, out int idx)
        {
            double b = double.PositiveInfinity;
            idx = 0;
            for (int i = 0; i < database_points.Count; i++)
            {
                if (test_points.Count == database_points[i].Count)
                {
                    for (int j = 0; j < database_points[i].Count; j++)
                    {
                        double d = Opt_Cosine_Dist(database_vectorized[i][j], test_vectorized[j]);
                        if (d < b)
                        {
                            b = d;
                            idx = i;
                        }
                    }
                }
            }
            score = 1 - b;
            score = score * 100;
        }

    }
}
