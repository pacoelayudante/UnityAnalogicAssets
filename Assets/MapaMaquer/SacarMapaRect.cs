using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCvSharp;

[CreateAssetMenu]
public class SacarMapaRect : ScriptableObject
{
    [SerializeField]
    private int _maxImageSize = 256;
    [SerializeField]
    private float _minSegmentLength = 50f;
    [SerializeField]
    private LineSegmentDetectorModes _mode = LineSegmentDetectorModes.RefineNone;

    private static Mat ExtraerMapaRectInicial(Texture2D inputTexture, SacarMapaRect settings)
    {
        using var inputMat = OpenCvSharp.Unity.TextureToMat(inputTexture);
        return ExtraerMapaRectInicial(inputMat, settings);
    }

    private static Mat ExtraerMapaRectInicial(Mat inputMat, SacarMapaRect settings)
    {
        using var inputGray = new Mat();
        float escala = 1f;
        float width = inputMat.Cols;
        float height = inputMat.Rows;
        if (width > settings._maxImageSize || height > settings._maxImageSize)
        {
            escala = Mathf.Max(width, height) / settings._maxImageSize;
            float newWidth = Mathf.FloorToInt(width * escala);
            float newHeight = Mathf.FloorToInt(height * escala);
            inputGray.Resize(Size.Zero, escala, escala);
        }

        Cv2.CvtColor(inputMat, inputGray, ColorConversionCodes.BGR2GRAY);

        using var detector = LineSegmentDetector.Create(settings._mode);
        detector.Detect(inputGray, out Vec4f[] lines, out double[] widths, out double[] prec, out double[] nfa);
        
        Vec4f lineTop, lineLeft, lineBottom, lineRight;
        float middleW = width / 2f;
        float middleH = height / 2f;
        foreach (var line in lines)
        {
            Vector2 pA = new Vector2(line[0], line[1]);
            Vector2 pB = new Vector2(line[2], line[3]);

            if (Vector2.Distance(pA, pB) > settings._minSegmentLength)
            {
                if ((pA.x > middleW) ^ (pB.x > middleW))
                {// horizontal

                }

                // if ((pA.y > middleH) ^ (pB.y > middleH))
            }
        }


        return null;
    }
}
