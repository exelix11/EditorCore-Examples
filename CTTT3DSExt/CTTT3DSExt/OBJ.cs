using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Ohana3DS_Rebirth.Ohana.Models.GenericFormats
{
	class OBJ
	{
		public static void ExportTextures(RenderBase.OModelGroup model, string ModelsPath)
		{
			for (int i = 0; i < model.texture.Count; i++)
			{
				if (model.texture[i].name.Contains("_alb"))
				{
					model.texture[i].texture.Save(ModelsPath + "\\Textures\\" + model.texture[i].name + ".bmp");
				}
			}
		}

		/// <summary>
		///     Exports a model to the Wavefront OBJ format.
		/// </summary>
		/// <param name="model">The model to be exported</param>
		/// <param name="fileName">The output file name</param>
		/// <param name="modelIndex">The index of the model that should be exported</param>
		public static void export(RenderBase.OModelGroup model, string fileName, int modelIndex)
		{
			for (int i = 0; i < model.texture.Count; i++)
			{
				if (model.texture[i].name.Contains("_alb"))
				{
					model.texture[i].texture.Save(Path.Combine(Path.GetDirectoryName(fileName), "Textures", model.texture[i].name + ".bmp"));
				}
			}

			if (model.model.Count == 0) return;

			StringBuilder output = new StringBuilder();
			RenderBase.OModel mdl = model.model[modelIndex];

			output.AppendLine($"mtllib {Path.GetFileNameWithoutExtension(fileName)}.mtl");

			int faceIndexBase = 1;
			for (int objIndex = 0; objIndex < mdl.mesh.Count; objIndex++)
			{
				output.AppendLine("g " + mdl.mesh[objIndex].name);
				output.AppendLine(null);

				output.AppendLine("usemtl " + mdl.material[mdl.mesh[objIndex].materialId].name0);

				output.AppendLine(null);

				MeshUtils.optimizedMesh obj = MeshUtils.optimizeMesh(mdl.mesh[objIndex]);
				foreach (RenderBase.OVertex vertex in obj.vertices)
				{
					output.AppendLine("v " + getString(vertex.position.x) + " " + getString(vertex.position.y) + " " + getString(vertex.position.z));
					output.AppendLine("vn " + getString(vertex.normal.x) + " " + getString(vertex.normal.y) + " " + getString(vertex.normal.z));
					output.AppendLine("vt " + getString(vertex.texture0.x) + " " + getString(vertex.texture0.y));
				}
				output.AppendLine(null);

				for (int i = 0; i < obj.indices.Count; i += 3)
				{
					output.AppendLine(
						string.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}",
						faceIndexBase + obj.indices[i],
						faceIndexBase + obj.indices[i + 1],
						faceIndexBase + obj.indices[i + 2]));
				}
				faceIndexBase += obj.vertices.Count;
				output.AppendLine(null);
			}

			File.WriteAllText(fileName, output.ToString());

			if (mdl.material.Count == 0) return;

			output = new StringBuilder();

			List<string> MatNames = new List<string>();

			foreach (RenderBase.OMaterial mat in mdl.material)
			{
				if (!MatNames.Contains(mat.name0))
				{
					MatNames.Add(mat.name0);
					output.AppendLine(null);
					output.AppendLine("newmtl " + mat.name0);
					output.AppendLine("Ka " + string.Format("{0} {1} {2}",
							mat.materialColor.ambient.R.ToString(),
							mat.materialColor.ambient.G.ToString(),
							mat.materialColor.ambient.B.ToString()));
					output.AppendLine("Kd " + string.Format("{0} {1} {2}",
							mat.materialColor.diffuse.R.ToString(),
							mat.materialColor.diffuse.G.ToString(),
							mat.materialColor.diffuse.B.ToString()));
					output.AppendLine("Ks 0 0 0");
					output.AppendLine("d 1");
					if (mat.name0 != null) output.AppendLine("map_Kd " + @"Textures\" + mat.name0 + ".bmp");
				}
			}
			File.WriteAllText(Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) + ".mtl", output.ToString());

		}

		/// <summary>
		///     Transforms a Float into a String that will always have "." into decimal places,
		///     even if the region uses ",".
		/// </summary>
		/// <param name="value">The Float value</param>
		/// <returns></returns>
		private static string getString(float value)
		{
			return value.ToString(CultureInfo.InvariantCulture);
		}
		
	}
}