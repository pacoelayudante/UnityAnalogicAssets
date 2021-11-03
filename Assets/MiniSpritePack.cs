using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MiniSpritePack : ScriptableObject
{
    public List<Sprite> sprites = new List<Sprite>();
#if UNITY_EDITOR
    public static void CreateAsset(IEnumerable<Sprite> sprites, string path)
    {
        var pack = ScriptableObject.CreateInstance<MiniSpritePack>();
        pack.sprites = sprites.ToList();
        CreateAsset(pack, path);
    }
    public static void CreateAsset(MiniSpritePack pack, string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            path = AssetDatabase.GetAssetPath(pack);
        }

        var name = System.IO.Path.GetFileNameWithoutExtension(path);
        AssetDatabase.CreateAsset(pack, path);

        var grupitos = pack.sprites.GroupBy(sp=>sp.texture);
        int tc = 0, sc = 0;
        foreach (var grupo in grupitos) {
            grupo.Key.hideFlags = HideFlags.NotEditable;
            AssetDatabase.AddObjectToAsset(grupo.Key, path);
            grupo.Key.name = $"{name}_t{tc++}";
            tc++;
            sc = 0;
            foreach(var sp in grupo) {
                sp.hideFlags = HideFlags.NotEditable;
                sp.name = $"{sp.texture.name}_s{sc}";
                AssetDatabase.AddObjectToAsset(sp, path);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    [CustomEditor(typeof(MiniSpritePack))]
    public class MiniSpritePackEditor : Editor
    {

    }
#endif
}
