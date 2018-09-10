using ByamlExt;
using EditorCore;
using EditorCore.Interfaces;
using EveryFileExplorer;
using SARCExt;
using ByamlExt.Byaml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace CTTT3DSExt
{
    public class ObjList : List<ILevelObj>, IObjList
	{
        IList<dynamic> bymlNode;
        public ObjList(string _name, IList<dynamic> _bymlNode)
        {
            name = _name;
            if (_bymlNode == null)
            {
                bymlNode = new List<dynamic>();
                return;
            }
            bymlNode = _bymlNode;
            foreach (var o in bymlNode)
            {
                var obj = LevelObj.FromNode(o);
                int objID = obj.ID_int;
                if (Level._HighestID < objID) Level._HighestID = objID;
                this.Add(obj);
            }
        }

        public void ApplyChanges()
        {
            bymlNode.Clear();
            foreach (var o in this) bymlNode.Add(o.Prop);
        }

		public bool IsHidden { get; set; } = false;
		public string name { get; set; } = "";
		public bool ReadOnly => false;
	}

    public class Level : ILevel
    {
        public Dictionary<string, byte[]> LevelFiles { get; set; }
        public Dictionary<string, IObjList> objs { get; set; }
        public dynamic LoadedLevelData { get; set; }
		public string FilePath { get; set; } = "";
		public int ScenarioCount => LoadedLevelData.Count;

		public int HighestID { get => _HighestID; set => _HighestID = value; }
		public static int _HighestID = 0;

		ushort version;
		BymlFileData makeBymlData(dynamic root) =>
			new BymlFileData { Version = version, byteOrder = Syroot.BinaryData.ByteOrder.LittleEndian, SupportPaths = false, RootNode = root };


		string GetName
		{
			get
			{
				string baseName = Path.GetFileNameWithoutExtension(FilePath);
				while (baseName.Length > 1 && baseName[baseName.Length - 1] > '0' && baseName[baseName.Length - 1] < '9')
					baseName = baseName.Substring(0, baseName.Length - 1);
				return baseName;
			}
		}

		public Level(bool empty, string levelN)
        {
            if (!empty) throw new Exception();
            LevelFiles = new Dictionary<string, byte[]>();
            FilePath = levelN;
            LoadedLevelData = new Dictionary<string,dynamic>();
			
			((Dictionary<string, dynamic>)LoadedLevelData).Add("FilePath", "d:/customLevel.muunt"); //probably not needed

			LevelFiles.Add(GetName + ".byml", ByamlFile.SaveN(makeBymlData(LoadedLevelData)));
			LoadByml();
        }

        public Level (string path)
        {
            FilePath = path;
            Load(File.ReadAllBytes(path));
        }        

        void Load(byte[] file)
        {
            LevelFiles = SARC.UnpackRam(YAZ0.Decompress(file));
			LoadByml();
        }

		void LoadByml()
		{
			Stream s = new MemoryStream(LevelFiles[GetName + ".byml"]);
			var byml = ByamlFile.LoadN(s);
			LoadedLevelData = byml.RootNode;
			version = byml.Version;

			LoadObjects();
		}

		void LoadObjects()
        {
			objs = new Dictionary<string, IObjList>();			
            var level = (Dictionary<string, dynamic>)LoadedLevelData;

            foreach (string k in level.Keys)
            {
				if (level[k] is IList<dynamic>)
					objs.Add(k, new ObjList(k, level[k]));
            }

			if (objs.Keys.Count == 0)
			{
				level.Add("ObjectList", new List<dynamic>());
				objs.Add("ObjectList", new ObjList("ObjectList", level["ObjectList"]));
			}
		}
		
        void ApplyChangesToByml() //this makes sure new objects are added
        {
            objs.OrderBy(k => k.Key);
            for (int i = 0; i < objs.Count; i++)
            {
                var values = objs.Values.ToArray();
                if (values[i].Count == 0) objs.Remove(objs.Keys.ToArray()[i--]);
                else values[i].ApplyChanges();
            }
        }

        public byte[] ToByaml()
        {
            ApplyChangesToByml();
            MemoryStream mem = new MemoryStream();
            ByamlFile.SaveN(mem, makeBymlData(LoadedLevelData));
            var res = mem.ToArray();
            return res;
        }
		
		public byte[] SaveSzs(string newPath = null)
        {
            if (newPath != null)
            {
                LevelFiles.Remove(GetName + ".byml");
                FilePath = newPath;
                LevelFiles.Add(GetName + ".byml",ToByaml());
            }
            else
                LevelFiles[GetName + ".byml"] = ToByaml();
            return YAZ0.Compress(SARC.pack(LevelFiles));
        }

        public bool HasList(string name) { return objs.ContainsKey(name); }
		
        public IObjList FindListByObj(ILevelObj o)
        {
            foreach (string k in objs.Keys)
            {
                if (objs[k].Contains(o)) return objs[k];
            }
            return null;
        }
    }
}
