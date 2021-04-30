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
using Autodesk.Gis.Map.Platform;
using Autodesk.AutoCAD.Windows;
using Autodesk.DesignScript.Runtime;
using System.Security.Cryptography.X509Certificates;
using System.Net.NetworkInformation;
using System.Xml;
using System.Xml.Linq;

namespace MapConnection
{
	public class CoordinateSystems
	{
    private CoordinateSystems() { }
    /// <summary>
    /// Node GetCurrentCoordinateSystem return the name of current CS (assigned to drawing)
    /// </summary>
    /// <returns></returns>
    public static string GetCurrentCoordinateSystem()
		{
			Autodesk.Gis.Map.Platform.AcMapMap map = Autodesk.Gis.Map.Platform.AcMapMap.GetCurrentMap();
			string wkt = map.GetMapSRS();
			OSGeo.MapGuide.MgCoordinateSystemFactory factory = new OSGeo.MapGuide.MgCoordinateSystemFactory();
			string csCode = factory.ConvertWktToCoordinateSystemCode(wkt);

			return csCode;
		}
    /// <summary>
    /// Node return WKT code of assigned CS of drawing
    /// </summary>
    /// <returns></returns>
    public static string GetWKTFromDrawing ()
		{
      Autodesk.Gis.Map.Platform.AcMapMap map = Autodesk.Gis.Map.Platform.AcMapMap.GetCurrentMap();
      string wkt = map.GetMapSRS();
      return wkt;
    }
    /// <summary>
    /// Assign CS to drawing from WKT code (as string)
    /// </summary>
    /// <param name="wkt"></param>
    public static void AssignCSFromWKT (string wkt)
		{
      CivilDocument c3d_doc = Autodesk.Civil.ApplicationServices.CivilApplication.ActiveDocument;
      var Units_Window = c3d_doc.Settings.DrawingSettings;

      Document m_Doc = Application.DocumentManager.MdiActiveDocument;
      Database db = m_Doc.Database;

      OSGeo.MapGuide.MgCoordinateSystemFactory factory = new OSGeo.MapGuide.MgCoordinateSystemFactory();
      string csCode = factory.ConvertWktToCoordinateSystemCode(wkt);

      using (Transaction tr = db.TransactionManager.StartTransaction())
      {
        Units_Window.UnitZoneSettings.CoordinateSystemCode = csCode;
        tr.Commit();
      }
    }


        /// <summary>
        /// Node GetWKT2Code_ofCSLis return an external file with all codes for input CS's file
        /// </summary>
        /// <param name="XML_MapCSLibrary_path">Path to CS_Library.xml</param>
        /// <param name="Folder_Path">Absolute path to folder where need to create WKT's codes library</param>
        /// <param name="TXT_CS_List_path">Path to file witl Lisf of CS's names</param>
        /// <param name="str_format">If = true, WKT-code if split to strings; if false - it = one string</param>
        /// <returns>String with Full-WKT2 code or result</returns>
        public static void GetWKT2Code_ofCSList(string XML_MapCSLibrary_path, string Folder_Path, string TXT_CS_List_path, bool str_format = false)
        {

            string[] CS_List = File.ReadAllLines(TXT_CS_List_path);
            var guid = Guid.NewGuid();
            string WKT_WritePath = $@"{Folder_Path}\{guid}CSLibrary_WKT.txt";


            XmlDocument CS_Lib = new XmlDocument();
            CS_Lib.Load(XML_MapCSLibrary_path);

            XDocument CS_Lib2 = XDocument.Load(XML_MapCSLibrary_path);
            XNamespace xmlns = "http://www.osgeo.org/mapguide/coordinatesystem";


            XmlElement xRoot = CS_Lib.DocumentElement;
            XmlNamespaceManager XmlNamespace = new XmlNamespaceManager(CS_Lib.NameTable);
            XmlNamespace.AddNamespace("Dictionary", xRoot.OwnerDocument.DocumentElement.NamespaceURI);


            //Обращаемся к словарю с системами координат (текстовой файл)
            for (int i1 = 0; i1 < CS_List.Length; i1++)
            {
                string CS_name = CS_List[i1];
                //Console.WriteLine($"{CS_name}");
                //Инициируем значения переменных
                //Параметры СК текстовые
                string CS_Descr_Name = null;
                string CS_Type_Name = null;
                string CS_Datum_Name = null;
                //Параметры СК численные
                List<string> proj_var = new List<string>();
                double CS_LongLONO = 0d;
                double CS_LatOFO = 0d;
                double CS_Scale = 0d;
                double CS_FE = 0d;
                double CS_FN = 0d;
                //Аффиновы параметры
                double A0 = 0d;
                double B0 = 0d;
                double A1 = 0d;
                double A2 = 0d;
                double B1 = 0d;
                double B2 = 0d;

                //Параметры границы СК
                double CS_BoundaryBox_LatMin_Value = 0d;
                double CS_BoundaryBox_LongMin_Value = 0d;
                double CS_BoundaryBox_LatMax_Value = 0d;
                double CS_BoundaryBox_LongMax_Value = 0d;
                //Параметры осей СК
                string AxisUnits = null;
                string AxisOrder1 = null;
                string AxisOrder2 = null;
                string AxisAbb1 = null;
                string AxisAbb2 = null;
                string Axis1_Direct = null;
                string Axis2_Direct = null;
                //Параметры датума
                string Datum_descr_Name = null;
                string Ellipsoid_Name = null;
                //Параметры эллипсоида
                string Ell_descr_Name = null;
                double Ell_MajorAxis = 0d;
                double Ell_MinorAxis = 0d;
                double Ell_Flattening = 0d;
                //Параметры геодезической трансформации
                List<string> datum_par = new List<string>();
                string GT_type = null;


                //Смотрим на библиотеку XML и делаем выборку с проективными СК
                var query_child_CS = CS_Lib2.Descendants().Where(m1 => m1.Name.LocalName == "ProjectedCoordinateSystem");
                foreach (var q1 in query_child_CS)
                {
                    //Отбираем блок определения СК по нужному имени
                    var child_CS_name = q1.Element(xmlns + "Name").Value;
                    if (child_CS_name == CS_name)
                    {   //Делаем серию выборок, начинаем с описания СК
                        CS_Descr_Name = q1.Element(xmlns + "Description").Value.ToString();
                        CS_Type_Name = q1.Element(xmlns + "Conversion").Element(xmlns + "Projection").Element(xmlns + "OperationMethodId").Value.ToString();
                        foreach (var CS_Parameters in q1.Element(xmlns + "Conversion").Element(xmlns + "Projection").Elements(xmlns + "ParameterValue"))
                        {
                            proj_var.Add(CS_Parameters.Element(xmlns + "Value").Value.ToString());
                        }
                        if (CS_Type_Name == "Transverse Mercator")
                        {
                            CS_LongLONO = Convert.ToDouble(proj_var[0]); //Longitude of natural origin
                            CS_LatOFO = Convert.ToDouble(proj_var[1]); //Latitude of false origin
                            CS_Scale = Convert.ToDouble(proj_var[2]); //Scaling factor for coord differences
                            CS_FE = Convert.ToDouble(proj_var[3]); //False easting
                            CS_FN = Convert.ToDouble(proj_var[4]); //False northing
                        }
                        else if (CS_Type_Name == "Gauss Kruger")
                        {
                            CS_LongLONO = Convert.ToDouble(proj_var[0]); //Longitude of natural origin
                            CS_LatOFO = Convert.ToDouble(proj_var[1]); //Latitude of false origin
                            CS_Scale = 1.0; //Scaling factor for coord differences
                            CS_FE = Convert.ToDouble(proj_var[2]); //False easting
                            CS_FN = Convert.ToDouble(proj_var[3]); //False northing
                        }
                        else if (CS_Type_Name == "Transverse Mercator with Affine Post Process")
                        {
                            CS_LongLONO = Convert.ToDouble(proj_var[0]); //Longitude of natural origin
                            A0 = Convert.ToDouble(proj_var[1]);
                            B0 = Convert.ToDouble(proj_var[2]);
                            A1 = Convert.ToDouble(proj_var[3]);
                            A2 = Convert.ToDouble(proj_var[4]);
                            B1 = Convert.ToDouble(proj_var[5]);
                            B2 = Convert.ToDouble(proj_var[6]);
                            CS_LatOFO = Convert.ToDouble(proj_var[7]); //Latitude of false origin
                            CS_Scale = Convert.ToDouble(proj_var[8]); //Scaling factor for coord differences
                            CS_FE = Convert.ToDouble(proj_var[9]); //False easting
                            CS_FN = Convert.ToDouble(proj_var[10]); //False northing
                        }
                        //Определяем границы зоны действия СК
                        CS_BoundaryBox_LatMin_Value = Convert.ToDouble(q1.Element(xmlns + "DomainOfValidity").Element(xmlns + "Extent").Element(xmlns + "GeographicElement").Element(xmlns + "GeographicBoundingBox").Element(xmlns + "SouthBoundLatitude").Value.ToString());
                        CS_BoundaryBox_LongMin_Value = Convert.ToDouble(q1.Element(xmlns + "DomainOfValidity").Element(xmlns + "Extent").Element(xmlns + "GeographicElement").Element(xmlns + "GeographicBoundingBox").Element(xmlns + "WestBoundLongitude").Value.ToString());
                        CS_BoundaryBox_LatMax_Value = Convert.ToDouble(q1.Element(xmlns + "DomainOfValidity").Element(xmlns + "Extent").Element(xmlns + "GeographicElement").Element(xmlns + "GeographicBoundingBox").Element(xmlns + "NorthBoundLatitude").Value.ToString());
                        CS_BoundaryBox_LongMax_Value = Convert.ToDouble(q1.Element(xmlns + "DomainOfValidity").Element(xmlns + "Extent").Element(xmlns + "GeographicElement").Element(xmlns + "GeographicBoundingBox").Element(xmlns + "EastBoundLongitude").Value.ToString());

                        CS_Datum_Name = q1.Element(xmlns + "DatumId").Value.ToString();
                        //Определяем параметры осей проекции СК
                        //Единица измерения
                        AxisUnits = q1.Element(xmlns + "Axis").Attribute("uom").Value.ToString();
                        //AxisOrder
                        List<string> AxisOrder = new List<string>();
                        //AxisOrder
                        List<string> AxisAbb = new List<string>();
                        //AxisOrder
                        List<string> AxisDir = new List<string>();
                        foreach (var q3_1 in q1.Element(xmlns + "Axis").Elements(xmlns + "CoordinateSystemAxis"))
                        {
                            AxisOrder.Add((q3_1.Element(xmlns + "AxisOrder").Value).ToString());
                            AxisAbb.Add((q3_1.Element(xmlns + "AxisAbbreviation").Value).ToString());
                            AxisDir.Add((q3_1.Element(xmlns + "AxisDirection").Value).ToString());
                        }
                        AxisOrder1 = AxisOrder[0];
                        AxisOrder2 = AxisOrder[1];
                        AxisAbb1 = AxisAbb[0];
                        AxisAbb2 = AxisAbb[1];
                        Axis1_Direct = AxisDir[0];
                        Axis2_Direct = AxisDir[1];
                    }
                }
                //Смотрим на библиотеку XML и отбираем датум с нужным именем
                var query_child_Datum = CS_Lib2.Descendants().Where(m2 => m2.Name.LocalName == "GeodeticDatum");
                foreach (var q2 in query_child_Datum)
                {
                    // Отбираем блок датума по нужному имени
                    var child_Datum_name = q2.Element(xmlns + "Name").Value;
                    if (child_Datum_name == CS_Datum_Name)
                    {
                        //Делаем серию выборок, начинаем с описания датума
                        Datum_descr_Name = (q2.Element(xmlns + "Description").Value).ToString();
                        Ellipsoid_Name = (q2.Element(xmlns + "EllipsoidId").Value).ToString();

                    }
                }


                //Смотрим на библиотеку XML и отбираем эллипсоид с нужным именем
                var query_child_Ellipsoid = CS_Lib2.Descendants().Where(m3 => m3.Name.LocalName == "Ellipsoid");
                foreach (var q3 in query_child_Ellipsoid)
                {
                    //Отбираем блок эллипсоида по нужному имени
                    var child_Ellipsoid_name = q3.Element(xmlns + "Name").Value;
                    if (child_Ellipsoid_name.ToString() == Ellipsoid_Name)
                    {
                        //Делаем серию выборок, начинаем с описания датума
                        Ell_descr_Name = q3.Element(xmlns + "Description").Value.ToString();
                        Ell_MajorAxis = Convert.ToDouble(q3.Element(xmlns + "SemiMajorAxis").Value.ToString());
                        Ell_MinorAxis = Convert.ToDouble(q3.Element(xmlns + "SecondDefiningParameter").Element(xmlns + "SemiMinorAxis").Value.ToString());
                        Ell_Flattening = Ell_MajorAxis / (Ell_MajorAxis - Ell_MinorAxis);

                    }
                }
                //Смотрим на библиотеку XML и отбираем геодезическое преобразование с нужным именем начального датума
                var query_transform = CS_Lib2.Descendants().Where(m4 => m4.Name.LocalName == "Transformation");


                foreach (var q in query_transform)
                {
                    var SourceDatumId = q.Element(xmlns + "SourceDatumId").Value;
                    var TargetDatumId = q.Element(xmlns + "TargetDatumId").Value;

                    if (SourceDatumId == CS_Datum_Name && TargetDatumId == "WGS84")
                    {
                        foreach (var pm in q.Elements(xmlns + "OperationMethod").Elements(xmlns + "ParameterValue"))
                        {
                            datum_par.Add((pm.Element(xmlns + "Value").Value).ToString());
                        }
                        GT_type = q.Element(xmlns + "OperationMethod").Element(xmlns + "OperationMethodId").Value.ToString();
                    }
                }
                //Численные параметры датума
                string a1 = null;
                string a2 = null;
                string a3 = null;
                double a4 = 0d;
                double a5 = 0d;
                double a6 = 0d;
                double a7 = 0d;
                double a7_1 = 0d;

                a1 = datum_par[0];
                a2 = datum_par[1];
                a3 = datum_par[2];
                if (GT_type == "Coordinate Frame Rotation (geog2D domain)")
                {
                    a4 = Convert.ToDouble(datum_par[3]) * 3600; //По умолчанию в градусах, и переводим в угловые секунды
                    a5 = Convert.ToDouble(datum_par[4]) * 3600;
                    a6 = Convert.ToDouble(datum_par[5]) * 3600;
                    a7_1 = Convert.ToDouble(datum_par[6]) * Math.Pow(10, 6);

                    if (a7_1 >= 0) { a7 = 1.0 + a7_1 / Math.Pow(10, 6); }
                    else { a7 = 1.0 - a7_1 / Math.Pow(10, 6); }
                }
                else if (GT_type == "Seven Parameter Transformation")
                {
                    a4 = -Convert.ToDouble(datum_par[3]) * 3600; //По умолчанию в градусах, и переводим в угловые секунды
                    a5 = -Convert.ToDouble(datum_par[4]) * 3600;
                    a6 = -Convert.ToDouble(datum_par[5]) * 3600;
                    a7_1 = Convert.ToDouble(datum_par[6]) * Math.Pow(10, 6);

                    if (a7_1 >= 0) { a7 = 1.0 + a7_1 / Math.Pow(10, 6); }
                    else { a7 = 1.0 - a7_1 / Math.Pow(10, 6); }
                }

                char K = '"';
                string NL = Environment.NewLine;
                if (str_format == true) { NL = null; }
                //Прочая хрень
                string AxisUnit = null;
                if (AxisUnits == "Meter") { AxisUnit = "metre"; }
                //Конструирование WKT-3 формы
                string Full_WKT2 = null;
                //Для проекции "Поперечная Меркатора", геодезическая, со своим датумом
                if (CS_Type_Name == "Transverse Mercator" || CS_Type_Name == "Gauss Kruger")
                {
                    Full_WKT2 = $"BOUNDCRS[{NL}SOURCECRS[{NL}PROJCRS[{K}{CS_name}{K},{NL}BASEGEOGCRS[{K}Unknown datum based upon the {Ellipsoid_Name} ellipsoid{K}," +
                         $"{NL}DATUM[{K}{CS_Datum_Name}{K},{NL}ELLIPSOID[{K}{Ell_descr_Name}{K},{Ell_MajorAxis},{Ell_Flattening:f8},{NL}LENGTHUNIT[{K}metre{K},1,{NL}ID[{K}EPSG{K},9001]]]]," +
                         $"{NL}PRIMEM[{K}Greenwich{K},0,{NL}ANGLEUNIT[{K}degree{K},0.0174532925199433],{NL}ID[{K}EPSG{K},8901]]]," +
                         $"{NL}CONVERSION[{K}{CS_name}{K},{NL}METHOD[{K}Transverse Mercator{K},{NL}ID[{K}EPSG{K},9807]]," +
                         $"{NL}PARAMETER[{K}Latitude of natural origin{K},{CS_LatOFO},{NL}ANGLEUNIT[{K}degree{K},0.0174532925199433],{NL}ID[{K}EPSG{K},8801]]," +
                         $"{NL}PARAMETER[{K}Longitude of natural origin{K},{CS_LongLONO},{NL}ANGLEUNIT[{K}degree{K},0.0174532925199433],{NL}ID[{K}EPSG{K},8802]]," +
                         $"{NL}PARAMETER[{K}Scale factor at natural origin{K},{CS_Scale},{NL}SCALEUNIT[{K}unity{K},1],{NL}ID[{K}EPSG{K},8805]]," +
                         $"{NL}PARAMETER[{K}False easting{K},{CS_FE},{NL}LENGTHUNIT[{K}metre{K},1],{NL}ID[{K}EPSG{K},8806]]," +
                         $"{NL}PARAMETER[{K}False northing{K},{CS_FN},{NL}LENGTHUNIT[{K}metre{K},1],{NL}ID[{K}EPSG{K},8807]]]," +
                         $"{NL}CS[Cartesian,2],{NL}AXIS[{K}({AxisAbb1}){K},{Axis1_Direct},{NL}ORDER[{AxisOrder1}],{NL}LENGTHUNIT[{K}{AxisUnit}{K},1,{NL}ID[{K}EPSG{K},9001]]]," +
                         $"{NL}AXIS[{K}({AxisAbb2}){K},{Axis2_Direct},{NL}ORDER[{AxisOrder2}],{NL}LENGTHUNIT[{K}{AxisUnit}{K},1,{NL}ID[{K}EPSG{ K},9001]]]," +
                         $"{NL}USAGE[BBOX[{CS_BoundaryBox_LatMin_Value},{CS_BoundaryBox_LongMin_Value},{CS_BoundaryBox_LatMax_Value},{CS_BoundaryBox_LongMax_Value}]]]]," +
                         $"{NL}TARGETCRS[{NL}GEOGCRS[{K}WGS 84{K},{NL}DATUM[{K}World Geodetic System 1984{K}," +
                         $"{NL}ELLIPSOID[{K}WGS 84{K},6378137,298.257223563,{NL}LENGTHUNIT[{K}metre{K},1]]],{NL}PRIMEM[{K}Greenwich{K},0,{NL}ANGLEUNIT[{K}degree{K},0.0174532925199433]]," +
                         $"{NL}CS[ellipsoidal,2],{NL}AXIS[{K}geodetic latitude(Lat){K},north,{NL}ORDER[1],{NL}ANGLEUNIT[{K}degree{K},0.0174532925199433]],{NL}AXIS[{K}geodetic longitude(Lon){K},east," +
                         $"{NL}ORDER[2],{NL}ANGLEUNIT[{K}degree{K},0.0174532925199433]],{NL}ID[{K}EPSG{K},4326]]]," +
                         $"{NL}ABRIDGEDTRANSFORMATION[{K}{Datum_descr_Name}{K},{NL}METHOD[{K}Coordinate Frame rotation{K},{NL}ID[{K}EPSG{K},9607]]," +
                         $"{NL}PARAMETER[{K}X-axis translation{K},{a1},{NL}ID[{K}EPSG{K},8605]]," +
                         $"{NL}PARAMETER[{K}Y-axis translation{K},{a2},{NL}ID[{K}EPSG{K},8606]]," +
                         $"{NL}PARAMETER[{K}Z-axis translation{K},{a3},{NL}ID[{K}EPSG{K},8607]]," +
                         $"{NL}PARAMETER[{K}X-axis rotation{K},{a4:f8},{NL}ID[{K}EPSG{K},8608]]," +
                         $"{NL}PARAMETER[{K}Y-axis rotation{K},{a5:f8},{NL}ID[{K}EPSG{K},8609]]," +
                         $"{NL}PARAMETER[{K}Z-axis rotation{K},{a6:f8},{NL}ID[{K}EPSG{K},8610]]," +
                         $"{NL}PARAMETER[{K}Scale difference{K},{a7},{NL}ID[{K}EPSG{K},8611]]]]";
                }
                else if (CS_Type_Name == "Transverse Mercator with Affine Post Process")
                {
                    Full_WKT2 = $"BOUNDCRS[{NL}SOURCECRS[{NL}DERIVEDPROJCRS[{K}{CS_name}{K},{NL}BASEGEOGCRS[{K}Unknown datum based upon the {Ellipsoid_Name} ellipsoid{K}," +
                          $"{NL}DATUM[{K}{CS_Datum_Name}{K},{NL}ELLIPSOID[{K}{Ell_descr_Name}{K},{Ell_MajorAxis},{Ell_Flattening:f8},{NL}LENGTHUNIT[{K}metre{K},1,{NL}ID[{K}EPSG{K},9001]]]]," +
                          $"{NL}PRIMEM[{K}Greenwich{K},0,{NL}ANGLEUNIT[{K}degree{K},0.0174532925199433],{NL}ID[{K}EPSG{K},8901]]]," +
                          $"{NL}CONVERSION[{K}{CS_name}{K},{NL}METHOD[{K}Transverse Mercator{K},{NL}ID[{K}EPSG{K},9807]]," +
                          $"{NL}PARAMETER[{K}Latitude of natural origin{K},{CS_LatOFO},{NL}ANGLEUNIT[{K}degree{K},0.0174532925199433],{NL}ID[{K}EPSG{K},8801]]," +
                          $"{NL}PARAMETER[{K}Longitude of natural origin{K},{CS_LongLONO},{NL}ANGLEUNIT[{K}degree{K},0.0174532925199433],{NL}ID[{K}EPSG{K},8802]]," +
                          $"{NL}PARAMETER[{K}Scale factor at natural origin{K},{CS_Scale},{NL}SCALEUNIT[{K}unity{K},1],{NL}ID[{K}EPSG{K},8805]]," +
                          $"{NL}PARAMETER[{K}False easting{K},{CS_FE},{NL}LENGTHUNIT[{K}metre{K},1],{NL}ID[{K}EPSG{K},8806]]," +
                          $"{NL}PARAMETER[{K}False northing{K},{CS_FN},{NL}LENGTHUNIT[{K}metre{K},1],{NL}ID[{K}EPSG{K},8807]]]]," +
                          $"{NL}DERIVINGCONVERSION[{K}Affine{K},{NL}METHOD[{K}Affine parametric transformation{K},{NL}ID[{K}EPSG{K},9624]]," +
                          $"{NL}PARAMETER[{K}A0{K},{A0},{NL}LENGTHUNIT[{K}metre{K},1],{NL}ID[{K}EPSG{K},8623]]," +
                          $"{NL}PARAMETER[{K}A1{K},{A1},{NL}LENGTHUNIT[{K}metre{K},1],{NL}ID[{K}EPSG{K},8624]]," +
                          $"{NL}PARAMETER[{K}A2{K},{A2},{NL}LENGTHUNIT[{K}metre{K},1],{NL}ID[{K}EPSG{K},8625]]," +
                          $"{NL}PARAMETER[{K}B0{K},{B0},{NL}LENGTHUNIT[{K}metre{K},1],{NL}ID[{K}EPSG{K},8639]]," +
                          $"{NL}PARAMETER[{K}B1{K},{B1},{NL}LENGTHUNIT[{K}metre{K},1],{NL}ID[{K}EPSG{K},8640]]," +
                          $"{NL}PARAMETER[{K}B2{K},{B2},{NL}LENGTHUNIT[{K}metre{K},1],{NL}ID[{K}EPSG{K},8641]]]," +
                          $"{NL}CS[Cartesian,2]," +
                          $"{NL}AXIS[{K}({AxisAbb1}){K},{Axis1_Direct},{NL}ORDER[{AxisOrder1}],{NL}LENGTHUNIT[{K}{AxisUnit}{K},1,{NL}ID[{K}EPSG{K},9001]]]," +
                          $"{NL}AXIS[{K}({AxisAbb2}){K},{Axis2_Direct},{NL}ORDER[{AxisOrder2}],{NL}LENGTHUNIT[{K}{AxisUnit}{K},1,{NL}ID[{K}EPSG{ K},9001]]]," +
                          $"{NL}USAGE[BBOX[{CS_BoundaryBox_LatMin_Value},{CS_BoundaryBox_LongMin_Value},{CS_BoundaryBox_LatMax_Value},{CS_BoundaryBox_LongMax_Value}]]]]," +
                          $"{NL}TARGETCRS[{NL}GEOGCRS[{K}WGS 84{K},{NL}DATUM[{K}World Geodetic System 1984{K}," +
                          $"{NL}ELLIPSOID[{K}WGS 84{K},6378137,298.257223563,{NL}LENGTHUNIT[{K}metre{K},1]]],{NL}PRIMEM[{K}Greenwich{K},0,{NL}ANGLEUNIT[{K}degree{K},0.0174532925199433]]," +
                          $"{NL}CS[ellipsoidal,2],{NL}AXIS[{K}geodetic latitude(Lat){K},north,{NL}ORDER[1],{NL}ANGLEUNIT[{K}degree{K},0.0174532925199433]],{NL}AXIS[{K}geodetic longitude(Lon){K},east,{NL}ORDER[2],{NL}ANGLEUNIT[{K}degree{K},0.0174532925199433]],{NL}ID[{K}EPSG{K},4326]]]," +
                          $"{NL}ABRIDGEDTRANSFORMATION[{K}{Datum_descr_Name}{K},{NL}METHOD[{K}Coordinate Frame rotation{K},{NL}ID[{K}EPSG{K},9607]]," +
                          $"{NL}PARAMETER[{K}X-axis translation{K},{a1},{NL}ID[{K}EPSG{K},8605]]," +
                          $"{NL}PARAMETER[{K}Y-axis translation{K},{a2},{NL}ID[{K}EPSG{K},8606]]," +
                          $"{NL}PARAMETER[{K}Z-axis translation{K},{a3},{NL}ID[{K}EPSG{K},8607]]," +
                          $"{NL}PARAMETER[{K}X-axis rotation{K},{a4:f8},{NL}ID[{K}EPSG{K},8608]]," +
                          $"{NL}PARAMETER[{K}Y-axis rotation{K},{a5:f8},{NL}ID[{K}EPSG{K},8609]]," +
                          $"{NL}PARAMETER[{K}Z-axis rotation{K},{a6:f8},{NL}ID[{K}EPSG{K},8610]]," +
                          $"{NL}PARAMETER[{K}Scale difference{K},{a7},{NL}ID[{K}EPSG{K},8611]]]]";
                }
                using (StreamWriter export_file = new StreamWriter(WKT_WritePath, true, Encoding.Default))
                {
                    export_file.WriteLine(Full_WKT2 + Environment.NewLine);
                    export_file.Close();
                    export_file.Dispose();
                }

            }


        }
        /// <summary>
        /// Create external TXT-file with CS parameters
        /// </summary>
        /// <param name="XML_MapCSLibrary_path">Path to XML-library</param>
        /// <param name="Folder_Path">Path to folder where there will creating file</param>
        /// <param name="TXT_CS_List_path">Path to saved file</param>
    public static void CreateCSList(string XML_MapCSLibrary_path, string Folder_Path, string TXT_CS_List_path)
        {

            string[] CS_List = File.ReadAllLines(TXT_CS_List_path);
            var guid = Guid.NewGuid();
            string WKT_WritePath = $@"{Folder_Path}\{guid}CS_Parameters_List.txt";

            XmlDocument CS_Lib = new XmlDocument();
            CS_Lib.Load(XML_MapCSLibrary_path);

            XDocument CS_Lib2 = XDocument.Load(XML_MapCSLibrary_path);
            XNamespace xmlns = "http://www.osgeo.org/mapguide/coordinatesystem";


            XmlElement xRoot = CS_Lib.DocumentElement;
            XmlNamespaceManager XmlNamespace = new XmlNamespaceManager(CS_Lib.NameTable);
            XmlNamespace.AddNamespace("Dictionary", xRoot.OwnerDocument.DocumentElement.NamespaceURI);


            //Обращаемся к словарю с системами координат (текстовой файл)
            for (int i1 = 0; i1 < CS_List.Length; i1++)
            {
                string CS_name = CS_List[i1];
                //Инициируем значения переменных
                //Параметры СК текстовые
                string CS_Descr_Name = null;
                string CS_Type_Name = null;
                string CS_Datum_Name = null;
                //Параметры СК численные
                List<string> proj_var = new List<string>();
                double CS_LongLONO = 0d;
                double CS_LatOFO = 0d;
                double CS_Scale = 0d;
                double CS_FE = 0d;
                double CS_FN = 0d;

        string Datum_descr_Name = null;
        string Ellipsoid_Name = null;

                //Смотрим на библиотеку XML и делаем выборку с проективными СК
        var query_child_CS = CS_Lib2.Descendants().Where(m1 => m1.Name.LocalName == "ProjectedCoordinateSystem");
                foreach (var q1 in query_child_CS)
                {
                    //Отбираем блок определения СК по нужному имени
                    var child_CS_name = q1.Element(xmlns + "Name").Value;
                    if (child_CS_name == CS_name)
                    {   //Делаем серию выборок, начинаем с описания СК
                        CS_Descr_Name = q1.Element(xmlns + "Description").Value.ToString();
                        CS_Type_Name = q1.Element(xmlns + "Conversion").Element(xmlns + "Projection").Element(xmlns + "OperationMethodId").Value.ToString();
                        foreach (var CS_Parameters in q1.Element(xmlns + "Conversion").Element(xmlns + "Projection").Elements(xmlns + "ParameterValue"))
                        {
                            proj_var.Add(CS_Parameters.Element(xmlns + "Value").Value.ToString());
                        }
                        if (CS_Type_Name == "Transverse Mercator")
                        {
                            CS_LongLONO = Convert.ToDouble(proj_var[0]); //Longitude of natural origin
                            CS_LatOFO = Convert.ToDouble(proj_var[1]); //Latitude of false origin
                            CS_Scale = Convert.ToDouble(proj_var[2]); //Scaling factor for coord differences
                            CS_FE = Convert.ToDouble(proj_var[3]); //False easting
                            CS_FN = Convert.ToDouble(proj_var[4]); //False northing
                        }
                        else if (CS_Type_Name == "Gauss Kruger")
                        {
                            CS_LongLONO = Convert.ToDouble(proj_var[0]); //Longitude of natural origin
                            CS_LatOFO = Convert.ToDouble(proj_var[1]); //Latitude of false origin
                            CS_Scale = 1.0; //Scaling factor for coord differences
                            CS_FE = Convert.ToDouble(proj_var[2]); //False easting
                            CS_FN = Convert.ToDouble(proj_var[3]); //False northing
                        }
                    }
                }


        using (StreamWriter export_file = new StreamWriter(WKT_WritePath, true, Encoding.ASCII))
                {
                    export_file.WriteLine(CS_name + ',' + CS_LatOFO + ',' + CS_LongLONO + ',' + CS_Scale + ',' + CS_FE + ','  + Environment.NewLine);
                }

            }


        }

    }

}
