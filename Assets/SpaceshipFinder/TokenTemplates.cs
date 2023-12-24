using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Rect = UnityEngine.Rect;
using CvRect = OpenCvSharp.Rect;
using OpenCvSharp;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu]
public class TokenTemplates : ScriptableObject
{
    [System.Serializable]
    public class TokenTemplate
    {
        public string Nombre = "Token";
        public Point[] contorno;
        public CvRect cvRect;
        // public PostPRoc
        public TipoTam tipoTam = TipoTam.Mayor;

        public int ordenDeDisparo = 0;
        public bool armaPoderosa;
        
        public List<Vector2> escudos = new();//x=porcentaje, y=tam
    }

    public enum TipoTam { Menor, Referencia, Mayor }

    public TokenTemplate[] tokenTemplates;

#if UNITY_EDITOR
    [CustomEditor(typeof(TokenTemplates))]
    public class TokenTemplatesEditor : Editor
    {
        RenderTexture _renderTexture;
        private Texture2D _texturaInput;
        private TokenTemplates _templates;
        private ColorBlobs _colorBlobs;
        TipoHue _tipoHue = TipoHue.HSV;

        private Material material;

        public void OnEnable()
        {
            _templates = (TokenTemplates)target;

            if (material == null)
                // Find the "Hidden/Internal-Colored" shader, and cache it for use.
                material = new Material(Shader.Find("Hidden/Internal-Colored"));
        }

        public override void OnInspectorGUI()
        {
            using (var changed2 = new EditorGUI.ChangeCheckScope())
            {
                _renderTexture = EditorGUILayout.ObjectField("Render Texture", _renderTexture, typeof(RenderTexture), allowSceneObjects: false) as RenderTexture;
                if (changed2.changed && _renderTexture)
                {
                    if (_texturaInput != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(_texturaInput)))
                        DestroyImmediate(_texturaInput);

                    _texturaInput = new Texture2D(_renderTexture.width, _renderTexture.height, TextureFormat.RGBA32, false, true);
                    RenderTexture.active = _renderTexture;
                    _texturaInput.ReadPixels(new Rect(0, 0, _texturaInput.width, _texturaInput.height), 0, 0, false);
                    _texturaInput.Apply(false);
                }
            }
            _texturaInput = (Texture2D)EditorGUILayout.ObjectField("Templates Image", _texturaInput, typeof(Texture2D), allowSceneObjects: false);
            _colorBlobs = (ColorBlobs)EditorGUILayout.ObjectField("Color Blobs", _colorBlobs, typeof(ColorBlobs), allowSceneObjects: false);
            _tipoHue = (TipoHue)EditorGUILayout.EnumPopup(_tipoHue);
            DrawDefaultInspector();

            if (GUILayout.Button("Extract Templates"))
            {

                if (_texturaInput != null)
                {
                    using (Mat outBlobMat = new Mat())
                    using (Mat testMat = OpenCvSharp.Unity.TextureToMat(_texturaInput))
                    {
                        Cv2.CvtColor(testMat, testMat, _tipoHue == TipoHue.HSV ? ColorConversionCodes.BGR2HSV : ColorConversionCodes.BGR2HLS);
                        _colorBlobs.FromHueMat(testMat, _tipoHue, outBlobMat, out Point[][] contornos, out HierarchyIndex[] jerarquias);

                        TokenTemplate[] tokenTemplates = new TokenTemplate[contornos.Length];
                        for (int i = 0; i < tokenTemplates.Length; i++)
                        {
                            tokenTemplates[i] = new TokenTemplate()
                            {
                                contorno = contornos[i],
                                cvRect = Cv2.BoundingRect(contornos[i]),
                            };
                        }
                        _templates.tokenTemplates = tokenTemplates;
                    }
                }

            }

            if (_templates.tokenTemplates != null)
            {
                //var queryTextureSize = _testTexture.texelSize;
                EditorGUILayout.BeginHorizontal();
                foreach (var token in _templates.tokenTemplates)
                {
                    var guirect = GUILayoutUtility.GetRect(token.cvRect.Width, token.cvRect.Height, GUILayout.ExpandWidth(false));
                    // if (guirect.xMax > EditorGUIUtility.currentViewWidth - 5f)
                    // {
                    //     EditorGUILayout.EndHorizontal();
                    //     EditorGUILayout.BeginHorizontal();
                    //     guirect = GUILayoutUtility.GetRect(token.cvRect.Width, token.cvRect.Height, GUILayout.ExpandWidth(false));
                    // }
                    //EditorGUI.DrawRect(guirect, Color.white);
                    DibujarContorno(guirect, -token.cvRect.TopLeft, token.contorno);
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DibujarContorno(Rect rect, Point offset, Point[] contorno)
        {
            if (Event.current.type == EventType.Repaint)
            {
                using (new GUI.ClipScope(rect))
                {
                    GL.PushMatrix();

                    // Clear the current render buffer, setting a new background colour, and set our
                    // material for rendering.
                    GL.Clear(true, false, Color.black);
                    material.SetPass(0);

                    // Start drawing in OpenGL Lines, to draw the lines of the grid.
                    GL.Begin(GL.LINE_STRIP);

                    GL.Color(Color.black);
                    foreach (var p in contorno)
                        GL.Vertex3(p.X + offset.X, p.Y + offset.Y, 0f);
                    GL.End();

                    // Pop the current matrix for rendering, and end the drawing clip.
                    GL.PopMatrix();

                }
            }
        }
    }
#endif
}
