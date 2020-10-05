using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Guazu.NanoWeb;
using OpenCvSharp;
using System.Linq;
using Rect = UnityEngine.Rect;

public class CapturadorSprites : EditorWindow
{
    Texture2D texturaSubida;
    ProcesarRecuadros procRecuadros;
    Dictionary<Recuadro, ExtraerSprites> extractoresSprites = null;
    Editor procRecuadrosEditor, extSpritesEditor;
    bool dontDestroy;
    Vector2 scrollControles, scrollSprites;
    float alturaPreview = 250f;

    Texture2D textPrimerPasada, textRecuadro;
    Recuadro recuadroExaminado;

    List<Object> destruime = new List<Object>();

    [InitializeOnLoadMethod]
    static void Init()
    {
        NanoWebEditorWindow.UsarRuta("recibir imagen", "subir", (ctx, parser) =>
        {
            var textura = new Texture2D(8, 8);
            textura.LoadImage(parser.FileContents);
            NanoWebEditorWindow.ResponderString(ctx.Response, "imagen subida", true);

            var win = AbrirCon(textura, new GUIContent($"Imagen Recibida {System.DateTime.Now}", textura));
        });
    }

    [MenuItem("Assets/Procesar Textura", true)]
    static bool ProcesarTexturaValidator() => Selection.activeObject is Texture2D;
    [MenuItem("Assets/Procesar Textura", false, 0)]
    static void ProcesarTextura() => AbrirCon((Texture2D)Selection.activeObject);

    static CapturadorSprites AbrirCon(Texture2D textura, GUIContent titulo = null)
    {
        var win = CreateInstance<CapturadorSprites>();
        if (titulo != null) win.titleContent = titulo;
        win.destruime.Add(win.texturaSubida = textura);
        win.Show();
        return win;
    }

    Texture2D ActualizarPreview(Texture2D textura, OpenCvSharp.Mat mat)
    {
        if (!textura)
        {
            destruime.Add(textura = new Texture2D(mat.Width, mat.Height));
            textura.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
        }
        else if (textura.width != mat.Width || textura.height != mat.Height)
        {
            textura.Resize(mat.Width, mat.Height);
        }
        return OpenCvSharp.Unity.MatToTexture(mat, textura);
    }

    void OnEnable()
    {
        recuadroExaminado = null;
        if (procRecuadros && procRecuadros.Recuadros != null)
        {
            Procesar();
        }
    }
    private void OnDestroy()
    {
        recuadroExaminado = null;
        foreach (var obj in destruime)
        {
            if (obj && !AssetDatabase.Contains(obj)) DestroyImmediate(obj);
        }
    }

    void OnGUI()
    {
        if (texturaSubida)
        {
            var curEvent = Event.current;
            var dobleColumna = recuadroExaminado!=null && position.width > 600;
            var anchoColumna = dobleColumna ? position.width/2f:0;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical(GUILayout.MaxWidth(anchoColumna));

            var aspectTextura = texturaSubida.width / (float)texturaSubida.height;
            // var maxWidth = position/2f;


            EditorGUILayout.BeginHorizontal();
            // var areaPreview = GUILayoutUtility.GetRect(0, position.width, 0, alturaPreview);

            // var rect = new UnityEngine.Rect(areaPreview.x, areaPreview.y, areaPreview.height * aspectTextura, areaPreview.height);//GUILayoutUtility.GetAspectRect(aspectTextura);
            var rect = GUILayoutUtility.GetAspectRect(aspectTextura,GUILayout.MaxWidth(aspectTextura* alturaPreview));
            
            // rect.width /= 2f;
            EditorGUI.DrawTextureTransparent(rect, texturaSubida);
            EditorGUI.DropShadowLabel(rect, $"({texturaSubida.width}x{texturaSubida.height})");
            if (procRecuadros && procRecuadros.Recuadros != null)
            {
                var escala = new Vector2(rect.width / textPrimerPasada.width, rect.height / textPrimerPasada.height);
                int conter = 0;
                foreach (var rec in procRecuadros.Recuadros)
                {
                    var cvbbox = OpenCvSharp.Rect.BoundingBoxForPoints(rec.quadReducido.Select(p => new Point(p.X * escala.x, p.Y * escala.y)).ToArray());
                    var bbox = new UnityEngine.Rect(cvbbox.X + rect.x, cvbbox.Y, cvbbox.Width, cvbbox.Height);
                    // EditorGUI.DrawRect(bbox, Color.green);                    
                    if (recuadroExaminado == rec) GUI.color = Color.green;
                    EditorGUI.DropShadowLabel(bbox, $"#{++conter}");
                    GUI.color = Color.white;

                    if (curEvent.type == EventType.MouseUp && bbox.Contains(curEvent.mousePosition))
                    {
                        recuadroExaminado = rec == recuadroExaminado ? null : rec;
                        // if (recuadroExaminado != null) textRecuadro = ActualizarPreview(textRecuadro, rec.matRecuadroNormalizado);
                        if (recuadroExaminado != null) textRecuadro = ActualizarPreview(textRecuadro, extractoresSprites[rec].matRecuadro);
                    }

                }
            }
            // rect.x += rect.width;
            if (textRecuadro && recuadroExaminado != null)
            {
            aspectTextura = textRecuadro.width / (float)textRecuadro.height;
            rect = GUILayoutUtility.GetAspectRect(aspectTextura,GUILayout.MaxWidth(aspectTextura* alturaPreview));
                // rect.width = rect.height * textRecuadro.width / textRecuadro.height;
                EditorGUI.DrawTextureTransparent(rect, textRecuadro);
                EditorGUI.DropShadowLabel(rect, $"({textRecuadro.width}x{textRecuadro.height})");
            }
            else if (textPrimerPasada) {
            rect = GUILayoutUtility.GetAspectRect(aspectTextura,GUILayout.MaxWidth(aspectTextura* alturaPreview));
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
        }
    }

    void DibujarControlSprites()
    {
        if (extractoresSprites.ContainsKey(recuadroExaminado))
        {
            Editor.CreateCachedEditor(extractoresSprites[recuadroExaminado], typeof(CustomExtSpriteEditor), ref extSpritesEditor);
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
                textRecuadro = ActualizarPreview(textRecuadro, extractoresSprites[recuadroExaminado].matRecuadro);
            }
        }
    }
    void SpritesEncontrados(bool dobleColumna)
    {
        if (extractoresSprites.ContainsKey(recuadroExaminado))
        {
            var extractor = extractoresSprites[recuadroExaminado];
            
            if (dobleColumna) {
                EditorGUILayout.EndVertical();
                EditorGUILayout.BeginVertical();
            }
            scrollSprites = EditorGUILayout.BeginScrollView(scrollSprites);
            if (!dobleColumna) EditorGUILayout.BeginHorizontal();
            
            int columna = 0;
            foreach(var texturaSprite in extractor.texturasResultantes) {
                if (!texturaSprite) continue;
                var aspect = texturaSprite.width/(float)texturaSprite.height;
                var maxWidth = GUILayout.MaxWidth( Mathf.Min(texturaSprite.width, dobleColumna ? position.width/2f : aspect*alturaPreview) );
                var rect = GUILayoutUtility.GetAspectRect(aspect, maxWidth);
                EditorGUI.DrawTextureTransparent(rect,texturaSprite);
                EditorGUI.DropShadowLabel(rect, $"({texturaSprite.width}x{texturaSprite.height})");

                if (!dobleColumna && ++columna >= 4) {
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
        if (extractoresSprites != null)
        {
            foreach (var combo in extractoresSprites)
            {
                if (combo.Value && !AssetDatabase.Contains(combo.Value)) DestroyImmediate(combo.Value);
                destruime.Remove(combo.Value);
            }
        }
        extractoresSprites = new Dictionary<Recuadro, ExtraerSprites>();
        foreach (var rec in procRecuadros.Recuadros)
        {
            var extractor = ScriptableObject.CreateInstance<ExtraerSprites>();
            extractoresSprites.Add(rec, extractor);
            ExtraerSpritesDeRecuadro(rec);
            destruime.Add(extractor);
        }
    }

    void ExtraerSpritesDeRecuadro(Recuadro rec)
    {
        var extractor = extractoresSprites[rec];
        extractor.Extraer(procRecuadros.MatOriginal, rec);
    }

    [CustomEditor(typeof(ProcesarRecuadros))]
    class CustomProcRecuadrosEditor : Editor
    {
        static readonly string[] excludeProps = new string[] { "conservarMatOriginal", "conservarEscalado", "m_Script",
            "recuadros", "conservarOriginal", "texturasResultantes", "procesarRecuadros" };

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
