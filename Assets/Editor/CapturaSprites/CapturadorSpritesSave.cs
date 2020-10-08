using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class CapturadorSpritesSave : ScriptableObject
{
    public ProcesarRecuadros procesarRecuadros;
    public List<ExtraerSprites> extractores;
    public Texture2D texturaOrigen;
    // [SerializeField] Texture2D[] texturasExtraidas;
    // [SerializeField] Sprite[] spritesExtraidos;

    void OnDestroy()
    {
        foreach (var extractor in extractores) DestroyImmediate(extractor);
        DestroyImmediate(procesarRecuadros);
    }

#if UNITY_EDITOR
    public void Save(string path)
    {
        var paraGuardar = Instantiate(this);

        paraGuardar.procesarRecuadros = Instantiate(procesarRecuadros);
        paraGuardar.procesarRecuadros.name = "Recuadros";
        paraGuardar.extractores = extractores.Take(paraGuardar.procesarRecuadros.Recuadros.Count).Select(ex => Instantiate(ex)).ToList();

        AssetDatabase.CreateAsset(paraGuardar, path);
        AssetDatabase.AddObjectToAsset(paraGuardar.procesarRecuadros, path);
        Debug.Log($"{AssetDatabase.Contains(texturaOrigen)} - {AssetDatabase.GetAssetPath(texturaOrigen)}");
        if (!AssetDatabase.Contains(texturaOrigen))
        {
            paraGuardar.texturaOrigen = Instantiate(texturaOrigen);
            paraGuardar.texturaOrigen.name = "principal";
            EditorUtility.CompressTexture(paraGuardar.texturaOrigen, TextureFormat.DXT1, TextureCompressionQuality.Normal);
            paraGuardar.texturaOrigen.hideFlags = HideFlags.NotEditable;
            AssetDatabase.AddObjectToAsset(paraGuardar.texturaOrigen, path);
        }
        foreach (var extractor in paraGuardar.extractores)
        {
            extractor.name = "Extractor Sprite";
            AssetDatabase.AddObjectToAsset(extractor, path);
        }
        int c = 0;
        foreach (var resultante in paraGuardar.extractores.SelectMany(ext => ext.texturasResultantes))
        {
            if (!AssetDatabase.Contains(resultante))
            {
                // EditorUtility.CompressTexture(resultante, TextureFormat., TextureCompressionQuality.Normal);
                // var copia = Instantiate(resultante);
                var copia = resultante;
                copia.hideFlags = HideFlags.NotEditable;
                copia.name = $"resultante #{c++}";
                try
                {
                    AssetDatabase.AddObjectToAsset(copia, path);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning(e);
                }
            }
        }
        c = 0;
        foreach (var sprite in paraGuardar.extractores.SelectMany(ext => ext.spriteResultantes))
        {
            if (!AssetDatabase.Contains(sprite))
            {
                sprite.hideFlags = HideFlags.NotEditable;
                sprite.name = $"sprite #{c++}";
                AssetDatabase.AddObjectToAsset(sprite, path);
            }
        }
        AssetDatabase.SaveAssets();
        Resources.UnloadUnusedAssets();
    }

    [CustomEditor(typeof(CapturadorSpritesSave))]
    public class MiEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var obj = target as CapturadorSpritesSave;
            if (GUILayout.Button("Editar"))
            {
                var instancia = Instantiate(obj);
                instancia.procesarRecuadros = Instantiate(instancia.procesarRecuadros);
                instancia.extractores = instancia.extractores.Select(ex => Instantiate(ex)).ToList();
                CapturadorSprites.AbrirCon(instancia, AssetDatabase.GetAssetPath(obj));
                Resources.UnloadUnusedAssets();
            }
        }
    }
#endif
}
