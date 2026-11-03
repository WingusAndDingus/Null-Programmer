using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace NullProgrammer.Day3Workshop.Editor
{
    public static class WorkshopUIValidation
    {
        private const string ArtPath = "Assets/Resources/Day3WorkshopUI";

        [MenuItem("Tools/Null Programmer/Day 3 Board Workshop/4 - Validate UI")]
        public static void Validate()
        {
            if (!Directory.Exists(ArtPath)) throw new InvalidOperationException("Copy the update's Resources/Day3WorkshopUI folder into Assets first.");
            int count = 0;
            foreach (string path in Directory.GetFiles(ArtPath, "*.png"))
            {
                string name = Path.GetFileNameWithoutExtension(path);
                if (Resources.Load<Sprite>("Day3WorkshopUI/" + name) == null)
                    throw new InvalidOperationException(name + " did not import as a Sprite. Run '5 - Refresh UI Sprite Imports'.");
                count++;
            }
            if (count != 29) throw new InvalidOperationException("Expected 29 UI sprites, found " + count + ".");
            var checks = BoardSimulation.CheckPatch(4, new[] { 1, 1, 1, 1 });
            if (checks.Count != WorkshopVisualModel.FinalSamples.Length)
                throw new InvalidOperationException("Final preview list no longer matches the regression suite.");
            Debug.Log("Day 3 UI validation passed: 29 sprites and " + checks.Count + " final visual test cases. Start Wednesday from DesktopUI to check the live layout.");
        }

        [MenuItem("Tools/Null Programmer/Day 3 Board Workshop/5 - Refresh UI Sprite Imports")]
        public static void RefreshImports()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before refreshing UI sprites.");
            if (!Directory.Exists(ArtPath)) throw new InvalidOperationException("UI art folder is missing.");
            foreach (string path in Directory.GetFiles(ArtPath, "*.png"))
            {
                string assetPath = path.Replace('\\', '/');
                var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer == null) throw new InvalidOperationException("Texture importer missing: " + assetPath);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);                
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.filterMode = FilterMode.Point;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                string name = Path.GetFileNameWithoutExtension(path);
                bool sliced = name.StartsWith("button-", StringComparison.Ordinal) || name.EndsWith("-frame", StringComparison.Ordinal) ||
                    name == "title-bar" || name == "grid-cell" || name == "placement-preview" || name == "collision-marker" ||
                    name == "occupied-cell" || name == "stale-cell" || name == "sfml-outline" || name == "hover-cell";
                importer.spriteBorder = sliced ? new Vector4(name == "sfml-outline" ? 10 : 6, name == "sfml-outline" ? 10 : 6, name == "sfml-outline" ? 10 : 6, name == "sfml-outline" ? 10 : 6) : Vector4.zero;
                importer.SaveAndReimport();
            }
            Validate();
        }
    }
}
