using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using OSGeo.MapGuide;
using Autodesk.AutoCAD.Windows;
using Autodesk.DesignScript.Runtime;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using System.Xml.Linq;
using System.Reflection;

namespace MapConnection
{
  public class CoordTransform
  { 
        private CoordTransform() { }
       

            /// <summary>
            /// Перевод геодезических координат на данном эллипсоиде в плоские прямоугольные
            /// </summary>
            /// <param name="Ellipsoid">Словарь с параметрами эллипсоида (из данной коллекции нодов)</param>
            /// <param name="CS_Params">Словарь с параметрами систем координат (из данной коллекции нодов)</param>
            /// <param name="Latitude">Широта в радианах (если подаете в градусах, она пересчитается автоматически)</param>
            /// <param name="Longitude">Долгота в радианах (если подаете в градусах, она пересчитается автоматически)</param>
            /// <returns></returns>
            [MultiReturn(new[] { "Coord X, meters", "Coord Y, meters" })]
            public static Dictionary<string, double> TM_FromGeodeticToRectangle (
                Dictionary <string,double> Ellipsoid, 
                Dictionary<string, double> CS_Params, 
                double Latitude, double Longitude)
			        {
      //parameters of CS for simplifying:
      double φ_0 = CS_Params["Latitude of natural origin"];
      double λ_0 = CS_Params["Longitude of natural origin"];
      double k0 = CS_Params["Scale factor at natural origin"];
      double FE = CS_Params["False easting"];
      double FN = CS_Params["False northing"];
      double e1 = Math.Pow(Ellipsoid["Eccentricity2"], 0.5);

      double φ = Latitude;
                double λ = Longitude;

      //Вспомогательные величины для расчета
      double n = Ellipsoid["Reverse flattening"] / (2d - Ellipsoid["Reverse flattening"]);
      //Для упрощения записи далее
      double n2 = Math.Pow(n, 2);
                double n3 = Math.Pow(n, 3);
                double n4 = Math.Pow(n, 4);
                double n5 = Math.Pow(n, 5);
                double n6 = Math.Pow(n, 6);
                double B = Ellipsoid["Axis a"] / (1 + n) * (1 + n2 / 4 + n4 / 64);

                double h1 = n * 0.5 - 2d / 3 * n2 + 5d / 16 * n3 + 41d / 180 * n4;
                double h2 = 13d / 48 * n2 - 3d / 5 * n3 + 557d / 1440 * n4;
                double h3 = 61d / 240 * n3 - 103d / 140 * n4;
                double h4 = 49561d / 161280 * n4;
                //Прямой пересчет из градусов в прямоугольные
                double M0;
                if (φ_0 == 0)
                { M0 = 0; }
                else if (φ_0 == Math.PI / 2)
                { M0 = B * Math.PI / 2; }
                else if (φ_0 == -Math.PI / 2)
                { M0 = -B * Math.PI / 2; }
                else
                {
        double Q0 = Math_asinh(Math.Tan(φ_0)) - e1 * Math_atanh(e1 * Math.Sin(φ_0));
        double β0 = Math.Atan(Math.Sinh(Q0));
                    double ξ00 = Math.Asin(Math.Sin(β0));

                    double ξ10 = h1 * Math.Sin(2 * ξ00);
                    double ξ20 = h2 * Math.Sin(4 * ξ00);
                    double ξ30 = h3 * Math.Sin(6 * ξ00);
                    double ξ40 = h4 * Math.Sin(8 * ξ00);
                    double ξO = ξ00 + ξ10 + ξ20 + ξ30 + ξ40;
                    M0 = B * ξO;

                }
                double Q = Math_asinh(Math.Tan(φ)) - e1 * Math_atanh(e1 * Math.Sin(φ));
      double β = Math.Atan(Math.Sinh(Q));

                double η0 = 0.5 * Math.Log((1 + Math.Cos(β) * Math.Sin(λ - λ_0)) / (1 - Math.Cos(β) * Math.Sin(λ - λ_0)));
                double ξ0 = Math.Asin(Math.Sin(β) * Math.Cosh(η0));

                double ξ1 = h1 * Math.Sin(2 * ξ0) * Math.Cosh(2 * η0);
                double ξ2 = h2 * Math.Sin(4 * ξ0) * Math.Cosh(4 * η0);
                double ξ3 = h3 * Math.Sin(6 * ξ0) * Math.Cosh(6 * η0);
                double ξ4 = h4 * Math.Sin(8 * ξ0) * Math.Cosh(8 * η0);
                double ξ = ξ0 + ξ1 + ξ2 + ξ3 + ξ4;

                double η1 = h1 * Math.Cos(2 * ξ0) * Math.Sinh(2 * η0);
                double η2 = h2 * Math.Cos(4 * ξ0) * Math.Sinh(4 * η0);
                double η3 = h3 * Math.Cos(6 * ξ0) * Math.Sinh(6 * η0);
                double η4 = h4 * Math.Cos(8 * ξ0) * Math.Sinh(8 * η0);
                double η = η0 + η1 + η2 + η3 + η4;

                //Итоговый результат для прямой задачи
                double Easting = FE + k0 * B * η;
                double Northing = FN + k0 * (B * ξ - M0);

                return new Dictionary<string, double>
                {
                    {"Coord X, meters", Easting},
                    {"Coord Y, meters", Northing},
                };
            }
            /// <summary>
            /// Перевод плоских прямоугольных координат в геодезические на данном эллипсоиде
            /// </summary>
            /// <param name="Ellipsoid">Словарь с параметрами эллипсоида (из данной коллекции нодов)</param>
            /// <param name="CS_Params">Словарь с параметрами систем координат (из данной коллекции нодов)</param>
            /// <param name="CoordX">Координата X, метры (Восток/Easting)</param>
            /// <param name="CoordY">Координата Y, метры (Север/North)</param>
            /// <returns></returns>
            [MultiReturn(new[] { "Longitude, radians", "Longitude, grades", "Latitude, radians", "Latitude, grades" })]
    public static Dictionary<string, double> TM_FromRectangleToGeodetic(
                Dictionary<string, double> Ellipsoid,
                Dictionary<string, double> CS_Params,
                double CoordX, double CoordY)
    {
      //parameters of CS for simplifying:
      double φ_0 = CS_Params["Latitude of natural origin"];
      double λ_0 = CS_Params["Longitude of natural origin"];
      double k0 = CS_Params["Scale factor at natural origin"];
      double FE = CS_Params["False easting"];
      double FN = CS_Params["False northing"];
      double e1 = Math.Pow(Ellipsoid["Eccentricity2"], 0.5);
      double E = CoordX;
      double N = CoordY;

      //Вспомогательные величины для расчета
      double n = Ellipsoid["Reverse flattening"] / (2d - Ellipsoid["Reverse flattening"]);
      //Для упрощения записи далее
      double n2 = Math.Pow(n, 2);
      double n3 = Math.Pow(n, 3);
      double n4 = Math.Pow(n, 4);
      double n5 = Math.Pow(n, 5);
      double n6 = Math.Pow(n, 6);
      double B = Ellipsoid["Axis a"] / (1 + n) * (1 + n2 / 4 + n4 / 64);


      double h1 = n * 0.5 - 2d / 3 * n2 + 5d / 16 * n3 + 41d / 180 * n4;
      double h2 = 13d / 48 * n2 - 3d / 5 * n3 + 557d / 1440 * n4;
      double h3 = 61d / 240 * n3 - 103d / 140 * n4;
      double h4 = 49561d / 161280 * n4;
      //Прямой пересчет из градусов в прямоугольные
      double M0;
      if (φ_0 == 0)
      { M0 = 0; }
      else if (φ_0 == Math.PI / 2)
      { M0 = B * Math.PI / 2; }
      else if (φ_0 == -Math.PI / 2)
      { M0 = -B * Math.PI / 2; }
      else
      {

        double Q0 = Math_asinh(Math.Tan(φ_0)) - e1 * Math_atanh(e1 * Math.Sin(φ_0));
        double β0 = Math.Atan(Math.Sinh(Q0));
        double ξ00 = Math.Asin(Math.Sin(β0));

        double ξ10 = h1 * Math.Sin(2 * ξ00);
        double ξ20 = h2 * Math.Sin(4 * ξ00);
        double ξ30 = h3 * Math.Sin(6 * ξ00);
        double ξ40 = h4 * Math.Sin(8 * ξ00);
        double ξO = ξ00 + ξ10 + ξ20 + ξ30 + ξ40;

        M0 = B * ξO;
      }

      //Обратный пересчет
      double h1l = n / 2d - 2d / 3 * n2 + 37d / 96 * n3 - 1d / 360 * n4;
      double h2l = 1d / 48 * n2 + 1d / 15 * n3 - 437d / 1440 * n4;
      double h3l = 17d / 480 * n3 - 37d / 840 * n4;
      double h4l = 4397d / 161280 * n4;
      double ηl = (E - FE) / (B * k0);
      double ξl = ((N - FN) + k0 * M0) / (B * k0);

      double ξ1l = h1l * Math.Sin(2 * ξl) * Math.Cosh(2 * ηl);
      double ξ2l = h2l * Math.Sin(4 * ξl) * Math.Cosh(4 * ηl);
      double ξ3l = h3l * Math.Sin(6 * ξl) * Math.Cosh(6 * ηl);
      double ξ4l = h4l * Math.Sin(8 * ξl) * Math.Cosh(8 * ηl);
      double ξ0l = ξl - (ξ1l + ξ2l + ξ3l + ξ4l);

      double η1l = h1l * Math.Cos(2 * ξl) * Math.Sinh(2 * ηl);
      double η2l = h2l * Math.Cos(4 * ξl) * Math.Sinh(4 * ηl);
      double η3l = h3l * Math.Cos(6 * ξl) * Math.Sinh(6 * ηl);
      double η4l = h4l * Math.Cos(8 * ξl) * Math.Sinh(8 * ηl);
      double η0l = ηl - (η1l + η2l + η3l + η4l);

      double βl = Math.Asin(Math.Sin(ξ0l) / Math.Cosh(η0l));
      double Q1 = Math_asinh(Math.Tan(βl));

      double Q11_1st = Q1 + e1 * Math_atanh(e1 * Math.Tanh(Q1));
      double Q11_2st = Q1 + e1 * Math_atanh(e1 * Math.Tanh(Q11_1st));
      double Q11_3st = Q1 + e1 * Math_atanh(e1 * Math.Tanh(Q11_2st));
      double Q11_4st = Q1 + e1 * Math_atanh(e1 * Math.Tanh(Q11_3st));

      //Итоговые результаты
      double φ1 = Math.Atan(Math.Sinh(Q11_4st));
      double λ1 = λ_0 + Math.Asin(Math.Tanh(η0l) / Math.Cos(βl));

      return new Dictionary<string, double>
                {
                    {"Longitude, radians", λ1},
                   {"Longitude, grades", λ1*180/Math.PI},
                   {"Latitude, radians", φ1},
                   {"Latitude, grades", φ1*180/Math.PI},
                };
    }
    public static double StringToDouble(string str) { return Convert.ToDouble(str); }
    public static double GradesToRadians(double grades) { return grades/180*Math.PI; }
    public static double RadiansToGrades(double radians) { return radians*180/Math.PI; }
    public static string StrFormatOfGraduses (double grades)
		{
      double Int_grad = Math.Floor(grades);
      double Int_min = Math.Floor((grades - Int_grad) * 60);
      string Str_min = Convert.ToString(Int_min);
      if (Int_min < 10) Str_min = 0 + Convert.ToString(Int_min);

      double Float_sec = Math.Round(((grades - Int_grad) * 60 - Int_min) * 60, 2);
      return Convert.ToString(Int_grad) + "°" + Str_min + "'" + Convert.ToString(Float_sec);

    }
    private static double Math_asinh(double x)
    {
      return Math.Log(x + Math.Pow(Math.Pow(x, 2) + 1, 0.5));
    }
    private static double Math_acosh(double x)
    {
      return Math.Log(x + Math.Pow(Math.Pow(x, 2) - 1, 0.5));
    }
    private static double Math_atanh(double x)
    {
      return 0.5 * Math.Log((1 + x) / (1 - x));
    }
    private static double Math_acoth(double x)
    {
      return 0.5 * Math.Log((x + 1) / (x - 1));
    }


  }

    public class GeneralTerms
    {
        private GeneralTerms() { }

        [MultiReturn(new[] { "For SK-42 to WGS-84 EPSG:5044", "For SK-42 to WGS-84 and SK-63 EPSG:1267", "For SK-42 to WGS-84 EPSG:1254", "For SK-95 to WGS-84 EPSG:5043",
        "For SK-95 to WGS-84 EPSG:1281", "MGGT to WGS-84"})]
        public static Dictionary <string,string> DatumList_ToWGS84 ()
		    {
            return new Dictionary<string, string>
                {
                    {"For SK-42 to WGS-84 EPSG:5044", "23.57,-140.95,-79.8,0,-0.35,-0.79,-0.22"},
                    {"For SK-42 to WGS-84 and SK-63 EPSG:1267", "23.92,-141.27,-80.9,0.0,-0.35,-0.82,-0.12"},
                    {"For SK-42 to WGS-84 EPSG:1254", "28.0,-130.0,-95.0,0.0,0.0,0.0,0.0"},
                    {"For SK-95 to WGS-84 EPSG:5043", "24.47,-130.89,-81.56,0.0,0.0,-0.13,-0.22"},
                    {"For SK-95 to WGS-84 EPSG:1281", "24.82,-131.21,-82.66,0.0,0.0,-0.16,-0.12"},
                    {"For MGGT to WGS-84", "316.151,78.924,589.65,1.57273,-2.69209,-2.34693,8.4507"},
                };
        }
        [MultiReturn(new[] { "For SK-42 to PZ-90.11", "For SK-95 to PZ-90.11", "For PZ-90 to PZ-90.11", "For WGS-84 to PZ-90.11",
        "For PZ-90.02 to PZ-90.11", "For GSK-2011 to PZ-90.11", "For SK-42 to GSK-2011", "For SK-95 to GSK-2011",})]
        public static Dictionary<string, string> DatumList_InternalRussia()
        {
            return new Dictionary<string, string>
                {
                    {"For SK-42 to PZ-90.11", "23.557,-140.844,-79.778,-2.30,-346.46,-794.21,-0.228"},
                    {"For SK-95 to PZ-90.11", "24.457,-130.784,-81.538,-2.30,3.54,-134.21,-0.228"},
                    {"For PZ-90 to PZ-90.11", "-1.443,0.156,0.222,-2.30,3.54,-134.21,-0.228"},
                    {"For WGS-84 to PZ-90.11", "-0.013,0.106,0.222,-2.30,3.54,-4.21,-0.008"},
                    {"For PZ-90.02 to PZ-90.11", "-0.373,0.186,0.202,-2.30,3.54,-4.21,-0.008"},
                    {"For GSK-2011 to PZ-90.11", "0.0,0.014,-0.008,-0.562,-0.019,0.053,-0.0006"},
                    {"For SK-42 to GSK-2011", "23.557,-140.858,-79.77,-0.001738,-0.346441,-0.794263,-0.2274"},
                    {"For SK-95 to GSK-2011", "24.457,-130.798,-81.53,-0.001738,0.003559,-0.134263,-0.2274"},
                };
        }
        /// <summary>
        /// Возвращает обратный датум (при необходимости использовать таковой)
        /// </summary>
        /// <param name="CurrentDatumStr">Текущая строчная формулировка датума</param>
        /// <returns></returns>
        public static string ReverseDatumParameters (string CurrentDatumStr)
		    { 
            string ReverseStr = null;
            foreach (string str in CurrentDatumStr.Split(','))
			      {
                ReverseStr = ReverseStr + (Convert.ToDouble(str) * (-1)).ToString() + ',';

            }
            return ReverseStr;

        }
        public static Dictionary<string, double> GetDatumInfo(string InfoAboutDatum)
        {
            //Example of datum's string: 23.57,-140.95,-79.8,0.00,-0.35,-0.79,-0.22
            string[] GetDatumParameters = InfoAboutDatum.Split(',');
            return new Dictionary<string, double>
                {
                    {"X-Offset", Convert.ToDouble(GetDatumParameters[0])},
                    {"Y-Offset", Convert.ToDouble(GetDatumParameters[1])},
                    {"Z-Offset", Convert.ToDouble(GetDatumParameters[2])},
                    {"Rotation angle x, rad", Convert.ToDouble(GetDatumParameters[3])/180/3600*Math.PI},
                    {"Rotation angle y, rad", Convert.ToDouble(GetDatumParameters[4])/180/3600*Math.PI},
                    {"Rotation angle z, rad", Convert.ToDouble(GetDatumParameters[5])/180/3600*Math.PI},
                    {"Scale factor", Convert.ToDouble(GetDatumParameters[6])*Math.Pow (10,-6)},
                };
        }
    public static Dictionary<string, double> Custom_EllipsoidParameters(string Axis_a_meters, string Flattening)
    {
      double a = Convert.ToDouble(Axis_a_meters); //Main axis, meters
      double flattening = 1/Convert.ToDouble(Flattening); //Reverse flatenning = 1/...

      double e2 = flattening * (2 - flattening); //Eccentricity in square

      return new Dictionary<string, double>
                {
                    {"Axis a", a},
                    {"Reverse flattening", flattening},
                    {"Eccentricity2", e2},
                };
    }
    public static Dictionary<string, double> EllipsoidParameters(string EllipsoidName)
        {
            double a = 0d; //Main axis, meters
            double flattening = 0d; //Reverse flatenning = 1/...
            

            switch (EllipsoidName)
            {
                case "Krassowsky 1940, EPSG:7024":
                    a = 6378245d;
                    flattening = 1 / 298.3;
                    break;
                case "GSK-2011, EPSG:1025":
                    a = 6378136.5d;
                    flattening = 1 / 298.2564151;
                    break;
                case "PZ 90.11":
                    a = 6378136d;
                    flattening = 1 / 298.25784;
                    break;
                case "PZ 90/PZ 90.02, EPSG:7054":
                    a = 6378136d;
                    flattening = 1 / 298.257839303;
                    break;
                case "WGS-84, EPSG:7030":
                    a = 6378137d;
                    flattening = 1 / 298.257223563;
                    break;
        case "Bessel 1841, EPSG:7004":
          a = 6377397.155;
          flattening = 1 / 299.1528128;
          break;
      }
          double e2 = flattening * (2 - flattening); //Eccentricity in square

                 return new Dictionary<string, double>
                {
                    {"Axis a", a},
                    {"Reverse flattening", flattening},
                    {"Eccentricity2", e2},
                };
        }
        [MultiReturn(new[] { "Latitude, radians", "Longitude, radians", "Latitude, grades", "Longitude, grades", "Height, meters" })]
    public static Dictionary<string, double> Datum_Recalculation(
             Dictionary<string, double> DatumInfo,
             Dictionary<string, double> SourceEllipsoid,
             Dictionary<string, double> FinishEllipsoid,
             double Latitude, double Longitude, double Height)
    {
      //General parameters between two ellipsoids
      double a = (SourceEllipsoid["Axis a"] + FinishEllipsoid["Axis a"]) / 2;
      double e2 = (SourceEllipsoid["Eccentricity2"] + FinishEllipsoid["Eccentricity2"]) / 2;
      double Δa = FinishEllipsoid["Axis a"] - SourceEllipsoid["Axis a"];
      double Δe2 = FinishEllipsoid["Eccentricity2"] - SourceEllipsoid["Eccentricity2"];
      //double e1 = Math.Pow(SourceEllipsoid["Eccentricity2"], 0.5);

      //Root variables (for simplify reading formulas)
      double φ1 = Latitude;
      double λ1 = Longitude;

      //Helpful parameters
      double M_ell = a * (1 - e2) * Math.Pow(1 - e2 * Math.Pow(Math.Sin(φ1), 2), -1.5); // Радиус кривизны меридиана
      double N_ell = a * Math.Pow(1 - e2 * Math.Pow(Math.Sin(φ1), 2), -0.5); // Радиус кривизны первого вертикала

      //Datum parameters
      double ΔX = DatumInfo["X-Offset"];
      double ΔY = DatumInfo["Y-Offset"];
      double ΔZ = DatumInfo["Z-Offset"];
      double ω_x_rad = DatumInfo["Rotation angle x, rad"];
      double ω_y_rad = DatumInfo["Rotation angle y, rad"];
      double ω_z_rad = DatumInfo["Rotation angle z, rad"];
      double m = DatumInfo["Scale factor"];
      //1.5.1 Вычисление величины поправок к координатам
      double dB = 1 / (M_ell + Height) * (N_ell / a * e2 * Δa * Math.Sin(φ1) * Math.Cos(φ1) + (Math.Pow(N_ell, 2) / Math.Pow(a, 2) + 1) * N_ell * Math.Sin(φ1) * Math.Cos(φ1) * Δe2 / 2 - (ΔX * Math.Cos(λ1) + ΔY * Math.Sin(λ1)) * Math.Sin(φ1) + ΔZ * Math.Cos(φ1)) - ω_x_rad * Math.Sin(λ1) * (1 + e2 * Math.Cos(2 * φ1)) + ω_y_rad * Math.Cos(λ1) * (1 + e2 * Math.Cos(2 * φ1)) - m * e2 * Math.Sin(φ1) * Math.Cos(φ1);
      double dL = 1 / ((N_ell + Height) * Math.Cos(φ1)) * (-ΔX * Math.Sin(λ1) + ΔY * Math.Cos(λ1)) + Math.Tan(φ1) * (1 - e2) * (ω_x_rad * Math.Cos(λ1) + ω_y_rad * Math.Sin(λ1)) - ω_z_rad;
      double dH = -a / N_ell * Δa + N_ell * Δe2 / 2 * Math.Pow(Math.Sin(φ1), 2) + (ΔX * Math.Cos(λ1) + ΔY * Math.Sin(λ1)) * Math.Cos(φ1) + ΔZ * Math.Sin(φ1) - N_ell * e2 * Math.Sin(φ1) * Math.Cos(φ1) * (ω_x_rad * Math.Sin(λ1) - ω_y_rad * Math.Cos(λ1)) + (Math.Pow(a, 2) / N_ell + Height) * m;
      //1.5.2 Вычисление геодезических координат в целевой системе (предварительно)
      double φ11 = (2 * φ1 + dB) / 2;
      double λ11 = (2 * λ1 + dL) / 2;
      double H11 = (2 * Height + dH) / 2;

      //1.5.3 Уточнение параметров
      double dB1 = 1 / (M_ell + Height) * (N_ell / a * e2 * Δa * Math.Sin(φ11) * Math.Cos(φ11) + (Math.Pow(N_ell, 2) / Math.Pow(a, 2) + 1) * N_ell * Math.Sin(φ11) * Math.Cos(φ11) * Δe2 / 2 - (ΔX * Math.Cos(λ11) + ΔY * Math.Sin(λ11)) * Math.Sin(φ11) + ΔZ * Math.Cos(φ11)) - ω_x_rad * Math.Sin(λ11) * (1 + e2 * Math.Cos(2 * φ11)) + ω_y_rad * Math.Cos(λ11) * (1 + e2 * Math.Cos(2 * φ11)) - m * e2 * Math.Sin(φ11) * Math.Cos(φ11);
      double dL1 = 1 / ((N_ell + Height) * Math.Cos(φ11)) * (-ΔX * Math.Sin(λ11) + ΔY * Math.Cos(λ11)) + Math.Tan(φ11) * (1 - e2) * (ω_x_rad * Math.Cos(λ11) + ω_y_rad * Math.Sin(λ11)) - ω_z_rad;
      double dH1 = -a / N_ell * Δa + N_ell * Δe2 / 2 * Math.Pow(Math.Sin(φ11), 2) + (ΔX * Math.Cos(λ11) + ΔY * Math.Sin(λ11)) * Math.Cos(φ11) + ΔZ * Math.Sin(φ11) - N_ell * e2 * Math.Sin(φ11) * Math.Cos(φ11) * (ω_x_rad * Math.Sin(λ11) - ω_y_rad * Math.Cos(λ11)) + (Math.Pow(a, 2) / N_ell + H11) * m;
      //1.6 Вычисление геодезических координат в целевой системе
      double φ2 = (φ1 + dB1);
      double λ2 = (λ1 + dL1);
      double H2 = (Height + dH1);

      return new Dictionary<string, double>
                {
                    {"Latitude, radians", φ2},
                    {"Longitude, radians", λ2},
                    {"Latitude, grades", φ2*180/Math.PI},
                    {"Longitude, grades", λ2*180/Math.PI},
                     {"Height, meters", H2},
                };
    }

    public static Dictionary<string, double> GetCSParameters(string CS_Name)
        {
            //string DataStr;
            //string CS_Needing = null;

            //var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            //using (Stream stream = assembly.GetManifestResourceStream("MapConnection.Resources.CS_Parameters.txt"))

            //using (StreamReader reader = new StreamReader(stream))
            //{
            //    while ((DataStr = reader.ReadLine()) != null)
            //    {

            //        if (DataStr.Contains(CS_Name))
            //        {
            //            CS_Needing = DataStr;
            //            break;
            //        }
            //    }
            //}
            string[] InfoAboutCS = CS_Name.Split(',');
            return new Dictionary<string, double>
                {
                  {"Latitude of natural origin", Convert.ToDouble (InfoAboutCS[1])  * Math.PI / 180},
                  {"Longitude of natural origin", Convert.ToDouble (InfoAboutCS[2])  * Math.PI / 180},
                  {"Scale factor at natural origin", Convert.ToDouble (InfoAboutCS[3])},
                  {"False easting", Convert.ToDouble (InfoAboutCS[4])},
                  {"False northing", Convert.ToDouble (InfoAboutCS[5])},
                };
        }
    
    }

}
