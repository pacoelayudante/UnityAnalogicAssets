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
public class TokenDetector : ScriptableObject
{
    public TipoHue _tipoHue = TipoHue.HSV;

    public TokenTemplates _tokenTemplates;
    public ColorBlobs _blobsPurpura;
    public ColorBlobs _blobsAmarillos;
    public ColorBlobs _blobsFuxia;

    [SerializeField]
    private ShapeMatchModes _shapeMatchModes = ShapeMatchModes.I3;

    public class ComparacionTemplate
    {
        public TokenTemplates.TokenTemplate token;
        public double divergencia;

        public float orientacionGrados;
    }

    public class TokenEncontrado
    {
        public Point[] contorno;
        public CvRect cvBBox;
        public Rect uvBBox;

        public int areaRect;
        public Point[] convexHull;
        public Point2d centroideContorno;
        public Point2d centroideHull;

        public List<ComparacionTemplate> comparacionesOrdenadas = new();
        public Dictionary<TokenTemplates.TokenTemplate, ComparacionTemplate> comparaciones = new();

        public TokenEncontrado(Point[] contorno, float hMat, TokenTemplates.TokenTemplate[] templates, ShapeMatchModes shapeMatchModes)
        {
            cvBBox = Cv2.BoundingRect(contorno);
            this.contorno = contorno;
            uvBBox = new Rect(cvBBox.Left, hMat - cvBBox.Bottom, cvBBox.Width, cvBBox.Height);

            foreach (var template in templates)
            {
                var comparacion = new ComparacionTemplate()
                {
                    token = template,
                    divergencia = Cv2.MatchShapes(contorno, template.contorno, shapeMatchModes)
                };
                comparaciones[template] = comparacion;
                comparacionesOrdenadas.Add(comparacion);
            }

            comparacionesOrdenadas.Sort((matchA, matchB) => matchA.divergencia.CompareTo(matchB.divergencia));

            // var mejorComparacion = comparacionesOrdenadas[0];
            areaRect = cvBBox.Width * cvBBox.Height;
            convexHull = Cv2.ConvexHull(contorno);

            var moments = Cv2.Moments(contorno);
            centroideContorno = new Point2d((moments.M10 / moments.M00), (moments.M01 / moments.M00));
            moments = Cv2.Moments(convexHull);
            centroideHull = new Point2d((moments.M10 / moments.M00), (moments.M01 / moments.M00));
        }
    }

    public class Resultados
    {
        public List<TokenEncontrado> tokensPurpura;
        public List<TokenEncontrado> tokensAmarillo;
        public Point[][] contornosFuxia;
    }

    public void Detectar(Texture2D texture2D, out Resultados resultados)
    {
        using (Mat outBlobMat = new Mat())
        using (Mat testMat = OpenCvSharp.Unity.TextureToMat(texture2D))
        {
            Cv2.CvtColor(testMat, testMat, _tipoHue == TipoHue.HSV ? ColorConversionCodes.BGR2HSV : ColorConversionCodes.BGR2HLS);
            Detectar(testMat, _tipoHue, out resultados);
        }
    }

    public void Detectar(Mat hueInputMat, TipoHue tipoHue, out Resultados resultados)
    {
        using (var resultadoBinario = new Mat())
        {
            resultados = new();
            _blobsPurpura.FromHueMat(hueInputMat, tipoHue, resultadoBinario, out Point[][] contornosP, out HierarchyIndex[] jerarquiasP);

            resultados.tokensPurpura = new();

            for (int i = 0; i < contornosP.Length; i++)
            {
                // resultados.tokensPurpura.Add(ProcesarTokenEncontrado(contornos[i], hueInputMat.Height));
                resultados.tokensPurpura.Add(new TokenEncontrado(contornosP[i], hueInputMat.Height, _tokenTemplates.tokenTemplates, _shapeMatchModes));
            }

            _blobsAmarillos.FromHueMat(hueInputMat, tipoHue, resultadoBinario, out Point[][] contornosA, out HierarchyIndex[] jerarquiasA);

            resultados.tokensAmarillo = new();

            for (int i = 0; i < contornosA.Length; i++)
            {
                // resultados.tokensAmarillo.Add(ProcesarTokenEncontrado(contornos[i], hueInputMat.Height));
                resultados.tokensAmarillo.Add(new TokenEncontrado(contornosA[i], hueInputMat.Height, _tokenTemplates.tokenTemplates, _shapeMatchModes));
            }

            _blobsFuxia.FromHueMat(hueInputMat, tipoHue, resultadoBinario, out resultados.contornosFuxia, out HierarchyIndex[] jerarquias2, resultadoBinario);

            Cv2.DistanceTransform(resultadoBinario, resultadoBinario, DistanceTypes.L2, DistanceMaskSize.Precise);
            // Cv2.ConvertScaleAbs(resultadoBinario, resultadoBinario, 60f, 0f);
        }
    }

    // public TokenEncontrado ProcesarTokenEncontrado(Point[] contorno, float hMat)
    // {
    //     // var cvBBox = Cv2.BoundingRect(contorno);
    //     var token = new TokenEncontrado(contorno, hMat, );
    //     // {
    //     //     contorno = contorno,
    //     //     cvBBox = cvBBox,
    //     //     uvBBox = new Rect(cvBBox.Left, hMat - cvBBox.Bottom, cvBBox.Width, cvBBox.Height)
    //     // };

    //     // for (int i=0; i<_tokenTemplates.tokenTemplates.Length; i++)
    //     // foreach (var template in _tokenTemplates.tokenTemplates)
    //     // {
    //     //     var comparacion = new ComparacionTemplate()
    //     //     {
    //     //         token = template,
    //     //         divergencia = Cv2.MatchShapes(contorno, template.contorno, _shapeMatchModes)
    //     //     };
    //     //     token.comparaciones[template] = comparacion;
    //     //     token.comparacionesOrdenadas.Add(comparacion);
    //     // }

    //     token.comparacionesOrdenadas.Sort((matchA, matchB) => matchA.divergencia.CompareTo(matchB.divergencia));

    //     var mejorComparacion = token.comparacionesOrdenadas[0];

    //     return token;
    // }

#if UNITY_EDITOR
    [CustomEditor(typeof(TokenDetector))]
    public class TokenDetectorEditor : Editor
    {
        RenderTexture _renderTexture;
        private Texture2D _texturaInput;
        Texture2D _testResultado;
        // List<TokenEncontrado> tokenEncontrados;

        private Material material;
        TokenDetector detector;
        // TipoHue _tipoHue = TipoHue.HSV;
        Resultados resultados;

        Vector2 scroll;

        public void OnEnable()
        {
            detector = (TokenDetector)target;

            if (material == null)
                // Find the "Hidden/Internal-Colored" shader, and cache it for use.
                material = new Material(Shader.Find("Hidden/Internal-Colored"));
        }

        void OnDisable()
        {
            if (_texturaInput != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(_texturaInput)))
                DestroyImmediate(_texturaInput);
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
            EditorGUILayout.ObjectField("Resultado", _testResultado, typeof(Texture2D), allowSceneObjects: false);
            // _tipoHue = (TipoHue)EditorGUILayout.EnumPopup(_tipoHue);
            DrawDefaultInspector();

            if (GUILayout.Button("Detectar"))
            {
                if (_texturaInput != null)
                {
                    // using (Mat outBlobMat = new Mat())
                    // {
                    detector.Detectar(_texturaInput, out resultados);
                    // _testResultado = OpenCvSharp.Unity.MatToTexture(outBlobMat, _testResultado);
                    // }
                }
            }

            using (var escroll = new EditorGUILayout.ScrollViewScope(scroll))
            {
                scroll = escroll.scrollPosition;
                if (detector._tokenTemplates != null)
                {
                    var textureSize = _texturaInput ? _texturaInput.texelSize : Vector2.one;
                    for (int i = 0; i < detector._tokenTemplates.tokenTemplates.Length; i++)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            var template = detector._tokenTemplates.tokenTemplates[i];

                            var guirect = GUILayoutUtility.GetRect(template.cvRect.Width, template.cvRect.Height, GUILayout.ExpandWidth(false));

                            DibujarContorno(guirect, -template.cvRect.TopLeft.X, -template.cvRect.TopLeft.Y, template.contorno, Color.black);
                            if (resultados?.tokensPurpura != null)
                            {
                                foreach (var encontrado in resultados.tokensPurpura)
                                {
                                    if (encontrado.comparacionesOrdenadas[0].token == template)
                                    {
                                        TokenEntcontradoGUI(guirect, encontrado, textureSize);
                                    }
                                }
                            }
                            if (resultados?.tokensAmarillo != null)
                            {
                                foreach (var encontrado in resultados.tokensAmarillo)
                                {
                                    if (encontrado.comparacionesOrdenadas[0].token == template)
                                    {
                                        TokenEntcontradoGUI(guirect, encontrado, textureSize);
                                    }
                                }
                            }

                        }
                    }
                }
            }
        }

        private void TokenEntcontradoGUI(Rect guirect, TokenEncontrado encontrado, Vector2 textureSize)
        {
            var bboxFound = encontrado.uvBBox;
            guirect = GUILayoutUtility.GetRect(bboxFound.width, bboxFound.height, GUILayout.ExpandWidth(false));
            if (_texturaInput)
                GUI.DrawTextureWithTexCoords(guirect, _texturaInput, new Rect(bboxFound.position * textureSize, bboxFound.size * textureSize));
            DibujarContorno(guirect, -encontrado.cvBBox.TopLeft.X, -encontrado.cvBBox.TopLeft.Y, encontrado.contorno, Color.black);
            DibujarContorno(guirect, -encontrado.cvBBox.TopLeft.X, -encontrado.cvBBox.TopLeft.Y, encontrado.convexHull, Color.yellow);

            var pA = encontrado.centroideContorno;
            var pB = -(encontrado.centroideHull - encontrado.centroideContorno) * 10 + encontrado.centroideContorno;
            DibujarLinea(guirect, -encontrado.cvBBox.TopLeft.X + (float)pA.X, -encontrado.cvBBox.TopLeft.Y + (float)pA.Y, -encontrado.cvBBox.TopLeft.X + (float)pB.X, -encontrado.cvBBox.TopLeft.Y + (float)pB.Y, Color.cyan);
        }

        private void DibujarContorno(Rect rect, float offsetX, float offsetY, Point[] contorno, Color col)
        {
            if (Event.current.type == EventType.Repaint)
            {
                using (new GUI.ClipScope(rect))
                {
                    GL.PushMatrix();

                    // Clear the current render buffer, setting a new background colour, and set our
                    // material for rendering.
                    GL.Clear(true, false, col);
                    material.SetPass(0);

                    // Start drawing in OpenGL Lines, to draw the lines of the grid.
                    GL.Begin(GL.LINE_STRIP);

                    GL.Color(col);
                    foreach (var p in contorno)
                        GL.Vertex3(p.X + offsetX, p.Y + offsetY, 0f);
                    GL.End();

                    // Pop the current matrix for rendering, and end the drawing clip.
                    GL.PopMatrix();

                }
            }
        }

        private void DibujarCirculo(Rect rect, float offsetX, float offsetY, float radio, Color col)
        {
            if (Event.current.type == EventType.Repaint)
            {
                using (new GUI.ClipScope(rect))
                {
                    GL.PushMatrix();

                    // Clear the current render buffer, setting a new background colour, and set our
                    // material for rendering.
                    GL.Clear(true, false, col);
                    material.SetPass(0);

                    // Start drawing in OpenGL Lines, to draw the lines of the grid.
                    GL.Begin(GL.LINE_STRIP);

                    GL.Color(col);
                    foreach (var p in new[] { 0, 30, 60, 90, 120, 180, 210, 240, 270, 300, 330, 360 })
                        GL.Vertex3(radio * Mathf.Cos(p * Mathf.Deg2Rad) + offsetX, radio * Mathf.Sin(p * Mathf.Deg2Rad) + offsetY, 0f);
                    GL.End();

                    // Pop the current matrix for rendering, and end the drawing clip.
                    GL.PopMatrix();

                }
            }
        }

        private void DibujarLinea(Rect rect, float aX, float aY, float bX, float bY, Color col)
        {
            if (Event.current.type == EventType.Repaint)
            {
                using (new GUI.ClipScope(rect))
                {
                    GL.PushMatrix();

                    // Clear the current render buffer, setting a new background colour, and set our
                    // material for rendering.
                    GL.Clear(true, false, col);
                    material.SetPass(0);

                    // Start drawing in OpenGL Lines, to draw the lines of the grid.
                    GL.Begin(GL.LINE_STRIP);

                    GL.Color(col);
                    GL.Vertex3(aX, aY, 0f);
                    GL.Vertex3(bX, bY, 0f);
                    GL.End();

                    // Pop the current matrix for rendering, and end the drawing clip.
                    GL.PopMatrix();

                }
            }
        }
    }
#endif
}
