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
    //public static Dictionary<string, object> SourceCS_Parameters(string CS_Description)
    //{
    //  string SourceCS = null;

    //  char K = '"';
    //  string[] WKT1 = SourceCS.Split(new char[] { ',' });
    //  string s_Proj_type1 = WKT1[17].Substring(WKT1[17].IndexOf(K) + 1, WKT1[17].Length - WKT1[17].IndexOf(K) - 2);
    //  string s_Ell_Name1 = WKT1[3].Substring(WKT1[3].IndexOf(K) + 1, WKT1[3].Length - WKT1[3].IndexOf(K) - 2);
    //  double Ell_a_1 = Convert.ToDouble(WKT1[4]);
    //  double Ell_alfa_1 = 1 / Convert.ToDouble(WKT1[5]);
    //  double φ_1 = Convert.ToDouble(WKT1[21]) * Math.PI / 180; //Latitude-широта
    //  double λ_1 = Convert.ToDouble(WKT1[27]) * Math.PI / 180; //Longitude - долгота
    //  double k0_1 = Convert.ToDouble(WKT1[33]) * 1.0;
    //  double FE_1 = Convert.ToDouble(WKT1[39]);
    //  double FN_1 = Convert.ToDouble(WKT1[45]);

    //  return new Dictionary<string, object>
    //  {
    //    {"Source ellipsoid name", s_Ell_Name1 },
    //    {"Length of main axis (a) of ellipsoid", Ell_a_1 },
    //    {"1/Inverse flattening", Ell_alfa_1 },
    //    {"Latitude of natural origin", φ_1 },
    //    {"Longitude of natural origin",λ_1 },
    //    { "Scale factor at natural origin", k0_1},
    //    {"False easting",FE_1 },
    //    {"False northing",FN_1 },
    //  };

    //}
    //public static Dictionary<string, object> FinishCS_Parameters(string CS_Description)
    //{
    //  string FinishCS = null;
    //  string Str = null;
    //  var assembly = System.Reflection.Assembly.GetExecutingAssembly();
    //  using (Stream stream = assembly.GetManifestResourceStream("MapConnection.Resources.WKT-RusMSK.txt"))
    //  using (StreamReader reader = new StreamReader(stream))
    //  {
    //    while ((Str = reader.ReadLine()) != null)
    //    {
    //      if (Str.Contains(CS_Description) == true)
    //      {
    //        FinishCS = Str;
    //        break;
    //      }
    //    }
    //  }

    //  int IndexOf_Lat = FinishCS.IndexOf("Latitude of natural origin") + 28;
    //  int IndexOf_Long = FinishCS.IndexOf("Longitude of natural origin") + 29;
    //  int IndexOf_Scale = FinishCS.IndexOf("Scale factor at natural origin") + 32;
    //  int IndexOf_FE = FinishCS.IndexOf("False easting") + 15;
    //  int IndexOf_FN = FinishCS.IndexOf("False northing") + 15;

    //  double CS_Lat = Convert.ToDouble(FinishCS.Substring(IndexOf_Lat).Split(',')[0]) * Math.PI / 180;
    //  double CS_Long = Convert.ToDouble(FinishCS.Substring(IndexOf_Long).Split(',')[0]) * Math.PI / 180;
    //  double CS_Scale = Convert.ToDouble(FinishCS.Substring(IndexOf_Scale).Split(',')[0]);
    //  double CS_FE = Convert.ToDouble(FinishCS.Substring(IndexOf_FE).Split(',')[0]);
    //  double CS_FN = Convert.ToDouble(FinishCS.Substring(IndexOf_FN).Split(',')[0]);

    //  return new Dictionary<string, object>
    //  {
    //    { "Latitude of natural origin", CS_Lat },
    //    {"Longitude of natural origin",CS_Long },
    //    { "Scale factor at natural origin",CS_Scale},
    //    {"False easting",CS_FE },
    //    {"False northing",CS_FN },
    //  };

    //}
    [MultiReturn(new[] { "Coord X", "Coord Y", "Latitude grades", "Longitude grades" })]
    public static Dictionary<string, object> Calc_ProjTM(string Source_CS_Name, string Finish_CS_Name, string X, string Y)
    {
      string Source_CS = null;
      string Finish_CS = null;
      List<string> ResultCS = new List<string>();

      string Str = null;
      var assembly = System.Reflection.Assembly.GetExecutingAssembly();
      using (Stream stream = assembly.GetManifestResourceStream("MapConnection.Resources.WKT-RusMSK.txt"))
      using (StreamReader reader = new StreamReader(stream))
      {
        while ((Str = reader.ReadLine()) != null)
        {
          if (Str.Contains(Source_CS_Name) == true)
          {
            Source_CS = Str;
          }
          else if (Str.Contains(Finish_CS_Name) == true)
          {
            Finish_CS = Str;
          }
        }
      }

      //Инициация определения системы координат
      //Чтение параметров МСК исходной системы
      char K = '"';
      string[] WKT1 = Source_CS.Split(new char[] { ',' });
      string s_Proj_type1 = WKT1[17].Substring(WKT1[17].IndexOf(K) + 1, WKT1[17].Length - WKT1[17].IndexOf(K) - 2);
      string s_Ell_Name1 = WKT1[3].Substring(WKT1[3].IndexOf(K) + 1, WKT1[3].Length - WKT1[3].IndexOf(K) - 2);
      double Ell_a_1 = Convert.ToDouble(WKT1[4]);
      double Ell_alfa_1 = 1 / Convert.ToDouble(WKT1[5]);
      double φ_1 = Convert.ToDouble(WKT1[21]) * Math.PI / 180; //Latitude-широта
      double λ_1 = Convert.ToDouble(WKT1[27]) * Math.PI / 180; //Longitude - долгота
      double k0_1 = Convert.ToDouble(WKT1[33]) * 1.0;
      double FE_1 = Convert.ToDouble(WKT1[39]);
      double FN_1 = Convert.ToDouble(WKT1[45]);

      string[] WKT2 = Finish_CS.Split(new char[] { ',' });
      string s_Proj_type2 = WKT2[17].Substring(WKT2[17].IndexOf(K) + 1, WKT2[17].Length - WKT2[17].IndexOf(K) - 2);
      string s_Ell_Name2 = WKT2[3].Substring(WKT2[3].IndexOf(K) + 1, WKT2[3].Length - WKT2[3].IndexOf(K) - 2);
      double Ell_a_2 = Convert.ToDouble(WKT2[4]);
      double Ell_alfa_2 = 1 / Convert.ToDouble(WKT2[5]);
      double φ_2 = Convert.ToDouble(WKT2[21]) * Math.PI / 180; //Latitude-широта
      double λ_2 = Convert.ToDouble(WKT2[27]) * Math.PI / 180; //Longitude - долгота
      double k0_2 = Convert.ToDouble(WKT2[33]) * 1.0;
      double FE_2 = Convert.ToDouble(WKT2[39]);
      double FN_2 = Convert.ToDouble(WKT2[45]);

      //Параметры эллипсоида
      //1.2 Геометрические параметры эллипсоидов
      double p_rad = 206264.80625; //Число угловых секунд в 1 радиане


      double Ell_e1_1 = Math.Pow((Ell_alfa_1 * (2d - Ell_alfa_1)), 0.5);//Вычисление величины эксцентриситета 1-го элл
      double Ell_e2_1 = 2 * Ell_alfa_1 - Math.Pow(Ell_alfa_1, 2); //Вычисление эксцентриситета для второго эллипсоида в квадрате


      //Параметры для второго эллипсоида - принимаются равными для WGS-84
      double Ell_e1_2 = Math.Pow((Ell_alfa_2 * (2d - Ell_alfa_2)), 0.5); //Вычисление величины эксцентриситета 2-го элл
      double Ell_e2_2 = Math.Pow(Ell_e1_2, 2); //Вычисление величины эксцентриситета в квадрате 2-го элл

      //Инициация исходных данных
      double CoordX = Convert.ToDouble(X);
      double CoordY = Convert.ToDouble(Y);

      //Вспомогательные величины для расчета
      double n = Ell_alfa_1 / (2d - Ell_alfa_1);
      //Для упрощения записи далее
      double n2 = Math.Pow(n, 2);
      double n3 = Math.Pow(n, 3);
      double n4 = Math.Pow(n, 4);
      double n5 = Math.Pow(n, 5);
      double n6 = Math.Pow(n, 6);
      double B = Ell_a_1 / (1 + n) * (1 + n2 / 4 + n4 / 64);

      double h1 = n * 0.5 - 2d / 3 * n2 + 5d / 16 * n3 + 41d / 180 * n4;
      double h2 = 13d / 48 * n2 - 3d / 5 * n3 + 557d / 1440 * n4;
      double h3 = 61d / 240 * n3 - 103d / 140 * n4;
      double h4 = 49561d / 161280 * n4;

      //Прямой пересчет из градусов в прямоугольные
      double M0 = 0d;
      if (φ_1 == 0)
      { M0 = 0; }
      else if (φ_1 == Math.PI / 2)
      { M0 = B * Math.PI / 2; }
      else if (φ_1 == -Math.PI / 2)
      { M0 = -B * Math.PI / 2; }
      else
      {
        double Q0 = Math.Log(Math.Tan(φ_1) + Math.Pow((Math.Pow(Math.Tan(φ_1), 2) + 1), 0.5)) - Ell_e1_1 * 0.5 * Math.Log((1 + Ell_e1_1 * Math.Sin(φ_1)) / (1 - Ell_e1_1 * Math.Sin(φ_1)));
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
      double ηl = (CoordX - FE_1) / (B * k0_1);
      double ξl = ((CoordY - FN_1) + k0_1 * M0) / (B * k0_1);

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
      double Q1 = Math.Log(Math.Tan(βl) + Math.Pow((Math.Pow(Math.Tan(βl), 2) + 1), 0.5));
      //Console.WriteLine($"{βl} {Q1}");
      double Q11_1st = Q1 + Ell_e1_1 * 0.5 * Math.Log((1d + Ell_e1_1 * Math.Tanh(Q1)) / (1 - Ell_e1_1 * Math.Tanh(Q1)));
      double Q11_2st = Q1 + Ell_e1_1 * 0.5 * Math.Log((1d + Ell_e1_1 * Math.Tanh(Q11_1st)) / (1 - Ell_e1_1 * Math.Tanh(Q11_1st)));
      double Q11_3st = Q1 + Ell_e1_1 * 0.5 * Math.Log((1d + Ell_e1_1 * Math.Tanh(Q11_2st)) / (1 - Ell_e1_1 * Math.Tanh(Q11_2st)));
      double Q11_4st = Q1 + Ell_e1_1 * 0.5 * Math.Log((1d + Ell_e1_1 * Math.Tanh(Q11_3st)) / (1 - Ell_e1_1 * Math.Tanh(Q11_3st)));
      //Итоговые результаты (геодезические координаты на первом эллипсоиде)
      double φ1 = Math.Atan(Math.Sinh(Q11_4st));
      double λ1 = λ_1 + Math.Asin(Math.Tanh(η0l) / Math.Cos(βl));

      if (s_Ell_Name1 != s_Ell_Name2)
      {
        //Если нужен датум
      }
      //Console.WriteLine($"{φ1 * 180 / Math.PI} {λ1 * 180 / Math.PI}");
      //Вспомогательные величины для расчета
      //double n = Ell_alfa_2 / (2d - Ell_alfa_2);
      ////Для упрощения записи далее
      //double n2 = Math.Pow(n, 2);
      //double n3 = Math.Pow(n, 3);
      //double n4 = Math.Pow(n, 4);
      //double n5 = Math.Pow(n, 5);
      //double n6 = Math.Pow(n, 6);
      //double B = Ell_a_2 / (1 + n) * (1 + n2 / 4 + n4 / 64);

      //double h1 = n * 0.5 - 2d / 3 * n2 + 5d / 16 * n3 + 41d / 180 * n4;
      //double h2 = 13d / 48 * n2 - 3d / 5 * n3 + 557d / 1440 * n4;
      //double h3 = 61d / 240 * n3 - 103d / 140 * n4;
      //double h4 = 49561d / 161280 * n4;

      //Прямой пересчет из градусов в прямоугольные
      double M0_2 = 0d;
      if (φ_2 == 0)
      { M0_2 = 0; }
      else if (φ_2 == Math.PI / 2)
      { M0_2 = B * Math.PI / 2; }
      else if (φ_2 == -Math.PI / 2)
      { M0_2 = -B * Math.PI / 2; }
      else
      {
        double Q0 = Math.Log(Math.Tan(φ_2) + Math.Pow((Math.Pow(Math.Tan(φ_2), 2) + 1), 0.5)) - Ell_e1_2 * 0.5 * Math.Log((1 + Ell_e1_2 * Math.Sin(φ_2)) / (1 - Ell_e1_2 * Math.Sin(φ_2)));
        double β0 = Math.Atan(Math.Sinh(Q0));
        double ξ00 = Math.Asin(Math.Sin(β0));

        double ξ10 = h1 * Math.Sin(2 * ξ00);
        double ξ20 = h2 * Math.Sin(4 * ξ00);
        double ξ30 = h3 * Math.Sin(6 * ξ00);
        double ξ40 = h4 * Math.Sin(8 * ξ00);
        double ξO = ξ00 + ξ10 + ξ20 + ξ30 + ξ40;
        M0_2 = B * ξO;

      }
      double Q = Math.Log(Math.Tan(φ1) + Math.Pow((Math.Pow(Math.Tan(φ1), 2) + 1), 0.5)) - Ell_e1_2 * 0.5 * Math.Log((1 + Ell_e1_2 * Math.Sin(φ1)) / (1 - Ell_e1_2 * Math.Sin(φ1)));
      double β = Math.Atan(Math.Sinh(Q));

      double η0 = 0.5 * Math.Log((1 + Math.Cos(β) * Math.Sin(λ1 - λ_2)) / (1 - Math.Cos(β) * Math.Sin(λ1 - λ_2)));
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
      double CoordX_2 = FE_2 + k0_2 * B * η;
      double CoordY_2 = FN_2 + k0_2 * (B * ξ - M0_2);

      return new Dictionary<string, object>
      {
        { "Coord X",CoordX_2 },
        { "Coord Y",CoordY_2 },
        {"Latitude grades", φ1},
        {"Longitude grades", λ1 },
      };
    }
  }
}
