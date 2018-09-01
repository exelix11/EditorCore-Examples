using EditorCore;
using EditorCore.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTTT3DSExt
{
	class CTTT3DSExt : ExtensionManifest
	{
		public string ModuleName => "Captain Toad Treasure Tracker EXT";
		public string Author => "Exelix11";
		public string ExtraText => null;

		public Version TargetVersion => new Version(1, 0, 0, 0);
		
		public IMenuExtension MenuExt => null;
		
		public bool HasGameModule => true;

		public IFileHander[] Handlers => null;

		public void CheckForUpdates() { }

		public IGameModule GetNewGameModule() => new CTTT3DSModule();
	}
}
