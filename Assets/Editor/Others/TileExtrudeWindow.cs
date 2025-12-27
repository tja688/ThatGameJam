// Assets/Editor/TileExtrudeWindow.cs
// Robust version: read source image bytes from disk, so it NEVER requires source texture "Read/Write Enabled".

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class TileExtrudeWindow : EditorWindow
{
    private int padPixels = 2;
    private string outputFolder = "Assets/_Extruded";
    private string suffixFormat = "_pad{0}";
    private bool overwriteExisting = false;

    // Import settings for OUTPUT textures
    private bool forceClamp = true;
    private bool disableMipmaps = true;
    private bool forceNoCompression = true;
    private FilterMode filterMode = FilterMode.Bilinear;

    [MenuItem("Tools/Tile Extrude (Padding)")]
    public static void Open()
    {
        var win = GetWindow<TileExtrudeWindow>("Tile Extrude");
        win.minSize = new Vector2(460, 280);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Generate padded textures for selected tile images", EditorStyles.boldLabel);
        EditorGUILayout.Space(6);

        padPixels = EditorGUILayout.IntSlider("Padding Pixels", padPixels, 1, 16);
        outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);
        suffixFormat = EditorGUILayout.TextField("Name Suffix Format", suffixFormat);
        overwriteExisting = EditorGUILayout.Toggle("Overwrite Existing", overwriteExisting);

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Import Settings for NEW texture (recommended)", EditorStyles.boldLabel);

        filterMode = (FilterMode)EditorGUILayout.EnumPopup("Filter Mode", filterMode);
        forceClamp = EditorGUILayout.Toggle("Wrap = Clamp", forceClamp);
        disableMipmaps = EditorGUILayout.Toggle("Disable Mipmaps", disableMipmaps);
        forceNoCompression = EditorGUILayout.Toggle("Compression = None", forceNoCompression);

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Generate Padded Sprites (Selected)", GUILayout.Height(38)))
        {
            GenerateForSelectionSafe();
        }

        EditorGUILayout.Space(8);
        EditorGUILayout.HelpBox(
            "This tool reads source PNG/JPG bytes from disk, so it does NOT require 'Read/Write Enabled' on source textures.\n" +
            "Output is a bigger PNG. The sprite rect is set to the original center region so in-game size stays the same.",
            MessageType.Info);
    }

    private void GenerateForSelectionSafe()
    {
        try
        {
            EnsureFolder(outputFolder);

            var paths = CollectSelectedTexturePaths();
            if (paths.Count == 0)
            {
                EditorUtility.DisplayDialog("Tile Extrude", "No textures found in selection.", "OK");
                return;
            }

            int processed = 0;
            int skipped = 0;
            int failed = 0;

            foreach (var srcAssetPath in paths)
            {
                try
                {
                    if (!IsImageFile(srcAssetPath))
                    {
                        skipped++;
                        continue;
                    }

                    // Read from disk -> guaranteed readable texture
                    if (!TryLoadImageFromDisk(srcAssetPath, out var readableTex))
                    {
                        Debug.LogWarning($"[TileExtrude] Cannot load image bytes: {srcAssetPath}");
                        failed++;
                        continue;
                    }

                    int w = readableTex.width;
                    int h = readableTex.height;

                    // Generate padded texture
                    Texture2D padded = CreatePaddedTexture(readableTex, padPixels);

                    // Output path
                    string fileName = Path.GetFileNameWithoutExtension(srcAssetPath);
                    string suffix = string.Format(suffixFormat, padPixels);
                    string outPath = $"{outputFolder}/{fileName}{suffix}.png";

                    if (File.Exists(outPath) && !overwriteExisting)
                    {
                        skipped++;
                        DestroyImmediate(readableTex);
                        DestroyImmediate(padded);
                        continue;
                    }

                    File.WriteAllBytes(outPath, padded.EncodeToPNG());
                    AssetDatabase.ImportAsset(outPath, ImportAssetOptions.ForceUpdate);

                    // Configure output importer: Multiple with one sprite rect excluding padding
                    ConfigureOutputImporter(outPath, fileName, w, h, padPixels, srcAssetPath);

                    processed++;

                    DestroyImmediate(readableTex);
                    DestroyImmediate(padded);
                }
                catch (Exception e)
                {
                    failed++;
                    Debug.LogError($"[TileExtrude] Failed on {srcAssetPath}\n{e}");
                }
            }

            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Tile Extrude",
                $"Done.\nProcessed: {processed}\nSkipped: {skipped}\nFailed: {failed}\nOutput: {outputFolder}",
                "OK");
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            EditorUtility.DisplayDialog("Tile Extrude", "Failed: " + e.Message, "OK");
        }
    }

    private static bool IsImageFile(string assetPath)
    {
        string lower = assetPath.ToLowerInvariant();
        return lower.EndsWith(".png") || lower.EndsWith(".jpg") || lower.EndsWith(".jpeg");
    }

    private static bool TryLoadImageFromDisk(string assetPath, out Texture2D tex)
    {
        tex = null;
        string fullPath = AssetPathToFullPath(assetPath);
        if (!File.Exists(fullPath)) return false;

        byte[] bytes = File.ReadAllBytes(fullPath);
        var t = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
        if (!ImageConversion.LoadImage(t, bytes, false))
        {
            DestroyImmediate(t);
            return false;
        }

        tex = t;
        return true;
    }

    private static string AssetPathToFullPath(string assetPath)
    {
        // assetPath like "Assets/xxx.png"
        // full path = <projectRoot>/Assets/xxx.png
        string projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
        return Path.Combine(projectRoot, assetPath).Replace("\\", "/");
    }

    private static Texture2D CreatePaddedTexture(Texture2D src, int pad)
    {
        int w = src.width;
        int h = src.height;
        int nw = w + pad * 2;
        int nh = h + pad * 2;

        Color32[] srcPixels = src.GetPixels32(); // This is safe: src was created by LoadImage
        Color32[] dstPixels = new Color32[nw * nh];

        Color32 SampleSrc(int sx, int sy)
        {
            sx = Mathf.Clamp(sx, 0, w - 1);
            sy = Mathf.Clamp(sy, 0, h - 1);
            return srcPixels[sy * w + sx];
        }

        // center
        for (int y = 0; y < h; y++)
        {
            int srcRow = y * w;
            int dstRow = (y + pad) * nw + pad;
            for (int x = 0; x < w; x++)
                dstPixels[dstRow + x] = srcPixels[srcRow + x];
        }

        // left/right extend
        for (int y = 0; y < h; y++)
        {
            for (int i = 1; i <= pad; i++)
            {
                dstPixels[(y + pad) * nw + (pad - i)] = SampleSrc(0, y);
                dstPixels[(y + pad) * nw + (pad + w - 1 + i)] = SampleSrc(w - 1, y);
            }
        }

        // top/bottom extend (includes corners via clamp)
        for (int x = -pad; x < w + pad; x++)
        {
            for (int i = 1; i <= pad; i++)
            {
                dstPixels[(pad - i) * nw + (x + pad)] = SampleSrc(x, 0);
                dstPixels[(pad + h - 1 + i) * nw + (x + pad)] = SampleSrc(x, h - 1);
            }
        }

        var dst = new Texture2D(nw, nh, TextureFormat.RGBA32, false, false);
        dst.SetPixels32(dstPixels);
        dst.Apply(false, false);
        return dst;
    }

    private void ConfigureOutputImporter(string outPath, string spriteName, int originalW, int originalH, int pad, string srcAssetPath)
    {
        var outImporter = AssetImporter.GetAtPath(outPath) as TextureImporter;
        if (outImporter == null) return;

        // Try preserve PPU from source if it's a sprite
        float ppu = 100f;
        var srcImporter = AssetImporter.GetAtPath(srcAssetPath) as TextureImporter;
        if (srcImporter != null) ppu = srcImporter.spritePixelsPerUnit;

        outImporter.textureType = TextureImporterType.Sprite;
        outImporter.spriteImportMode = SpriteImportMode.Multiple;
        outImporter.spritePixelsPerUnit = ppu;
        outImporter.alphaIsTransparency = true;

        outImporter.mipmapEnabled = !disableMipmaps ? outImporter.mipmapEnabled : false;
        outImporter.filterMode = filterMode;

        if (forceClamp) outImporter.wrapMode = TextureWrapMode.Clamp;
        if (forceNoCompression) outImporter.textureCompression = TextureImporterCompression.Uncompressed;

        outImporter.isReadable = false;

        // Center rect sprite
        var meta = new SpriteMetaData
        {
            name = spriteName,
            rect = new Rect(pad, pad, originalW, originalH),
            alignment = (int)SpriteAlignment.Center,
            pivot = new Vector2(0.5f, 0.5f)
        };

        outImporter.spritesheet = new[] { meta };
        outImporter.SaveAndReimport();
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;

        string[] parts = folder.Split('/');
        if (parts.Length == 0 || parts[0] != "Assets")
            throw new Exception("Output folder must be under Assets/");

        string current = "Assets";
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private static List<string> CollectSelectedTexturePaths()
    {
        var results = new List<string>();
        var selection = Selection.objects;
        if (selection == null || selection.Length == 0) return results;

        foreach (var obj in selection)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path)) continue;

            if (AssetDatabase.IsValidFolder(path))
            {
                string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { path });
                foreach (string g in guids)
                {
                    string p = AssetDatabase.GUIDToAssetPath(g);
                    if (!results.Contains(p)) results.Add(p);
                }
            }
            else
            {
                // Any selected texture asset
                if (obj is Texture2D)
                {
                    if (!results.Contains(path)) results.Add(path);
                }
                else
                {
                    // If user selected Sprite sub-assets, get their texture path
                    if (obj is Sprite s)
                    {
                        string p = AssetDatabase.GetAssetPath(s.texture);
                        if (!string.IsNullOrEmpty(p) && !results.Contains(p))
                            results.Add(p);
                    }
                }
            }
        }

        return results;
    }
}
