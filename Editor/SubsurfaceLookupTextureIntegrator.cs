using UnityEngine;
using UnityEditor;

public class SubsurfaceLookupTextureIntegratorWindow : EditorWindow
{
    [MenuItem("Tools/Subsurface LUT Integrator", false, 1000)]
    static void ShowIntegratorWindow()
    {
        GetWindow(typeof(SubsurfaceLookupTextureIntegratorWindow), false, "Subsurface LUT Integrator");
    }

    private Color FalloffColor = new Color(1.0f, 0.3f, 0.2f);
    private bool KeepDirectBounce = false;
    private ComputeShader IntegratorShader = null;
    private RenderTexture IntegratedLUT = null;
    private int Resolution = 512;

    private void OnEnable()
    {
        IntegratorShader = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/SubsurfaceLightingLUTGen/Editor/SubsurfaceLookupTextureIntegrator.compute");
    }

    private void OnDestroy()
    {
        if (IntegratedLUT != null)
        {
            DestroyImmediate(IntegratedLUT);
        }
    }

    void OnGUI()
    {
        GUILayout.Label("Base Settings", EditorStyles.boldLabel);

        FalloffColor = EditorGUILayout.ColorField("Fallof Color", FalloffColor);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Bake") && IntegratorShader != null)
        {
            if (IntegratedLUT != null)
            {
                DestroyImmediate(IntegratedLUT);
            }

            IntegratedLUT = new RenderTexture(Resolution, Resolution, 0, RenderTextureFormat.ARGBHalf, 0);
            IntegratedLUT.enableRandomWrite = true;
            IntegratedLUT.Create();

            IntegratorShader.SetTexture(0, "_IntegratedLUT", IntegratedLUT);
            IntegratorShader.SetVector("_FalloffColor", FalloffColor);
            IntegratorShader.SetFloat("_Resoultion", (float)Resolution);
            IntegratorShader.Dispatch(0, Resolution/8, Resolution/8, 1);
        }
        if (GUILayout.Button("Save") && IntegratedLUT != null)
        {
            var rt = RenderTexture.GetTemporary(Resolution, Resolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            Graphics.Blit(IntegratedLUT, rt);

            RenderTexture.active = rt;
            var tex = new Texture2D(IntegratedLUT.width, IntegratedLUT.height, TextureFormat.RGB24, false, true);
            tex.ReadPixels(new Rect(0, 0, IntegratedLUT.width, IntegratedLUT.height), 0, 0);
            RenderTexture.active = null;

            RenderTexture.ReleaseTemporary(rt);

            var path = "Assets/Baked_SubsurfaceLookupTexture.TGA";
            System.IO.File.WriteAllBytes(path, tex.EncodeToTGA());
            AssetDatabase.ImportAsset(path);

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            importer.sRGBTexture = true;
            importer.maxTextureSize = 64;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        var rect = EditorGUILayout.GetControlRect(false, position.height, GUIStyle.none);
        if (IntegratedLUT != null)
        {
            EditorGUI.DrawPreviewTexture(rect, IntegratedLUT);
        }
        else
        {
            EditorGUI.DrawRect(rect, Color.black);
        }
    }
}
