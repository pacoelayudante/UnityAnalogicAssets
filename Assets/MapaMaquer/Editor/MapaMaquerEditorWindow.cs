using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using OpenCvSharp;

public class MapaMaquerEditorWindow : FiltroGenericoWindow
{
    [SerializeField]
    private int _maxImageSize = 256;
    [SerializeField]
    private float _minSegmentLength = 50f;
    [SerializeField]
    private LineSegmentDetectorModes _mode = LineSegmentDetectorModes.RefineNone;
    [SerializeField]
    private HomographyMethods _homographyMethod = HomographyMethods.Ransac;
    [SerializeField]
    private double _ransacReprojThreshold = 5d;

    public Texture2D inputTexture;
    public Texture2D outputTexture;
    public Texture2D warpedOutput;
    private Vector2 _scrollPos;

    [MenuItem("OpenCV/Mapa Maquer")]
    private static void Open() => GetWindow<MapaMaquerEditorWindow>();

    public void Procesar()
    {
        using var inputMat = OpenCvSharp.Unity.TextureToMat(inputTexture);
        using var inputGray = new Mat();
        Cv2.CvtColor(inputMat, inputGray, ColorConversionCodes.BGR2GRAY);

        float escalaInversa = 1f;
        float width = inputMat.Width;
        float height = inputMat.Height;
        if (_maxImageSize > 0 && (width > _maxImageSize || height > _maxImageSize))
        {
            escalaInversa = Mathf.Max(width, height) / _maxImageSize;
            float escala = 1f / escalaInversa;
            Cv2.Resize(inputGray, inputGray, Size.Zero, escala, escala);
            // Debug.Log($"{escalaInversa}=>{escala}=>{inputGray.Width}");
            width = inputGray.Width;//Mathf.FloorToInt(width / escala);
            height = inputGray.Height;//Mathf.FloorToInt(height / escala);
        }

        using var detector = LineSegmentDetector.Create(_mode);
        detector.Detect(inputGray, out Vec4f[] lines, out double[] widths, out double[] prec, out double[] nfa);

        // 0 is top, height is bottom
        Vec4f lineBottom = new Vec4f(0, height, width, height),
            lineLeft = new Vec4f(0, 0, 0, height),
            lineTop = new Vec4f(0, 0, width, 0),
            lineRight = new Vec4f(width, 0, width, height);
        float middleW = width / 2f;
        float middleH = height / 2f;
        foreach (var line in lines)
        {
            Vector2 pA = new Vector2(line[0], line[1]);
            Vector2 pB = new Vector2(line[2], line[3]);

            if (Vector2.Distance(pA, pB) > _minSegmentLength)
            {
                if ((pA.x > middleW) ^ (pB.x > middleW))
                {// horizontal
                    if ((pA.y > middleH) && (pB.y > middleH))
                    {// arriba
                        if (pA.y < lineBottom[1])
                            lineBottom = line;
                    }

                    if ((pA.y < middleH) && (pB.y < middleH))
                    {// abajo
                        if (pA.y > lineTop[1])
                            lineTop = line;
                    }
                }

                if ((pA.y > middleH) ^ (pB.y > middleH))
                {//vertical
                    if ((pA.x > middleW) && (pB.x > middleW))
                    {// der
                        if (pA.x < lineRight[0])
                            lineRight = line;
                    }

                    if ((pA.x < middleW) && (pB.x < middleW))
                    {// izq
                        if (pA.x > lineLeft[0])
                            lineLeft = line;
                    }
                }
            }
        }

        Vector2 topLeft = Vector2.zero;
        if (LineIntersection(lineTop, lineLeft, ref topLeft))
        {
            // lineTop[2] = lineLeft[0] = topLeft.x;
            // lineTop[3] = lineLeft[1] = topLeft.y;
        }
        Vector2 topRight = Vector2.zero;
        if (LineIntersection(lineTop, lineRight, ref topRight))
        {
            // lineTop[0] = lineRight[0] = topRight.x;
            // lineTop[1] = lineRight[1] = topRight.y;
        }
        Vector2 bottomLeft = Vector2.zero;
        if (LineIntersection(lineBottom, lineLeft, ref bottomLeft))
        {
            // lineBottom[0] = lineLeft[2] = topLeft.x;
            // lineBottom[1] = lineLeft[3] = topLeft.y;
        }
        Vector2 bottomRight = Vector2.zero;
        if (LineIntersection(lineBottom, lineRight, ref bottomRight))
        {
            // lineBottom[2] = lineRight[2] = topLeft.x;
            // lineBottom[3] = lineRight[3] = topLeft.y;
        }

        Cv2.CvtColor(inputGray, inputGray, ColorConversionCodes.GRAY2BGR);
        Cv2.Line(inputGray, (int)topLeft.x, (int)topLeft.y, (int)topRight.x, (int)topRight.y, Scalar.Blue, 2);
        Cv2.Line(inputGray, (int)bottomLeft.x, (int)bottomLeft.y, (int)bottomRight.x, (int)bottomRight.y, Scalar.Orange, 2);
        Cv2.Line(inputGray, (int)topRight.x, (int)topRight.y, (int)bottomRight.x, (int)bottomRight.y, Scalar.SkyBlue, 2);
        Cv2.Line(inputGray, (int)bottomLeft.x, (int)bottomLeft.y, (int)topLeft.x, (int)topLeft.y, Scalar.OrangeRed, 2);
        outputTexture = OpenCvSharp.Unity.MatToTexture(inputGray);

        // volvemos a tamaño original de imagen
        topLeft *= escalaInversa;
        topRight *= escalaInversa;
        bottomLeft *= escalaInversa;
        bottomRight *= escalaInversa;
        var maxWidth = Mathf.Max(Vector2.Distance(topLeft, topRight), Vector2.Distance(bottomLeft, bottomRight));
        var maxHeight = Mathf.Max(Vector2.Distance(topLeft, bottomLeft), Vector2.Distance(topRight, bottomRight));
        var srcPoints = new[]
        {
            new Point2d(topLeft.x, topLeft.y),
            new Point2d(topRight.x, topRight.y),
            new Point2d(bottomRight.x, bottomRight.y),
            new Point2d(bottomLeft.x, bottomLeft.y),
        };
        var dstPoints = new[]
        {
            new Point2d(0, 0),
            new Point2d(maxWidth, 0),
            new Point2d(maxWidth, maxHeight),
            new Point2d(0, maxHeight),
        };
        using var homography = Cv2.FindHomography(srcPoints, dstPoints, _homographyMethod, _ransacReprojThreshold);
        using var warpedImg = new Mat();
        Cv2.WarpPerspective(inputMat, warpedImg, homography, new Size(maxWidth, maxHeight));

        warpedOutput = OpenCvSharp.Unity.MatToTexture(warpedImg);
    }

    // Infinite Line Intersection (line1 is p1-p2 and line2 is p3-p4)
    internal static bool LineIntersection(Vec4f l1, Vec4f l2, ref Vector2 result)
    {
        float bx = l1[2] - l1[0];
        float by = l1[3] - l1[1];
        float dx = l2[2] - l2[0];
        float dy = l2[3] - l2[1];
        float bDotDPerp = bx * dy - by * dx;
        if (bDotDPerp == 0)
        {
            return false;
        }
        float cx = l2[0] - l1[0];
        float cy = l2[1] - l1[1];
        float t = (cx * dy - cy * dx) / bDotDPerp;

        result.x = l1[0] + t * bx;
        result.y = l1[1] + t * by;
        return true;
    }

    private void OnGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Procesar"))
        {
            Procesar();
        }

        using (var scroll = new EditorGUILayout.ScrollViewScope(_scrollPos))
        {
            _scrollPos = scroll.scrollPosition;
            if (outputTexture != null)
            {
                var rect = GUILayoutUtility.GetAspectRect(outputTexture.width / (float)outputTexture.height);
                EditorGUI.DrawPreviewTexture(rect, outputTexture, null, ScaleMode.ScaleToFit);
            }

            if (warpedOutput != null)
            {
                var rect = GUILayoutUtility.GetAspectRect(warpedOutput.width / (float)warpedOutput.height);
                EditorGUI.DrawPreviewTexture(rect, warpedOutput, null, ScaleMode.ScaleToFit);
            }
        }
    }
}
