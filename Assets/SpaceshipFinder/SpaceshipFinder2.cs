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
public class SpaceshipFinder2 : ScriptableObject
{
    //public Color _colPurpura = Color.HSVToRGB(250,100,42);//new Color(0.06666667f,0f,0.3960785f);
    //public Color _colAmarillo = Color.HSVToRGB(41,100,64);//new Color(0.6352941f,0.4313726f,0f);
    public enum TipoColor
    {
        HSV, HLS
    }

    [SerializeField]
    Texture2D _inputTexture;

    [SerializeField]
    TipoColor _tipoColor = TipoColor.HSV;

    [SerializeField, Range(0, 255)]
    int _hue = 130;
    [SerializeField, Range(1, 60)]
    int _hueTolerance = 12;
    [SerializeField, MinMaxSlider(0, 255)]
    Vector2Int _saturacionValida = new Vector2Int(0, 255);
    [SerializeField, MinMaxSlider(0, 255)]
    Vector2Int _brilloValido = new Vector2Int(0, 255);

    [Space]
    [SerializeField, MinMaxSlider(1, 120)]
    Vector2Int _labelBBoxSideLimits = new Vector2Int(1, 120);

    // [SerializeField]
    // ThresholdTypes _saturationThreshType = ThresholdTypes.Binary;

    [SerializeField]
    int dilateCount = 2;
    [SerializeField]
    int erodeCount = 2;
    [SerializeField]
    double approxPolyEpsilon = 0.02;

    [Space]
    [SerializeField]
    private RetrievalModes _retrievalModes = RetrievalModes.External;
    [SerializeField]
    private ContourApproximationModes _contourApproximationModes = ContourApproximationModes.ApproxTC89KCOS;

    // [SerializeField, Range(1, 255)]
    // float _saturationThreshold = 127f;
    Point[][] _contornos;

    public struct Resultados
    {
        public System.Action<Mat> resultadoPrimerFiltroConExtras;
        public System.Action<Point[][]> resultadoContornos;
    }

    public void ProcesarTextura(Texture2D inputTex2D, Resultados resultados)
    {
        using (Mat inputMat = OpenCvSharp.Unity.TextureToMat(inputTex2D))
        {
            ProcesarTextura(inputMat, resultados);
        }
    }

    public void ProcesarTextura(Mat inputMat, Resultados resultados)
    {
        // using (Mat emptyMat = new Mat())
        using (Mat inputConverted = new Mat())
        using (Mat tempOutput = new Mat(inputMat.Rows, inputMat.Cols, inputMat.Type()))
        {
            Cv2.CvtColor(inputMat, inputConverted, _tipoColor == TipoColor.HSV ? ColorConversionCodes.BGR2HSV : ColorConversionCodes.BGR2HLS);

            int hueMin = _hue - _hueTolerance;
            int hueMax = _hue + _hueTolerance;

            // cuando HSV: es hue, saturacion y valor (brillo)
            // cuando HLS: es hue, brillo y saturacion
            var segundoComponente = _tipoColor == TipoColor.HSV ? _saturacionValida : _brilloValido;
            var tercerComponente = _tipoColor == TipoColor.HLS ? _saturacionValida : _brilloValido;

            var scalarMinimo = new Scalar(hueMin, segundoComponente[0], tercerComponente[0]);
            var scalarMaximo = new Scalar(hueMax, segundoComponente[1], tercerComponente[1]);
            using (Mat primerFiltro = new Mat())
            {
                Cv2.InRange(inputConverted, scalarMinimo, scalarMaximo, primerFiltro);

                if (erodeCount > 0)
                {
                    using var kernel = new Mat();
                    Cv2.Erode(primerFiltro, primerFiltro, kernel, null, erodeCount);
                }

                using (Mat clonePrimerFiltro = primerFiltro.Clone())
                {
                    Cv2.FindContours(clonePrimerFiltro, out _contornos, out HierarchyIndex[] jerarquia, _retrievalModes, _contourApproximationModes);
                }

                Cv2.DrawContours(primerFiltro, _contornos, -1, Scalar.White, -1);

                tempOutput.SetTo(Scalar.DarkSlateGray);

                inputMat.CopyTo(tempOutput, primerFiltro);

                using (Mat labelCentroids = new Mat())
                using (Mat labelData = new Mat())
                {
                    Cv2.ConnectedComponentsWithStats(primerFiltro, primerFiltro, labelData, labelCentroids, PixelConnectivity.Connectivity8);
                    List<CvRect> labelsBBox = new();

                    primerFiltro.ConvertTo(primerFiltro, MatType.CV_8UC1);

                    for (int i = 0, count = labelData.Rows; i < count; i++)
                    {
                        int left = labelData.Get<int>(i, (int)ConnectedComponentsTypes.Left) - 1;
                        int top = labelData.Get<int>(i, (int)ConnectedComponentsTypes.Top) - 1;
                        int w = labelData.Get<int>(i, (int)ConnectedComponentsTypes.Width) + 2;
                        int h = labelData.Get<int>(i, (int)ConnectedComponentsTypes.Height) + 2;



                        CvRect bboxRect = new CvRect(left, top, w, h);

                        if (left < 0 || top < 0 || left + w >= inputMat.Width || top + h >= inputMat.Height)
                            continue;

                        if (w > _labelBBoxSideLimits[0] && w < _labelBBoxSideLimits[1] && h > _labelBBoxSideLimits[0] && h < _labelBBoxSideLimits[1])
                        {
                            labelsBBox.Add(bboxRect);
                            // Cv2.Rectangle(tempOutput, bboxRect, Scalar.IndianRed, 2);

                            using (Mat distTransformConRoi = new Mat())
                            using (Mat outputConRoi = new Mat(tempOutput, bboxRect))
                            using (Mat primerFiltroConRoi = new Mat(primerFiltro, bboxRect))
                            {
                                //probar hacer adaptive threshold aca en vez de distance trasnform
                                Cv2.DistanceTransform(primerFiltroConRoi, distTransformConRoi, DistanceTypes.L2, DistanceMaskSize.Mask5);
                                Cv2.ConvertScaleAbs(distTransformConRoi, distTransformConRoi, 20f, 0f);

                                /*Cv2.CvtColor(distTransformConRoi, distTransformConRoi, ColorConversionCodes.GRAY2BGR);
                                Cv2.Multiply(distTransformConRoi, outputConRoi, outputConRoi);
                                distTransformConRoi.CopyTo(outputConRoi);*/

                                //ver de no tapar el dibujo sino crear mini texturas con cada roi
                            }
                            // break;
                        }
                    }

                    //Cv2.DrawContours(tempOutput, _contornos, -1, Scalar.AliceBlue);
                    foreach (var cont in _contornos)
                    {
                        if (cont.Length < 5) continue;

                        var line = Cv2.FitLine(cont, DistanceTypes.L12, 0d, 0.01d, 0.01d);
                        var ellipse = Cv2.FitEllipse(cont);
                        // ellipse.Size = new Size2f(ellipse.Size.Width *.5f,ellipse.Size.Height*.5f);
                        // Cv2.Ellipse(tempOutput, ellipse, Scalar.Violet);
                        // var pts = ellipse.Points();
                        // Cv2.Line( tempOutput,(pts[1]+pts[2])*0.5f, (pts[3]+pts[0])*0.5f,Scalar.PaleVioletRed );
                        var dir = Quaternion.Euler(0, 0, ellipse.Angle) * Vector2.up * ellipse.Size.Width;
                        var dircv = new Point2f(dir.x, dir.y);


                        var moments = Cv2.Moments(cont);
                        // var centroidX = moments.M10 / moments.M00;
                        // var centroidY = moments.M01 / moments.M00;
                        var centroid = new Point2f((float)(moments.M10 / moments.M00), (float)(moments.M01 / moments.M00));

                        var pA = ellipse.Center + dircv;
                        var pB = ellipse.Center - dircv;
                        if ( centroid.DistanceTo(ellipse.Center + dircv) < centroid.DistanceTo(ellipse.Center - dircv))
                        Cv2.Line(tempOutput, ellipse.Center + dircv, ellipse.Center, Scalar.DarkRed);
                        else
                        Cv2.Line(tempOutput, ellipse.Center - dircv, ellipse.Center, Scalar.DarkRed);

                        Cv2.MinEnclosingCircle(cont,out Point2f centro, out float radio);
                        Cv2.Circle(tempOutput, centro, (int)radio, Scalar.Azure);

                        // Cv2.Line(tempOutput,
                        //     (int)(line.X1 - line.Vx * 10000), (int)(line.Y1 - line.Vy * 10000),
                        //     (int)(line.X1 + line.Vx * 10000), (int)(line.Y1 + line.Vy * 10000), Scalar.Purple);

                        var contred = cont;
                        contred = Cv2.ConvexHull(contred);
                        contred = Cv2.ApproxPolyDP(contred, approxPolyEpsilon, true);

                        Point sharpest = contred[0];
                        float sharpness = 360f;
                        for (int i = 0; i < contred.Length; i++)
                        {
                            var prev = contred[(i + contred.Length - 1) % contred.Length] - contred[i];
                            var next = contred[(i + 1) % contred.Length] - contred[i];
                            // var este = contred[i];
                            // var ang = Vector2.Angle( new Vector2(prev.X-este.X, prev.Y-este.Y), new Vector2(next.X-este.X, next.Y-este.Y) );
                            var ang = Vector2.Angle(new Vector2(prev.X, prev.Y), new Vector2(next.X, next.Y));
                            if (ang < sharpness)
                            {
                                sharpness = ang;
                                sharpest = contred[i];
                            }
                        }

                        // Cv2.Circle(tempOutput, sharpest, 6, Scalar.AliceBlue);

                        // Cv2.Polylines(tempOutput, new Point[][]{contred}, isClosed:true, Scalar.AliceBlue);
                    }

                    resultados.resultadoPrimerFiltroConExtras?.Invoke(tempOutput);
                    resultados.resultadoContornos?.Invoke(_contornos);
                }
            }
        }
    }

    public class MinMaxSlider : PropertyAttribute
    {
        public readonly float min;
        public readonly float max;
        public MinMaxSlider(float min, float max)
        {
            this.min = min;
            this.max = max;
        }
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(MinMaxSlider))]
    private class MinMaxSliderDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.Vector2 && property.propertyType != SerializedPropertyType.Vector2Int)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            var valActual = property.propertyType == SerializedPropertyType.Vector2 ? property.vector2Value : property.vector2IntValue;
            float minActual = valActual[0];
            float maxActual = valActual[1];

            var minMaxSliderAtt = (MinMaxSlider)attribute;
            using (new EditorGUI.PropertyScope(position, label, property))
            {
                using (var change = new EditorGUI.ChangeCheckScope())
                {
                    EditorGUI.MinMaxSlider(position, label, ref minActual, ref maxActual, minMaxSliderAtt.min, minMaxSliderAtt.max);
                    if (change.changed)
                    {
                        if (property.propertyType == SerializedPropertyType.Vector2)
                            property.vector2Value = new Vector2(minActual, maxActual);
                        else //if (property.propertyType != SerializedPropertyType.Vector2Int)
                            property.vector2IntValue = new Vector2Int((int)minActual, (int)maxActual);
                    }
                }
            }
        }
    }

    [CustomEditor(typeof(SpaceshipFinder2))]
    public class SpaceshipFinderEditor : Editor
    {
        Texture2D texturaInput;
        Texture2D texPrimerFiltro;
        SpaceshipFinder2 _finderTarget;

        private void OnEnable()
        {
            _finderTarget = (SpaceshipFinder2)target;

            if (!texturaInput)
                texturaInput = _finderTarget._inputTexture;
        }

        private void OnDisable()
        {
            AssetDatabase.SaveAssets();

            if (texPrimerFiltro != null)
                DestroyImmediate(texPrimerFiltro);
            texPrimerFiltro = null;
        }

        private void OnPrimerResultado(Mat mat)
        {
            texPrimerFiltro = OpenCvSharp.Unity.MatToTexture(mat, texPrimerFiltro);
            texPrimerFiltro.name = "Primer Filtro";
        }

        public override void OnInspectorGUI()
        {
            using (var changed = new EditorGUI.ChangeCheckScope())
            {
                texturaInput = EditorGUILayout.ObjectField("Input", texturaInput, typeof(Texture2D), allowSceneObjects: false) as Texture2D;
                EditorGUILayout.ObjectField("Resultado", texPrimerFiltro, typeof(Texture2D), allowSceneObjects: false);

                DrawDefaultInspector();

                if (changed.changed)
                {
                    if (texturaInput != null)
                        _finderTarget.ProcesarTextura(texturaInput, new Resultados() { resultadoPrimerFiltroConExtras = OnPrimerResultado });
                }
            }
        }
    }
#endif
}
