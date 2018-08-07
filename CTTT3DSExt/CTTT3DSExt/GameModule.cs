using ByamlExt;
using CTTT3DSExt.Properties;
using EditorCore;
using EditorCore.EditorFroms;
using EditorCore.Interfaces;
using EveryFileExplorer;
using SARCExt;
using Syroot.NintenTools.Byaml.Dynamic;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CTTT3DSExt
{
	public class CTTT3DSModule : IGameModule
	{
		public string ModuleName => "Captain Toad Treasure Tracker";

		public Tuple<Type, Type>[] GetClassConverters { get; } =
		new Tuple<Type, Type>[] {
			new Tuple<Type, Type>(typeof(LinksNode), typeof(LinksConveter))
		};

		public string[] ReservedPropNames => LevelObj.CantRemoveNames;
		public string[] ModelFieldPropNames => LevelObj.ModelFieldNames;

		public bool IsAddListSupported => true;
		public bool IsPropertyEditingSupported => true;
		public string[] AutoHideList => new string[] { "AreaList", "SkyList" };

		public IEditorFormContext ViewForm { get; set; } = null;
		public string GameFolder => ViewForm.GameFolder;

		public string ModelsFolder => "CTTT3DSModels";

		// Code to convert models return false == fail
		public bool ConvertModelFile(string ObjName, string path)
		{
			try
			{
				string targetSZS = Path.Combine(GameFolder, "ObjectData", ObjName + ".szs");
				if (!File.Exists(targetSZS))
					return false;
				var Sarc = SARCExt.SARC.UnpackRam(YAZ0.Decompress(File.ReadAllBytes(targetSZS)));
				string keyName = ObjName + ".bch";
				if (!Sarc.ContainsKey(keyName))
					return false;
				var mod = Ohana3DS_Rebirth.Ohana.Models.BCH.load(new MemoryStream(Sarc[keyName]));
				Ohana3DS_Rebirth.Ohana.Models.GenericFormats.OBJ.export(mod, path, 0);
			}
			catch
			{
				return false;
			}
			return true;
		}

		public string GetPlaceholderModel(string name, string listName)
		{
			string PlaceholderModel ="UnkBlue.obj";
			if (listName == "AreaList") PlaceholderModel = "UnkYellow.obj";
			else if (listName == "DebugList") PlaceholderModel = "UnkRed.obj";
			else if (listName == "CameraAreaInfo") PlaceholderModel = "UnkGreen.obj";
			
			return PlaceholderModel;
		}
		
		public void InitModule(IEditorFormContext currentView)
		{
			ViewForm = currentView;
		}

		public void ParseArgs(string[] Args)
		{
			foreach (string file in Args)
			{
				if (File.Exists(file))
				{
					if (file.EndsWith(".szs"))
					{
						ViewForm.LoadLevel(file);
						return;
					}
				}
			}
		}

		string LevelFormatFilter => "szs file | *.szs";
		public ILevel LoadLevel(string file = null)
		{
			if (file == null)
			{
				var opn = new OpenFileDialog()
				{
					Filter = LevelFormatFilter,
					Title = "Select a level",
				};
				if (opn.ShowDialog() != DialogResult.OK)
					return null;
				file = opn.FileName;
			}
			return new Level(file);
		}

		public ILevel NewLevel(string file = null)
		{
			if (file == null)
			{
				var sav = new SaveFileDialog()
				{
					Filter = LevelFormatFilter
				};
				if (sav.ShowDialog() != DialogResult.OK)
					return null;
				file = sav.FileName;
			}
			return new Level(true, file);
		}

		public void SaveLevel(ILevel level) => File.WriteAllBytes(level.FilePath, ((Level)level).SaveSzs());
		public void SaveLevelAs(ILevel level)
		{
			var sav = new SaveFileDialog() { Filter = LevelFormatFilter };
			if (sav.ShowDialog() != DialogResult.OK)
				return;
			File.WriteAllBytes(level.FilePath, ((Level)level).SaveSzs(sav.FileName));
		}

		public IObjList CreateObjList(string name, IList<dynamic> baseList) =>
			 new ObjList(name, baseList);

		public ILevelObj NewObject() => new LevelObj(); //TODO : show a dialog

		public bool OpenLevelFile(string name, Stream file)
		{
			ByamlViewer.OpenByml(file, name);
			return true;
		}			

		public string AddObjList(ILevel level)
		{
			string name = "";
			EditorCore.InputDialog.Show("enter name", "Enter a name for the list", ref name);
			if (name.Trim() == "") return null;
			level.objs.Add(name, new ObjList(name, null));
			return name;
		}

		public void EditChildrenNode(ILevelObj obj)
		{
			if (obj[LevelObj.N_Links] != null)
			{
				var BakLinks = ((LinksNode)obj[LevelObj.N_Links]).Clone();

				ViewForm.AddToUndo((dynamic arg) =>
				{
					((ILevelObj)arg[0])[LevelObj.N_Links] = arg[1];
				},
					$"Edited links of {obj.ToString()}",
					new dynamic[] { obj, BakLinks });

				new EditorFroms.LinksEditor(obj[LevelObj.N_Links], ViewForm).ShowDialog();
			}
		}

		public Tuple<string, dynamic> GetNewProperty(dynamic target) => AddBymlPropertyDialog.newProperty(target is IDictionary<string, dynamic>);

		public void FormLoaded()
		{
			if (!Directory.Exists(ModelsFolder))
			{
				Directory.CreateDirectory(ModelsFolder);
				ZipArchive z = new ZipArchive(new MemoryStream(Resources.baseModels));
				z.ExtractToDirectory(ModelsFolder);
			}
			if (!Directory.Exists($"{ModelsFolder}/Textures"))
			{
				if (GameFolder == "" || !Directory.Exists(GameFolder))
					MessageBox.Show("The game path is not set or not valid, can't extract texture archives");
				else
				{
					Directory.CreateDirectory($"{ModelsFolder}/Textures");
					MessageBox.Show($"The game texture archives will be extracted in {ModelsFolder}/Textures, this might take a while");
					LoadingForm.ShowLoading(ViewForm as Form, "Extracting textures...\r\nThis might take a while");
					foreach (var a in Directory.GetFiles($"{GameFolder}ObjectData\\").Where(x => x.EndsWith("Texture.szs") || x.EndsWith("Textures.szs")))
					{
						var Sarc = SARCExt.SARC.UnpackRam(YAZ0.Decompress(File.ReadAllBytes(a)));
						string keyName = Path.GetFileNameWithoutExtension(a) + ".bch";
						if (!Sarc.ContainsKey(keyName)) continue;
						var mod = Ohana3DS_Rebirth.Ohana.Models.BCH.load(new MemoryStream(Sarc[keyName]));
						Ohana3DS_Rebirth.Ohana.Models.GenericFormats.OBJ.ExportTextures(mod, ModelsFolder);
					}
					LoadingForm.EndLoading();
				}
			}
		}
	}

	public class LinksConveter : System.ComponentModel.TypeConverter
	{
		public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext context, Type sourceType)
		{
			return false;
		}

		public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
		{
			return "<Links>";
		}
	}
}
