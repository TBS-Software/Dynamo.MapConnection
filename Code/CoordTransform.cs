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

    

}
