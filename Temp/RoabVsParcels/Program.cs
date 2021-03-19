using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Security.Cryptography.X509Certificates;
using System.Xml;
using System.Xml.Linq;
using System.Reflection;

namespace ConsoleApp8
{
	class Program
	{
		static void Main(string[] args)
		{
			MID_LinePolygonsIntersection(@"C:\Users\GeorgKeneberg\Documents\Temp\OD_Line.mif", @"C:\Users\GeorgKeneberg\Documents\Temp\OD_Parcels.mif", @"C:\Users\GeorgKeneberg\Documents\Temp\OD_Parcels.mid");
			Console.ReadKey();
		}
		public static void MID_LinePolygonsIntersection(string MIF_PathToLine, string MIF_PathToPolygons, string MID_Attr)
		{
			var guid = Guid.NewGuid();
			string LogPath = $@"C:\Users\GeorgKeneberg\Documents\Temp\{guid}.txt";
			string PathToTempF = $@"C:\Users\{Environment.UserName}\AppData\Local\Temp";

			StringBuilder Line = new StringBuilder();
			int LinePoints = 0;
			foreach (string str in File.ReadLines (MIF_PathToLine))
			{
				bool IsNumber = true;
				foreach (char symbol in str.Replace(" ", String.Empty))
				{
					if (!char.IsDigit(symbol))
					{
						IsNumber = false;
					}
					break;
				}
				if (IsNumber == true) { LinePoints++; Line.Append(str + ' '); }//.Replace(' ',',')
			}
			Line.Remove(Line.Length - 1, 1);
			//File.AppendAllText((PathToTempF + $@"\Line_{guid}.txt"), Line.ToString());
			//Line.Clear();


			//Console.WriteLine(Line.ToString());
			//Чтение атриубтов линий (заголовки)
			StringBuilder FinishFile = new StringBuilder();
			StringBuilder TempPolygons = new StringBuilder();

			FinishFile.Append("Mumber;StartPoint;EndPoint;Length");
			int a1 = 0;
			int a2 = 0;
			foreach (string str in File.ReadLines(MIF_PathToPolygons))
			{
				a1++;
				if (str.Contains("COLUMNS") == true) { a2 = Convert.ToInt32(str.Split(' ')[1]); break; }
			}
			//Console.WriteLine(a1);

			foreach (string str in File.ReadLines(MIF_PathToPolygons).Skip(a1)) 
			{
				if (str == "DATA") break;
				
				string AttName = str.Remove(0, 4).Split(' ')[0];
				FinishFile.Append(";" + AttName);
				
			}
			//Console.WriteLine(FinishFile.ToString());
			//Читаем файл с полигонами и записываем во временный файл точки вершины
			int Counter1 = 0;
			//Console.WriteLine(a1 + a2 + 1);
			foreach (string str in File.ReadLines(MIF_PathToPolygons).Skip(a1 + a2+2))
			{
				//Counter1++;
				if (str.Contains("REGION"))
				{
					TempPolygons.Remove(TempPolygons.Length - 1, 1);
					Counter1++;
					TempPolygons.Append(Environment.NewLine); //+ Counter1 + Environment.NewLine
					File.AppendAllText((PathToTempF + $@"\Polygons_{guid}.txt"), TempPolygons.ToString());
					TempPolygons.Clear();
				}
				else
				{
					bool IsNumber = true;
					foreach (char symbol in str.Replace(" ", String.Empty)) //Пробелы между числами
					{
						if (!char.IsDigit(symbol))
						{
							IsNumber = false;
						}
						break;
					}
					if (IsNumber == true && str.Length > 3) TempPolygons.Append(str + ' '); //&& str.Length > 3 IsNumber == true char.IsDigit(str[0]) .Replace(' ', ',')
					else continue;
				}
			}
			TempPolygons.Remove(TempPolygons.Length - 1, 1);
			File.AppendAllText((PathToTempF + $@"\Polygons_{guid}.txt"), TempPolygons.ToString());
			TempPolygons.Clear();
			int CounterPlg = 0;
			int CounterIntersectionCases = 0;

			foreach (string str in File.ReadLines(PathToTempF + $@"\Polygons_{guid}.txt"))
			{
				CounterPlg++;
				string[] SingleStr = str.Split(' ');
				AreVectorsIsIntersect = false;
				for (int i2 = 0; i2 <= LinePoints -3; i2 += 4) //Обход сегментов линии
				{	
					double L1x = Convert.ToDouble(SingleStr[i2]);
					double L1y = Convert.ToDouble(SingleStr[i2+1]);

					double L2x = Convert.ToDouble(SingleStr[i2+3]);
					double L2y = Convert.ToDouble(SingleStr[i2 + 4]);

					for (int i1 = 0; i1 < SingleStr.Length - 3; i1 += 4)
					{

						double x1 = Convert.ToDouble(SingleStr[i1]);
						double y1 = Convert.ToDouble(SingleStr[i1 + 1]);
						double x2 = Convert.ToDouble(SingleStr[i1 + 2]);
						double y2 = Convert.ToDouble(SingleStr[i1 + 3]);
						//Проверка
						VectorIntersection(new Dictionary<int, double>
						{
							{ 1,L1x },
							{ 2,L1y },
							{ 3,L2x },
							{ 4,L2y },
							{ 5,x1 },
							{ 6,y1 },
							{ 7,x2 },
							{ 8,y2 },

						});
						if (AreVectorsIsIntersect == true)
						{
							CounterIntersectionCases++;//Счетчик пересечений
							TempPolygons.AppendLine(CounterIntersectionCases.ToString() +';'
								+ IP_X.ToString() + ';'
								+ IP_Y.ToString() + ';'
								+ L1x.ToString() + ';'
								+ L1y.ToString());
						}
					}
					
				}
				////При условии что пересечение было - добавляем ObjectData
				//if (AreVectorsIsIntersect == true) { //Чтение файла атрибутов}
			}

			//Console.WriteLine(TempPolygons.ToString());

		}
		public static bool AreVectorsIsIntersect; //Boolean of lines intersection
																											//public static Point IntersectByVectors; //Value of point
		public static int Counter = 0;
		public static double IP_X;
		public static double IP_Y;
		public static void VectorIntersection(Dictionary<int, double> Coordinates)
		{
			double a1 = -(Coordinates[4] - Coordinates[2]);
			double b1 = Coordinates[3] - Coordinates[1];
			double d1 = -(a1 * Coordinates[1] + b1 * Coordinates[2]);

			double a2 = -(Coordinates[8] - Coordinates[6]);
			double b2 = Coordinates[7] - Coordinates[5];
			double d2 = -(a2 * Coordinates[5] + b2 * Coordinates[6]);

			//подставляем концы отрезков, для выяснения в каких полуплоскотях они
			double seg1_line2_start = a2 * Coordinates[1] + b2 * Coordinates[2] + d2;
			double seg1_line2_end = a2 * Coordinates[3] + b2 * Coordinates[4] + d2;

			double seg2_line1_start = a1 * Coordinates[5] + b1 * Coordinates[6] + d1;
			double seg2_line1_end = a1 * Coordinates[7] + b1 * Coordinates[8] + d1;

			//если концы одного отрезка имеют один знак, значит он в одной полуплоскости и пересечения нет.
			if (seg1_line2_start * seg1_line2_end <= 0 && seg2_line1_start * seg2_line1_end <= 0)
			{
				AreVectorsIsIntersect = true;
				double u = seg1_line2_start / (seg1_line2_start - seg1_line2_end);
				IP_X = Coordinates[1] + u * (Coordinates[3] - Coordinates[1]);
				IP_Y = Coordinates[2] + u * (Coordinates[4] - Coordinates[2]);
			}
		}
	}
}
