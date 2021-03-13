public static string CStoCategory() //string Category_name
		{
			MgCoordinateSystemFactory coordSysFactory = new MgCoordinateSystemFactory();
			MgCoordinateSystemCatalog csCatalog = coordSysFactory.GetCatalog();

			MgCoordinateSystemDictionary csDict = csCatalog.GetCoordinateSystemDictionary();

			MgCoordinateSystemEnum csDictEnum = csDict.GetEnum();
			int csCount = csDict.GetSize();
			MgStringCollection csNames = csDictEnum.NextName(csCount);


			MgCoordinateSystem cs = null;
			string csName = null;
			bool csProtect;

			//Перебираем словарь СК
			int i1 = 0;
			for (int i2 = 0; i2 < csCount; i2++)
			{
				csName = csNames.GetItem(i2);
				cs = csDict.GetCoordinateSystem(csName);
				csProtect = cs.IsProtected();

				//Условие - если данная СК пользовательская (импортированная из XML)
				if (csProtect == false)
				{   //Пусть для примера для созданной категории с одной внесеной ск по имени "NewCategory"
					new MgCoordinateSystemFactory().GetCatalog().GetCategoryDictionary().GetCategory("NewCategory").AddCoordinateSystem(csName);
					i1++;
				}
			}
			return i1.ToString();
		}
		
[MultiReturn(new[] { "Ell_name", "Ell_a", "Ell_flat",  "Datum_X", "Datum_Y", "Datum_Z", "Datum_ωx", "Datum_ωy", 
	"Datum_ωz", "Datum_m", "CS_Name", "CS_CentrMerid", "CS_OrigLat", "CS_FN", "CS_FE", "CS_k", "Units", })]
		public static Dictionary<string, object> GetGeodedictDescrOfCS_TM (string Full_WKT_string)
        {
			var guid = Guid.NewGuid();
			//Запешем определение WKT во временный файл (ограничение в 255 символов для строки)
			//string writePath = $@"%TEMP%\{guid}_Dynamo.log";
			string writePath = $@"E:\Work\Temp\Temp\{guid}_Dynamo.log";
			using (StreamWriter WKT_file = new StreamWriter(writePath, true, Encoding.Default)) //Запись в итоговый файл первой строки PTS-файла
			{
				WKT_file.WriteLine(Full_WKT_string);
				WKT_file.Close();
				WKT_file.Dispose();
			}

			string[] data_strings = File.ReadAllLines(writePath);
			string Full_WKT = data_strings[0];
			//Определение параметров эллипсоида
			int i1 = Full_WKT.IndexOf("SPHEROID") + 10; //Начало строки [ для эллипсоида
			int i2 = Full_WKT.Substring(i1).IndexOf("]"); //Конец строки ] для эллипсоида

			string Ell = Full_WKT.Substring(i1, i2);
			string[] Ell_string = Ell.Split(new char[] { ',' });

			string Ell_name = Ell_string[0].Substring(0, Ell_string[0].Length - 2);
			double Ell_a = Convert.ToDouble(Ell_string[1]);
			double Ell_flat = Convert.ToDouble(Ell_string[2]);

			//Определение параметров датума
			int i3 = Full_WKT.IndexOf("TOWGS84") + 8; //Начало строки [ для датума
			int i4 = Full_WKT.Substring(i3).IndexOf("]"); //Конец строки ] для датума
			string Datum = Full_WKT.Substring(i3, i4);
			string[] Datum_string = Datum.Split(new char[] { ',' });

			double Datum_X = Convert.ToDouble(Datum_string[0]);
			double Datum_Y = Convert.ToDouble(Datum_string[1]);
			double Datum_Z = Convert.ToDouble(Datum_string[2]);
			double Datum_ωx = Convert.ToDouble(Datum_string[3]);
			double Datum_ωy = Convert.ToDouble(Datum_string[4]);
			double Datum_ωz = Convert.ToDouble(Datum_string[5]);
			double Datum_m = Convert.ToDouble(Datum_string[6]);

			//Определение параметров проекции (СК)
			//Определяем имя СК
			int i7 = Full_WKT.IndexOf("PROJECTION") + 12; //Начало строки [ для датума
			int i8 = Full_WKT.Substring(i7).IndexOf(",");
			string CS_Name = Full_WKT.Substring(i7, i8 - 2);

			//Численные параметры проекции
			int i5 = Full_WKT.IndexOf("PARAMETER") - 1; //Начало строки [ для датума
			int i6 = Full_WKT.Substring(i5).IndexOf("UNIT") - 1; //Конец перечисления параметров СК
			string CS_Descr = Full_WKT.Substring(i5, i6);
			string[] CSDescr_string = CS_Descr.Split(new char[] { ',' });

			double CS_FE = Convert.ToDouble(CSDescr_string[2].Substring(0, CSDescr_string[2].Length - 1));
			double CS_FN = Convert.ToDouble(CSDescr_string[4].Substring(0, CSDescr_string[4].Length - 1));
			double CS_CentrMerid = Convert.ToDouble(CSDescr_string[6].Substring(0, CSDescr_string[6].Length - 1));
			double CS_OrigLat = Convert.ToDouble(CSDescr_string[8].Substring(0, CSDescr_string[8].Length - 1));
			double CS_k = 1d;
			//Для проекций типа Гаусса-Крюгера k=1
			if (CS_Name == "Gauss_Kruger")
			{
				CS_k = 1d;

			}
			//else //Для проекций типа "Поперечная Меркатора"
			//{

			//}
			string Units_str = Full_WKT.Substring(1 + i6 + i5);
			string[] Units_string = Units_str.Split(new char[] { ',' });
			string Units = Units_string[0].Substring(6, Units_string[0].Length - 7);

			Console.WriteLine($"{Ell_name}, {Ell_a}, {Ell_flat}");
			Console.WriteLine($"{Datum_X}, {Datum_Y}, {Datum_Z},{Datum_ωx}, {Datum_ωy}, {Datum_ωz}, {Datum_m}");
			Console.WriteLine($"{CS_Name}, {CS_FE}, {CS_FN}, {CS_CentrMerid}, {CS_OrigLat}, {CS_k}");
			Console.WriteLine($"{Units}");

			return new Dictionary<string, object>
			{
				{"Ell_name", Ell_name},
				{"Ell_a", Ell_a},
				{"Ell_flat", Ell_flat},
				{"Datum_X", Datum_X},
				{"Datum_Y", Datum_Y},
				{"Datum_Z", Datum_Z},
				{"Datum_ωx", Datum_ωx},
				{"Datum_ωy", Datum_ωy},
				{"Datum_ωz", Datum_ωz},
				{"Datum_m", Datum_m},
				{"CS_Name", CS_Name},
				{"CS_CentrMerid", CS_CentrMerid},
				{"CS_OrigLat", CS_OrigLat},
				{"CS_FN", CS_FN},
				{"CS_FE", CS_FE},
				{"CS_k", CS_k},
				{"Units", Units},

			};


		}
		
		
		
		public static string CStoCategory() //string Category_name
		{
			MgCoordinateSystemFactory coordSysFactory = new MgCoordinateSystemFactory();
			MgCoordinateSystemCatalog csCatalog = coordSysFactory.GetCatalog();

			MgCoordinateSystemDictionary csDict = csCatalog.GetCoordinateSystemDictionary();

			MgCoordinateSystemEnum csDictEnum = csDict.GetEnum();
			int csCount = csDict.GetSize();
			MgStringCollection csNames = csDictEnum.NextName(csCount);


			MgCoordinateSystem cs = null;
			string csName = null;
			bool csProtect;

			//Перебираем словарь СК
			int i1 = 0;
			for (int i2 = 0; i2 < csCount; i2++)
			{
				csName = csNames.GetItem(i2);
				cs = csDict.GetCoordinateSystem(csName);
				csProtect = cs.IsProtected();

				//Условие - если данная СК пользовательская (импортированная из XML)
				if (csProtect == false)
				{   //Пусть для примера для созданной категории с одной внесеной ск по имени "NewCategory"
					new MgCoordinateSystemFactory().GetCatalog().GetCategoryDictionary().GetCategory("NewCategory").AddCoordinateSystem(csName);
					i1++;
				}
			}
			return i1.ToString();
		}