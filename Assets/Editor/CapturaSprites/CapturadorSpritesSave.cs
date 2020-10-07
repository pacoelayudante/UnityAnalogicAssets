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

    void OnDestroy() {
        foreach(var extractor in extractores) DestroyImmediate(extractor);
        DestroyImmediate(procesarRecuadros);
    }

#if UNITY_EDITOR
    public void Save(string path)
    {
        var paraGuardar = Instantiate(this);

        procesarRecuadros = Instantiate(procesarRecuadros);
        extractores = extractores.Select(ex=>Instantiate(ex)).ToList();
        
        AssetDatabase.CreateAsset(this, path);
        AssetDatabase.AddObjectToAsset(procesarRecuadros, path);
        if (!AssetDatabase.Contains(texturaOrigen))
        {
            texturaOrigen.hideFlags = HideFlags.NotEditable;
            AssetDatabase.AddObjectToAsset(texturaOrigen, path);
        }
        foreach(var extractor in extractores) {
            AssetDatabase.AddObjectToAsset(extractor, path);
        }
        foreach (var texturas in extractores.SelectMany(ext=>ext.texturasResultantes))
        {
            if (!AssetDatabase.Contains(texturas))
            {
                texturas.hideFlags = HideFlags.NotEditable;
                AssetDatabase.AddObjectToAsset(texturas, path);
            }
            foreach (var sprite in extractores.SelectMany(ext=>ext.spriteResultantes))
            {
                if (!AssetDatabase.Contains(sprite))
                {
                    sprite.hideFlags = HideFlags.NotEditable;
                    AssetDatabase.AddObjectToAsset(sprite, path);
                }
            }
        }
        AssetDatabase.SaveAssets();
        Resources.UnloadUnusedAssets();
    }

    [CustomEditor(typeof(CapturadorSpritesSave))]
    public class MiEditor : Editor {
        public override void OnInspectorGUI() {
            DrawDefaultInspector();
            var obj = target as CapturadorSpritesSave;
            if (GUILayout.Button("Editar")) {
                var instancia = Instantiate(obj);
                instancia.procesarRecuadros = Instantiate(instancia.procesarRecuadros);
                instancia.extractores = instancia.extractores.Select(ex=>Instantiate(ex)).ToList();
                CapturadorSprites.AbrirCon(instancia, AssetDatabase.GetAssetPath(obj));
                Resources.UnloadUnusedAssets();
            }
        }
    }
#endif
}
