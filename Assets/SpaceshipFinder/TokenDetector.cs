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
    public const int EQUIPO_PURPURA = 0;
    public const int EQUIPO_AMARILLO = 1;

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
    }

    public class TokenEncontrado
    {
        public int equipo;

        public Point[] contorno;
        public CvRect cvBBox;
        public Rect uvBBox;

        public int areaRect;
        public Point[] convexHull;
        public Point2d centroideContorno;
        public Point2d centroideHull;

        public Point2f centroCirculo;
        public float radioCirculo;

        public List<Point2d> puntosArmas = new();
        public int indiceArmaCentral = 0;

        public Point2d ArmaCentral => puntosArmas.Count > 0 ? puntosArmas[indiceArmaCentral] : centroideContorno;

        public List<ComparacionTemplate> comparacionesOrdenadas = new();
        public Dictionary<TokenTemplates.TokenTemplate, ComparacionTemplate> comparaciones = new();

        public TokenTemplates.TokenTemplate TemplateMasPosible => comparacionesOrdenadas == null || comparacionesOrdenadas.Count == 0 ? null : comparacionesOrdenadas[0].token;
        public TokenTemplates.TipoTam TipoTam => TemplateMasPosible == null ? TokenTemplates.TipoTam.Menor : TemplateMasPosible.tipoTam;
        public int OrdenDeDisparo => TemplateMasPosible == null ? -1 : TemplateMasPosible.ordenDeDisparo;

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

            areaRect = cvBBox.Width * cvBBox.Height;
            convexHull = Cv2.ConvexHull(contorno);

            Cv2.MinEnclosingCircle(contorno, out centroCirculo, out radioCirculo);// pa los escudos

            var moments = Cv2.Moments(contorno);
            if (moments.M00 == 0d)
                centroideContorno = cvBBox.Center;
            else
                centroideContorno = new Point2d((moments.M10 / moments.M00), (moments.M01 / moments.M00));

            moments = Cv2.Moments(convexHull);
            if (moments.M00 == 0d)
                centroideContorno = contorno[0];
            else
                centroideHull = new Point2d((moments.M10 / moments.M00), (moments.M01 / moments.M00));
        }

        public void AgregarArma(Point[] contorno, Point centroFallback)
        {
            var moments = Cv2.Moments(contorno);
            Point2d centroide = moments.M00 == 0 ? centroFallback : new Point2d((moments.M10 / moments.M00), (moments.M01 / moments.M00));

            if (puntosArmas.Count > 0)
            {
                var centralActual = puntosArmas[indiceArmaCentral];
                var dMin = centroideContorno.DistanceTo(centralActual);
                var dNueva = centroideContorno.DistanceTo(centroide);
                if (dNueva < dMin)
                {
                    indiceArmaCentral = puntosArmas.Count;
                }
            }

            puntosArmas.Add(centroide);
        }
    }

    public class TokenDisparador
    {
        public CvRect cvBBox;
        public Rect uvBBox;

        public Point[] contorno;
        public Point2d[] localMaximas;

        public int indiceCentral = 0;
        public Point2d LocalMaximaCentral => localMaximas[indiceCentral];

        public Texture2D resultado;

        public List<TokenEncontrado> tokenConArmasCercanos;

        public TokenDisparador(Point[] contorno, CvRect cvBBox, Mat matBinario)
        {
            this.cvBBox = cvBBox;
            this.contorno = contorno;
            uvBBox = new Rect(cvBBox.Left, matBinario.Height - cvBBox.Bottom, cvBBox.Width, cvBBox.Height);

            cvBBox.X -= 1;
            cvBBox.Y -= 1;
            cvBBox.Width += 2;
            cvBBox.Height += 2;

            using (var matBinarioRoi = new Mat(matBinario, cvBBox))
            using (var matDilate = new Mat())
            // using (var matKernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(5, 5)))
            // using (var matKernel = new Mat())
            {
                Cv2.DistanceTransform(matBinarioRoi, matBinarioRoi, DistanceTypes.C, DistanceMaskSize.Mask3);
                Cv2.ConvertScaleAbs(matBinarioRoi, matBinarioRoi, 60f, 0f);
                Cv2.MinMaxLoc(matBinarioRoi, out double minValue, out double maxValue);
                // Debug.Log($"min value es {minValue} y maxValue es {maxValue}");
                // Cv2.Dilate(matBinarioRoi, matDilate, matKernel);
                // Cv2.Compare(matBinarioRoi, matDilate, matBinarioRoi, CmpTypes.EQ);
                Cv2.Threshold(matBinarioRoi, matDilate, maxValue * .5f, 255d, ThresholdTypes.Binary);

                // resultado = OpenCvSharp.Unity.MatToTexture(matDilate);

                Cv2.FindContours(matDilate, out Point[][] contornos, out HierarchyIndex[] indices, RetrievalModes.External, ContourApproximationModes.ApproxNone);
                localMaximas = new Point2d[contornos.Length];

                // Cv2.FindNonZero(matBinarioRoi, matKernel);
                // localMaximas = new Point[matKernel.Rows];

                double distMinCentral = double.MaxValue;
                for (int i = 0; i < localMaximas.Length; i++)
                {
                    // var subBBox = Cv2.BoundingRect(contornos[i]);
                    // using (var distTransfRoi = new Mat(matBinarioRoi, subBBox))
                    // {
                    //     Cv2.MinMaxLoc(distTransfRoi, out Point minLoc, out Point maxLoc);
                    //     localMaximas[i] = maxLoc + subBBox.TopLeft;
                    // }

                    // localMaximas[i] = matKernel.At<Point>(i);
                    // localMaximas[i] = contornos[i][0];

                    // localMaximas[i] = Cv2.BoundingRect(contornos[i]).Center;

                    var moments = Cv2.Moments(contornos[i]);
                    if (moments.M00 == 0d)
                    {
                        localMaximas[i] = Cv2.BoundingRect(contornos[i]).Center + cvBBox.TopLeft;
                    }
                    else
                    {
                        var centroide = new Point2d((moments.M10 / moments.M00) + cvBBox.TopLeft.X, (moments.M01 / moments.M00) + cvBBox.TopLeft.Y);
                        // localMaximas[i] = centroide;
                        localMaximas[i] = centroide;//new Point(centroide.X, centroide.Y);
                    }

                    var distConCentral = cvBBox.Center.DistanceTo(localMaximas[i]);
                    if (distConCentral < distMinCentral)
                    {
                        distMinCentral = distConCentral;
                        indiceCentral = i;
                    }
                }
            }
        }

        public void AgregarTokensConArmas(List<TokenEncontrado> tokenEncontradosConArmas)
        {
            tokenConArmasCercanos = new List<TokenEncontrado>(tokenEncontradosConArmas);
            tokenConArmasCercanos.Sort((a, b) => a.ArmaCentral.DistanceTo(LocalMaximaCentral).CompareTo(b.ArmaCentral.DistanceTo(LocalMaximaCentral)));
        }

        ~TokenDisparador()
        {
            if (resultado)
                DestroyImmediate(resultado);
        }
    }

    public class Resultados
    {
        public List<TokenEncontrado> tokensPurpura;
        public List<TokenEncontrado> tokensAmarillo;
        public List<TokenEncontrado> todosLosTokens;
        public List<TokenDisparador> tokensDisparadores;
        public List<float> areasReferencia = new();
        public float medianArea = 0f;
    }

    public void Detectar(Texture2D texture2D, out Resultados resultados)
    {
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
                if (contornosP[i].Length <= 2) // una lina sin area ni nada muy complicado.. o un punto osea nada que ver
                    continue;

                var nuevoToken = new TokenEncontrado(contornosP[i], hueInputMat.Height, _tokenTemplates.tokenTemplates, _shapeMatchModes)
                { equipo = EQUIPO_PURPURA };
                resultados.tokensPurpura.Add(nuevoToken);

                if (nuevoToken.TipoTam == TokenTemplates.TipoTam.Referencia)
                    resultados.areasReferencia.Add(nuevoToken.areaRect);
            }

            _blobsAmarillos.FromHueMat(hueInputMat, tipoHue, resultadoBinario, out Point[][] contornosA, out HierarchyIndex[] jerarquiasA);

            resultados.tokensAmarillo = new();

            for (int i = 0; i < contornosA.Length; i++)
            {
                if (contornosA[i].Length <= 2) // una lina sin area ni nada muy complicado.. o un punto osea nada que ver
                    continue;

                var nuevoToken = new TokenEncontrado(contornosA[i], hueInputMat.Height, _tokenTemplates.tokenTemplates, _shapeMatchModes)
                { equipo = EQUIPO_AMARILLO };
                resultados.tokensAmarillo.Add(nuevoToken);

                if (nuevoToken.TipoTam == TokenTemplates.TipoTam.Referencia)
                    resultados.areasReferencia.Add(nuevoToken.areaRect);
            }

            if (resultados.areasReferencia.Count > 0)
            {
                resultados.areasReferencia.Sort();
                resultados.medianArea = resultados.areasReferencia[resultados.areasReferencia.Count / 2];
            }

            _blobsFuxia.FromHueMat(hueInputMat, tipoHue, resultadoBinario, out Point[][] contornosF, out HierarchyIndex[] jerarquias2, resultadoBinario);

            resultados.todosLosTokens = new List<TokenEncontrado>();
            resultados.todosLosTokens.AddRange(resultados.tokensPurpura);
            resultados.todosLosTokens.AddRange(resultados.tokensAmarillo);

            foreach (var token in resultados.todosLosTokens)
            {
                if (token.areaRect < resultados.medianArea)
                {
                    token.comparacionesOrdenadas.RemoveAll(comp => comp.token.tipoTam == TokenTemplates.TipoTam.Mayor);
                }
            }

            resultados.tokensDisparadores = new();

            for (int i = 0; i < contornosF.Length; i++)
            {
                var cvBBox = Cv2.BoundingRect(contornosF[i]);

                // si esta adentro de algun contorno, el valor es positivo y esta dentro de otro token asique lo ignoramos
                // en realidad, se lo tendriamos que agregar al token tal vez
                if (resultados.todosLosTokens.Find(test => Cv2.PointPolygonTest(test.contorno, cvBBox.Center, false) > 0d) is TokenEncontrado token)
                {
                    token.AgregarArma(contornosF[i], cvBBox.Center);
                    continue;
                }

                var nuevoTokenDisparo = new TokenDisparador(contornosF[i], cvBBox, resultadoBinario);
                resultados.tokensDisparadores.Add(nuevoTokenDisparo);
            }

            var navesConArmas = new List<TokenEncontrado>(resultados.todosLosTokens);
            navesConArmas.RemoveAll(el => el.puntosArmas.Count == 0);
            foreach (var tokenDisparo in resultados.tokensDisparadores)
            {
                tokenDisparo.AgregarTokensConArmas(navesConArmas);
            }

            // Cv2.DistanceTransform(resultadoBinario, resultadoBinario, DistanceTypes.L2, DistanceMaskSize.Precise);
            // Cv2.ConvertScaleAbs(resultadoBinario, resultadoBinario, 60f, 0f);
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(TokenDetector))]
    public class TokenDetectorEditor : Editor
    {
        RenderTexture _renderTexture;
        private Texture2D _texturaInput;
        Texture2D _testResultado;

        private Material material;
        TokenDetector detector;
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
            DrawDefaultInspector();

            if (GUILayout.Button("Detectar"))
            {
                if (_texturaInput != null)
                {
                    detector.Detectar(_texturaInput, out resultados);
                }
            }

            var textureSize = _texturaInput ? _texturaInput.texelSize : Vector2.one;

            using (var escroll = new EditorGUILayout.ScrollViewScope(scroll))
            {
                scroll = escroll.scrollPosition;
                if (detector._tokenTemplates != null)
                {
                    if (resultados != null)
                        EditorGUILayout.FloatField("Median Area Refe", resultados.medianArea);

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
                                    if (encontrado.TemplateMasPosible == template)
                                    {
                                        TokenEntcontradoGUI(encontrado, textureSize);
                                    }
                                }
                            }

                            if (resultados?.tokensAmarillo != null)
                            {
                                foreach (var encontrado in resultados.tokensAmarillo)
                                {
                                    if (encontrado.TemplateMasPosible == template)
                                    {
                                        TokenEntcontradoGUI(encontrado, textureSize);
                                    }
                                }
                            }

                        }
                    }
                }

                if (resultados?.tokensDisparadores != null)
                {
                    foreach (var disparadores in resultados.tokensDisparadores)
                    {
                        TokenDisparadorGUI(disparadores, textureSize);
                    }
                }
            }
        }

        private void TokenEntcontradoGUI(TokenEncontrado encontrado, Vector2 textureSize)
        {
            var bboxFound = encontrado.uvBBox;
            var guirect = GUILayoutUtility.GetRect(bboxFound.width, bboxFound.height, GUILayout.ExpandWidth(false));
            if (_texturaInput)
                GUI.DrawTextureWithTexCoords(guirect, _texturaInput, new Rect(bboxFound.position * textureSize, bboxFound.size * textureSize));
            DibujarContorno(guirect, -encontrado.cvBBox.TopLeft.X, -encontrado.cvBBox.TopLeft.Y + 1, encontrado.contorno, Color.black);
            DibujarContorno(guirect, -encontrado.cvBBox.TopLeft.X, -encontrado.cvBBox.TopLeft.Y + 1, encontrado.convexHull, Color.yellow);

            var pA = (encontrado.centroideHull - encontrado.centroideContorno) * 5 + encontrado.centroideContorno;
            var pB = (encontrado.centroideHull - encontrado.centroideContorno) * 10 + encontrado.centroideContorno;
            DibujarLinea(guirect, -encontrado.cvBBox.TopLeft.X + (float)pA.X, -encontrado.cvBBox.TopLeft.Y + (float)pA.Y, -encontrado.cvBBox.TopLeft.X + (float)pB.X, -encontrado.cvBBox.TopLeft.Y + (float)pB.Y, Color.cyan);

            foreach (var p in encontrado.puntosArmas)
            {
                DibujarCirculo(guirect, (float)p.X - encontrado.cvBBox.TopLeft.X, (float)p.Y + 1 - encontrado.cvBBox.TopLeft.Y, .5f, Color.cyan);
                if (p == encontrado.puntosArmas[encontrado.indiceArmaCentral])
                    DibujarCirculo(guirect, (float)p.X - encontrado.cvBBox.TopLeft.X, (float)p.Y + 1 - encontrado.cvBBox.TopLeft.Y, 1.2f, Color.cyan);
            }

            var tooltip = $"Area:{encontrado.areaRect}\nArmas: {encontrado.puntosArmas.Count}\n";
            foreach (var arma in encontrado.puntosArmas)
            {
                tooltip += $"({arma.X},{arma.Y})\n";
            }


            GUI.Label(guirect, new GUIContent(string.Empty, tooltip));
        }

        private void TokenDisparadorGUI(TokenDisparador disparador, Vector2 textureSize)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                var bboxFound = disparador.uvBBox;
                var guirect = GUILayoutUtility.GetRect(bboxFound.width * 3, bboxFound.height * 3, GUILayout.ExpandWidth(false));
                if (_texturaInput)
                    GUI.DrawTextureWithTexCoords(guirect, _texturaInput, new Rect(bboxFound.position * textureSize, bboxFound.size * textureSize));

                foreach (var p in disparador.localMaximas)
                {
                    DibujarCirculo(guirect, (float)(p.X - disparador.cvBBox.TopLeft.X) * 3, (float)(p.Y - disparador.cvBBox.TopLeft.Y) * 3, .5f, Color.cyan);
                    if (p == disparador.localMaximas[disparador.indiceCentral])
                        DibujarCirculo(guirect, (float)(p.X - disparador.cvBBox.TopLeft.X) * 3, (float)(p.Y - disparador.cvBBox.TopLeft.Y) * 3, 1.2f, Color.cyan);
                }

                if (disparador.resultado)
                {
                    guirect = GUILayoutUtility.GetRect(disparador.resultado.width * 3, disparador.resultado.height * 3, GUILayout.ExpandWidth(false));
                    GUI.DrawTexture(guirect, disparador.resultado);
                    foreach (var p in disparador.localMaximas)
                    {
                        DibujarCirculo(guirect, (float)p.X * 3, (float)p.Y * 3, .5f, Color.cyan);
                        if (p == disparador.localMaximas[disparador.indiceCentral])
                            DibujarCirculo(guirect, (float)p.X * 3, (float)p.Y * 3, 1.2f, Color.cyan);
                    }
                }

                if (disparador.tokenConArmasCercanos.Count > 0)
                {
                    var tc = disparador.tokenConArmasCercanos[0];
                    TokenEntcontradoGUI(tc, textureSize);
                }

                GUILayout.Label($"Cant Maximas: {disparador.localMaximas.Length}", GUILayout.ExpandWidth(false));
                GUILayout.Label($"Cant Cercanos: {disparador.tokenConArmasCercanos.Count}", GUILayout.ExpandWidth(false));
            }
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
