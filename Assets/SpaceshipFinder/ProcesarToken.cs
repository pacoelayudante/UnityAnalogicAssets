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
public class ProcesarToken : ScriptableObject
{
    public enum Modo
    {
        ConvexHull,
        CentroDeMasa
    }

    [SerializeField]
    private double _approxPolyEpsilon = 2.6f;

    public static void Procesar(Modo modo, Point[] contorno, float approxPolyEpsilon, out Vector2 centroide, out Vector2 puntaRelativaCentroide)
    {
        if (modo == Modo.CentroDeMasa)
            ProcesarModoCentroDeMasa(contorno, out centroide, out puntaRelativaCentroide);
        else
            ProcesarModoConvexHull(contorno, approxPolyEpsilon, out centroide, out puntaRelativaCentroide);
    }

    public static void ProcesarModoCentroDeMasa(Point[] contorno, out Vector2 centroide, out Vector2 puntaRelativaCentroide)
    {
        var moments = Cv2.Moments(contorno);
        centroide = new Vector2((float)(moments.M10 / moments.M00), (float)(moments.M01 / moments.M00));

        var ellipse = Cv2.FitEllipse(contorno);

        Vector2 dir = Quaternion.Euler(0, 0, ellipse.Angle) * Vector2.up * ellipse.Size.Width;
        // var dircv = new Point2f(dir.x, dir.y);

        var centroElipse = new Vector2(ellipse.Center.X, ellipse.Center.Y) - centroide;

        var pA = centroElipse + dir;
        var pB = centroElipse - dir;
        puntaRelativaCentroide = pA.sqrMagnitude < pB.sqrMagnitude ? pA : pB;
    }

    public static void ProcesarModoConvexHull(Point[] contorno, float approxPolyEpsilon, out Vector2 centroide, out Vector2 puntaRelativaCentroide)
    {
        var moments = Cv2.Moments(contorno);
        centroide = new Vector2((float)(moments.M10 / moments.M00), (float)(moments.M01 / moments.M00));

        var contReducido = Cv2.ConvexHull(contorno);
        contReducido = Cv2.ApproxPolyDP(contReducido, approxPolyEpsilon, closed: false);

        Point sharpest = contReducido[0];
        float sharpness = 360f;
        for (int i = 0; i < contReducido.Length; i++)
        {
            var prev = contReducido[(i + contReducido.Length - 1) % contReducido.Length] - contReducido[i];
            var next = contReducido[(i + 1) % contReducido.Length] - contReducido[i];

            var ang = Vector2.Angle(new Vector2(prev.X, prev.Y), new Vector2(next.X, next.Y));
            if (ang < sharpness)
            {
                sharpness = ang;
                sharpest = contReducido[i];
            }
        }

        // angulo = Vector2.SignedAngle(Vector2.right, new Vector2(sharpest.X-centroide.x,sharpest.Y-centroide.y));
        puntaRelativaCentroide = new Vector2(sharpest.X, sharpest.Y) - centroide;
    }

}
