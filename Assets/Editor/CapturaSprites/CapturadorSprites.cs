using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using OpenCvSharp;
using System.Linq;
using Rect = UnityEngine.Rect;

public class CapturadorSprites : EditorWindow
{
    Texture2D texturaSubida;
    ProcesarRecuadros procRecuadros;
    // Dictionary<Recuadro, ExtraerSprites> extractoresSprites = null;
    List<ExtraerSprites> extractoresSprites = null;
    Editor procRecuadrosEditor, extSpritesEditor;
    bool dontDestroy;
    Vector2 scrollControles, scrollSprites;
    float alturaPreview = 250f;

    Texture2D textPrimerPasada, textRecuadro;
    Recuadro recuadroExaminado;

    CapturadorSpritesSave saveFile;
    string pathOrigen;

    List<Object> destruime = new List<Object>();

    [MenuItem("Assets/Extraer Sprites", true)]
    static bool ProcesarTexturaValidator() => Selection.activeObject is Texture2D;
    [MenuItem("Assets/Extraer Sprites", false, 0)]
    static void ProcesarTextura() => AbrirCon((Texture2D)Selection.activeObject);

    public static CapturadorSprites AbrirCon(Texture2D textura, GUIContent titulo = null)
    {
        var win = CreateWindow<CapturadorSprites>();
        if (titulo != null) win.titleContent = titulo;
        win.destruime.Add(win.texturaSubida = textura);
        win.Show();
        return win;
    }
    public static CapturadorSprites AbrirCon(CapturadorSpritesSave saveFile, string pathOrigen)
    {
        var win = CreateWindow<CapturadorSprites>();
        win.titleContent = new GUIContent(saveFile.name);
        win.destruime.Add(win.saveFile = saveFile);
        win.destruime.Add(win.texturaSubida = saveFile.texturaOrigen);
        win.destruime.Add(win.procRecuadros = saveFile.procesarRecuadros);
        win.pathOrigen = pathOrigen;
        win.extractoresSprites = saveFile.extractores;
        foreach (var extr in win.extractoresSprites) win.destruime.Add(extr);
        win.Procesar();
        win.Show();
        return win;
    }

    // void OnDisable() {

    // }

    Texture2D ActualizarPreview(Texture2D textura, OpenCvSharp.Mat mat)
    {
        if (!textura)
        {
            destruime.Add(textura = new Texture2D(mat.Width, mat.Height));
            textura.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
        }
        else if (textura.width != mat.Width || textura.height != mat.Height)
        {
            textura.Reinitialize(mat.Width, mat.Height);
        }
        return OpenCvSharp.Unity.MatToTexture(mat, textura);
    }

    void OnEnable()
    {
        if (texturaSubida)
        {
            recuadroExaminado = null;
            if (procRecuadros && procRecuadros.Recuadros != null)
            {
                Procesar();
            }
        }
    }
    private void OnDestroy()
    {
        if (texturaSubida)
        {
            recuadroExaminado = null;
            foreach (var obj in destruime)
            {
                if (obj && !AssetDatabase.Contains(obj)) DestroyImmediate(obj);
            }
        }
    }

    void OnGUI()
    {
        if (texturaSubida)
        {
            var curEvent = Event.current;
            var dobleColumna = recuadroExaminado != null && position.width > 600;
            var anchoColumna = dobleColumna ? position.width / 2f : 0;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical(GUILayout.MaxWidth(anchoColumna));

            var aspectTextura = texturaSubida.width / (float)texturaSubida.height;
            // var maxWidth = position/2f;


            EditorGUILayout.BeginHorizontal();
            // var areaPreview = GUILayoutUtility.GetRect(0, position.width, 0, alturaPreview);

            // var rect = new UnityEngine.Rect(areaPreview.x, areaPreview.y, areaPreview.height * aspectTextura, areaPreview.height);//GUILayoutUtility.GetAspectRect(aspectTextura);
            var rect = GUILayoutUtility.GetAspectRect(aspectTextura, GUILayout.MaxWidth(aspectTextura * alturaPreview));

            // rect.width /= 2f;
            EditorGUI.DrawTextureTransparent(rect, texturaSubida);
            EditorGUI.DropShadowLabel(rect, $"({texturaSubida.width}x{texturaSubida.height})");
            if (procRecuadros && procRecuadros.Recuadros != null)
            {
                var escala = new Vector2(rect.width / textPrimerPasada.width, rect.height / textPrimerPasada.height);
                foreach (var rec in procRecuadros.Recuadros)
                {
                    var cvbbox = OpenCvSharp.Rect.BoundingBoxForPoints(rec.quadReducido.Select(p => new Point(p.X * escala.x, p.Y * escala.y)).ToArray());
                    var bbox = new UnityEngine.Rect(cvbbox.X + rect.x, cvbbox.Y, cvbbox.Width, cvbbox.Height);
                    // EditorGUI.DrawRect(bbox, Color.green);                    
                    if (recuadroExaminado == rec) GUI.color = Color.green;
                    EditorGUI.DropShadowLabel(bbox, $"#{procRecuadros.Recuadros.IndexOf(rec)}");
                    GUI.color = Color.white;

                    if (curEvent.type == EventType.MouseUp && bbox.Contains(curEvent.mousePosition))
                    {
                        recuadroExaminado = rec == recuadroExaminado ? null : rec;
                        // if (recuadroExaminado != null) textRecuadro = ActualizarPreview(textRecuadro, rec.matRecuadroNormalizado);
                        if (recuadroExaminado != null) textRecuadro = ActualizarPreview(textRecuadro, ExtractorDe(rec).matRecuadro);
                    }

                }
            }
            // rect.x += rect.width;
            if (textRecuadro && recuadroExaminado != null)
            {
                aspectTextura = textRecuadro.width / (float)textRecuadro.height;
                rect = GUILayoutUtility.GetAspectRect(aspectTextura, GUILayout.MaxWidth(aspectTextura * alturaPreview));
                // rect.width = rect.height * textRecuadro.width / textRecuadro.height;
                EditorGUI.DrawTextureTransparent(rect, textRecuadro);
                EditorGUI.DropShadowLabel(rect, $"({textRecuadro.width}x{textRecuadro.height})");
            }
            else if (textPrimerPasada)
            {
                rect = GUILayoutUtility.GetAspectRect(aspectTextura, GUILayout.MaxWidth(aspectTextura * alturaPreview));
                EditorGUI.DrawTextureTransparent(rect, textPrimerPasada);
            }

            EditorGUILayout.EndHorizontal();

            if (!procRecuadros)
            {
                destruime.Add(procRecuadros = ScriptableObject.CreateInstance<ProcesarRecuadros>());
                Procesar();
                Editor.CreateCachedEditor(procRecuadros, typeof(CustomProcRecuadrosEditor), ref procRecuadrosEditor);
            }

            if (extractoresSprites != null && recuadroExaminado != null)
            {
                DibujarControlSprites();
                SpritesEncontrados(dobleColumna);
            }
            else
            {
                DibujarControlRecuadros();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(pathOrigen));
            if (GUILayout.Button("Save"))
            {
                Save(pathOrigen);
            }
            EditorGUI.EndDisabledGroup();
            if (GUILayout.Button("Save As.."))
            {
                var path = EditorUtility.SaveFilePanelInProject("Guardar Extractor De Sprites", "auto_sprites", "asset", "Puedes volver a cambiar parametros y volver a procesar los sprites");
                if (!string.IsNullOrEmpty(path))
                {
                    Save(pathOrigen = path);
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Export PNGs...")) {                
                var path = EditorUtility.SaveFilePanelInProject("Wont create folder", "exported_sprites_", "", "Exportar sprites como png");
                if (!string.IsNullOrEmpty(path))
                {
                    ExportPNGs(path);
                }
            }
            if (GUILayout.Button("Export Sprite Pack...")) {                
                var path = EditorUtility.SaveFilePanelInProject("Save single asset with sprites", "sprites_pack", "asset", "Exportar sprites como un solo asset");
                if (!string.IsNullOrEmpty(path))
                {
                    ExportarSpritePack(path);
                }
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    ExtraerSprites ExtractorDe(Recuadro rec)
    {
        var index = procRecuadros.Recuadros.IndexOf(rec);
        return extractoresSprites[index];
    }
    bool ExtractorExisteDe(Recuadro rec)
    {
        var index = procRecuadros.Recuadros.IndexOf(rec);
        return index != -1 && extractoresSprites[index] != null;
    }

    void Save(string path)
    {
        Debug.Log($"saving at {path}");
        if (!saveFile)
        {
            saveFile = ScriptableObject.CreateInstance<CapturadorSpritesSave>();
            saveFile.texturaOrigen = texturaSubida;
            saveFile.procesarRecuadros = procRecuadros;
            saveFile.extractores = extractoresSprites;
        }
        saveFile.Save(path);
        // AssetDatabase.CreateAsset(procRecuadros,path);
        // // if (!AssetDatabase.Contains(texturaSubida)) 
        // {
        //     textPrimerPasada.hideFlags = texturaSubida.hideFlags;
        //     Debug.Log($"saving texture too");
        //     AssetDatabase.AddObjectToAsset(textPrimerPasada,path);
        // }
        // AssetDatabase.SaveAssets();
    }

    void ExportPNGs(string path) {
        int c = 0;
        foreach(var text in extractoresSprites.SelectMany(ext=>ext.texturasResultantes)) {
            System.IO.File.WriteAllBytes($"{path}_{c++}.png", text.EncodeToPNG());
        }
        AssetDatabase.Refresh();
    }
    void ExportarSpritePack(string path) {
        MiniSpritePack.CreateAsset(extractoresSprites.SelectMany(ext=>ext.spriteResultantes), path);
        AssetDatabase.Refresh();
    }

    void DibujarControlSprites()
    {
        if (ExtractorExisteDe(recuadroExaminado))
        {
            Editor.CreateCachedEditor(ExtractorDe(recuadroExaminado), typeof(CustomExtSpriteEditor), ref extSpritesEditor);
        }
        if (extSpritesEditor != null)
        {
            EditorGUI.BeginChangeCheck();
            scrollControles = EditorGUILayout.BeginScrollView(scrollControles);
            extSpritesEditor.OnInspectorGUI();
            EditorGUILayout.EndScrollView();
            if (EditorGUI.EndChangeCheck())
            {
                ExtraerSpritesDeRecuadro(recuadroExaminado);
                textRecuadro = ActualizarPreview(textRecuadro, ExtractorDe(recuadroExaminado).matRecuadro);
            }
        }
    }
    void SpritesEncontrados(bool dobleColumna)
    {
        if (ExtractorExisteDe(recuadroExaminado))
        {
            var extractor = ExtractorDe(recuadroExaminado);

            if (dobleColumna)
            {
                EditorGUILayout.EndVertical();
                EditorGUILayout.BeginVertical();
            }
            scrollSprites = EditorGUILayout.BeginScrollView(scrollSprites);
            if (!dobleColumna) EditorGUILayout.BeginHorizontal();

            int columna = 0;
            foreach (var texturaSprite in extractor.texturasResultantes)
            {
                if (!texturaSprite) continue;
                var aspect = texturaSprite.width / (float)texturaSprite.height;
                var maxWidth = GUILayout.MaxWidth(Mathf.Min(texturaSprite.width, dobleColumna ? position.width / 2f : aspect * alturaPreview));
                var rect = GUILayoutUtility.GetAspectRect(aspect, maxWidth);
                EditorGUI.DrawTextureTransparent(rect, texturaSprite);
                EditorGUI.DropShadowLabel(rect, $"({texturaSprite.width}x{texturaSprite.height})");

                if (!dobleColumna && ++columna >= 4)
                {
                    columna = 0;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                }
            }

            if (!dobleColumna) EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();
        }
    }

    void DibujarControlRecuadros()
    {
        if (procRecuadrosEditor == null) Editor.CreateCachedEditor(procRecuadros, typeof(CustomProcRecuadrosEditor), ref procRecuadrosEditor);
        if (procRecuadrosEditor != null)
        {
            EditorGUI.BeginChangeCheck();
            scrollControles = EditorGUILayout.BeginScrollView(scrollControles);
            procRecuadrosEditor.OnInspectorGUI();
            EditorGUILayout.EndScrollView();
            if (EditorGUI.EndChangeCheck())
            {
                Procesar();
            }
        }
    }

    void Procesar()
    {
        recuadroExaminado = null;
        var matProcesado = procRecuadros.ProcesarTextura(texturaSubida, true);
        var escala = matProcesado.Width / (double)matProcesado.Width;
        if (matProcesado.Channels() == 1) Cv2.CvtColor(matProcesado, matProcesado, ColorConversionCodes.GRAY2BGR);

        var arbol = procRecuadros.mapaDeContornos.contornosExteriores;
        Cv2.DrawContours(matProcesado, arbol.Select(c => c.contorno.Select(p => p * escala)), -1, ProcesarRecuadros.ColEscalarAzul);
        foreach (var rec in procRecuadros.Recuadros)
        {
            // rec.DibujarDebug(matProcesado, ProcesarRecuadros.ColEscalarRojo, escala);
            // var rect = Cv2.MinAreaRect(rec.contornoOriginal);
            Cv2.Polylines(matProcesado, new[] { rec.quadReducido }, true, ProcesarRecuadros.ColEscalarRojo, 1);
        }
        textPrimerPasada = ActualizarPreview(textPrimerPasada, matProcesado);
        ActualizarExtractoresSprites();
    }

    void ActualizarExtractoresSprites()
    {
        if (extractoresSprites == null) extractoresSprites = new List<ExtraerSprites>();
        // {
        //     foreach (var extract in extractoresSprites)
        //     {
        //         if (extract && !AssetDatabase.Contains(extract)) DestroyImmediate(extract);
        //         destruime.Remove(extract);
        //     }
        // }

        for (int i = 0; i < procRecuadros.Recuadros.Count; i++)
        {
            if (i == extractoresSprites.Count)
            {
                extractoresSprites.Add(ScriptableObject.CreateInstance<ExtraerSprites>());
                destruime.Add(extractoresSprites[i]);
            }
            ExtraerSpritesDeRecuadro(procRecuadros.Recuadros[i]);
        }
    }

    void ExtraerSpritesDeRecuadro(Recuadro rec)
    {
        var extractor = ExtractorDe(rec);
        extractor.Extraer(procRecuadros.MatOriginal, rec);
    }

    [CustomEditor(typeof(ProcesarRecuadros))]
    class CustomProcRecuadrosEditor : Editor
    {
        static readonly string[] excludeProps = new string[] { "conservarMatOriginal", "conservarEscalado", "m_Script",
            "recuadros", "conservarOriginal", "texturasResultantes", "procesarRecuadros", "spriteResultantes" };

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            DrawPropertiesExcluding(serializedObject, excludeProps);
            if (EditorGUI.EndChangeCheck()) serializedObject.ApplyModifiedProperties();
        }
    }

    [CustomEditor(typeof(ExtraerSprites))]
    class CustomExtSpriteEditor : CustomProcRecuadrosEditor { }
}
