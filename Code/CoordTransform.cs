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

    [MultiReturn(new[] { "Coord X, meters", "Coord Y, meters" })]
    public static Dictionary<string, double> P2_PZ9011_GeodToRect(double Latitude, double Longitude, Dictionary<string, double> Ellipsoid)
    {
      double pow(double value, int o) { return Math.Pow(value, o); }
      //Step 1 - Исходные данные
      //Параметры эллипсоида
      double a = Ellipsoid["Axis a"]; double b = Ellipsoid["Axis b"]; double e2 = Ellipsoid["Eccentricity2"]; double e1 = Ellipsoid["Second eccentricity"];
      //Initial data
      double B = Latitude; double L = Longitude;

      //Step 2 - Определение номера и осевого меридиана зоны
      double n = Math.Truncate((6 + L * 180 / Math.PI) / 6); //Цулая часть выражения
      double L0 = (6 * n - 3) * Math.PI / 180; //Долгота осевого меридиана зоны, рад

      //Step 3 - Определение разности долгот l заданной точки и осевого меридиана зоны, рад:
      double l = L - L0;

      //Step 4 - Вычисление плоских прямоугольных координат x,y в проекции Гаусса-Крюгера, м и геодезической долготы(?)
      //Step 4.1 - Вычисление S и коэффициентов a1,a2 ...:
      double k = (a - b) / (a + b);
      double S = a / (1 + k) * ((1 + pow(k, 2) / 4 + pow(k, 4) / 64) * B - (1.5 * k - 3 / 16 * pow(k, 3)) * Math.Sin(2 * B)
        + (15 / 16 * pow(k, 2) - 15 / 16 * pow(k, 4)) * Math.Sin(4 * B) - 35 / 48 * k * Math.Sin(6 * B)); //Начальное значение абсциссы, м

      double N = a * Math.Pow((1 - e2 * pow(Math.Sin(B), 2)), 0.5); //Радиус кривизны первого вертикала, м;
      double η = e1 * Math.Cos(B);
      double a1 = N * Math.Cos(B);
      double a2 = 0.5 * Math.Sin(B) * Math.Cos(B);
      double a3 = 1 / 6 * N * pow(Math.Cos(B), 3) * (1 - pow(Math.Tan(B), 2) + pow(η, 2));
      double a4 = 1 / 24 * N * Math.Sin(B) * pow(Math.Cos(B), 3) * (5 - pow(Math.Tan(B), 2) + 9 * pow(η, 2) + 4 * pow(η, 4));
      double a5 = 1 / 120 * N * pow(Math.Cos(B), 5) * (5 - 18 * pow(Math.Tan(B), 2) + pow(Math.Tan(B), 4) + 14 * pow(η, 2) - 58 * pow(η, 2) * pow(Math.Tan(B), 2));
      double a6 = 1 / 720 * N * Math.Sin(B) * pow(Math.Cos(B), 5) * (61 - 58 * pow(Math.Tan(B), 2) + pow(Math.Tan(B), 4) + 270 * pow(η, 2) - 330 * pow(η, 2) * pow(Math.Tan(B), 2));
      double a7 = 1 / 5040 * N * pow(Math.Cos(B), 7) * (61 - 479 * pow(Math.Tan(B), 2) + 179 * pow(Math.Tan(B), 4) - pow(Math.Tan(B), 6));
      double a8 = 1 / 40320 * N * Math.Sin(B) * pow(Math.Cos(B), 7) * (1385 - 3111 * pow(Math.Tan(B), 2) + 543 * pow(Math.Tan(B), 4) - pow(Math.Tan(B), 6));

      double x = S + a2 * pow(l, 2) + a4 * pow(l, 4) + a6 * pow(l, 6) + a8 * pow(l, 8);
      double y = a1 * l + a3 * pow(l, 3) + a5 * pow(l, 5) + a7 * pow(l, 7);

      return new Dictionary<string, double>
      {
        {"Coord X, meters", x},
        {"Coord Y, meters", y },
      };
    }
    [MultiReturn(new[] { "Latitude, rad", "Longitude, rad" })]
    public static Dictionary <string,double> P2_PZ9011_RectToGeod(double x, double y, int n, Dictionary<string, double> Ellipsoid)
    {
      double pow(double value, int o) { return Math.Pow(value, o); }
      //Step 0 - Исходные данные

      //Параметры эллипсоида
      double a = Ellipsoid["Axis a"]; double b = Ellipsoid["Axis b"]; double e2 = Ellipsoid["Eccentricity2"]; double e1 = Ellipsoid["Second eccentricity"];

      //Step 1 - Выделание из условной координаты y1 номера зоны n:
      double y1 = y + (5 + 10 * n) * 100000d;
      n = Convert.ToInt32(Math.Truncate(y1 * pow(10, -6)));
      //Step 2 - Вычисление долготы L0 осевого меридиана зоны, рад
      double L0 = (6 * n - 3) * Math.PI / 180;
      //Step 3 - Вычисление значения ординаты y, м:
      y = y1 - (5 + 10 * n) * pow(10, 5);
      //Step 4 - Вычмсление геодезических координат, рад:
      //Step 4.1 Вычисление коэффициентов A1,A2... и Bx
      double e0 = 1 - 1 / 4 * e2 - 3 / 64 * pow(e2, 2) - 2 / 256 * pow(e2, 3) - 175 / 16384 * pow(e2, 4) - 411 / 65536 * pow(e2, 5);
      double C2 = 3 / 8 * e2 + 3 / 16 * pow(e2, 4) + 213 / 2048 * pow(e2, 3) + 255 / 4096 * pow(e2, 4) + 166479 / 655360 * pow(e2, 5);
      double C4 = 21 / 256 * pow(e2, 4) + 21 / 256 * pow(e2, 3) + 533 / 8192 * pow(e2, 4) - 120563 / 327680 * pow(e2, 5);
      double C6 = 151 / 6144 * pow(e2, 3) + 147 / 4096 * pow(e2, 4) + 2732071 / 9175040 * pow(e2, 5);
      double C8 = 1097 / 131072 * pow(e2, 4) - 273697 / 4587520 * pow(e2, 5);
      double Bx = x / (a * e0) + C2 * Math.Sin(2 * x / (a * e0)) + C4 * Math.Sin(4 * x / (a * e0)) + +C6 * Math.Sin(6 * x / (a * e0)) + C8 * Math.Sin(8 * x / (a * e0));

      double Nx = a / (Math.Pow(1 - e2 * pow(Math.Sin(Bx), 2), 0.5));
      double ηx = e1 * Math.Cos(Bx);
      double Vx2 = 1 + pow(ηx, 2);

      double tg2Bx = pow(Math.Tan(Bx), 2);
      double tg4Bx = pow(Math.Tan(Bx), 4);
      double tg6Bx = pow(Math.Tan(Bx), 6);

      double A1 = 1 / (Nx * Math.Cos(Bx));
      double A2 = -0.5 * Math.Tan(Bx) * Vx2 / pow(Nx, 2);
      double A3 = -1 / 6 * A1 / pow(Nx, 2) * (1 + 2 * tg2Bx + pow(ηx, 2));
      double A4 = -1 / 12 * A2 / pow(Nx, 2) * (5 + 3 * tg2Bx + pow(ηx, 2) - 9 * pow(ηx, 2) * tg2Bx - 4 * pow(ηx, 4));
      double A5 = 1 / 120 * A1 / pow(Nx, 4) * (5 + 28 * tg2Bx + 24 * tg4Bx + 6 * pow(ηx, 2) + 8 * pow(ηx, 2) * tg2Bx);
      double A6 = 1 / 360 * A2 / pow(Nx, 4) * (61 + 90 * tg2Bx + 45 * tg4Bx + 46 * pow(ηx, 2) - 252 * pow(ηx, 2) * tg2Bx - 90 * pow(ηx, 2) * tg4Bx);
      double A7 = -1 / 5040 * A1 / pow(Nx, 6) * (61 + 662 * tg2Bx + 1320 * tg4Bx + 720 * tg6Bx);
      double A8 = -1 / 20160 * A2 / pow(Nx, 6) * (1385 + 3633 * tg2Bx + 4095 * tg4Bx + 1575 * tg6Bx);

      double B = Bx + A2 * pow(y, 2) + A4 * pow(y, 4) + A6 * pow(y, 6) + A8 * pow(y, 8);
      double l = A1 * y + A3 * pow(y, 3) + A5 * pow(y, 5) + A7 * pow(y, 7);
      double L = L0 + l;

      return new Dictionary<string, double>
      {
        {"Latitude, rad", B},
        {"Longitude, rad", L },
      };
    }

    [MultiReturn(new[] { "Latitude, rad", "Longitude, rad" })]
    public static Dictionary<string, double> PZ9011_GeodToRect(double Latitude, double Longitude)
    {
      double pow(double value, int o) { return Math.Pow(value, o); }
      //Step 0 - Исходные данные
      double B = Latitude; double L = Longitude;


      return new Dictionary<string, double>
      {
        {"Latitude, rad", B},
        {"Longitude, rad", L },
      };
    }
  }

    

}
