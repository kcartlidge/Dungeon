using System.Text;
using Dungeon.Models;

namespace Dungeon.Renderers;

/// <summary>
/// Provides functionality to render a dungeon grid as an OBJ file.
/// </summary>
public static class RenderOBJ
{
    /// <summary>
    /// Renders the dungeon grid as an OBJ file.
    /// </summary>
    /// <param name="grid">The dungeon grid to render.</param>
    /// <param name="config">The configuration settings for rendering.</param>
    public static void ToFile(Grid grid, Config config)
    {
        // Generate primitives using Base3D
        var primitive = Base3D.GeneratePrimitives(grid, config);

        // Get base filename without extension
        var baseFilename = Path.ChangeExtension(config.Filename, null);
        var objFilename = baseFilename + ".obj";
        var mtlFilename = baseFilename + ".mtl";

        // Write MTL file
        var mtl = new StringBuilder();
        mtl.AppendLine("# Dungeon MTL File");
        foreach (var (name, color) in primitive.Materials)
        {
            mtl.AppendLine($"newmtl {name}");
            mtl.AppendLine($"Kd {color.r} {color.g} {color.b}");
            mtl.AppendLine("d 1.0");
            mtl.AppendLine("illum 1");
        }
        File.WriteAllText(mtlFilename, mtl.ToString());

        // Write OBJ file
        var obj = new StringBuilder();
        obj.AppendLine("# Dungeon OBJ File");
        obj.AppendLine($"mtllib {Path.GetFileName(mtlFilename)}");

        // Write vertices
        foreach (var v in primitive.Vertices)
        {
            obj.AppendLine($"v {v.x} {v.y} {v.z}");
        }

        // Write faces grouped by material
        string currentMaterial = null;
        foreach (var f in primitive.Faces)
        {
            if (f.material != currentMaterial)
            {
                currentMaterial = f.material;
                obj.AppendLine($"usemtl {currentMaterial}");
            }
            obj.AppendLine($"f {f.v1} {f.v2} {f.v3} {f.v4}");
        }

        // Write file
        File.WriteAllText(objFilename, obj.ToString());
    }
}

