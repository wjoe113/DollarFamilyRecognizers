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
    class ShortStraw_Ext
    {
        public static bool Scribbletest(Stroke ipstroke)
        {
            Stroke scribble_resam = Resampts(ipstroke);
            Stroke corners = GetCorners(scribble_resam, 3);
            double[] angles = Get_Angles(corners);
            double meanangle = 0;
            for (int i = 0; i < angles.Length; i++)
            {
                meanangle = meanangle + angles[i];
            }
            meanangle = meanangle / (angles.Length - 2);
            if (meanangle < 10 && ipstroke.StylusPoints.Count > 200)
                return true;
            else
                return false;
        }

        public static Stroke Resampts(Stroke ipstroke)
        {
            StylusPointCollection ip_sty_coll = new StylusPointCollection();
            ip_sty_coll = ipstroke.StylusPoints;
            Rect bd = ipstroke.GetBounds();
            double isdist;
            isdist = Math.Sqrt(Math.Pow(bd.BottomRight.X - bd.TopLeft.X, 2) + Math.Pow(bd.BottomRight.Y - bd.TopLeft.Y, 2));
            isdist = (isdist / 40);

            StylusPointCollection resamp_stroke_stycoll = new StylusPointCollection();
            resamp_stroke_stycoll.Add(ip_sty_coll[0]);
            double D = 0;

            for (int i = 1; i < ipstroke.StylusPoints.Count; i++)
            {
                double prevptx = ip_sty_coll[i - 1].X;
                double prevpty = ip_sty_coll[i - 1].Y;
                double currptx = ip_sty_coll[i].X;
                double currpty = ip_sty_coll[i].Y;
                double tempdist = Math.Sqrt(Math.Pow((prevptx - currptx), 2) + Math.Pow((prevpty - currpty), 2));
                if ((D + tempdist) >= isdist)
                {
                    StylusPoint q = new StylusPoint();
                    q.X = (prevptx + ((isdist - D) / tempdist) * (currptx - prevptx));
                    q.Y = (prevpty + ((isdist - D) / tempdist) * (currpty - prevpty));
                    ip_sty_coll.Insert(i, q);
                    resamp_stroke_stycoll.Add(q);
                    D = 0;
                }
                else
                {
                    D = D + tempdist;
                }
            }
            Stroke resamp_stroke = new Stroke(resamp_stroke_stycoll);
            resamp_stroke.StylusPoints = resamp_stroke_stycoll;

            return resamp_stroke;
        }

        public static Stroke GetCorners(Stroke resampled, int W)
        {
            StylusPointCollection corner_coll = new StylusPointCollection();
            List<double> straws = new List<double>();

            for (int i = 0; i < W; i++)
            {
                straws.Insert(0, 0);
            }
            for (int i = W; i < resampled.StylusPoints.Count - W; i++)
            {
                straws.Add(GetEuDist(resampled.StylusPoints[i - W], resampled.StylusPoints[i + W]));
            }

            double t = Median(straws);

            double localmin;
            int localmin_index;
            List<int> indices = new List<int>();
            for (int i = W; i < resampled.StylusPoints.Count - W; i++)
            {
                if (straws[i] < t)
                {
                    localmin = Double.PositiveInfinity;
                    localmin_index = i;
                    while (i < straws.Count && straws[i] < t)
                    {
                        if (straws[i] < localmin)
                        {
                            localmin = straws[i];
                            localmin_index = i;
                        }
                        i = i + 1;
                    }
                    corner_coll.Add(resampled.StylusPoints[localmin_index]);
                    indices.Add(localmin_index);
                }
            }

            corner_coll = PostProcessCorners(corner_coll, resampled, indices, straws);
            if (corner_coll.Count == 0)
            {
                StylusPointCollection aa = new StylusPointCollection();
                aa.Add(new StylusPoint(0, 0));
                Stroke corners = new Stroke(aa);
                return corners;
            }
            else
            {
                Stroke corners = new Stroke(corner_coll);
                corners.StylusPoints = corner_coll;
                return corners;
            }
        }

        public static double GetEuDist(StylusPoint a, StylusPoint b)
        {
            double delX = a.X - b.X;
            double delY = a.Y - b.Y;
            double eudist = Math.Abs(Math.Sqrt(Math.Pow(delX, 2) + Math.Pow(delY, 2)));
            return eudist;
        }

        public static double GetPathDist(Stroke resamp_data, int a_ind, int b_ind)
        {
            double dist = 0;
            for (int i = a_ind; i < b_ind; i++)
            {
                dist = dist + GetEuDist(resamp_data.StylusPoints[i], resamp_data.StylusPoints[i + 1]);
            }
            return dist;
        }

        public static double Median(List<double> val)
        {
            float Median = 0;
            int size = val.Count;
            int mid = size / 2;
            Median = (size % 2 != 0) ? (float)val[mid] : ((float)val[mid] + (float)val[mid + 1]) / 2;
            return Math.Round(Median);
        }

        public static StylusPointCollection PostProcessCorners(StylusPointCollection corner_data, Stroke resamp_data, List<int> indices, List<double> straws)
        {
            bool continu = false;
            while (continu == true)
            {
                continu = true;
                for (int i = 1; i < corner_data.Count; i++)
                {
                    int c1_ind = indices[i - 1];
                    int c2_ind = indices[i];
                    if (IsLine(resamp_data, c1_ind, c2_ind))
                    {
                        int newcorner_index = Halfway_Corner(straws, c1_ind, c2_ind);
                        corner_data.Insert(newcorner_index, resamp_data.StylusPoints[newcorner_index]);
                        continu = false;
                    }
                }
            }

            for (int i = 1; i < (corner_data.Count - 1); i++)
            {
                int c1_ind = indices[i - 1];
                int c2_ind = indices[i + 1];
                if (IsLine(resamp_data, c1_ind, c2_ind))
                {
                    corner_data.Remove(corner_data[i]);
                    i = i - 1;
                }
            }
            return corner_data;
        }

        public static bool IsLine(Stroke resamp_data, int a_ind, int b_ind)
        {
            float threshold = 0.95F;
            double dist = GetEuDist(resamp_data.StylusPoints[a_ind], resamp_data.StylusPoints[b_ind]);
            double pathdist = GetPathDist(resamp_data, a_ind, b_ind);
            if (dist / pathdist > threshold)
                return true;
            else
                return false;
        }

        public static int Halfway_Corner(List<double> straws, int a_ind, int b_ind)
        {
            int quarter = (b_ind - a_ind) / 4;
            double min_val = Double.NegativeInfinity;
            int min_index = 0;
            for (int i = a_ind + quarter; i < b_ind - quarter; i++)
            {
                if (straws[i] < min_val)
                {
                    min_val = straws[i];
                    min_index = i;
                }
            }
            return min_index;
        }

        public static double[] Get_Angles(Stroke Corners)
        {
            double cornercount = Corners.StylusPoints.Count;
            Stroke vertices = new Stroke(Corners.StylusPoints);

            double[] angles = new double[(int)cornercount];
            for (int i = 0; i < cornercount - 2; i++)
            {
                StylusPoint a = vertices.StylusPoints[i];
                StylusPoint b = vertices.StylusPoints[i + 1];
                StylusPoint c = vertices.StylusPoints[i + 2];

                double ab = GetEuDist(a, b);
                double bc = GetEuDist(b, c);
                double ac = GetEuDist(a, c);

                Point aa = (Point)a;
                Point bb = (Point)b;
                Point cc = (Point)c;

                angles[i] = Math.Acos((Math.Pow(ab, 2) + Math.Pow(bc, 2) - Math.Pow(ac, 2)) / (2 * ab * bc));
                angles[i] = angles[i] * 57.2958;
            }
            return angles;
        }
    }

}
