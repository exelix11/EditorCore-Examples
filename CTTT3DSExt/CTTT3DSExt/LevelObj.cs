using EditorCore;
using EditorCore.Interfaces;
using ExtensionMethods;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using System.Windows.Media.Media3D;
using static EditorCore.PropertyGridTypes;

namespace CTTT3DSExt
{
    public class LevelObj : ILevelObj
	{
		public static ILevelObj FromNode(dynamic bymlNode)
		{
			if (!(bymlNode is Dictionary<string, dynamic>)) throw new Exception("Not a dictionary");
			var node = (Dictionary<string, dynamic>)bymlNode;
			//if (node.ContainsKey(N_Id) && ((string)node[N_Id]).StartsWith("rail"))
			//	return new Rail(node);
			//else
				return new LevelObj(node);
		}

		[Browsable(false)]
		public bool CanDrag { get; set; } = true; //if the model is static and doesn't appear in the list (mk8 courses)
		
        public const string N_Translate = "Translate";
        public const string N_Rotate = "Rotate";
        public const string N_Scale = "Scale";
        public const string N_Id = "Id";
        public const string N_Name = "UnitConfigName";
        public const string N_ModelName = "ModelName";
        public const string N_Links = "Links"; 
		public static readonly string[] CantRemoveNames = { N_Translate, N_Rotate, N_Scale, N_Id , N_Name , N_Links };
		public static readonly string[] ModelFieldNames = { N_Name, N_ModelName };

		public const string N_UnitConfig = "UnitConfig";
		public const string N_UnitConfigPos = "DisplayTranslate";
		public const string N_UnitConfigRot = "DisplayRotate";
		public const string N_UnitConfigScale = "DisplayScale";

		[Browsable(false)]
		public Dictionary<string, dynamic> Prop { get; set; } = new Dictionary<string, dynamic>();

		public LevelObj(Dictionary<string, dynamic> bymlNode)
        {
			Prop = bymlNode;
			if (Prop.ContainsKey(N_Links) && !(Prop[N_Links] is LinksNode)) Prop[N_Links] = new LinksNode(Prop[N_Links]);
        }
        
		public LevelObj(bool empty = false)
        {
            if (empty) return;
            Prop.Add(N_Translate, new Dictionary<string, dynamic>());
            Prop[N_Translate].Add("X", (Single)0);
            Prop[N_Translate].Add("Y", (Single)0);
            Prop[N_Translate].Add("Z", (Single)0);
            Prop.Add(N_Rotate, new Dictionary<string, dynamic>());
            Prop[N_Rotate].Add("X", (Single)0);
            Prop[N_Rotate].Add("Y", (Single)0);
            Prop[N_Rotate].Add("Z", (Single)0);
            Prop.Add(N_Scale, new Dictionary<string, dynamic>());
            Prop[N_Scale].Add("X", (Single)1);
            Prop[N_Scale].Add("Y", (Single)1);
            Prop[N_Scale].Add("Z", (Single)1);
            Prop.Add(N_Links, new LinksNode());
            this[N_Name] = "newObj";
            this[N_Id] = "obj0";
			Prop.Add(N_UnitConfig, new Dictionary<string, dynamic>());
			Prop[N_UnitConfig].Add(N_UnitConfigPos, new Dictionary<string, dynamic>());
			Prop[N_UnitConfig][N_UnitConfigPos].Add("X", (Single)0);
			Prop[N_UnitConfig][N_UnitConfigPos].Add("Y", (Single)0);
			Prop[N_UnitConfig][N_UnitConfigPos].Add("Z", (Single)0);
			Prop[N_UnitConfig].Add(N_UnitConfigRot, new Dictionary<string, dynamic>());
			Prop[N_UnitConfig][N_UnitConfigRot].Add("X", (Single)0);
			Prop[N_UnitConfig][N_UnitConfigRot].Add("Y", (Single)0);
			Prop[N_UnitConfig][N_UnitConfigRot].Add("Z", (Single)0);
			Prop[N_UnitConfig].Add(N_UnitConfigScale, new Dictionary<string, dynamic>());
			Prop[N_UnitConfig][N_UnitConfigScale].Add("X", (Single)1);
			Prop[N_UnitConfig][N_UnitConfigScale].Add("Y", (Single)1);
			Prop[N_UnitConfig][N_UnitConfigScale].Add("Z", (Single)1);
		}

        public dynamic this [string name]
        {
            get
            {
                if (Prop.ContainsKey(name)) return Prop[name];
                else return null;
            }
            set
            {
                if (Prop.ContainsKey(name)) Prop[name] = value;
                else Prop.Add(name,value);
            }
        }

		public bool ContainsKey(string name) => Prop.ContainsKey(name);
		
		[DisplayName("Position")]
		[TypeConverter(typeof(PropertyGridTypes.Vector3DConverter))]
		[Category(" Transform")]
		public virtual Vector3D Pos
        {
            get { return new Vector3D(this[N_Translate]["X"], this[N_Translate]["Y"], this[N_Translate]["Z"]); }
            set {              
                this[N_Translate]["X"] = (Single)value.X;
                this[N_Translate]["Y"] = (Single)value.Y;
                this[N_Translate]["Z"] = (Single)value.Z;
            }
        }

		[System.ComponentModel.DisplayName("Rotation")]
		[TypeConverter(typeof(PropertyGridTypes.Vector3DConverter))]
		[Category(" Transform")]
		public Vector3D Rot
        {
            get { return new Vector3D(this[N_Rotate]["X"], this[N_Rotate]["Y"], this[N_Rotate]["Z"]); }
            set
            {
                this[N_Rotate]["X"] = (Single)value.X;
                this[N_Rotate]["Y"] = (Single)value.Y;
                this[N_Rotate]["Z"] = (Single)value.Z;
            }
        }

        [Browsable(false)]
        public int ID_int
        {
            get
            {
                int res = -1;
                if (ID.StartsWith("obj"))
                    int.TryParse(ID.Substring(3), out res);
                return res;
            }
			set
			{
				ID = "obj" + value.ToString();
			}
        }

        [Browsable(false)]
        public Vector3D ModelView_Pos
        {
            get => new Vector3D(this[N_Translate]["X"], -this[N_Translate]["Z"], this[N_Translate]["Y"]);
			set => Pos = new Vector3D(value.X, value.Z, -value.Y);
		}

        [Browsable(false)]
        public Vector3D ModelView_Rot
        {
            get { return new Vector3D(this[N_Rotate]["X"], -this[N_Rotate]["Z"], this[N_Rotate]["Y"]); } //TODO: check if it matches in-game
        }

		[System.ComponentModel.DisplayName("Scale")]
		[TypeConverter(typeof(PropertyGridTypes.Vector3DConverter))]
		[Category(" Transform")]
		public Vector3D Scale
        {
            get { return new Vector3D(this[N_Scale]["X"], this[N_Scale]["Y"], this[N_Scale]["Z"]); }
            set
            {
                this[N_Scale]["X"] = (Single)value.X;
                this[N_Scale]["Y"] = (Single)value.Y;
                this[N_Scale]["Z"] = (Single)value.Z;
            }
        }

        [Browsable(false)]
        public Vector3D ModelView_Scale
        {
            get { return new Vector3D(this[N_Scale]["X"], this[N_Scale]["Z"], this[N_Scale]["Y"]); }           
        }

        [Browsable(false)]
        public Transform transform
        {
            get => new Transform() { Pos = Pos, Rot = Rot, Scale = Scale };
            set
            {
                Pos = value.Pos;
                Rot = value.Rot;
                Scale = value.Scale;
            }
        }

        public string ID
        {
            get { return this[N_Id]; }
            set { this[N_Id] = value;}
        }

        public string Name
        {
            get { return this.ToString(); }
            set { this[N_Name] = value; }
        }

        public string ModelName
        {
            get => Prop.ContainsKey(N_ModelName) && Prop[N_ModelName] != null ? Prop[N_ModelName] : this.ToString();
        }

        public override string ToString()
        {
            string name = this[N_Name];
            if (name == null) name = "LevelObj id: " + this[N_Id];
            if (name == null) name = "LevelObj";
            return name;
        }

        public LevelObj Clone()
        {
            return new LevelObj(DeepCloneDictArr.DeepClone(Prop));
        }        

        object ICloneable.Clone()
        {
            return Clone();
        }

        //[Editor(typeof(LevelObjEditor), typeof(UITypeEditor))]
        [TypeConverter(typeof(DictionaryConverter))]
        [Description("This contains every property of this object")]
        public Dictionary<string, dynamic> Properties
        {
            get { return Prop; }
            set { Prop = value; }
        }

		public bool ReadOnly { get; set; }
	}

    [Editor("CTTT3DSExt.LinksEditor", typeof(UITypeEditor))] //needs to be string to work (?)
    public class LinksNode : Dictionary<string, dynamic>, ICloneable //wrapper so we can use the custom editor
    {
        public LinksNode(Dictionary<string, dynamic> dict) : base(dict)
        {

        }

        public LinksNode() : base()
        {

        }

        public LinksNode Clone()
        {
            return new LinksNode(DeepCloneDictArr.DeepClone(this));
        }

        object ICloneable.Clone()
        {
            return Clone();
        }
    }

    public class LinksEditor : UITypeEditor
    {
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.Modal;
        }
        public override object EditValue(ITypeDescriptorContext context, System.IServiceProvider provider, object value)
        {
			LinksNode node = value as LinksNode;
			/*context should be of type System.Windows.Forms.PropertyGridInternal.PropertyDescriptorGridEntry
			OwnerGrid is not a public property, we get it through reflection
			A reference to the parent form is needed to add the ObjList to the ListEditingStack*/
			Type t = context.GetType();
			if (!(t.GetProperty("OwnerGrid").GetValue(context, null) is PropertyGrid targetGrid))
				throw new Exception("context is not of the expected type");
			if (!(targetGrid.ParentForm is IEditorFormContext TargetForm))
				throw new Exception("context is not of the expected type");
			if (node != null)
			{
				using (var form = new EditorFroms.LinksEditor(node, TargetForm))
				{
					form.ShowDialog();
				}
			}
			return node;
		}
    }
}
