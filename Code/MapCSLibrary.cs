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

namespace MapConnection
{
	public class MapCSLibrary
		
	{
		private MapCSLibrary() { }
		/// <summary>
		/// CS_Language_RUS return system values for creation LSP file, if program language is Russian
		/// </summary>
		/// <returns>CS_value, CS_Agree - give them to GetPartOfMAPCSLIBRARY or GetAllOfMAPCSLIBRARY</returns>
		[MultiReturn(new[] { "CS_value", "CS_Agree" })]
		public static Dictionary<string, object> CS_Language_RUS()
		{
			string CS_value = "СистемаКоординат";
			string CS_Agree = "Д";
			return new Dictionary<string, object>
			{
				{"CS_value", CS_value},
				{"CS_Agree", CS_Agree},
			};
		}
		/// <summary>
		/// CS_Language_ENG return system values for creation LSP file, if program language is English
		/// </summary>
		/// <returns>CS_value, CS_Agree - give them to GetPartOfMAPCSLIBRARY or GetAllOfMAPCSLIBRARY</returns>
		[MultiReturn(new[] { "CS_value", "CS_Agree" })]
		public static Dictionary<string, object> CS_Language_ENG()
		{
			string CS_value = "C";
			string CS_Agree = "Y";
			return new Dictionary<string, object>
			{
				{"CS_value", CS_value},
				{"CS_Agree", CS_Agree},
			};
		}

		/// <summary>
		/// Node GetListOfMAPCSLIBRAR for export to LSP file naming of coordinate systems (user or system)
		/// </summary>
		/// <param name="CS_value">Name of type "CoordinateSystem"</param>
		/// <param name="selection">If false - exporting only User's definitions; if true - exporting only system definitions</param>
		/// <param name="CS_Agree">Permit to add other associated definitions</param>
		/// <param name="Folder_Path">Directory path to save LSP file</param>
		 public static void GetPartOfMAPCSLIBRARY(string Folder_Path, string CS_value, string CS_Agree, bool selection)
		{
			var guid = Guid.NewGuid();
			string writePath = $@"{Folder_Path}\{guid}.lsp";
			StringBuilder sb = new StringBuilder();

			MgCoordinateSystemFactory coordSysFactory = new MgCoordinateSystemFactory();
			MgCoordinateSystemCatalog csCatalog = coordSysFactory.GetCatalog();
			MgCoordinateSystemDictionary csDict = csCatalog.GetCoordinateSystemDictionary();

			MgCoordinateSystemEnum csDictEnum = csDict.GetEnum();
			int csCount = csDict.GetSize();
			sb.AppendLine(@"(command ""MAPCSLIBRARYEXPORT""");

			MgStringCollection csNames = csDictEnum.NextName(csCount);
			string csName = null;

			MgCoordinateSystem cs = null;
			bool csProtect;

			for (int i = 0; i < csCount; i++)
			{
				csName = csNames.GetItem(i);
				cs = csDict.GetCoordinateSystem(csName);
				csProtect = cs.IsProtected();

					if (csProtect == selection)
					{
					sb.AppendLine($@"""{csName}""" + " " + $"\"{CS_value}\"");

					}
			}
			string space = " ";
			sb.AppendLine(" " + $@"""{space}""" + " " + $@"""{CS_Agree}""" + " " + @"""""" + ")");
			using (StreamWriter export_file = new StreamWriter(writePath, true, Encoding.UTF8))
			{
				export_file.Write(sb.ToString());
			}

		}
		/// <summary>
		/// Node GetListOfMAPCSLIBRAR for export to LSP file naming of coordinate systems (all library)
		/// </summary>
		/// <param name="CS_value">Name of type "CoordinateSystem"</param>
		/// <param name="CS_Agree">Permit to add other associated definitions</param>
		/// <param name="Folder_Path">Directory path to save LSP file</param>
		public static void GetAllOfMAPCSLIBRARY(string Folder_Path, string CS_value, string CS_Agree)
		{
			StringBuilder sb = new StringBuilder();
			var guid = Guid.NewGuid();
			string writePath = $@"{Folder_Path}\{guid}.lsp";

			MgCoordinateSystemFactory coordSysFactory = new MgCoordinateSystemFactory();
			MgCoordinateSystemCatalog csCatalog = coordSysFactory.GetCatalog();
			MgCoordinateSystemDictionary csDict = csCatalog.GetCoordinateSystemDictionary();

			MgCoordinateSystemEnum csDictEnum = csDict.GetEnum();
			int csCount = csDict.GetSize();
			sb.AppendLine(@"(command ""MAPCSLIBRARYEXPORT""");

			MgStringCollection csNames = csDictEnum.NextName(csCount);
			string csName = null;
			
			for (int i = 0; i < csCount; i++)
			{
				csName = csNames.GetItem(i);
				sb.AppendLine($@"""{csName.ToString()}""" + " " + $"\"{CS_value}\"");
				
			}
			string space = " ";
			sb.AppendLine(" " + $@"""{space}""" + " " + $@"""{CS_Agree}""" + " " + @"""""" + ")");
			using (StreamWriter export_file = new StreamWriter(writePath, true, Encoding.UTF8))
			{
				export_file.Write(sb.ToString());
			}

		}
		/// <summary>
		/// Node GetListAndCountOfCategories returns a list of all Categories in MapCSLibrary and count of them
		/// </summary>
		/// <returns>Count of Categories and result List</returns>
		[MultiReturn(new[] { "CatCount", "CatList" })]
		public static Dictionary<string, object> GetListAndCountOfCategories() //Работает!
		{
			var guid = Guid.NewGuid();
			MgCoordinateSystemFactory coordSysFactory = new MgCoordinateSystemFactory();
			MgCoordinateSystemCatalog csCatalog = coordSysFactory.GetCatalog();
			MgCoordinateSystemCategoryDictionary csDictCat = csCatalog.GetCategoryDictionary();

			MgCoordinateSystemEnum csDictCatEnum = csDictCat.GetEnum();
			int csCatCount = csDictCat.GetSize();
			MgStringCollection csCatNames = csDictCatEnum.NextName(csCatCount);
			string csCategoryName = null;

			List<string> csCatNamesL = new List<string>();
			for (int i3 = 0; i3 < csCatCount; i3++)
			{
				csCategoryName = csCatNames.GetItem(i3);
				csCatNamesL.Add(csCategoryName);
			}
			return new Dictionary<string, object>
			{
				{"CatCount", csCatCount},
				{"CatList", csCatNamesL},
			};
		}
		
		/// <summary>
		/// Node RenameCurrentCategory rename Category form MapCSLibrary
		/// </summary>
		/// <param name="new_cat_name"></param>
		/// <param name="old_cat_name"></param>
		public static void RenameCurrentCategory(string new_cat_name, string old_cat_name) //Работает!
		{
			MgCoordinateSystemFactory coordSysFactory = new MgCoordinateSystemFactory();
			MgCoordinateSystemCatalog csCatalog = coordSysFactory.GetCatalog();
			MgCoordinateSystemCategoryDictionary csDictCat = csCatalog.GetCategoryDictionary();
			csDictCat.Rename(old_cat_name, new_cat_name);

		}
		/// <summary>
		/// Node GetCSList returns an external txt file that Contains strings with CS's names
		/// </summary>
		/// <param name="Folder_Path">Directory path to save TXT file</param>
		/// <param name="selection">If = true, export all CS in Library, if false - only Users collection</param>
		public static string GetCSList (string Folder_Path, bool selection)
		{
			StringBuilder sb = new StringBuilder();
			var guid = Guid.NewGuid();
			string writePath = $@"{Folder_Path}\{guid}CS_List.txt";

			MgCoordinateSystemFactory coordSysFactory = new MgCoordinateSystemFactory();
			MgCoordinateSystemCatalog csCatalog = coordSysFactory.GetCatalog();
			MgCoordinateSystemDictionary csDict = csCatalog.GetCoordinateSystemDictionary();

			MgCoordinateSystemEnum csDictEnum = csDict.GetEnum();
			int csCount = csDict.GetSize();
		

			MgStringCollection csNames = csDictEnum.NextName(csCount);
			string csName = null;

			MgCoordinateSystem cs = null;
			bool csProtect;

			for (int i = 0; i < csCount; i++)
			{
				csName = csNames.GetItem(i);
				cs = csDict.GetCoordinateSystem(csName);
				csProtect = cs.IsProtected();

				if (csProtect == selection)
				{
					sb.AppendLine(csName.ToString());

				}
			}
			using (StreamWriter export_file = new StreamWriter(writePath, true, Encoding.UTF8))
			{
				export_file.Write(sb.ToString());
			}
			return writePath;
		}

		
	}
		
}



