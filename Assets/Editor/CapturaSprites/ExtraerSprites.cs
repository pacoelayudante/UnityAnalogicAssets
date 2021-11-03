using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCvSharp;
using OCVUnity = OpenCvSharp.Unity;
using System.Linq;
using Mathd = System.Math;
using Guazu.DrawersCopados;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ExtraerSprites : ScriptableObject
{
    public ProcesarRecuadros procesarRecuadros;
    public bool conservarOriginal = true;
    [Range(0, 3)]
    public int ajustarRotacion = 0;
    [Min(1)]
    public int maxTamLadoImagenProcesada = 512;
    public FiltroAdaptativo filtroAdaptativo = new FiltroAdaptativo() { blockSize = 11, C = 4, thresholdType = ThresholdTypes.BinaryInv };
    [Min(0)]
    public int margenCorrector = 0;
    [SoloImpares]
    public int tamKernelDilate = 3;
    public int repeatDilate = 1;
    [SoloImpares]
    public int tamMedianBlur = 7;
    public FiltroContornos filtroContornos = new FiltroContornos() { mode = RetrievalModes.External, method = ContourApproximationModes.ApproxTC89KCOS };
    public float tamMinimoSprite = 0.045f;

    public bool equalizarHistograma = false;
    public bool equalizarHistogramaDeSat = false;

    public enum OtsuAlgo {
        Nop, OtsuSaturacionYBrillo, OtsuFiltroDameColores, OtsuFiltroDameColoresYBlanco
    }
    public OtsuAlgo otsu = OtsuAlgo.Nop;

    [Range(0, 256f)]
    public float ajusteTresh = 0f;


    public List<Texture2D> texturasResultantes = new List<Texture2D>();
    public List<Sprite> spriteResultantes = new List<Sprite>();

    List<Contorno> contornos;
    public Mat matRecuadro { get; private set; }
    // List<Contorno> contornosDeSprites;
    // public List<Contorno> ContornosDeSprites => contornosDeSprites;

    public void Extraer()
    {
        Extraer(procesarRecuadros);
    }
    public void Extraer(ProcesarRecuadros procesadorRecuadros, int[] indicesRecuadros = null)
    {
        // indicesRecuadros == 0 >> PROCESAR TODOS
        if (procesadorRecuadros == null) return;

        var recuadros = procesadorRecuadros.Recuadros;
        if (recuadros == null || recuadros.Count == 0) return;
        Extraer(procesarRecuadros.MatOriginal, recuadros[0]);
    }
    public void Extraer(Mat matOriginal, Recuadro recuadro)
    {
        if (recuadro.matRecuadroNormalizado == null)
        {
            recuadro.Normalizar(matOriginal, 0);
        }
        var matRecuadro = recuadro.matRecuadroNormalizado.Clone();
        var escalaSalida = maxTamLadoImagenProcesada / Mathd.Max((double)matRecuadro.Width, (double)matRecuadro.Height);
        if (escalaSalida < 1)
        {
            Cv2.Resize(matRecuadro, matRecuadro, new Size(), escalaSalida, escalaSalida);
        }
        else if (escalaSalida > 1) escalaSalida = 1;
        if (conservarOriginal) matRecuadro = matRecuadro.Clone();
        Cv2.CvtColor(matRecuadro, matRecuadro, ColorConversionCodes.BGR2GRAY);
        matRecuadro = filtroAdaptativo.Procesar(matRecuadro);
        if (margenCorrector > 0) Cv2.Rectangle(matRecuadro, new Point(0, 0), new Point(matRecuadro.Width, matRecuadro.Height), Scalar.Black, margenCorrector);

        if (repeatDilate > 0)
        {
            var kernelDilate = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(tamKernelDilate, tamKernelDilate));
            Cv2.Dilate(matRecuadro, matRecuadro, kernelDilate, null, repeatDilate);
        }
        if (tamMedianBlur > 1) Cv2.MedianBlur(matRecuadro, matRecuadro, tamMedianBlur);

        var contornosSinProcesar = filtroContornos.Procesar(matRecuadro);
        texturasResultantes.Clear();
        spriteResultantes.Clear();
        contornos = new List<Contorno>();

        var tamRecuadro = recuadro.matRecuadroNormalizado.Size();
        var tamMinimo = tamMinimoSprite * Mathf.Min(tamRecuadro.Width, tamRecuadro.Height);
        for (int i = 0; i < contornosSinProcesar.Length; i++)
        {
            var contniu = new Contorno(i, contornosSinProcesar);
            contniu.Escalar(1d / escalaSalida);
            var bbox = contniu.BoundingRect;
            if (bbox.Width >= tamMinimo && bbox.Height >= tamMinimo
            && bbox.Left > 0 && bbox.Right < tamRecuadro.Width - 1 && bbox.Top > 0 && bbox.Bottom < tamRecuadro.Height - 1)
            {
                var textExtraida = ExtraerSprite(recuadro.matRecuadroNormalizado, contniu, ajustarRotacion);
                texturasResultantes.Add(textExtraida);
                contornos.Add(contniu);
                var spriteGen = Sprite.Create(textExtraida, new UnityEngine.Rect(0, 0, textExtraida.width, textExtraida.height)
                    , Vector2.one / 2f, 100f, 1, SpriteMeshType.Tight, Vector4.zero, false);
                spriteResultantes.Add(spriteGen);
            }
        }

        this.matRecuadro = matRecuadro;
    }

    public Texture2D ExtraerSprite(Mat matOriginal, Contorno contorno, int ajustarRotacion)
    {
        var matTexturaAlfa = new Mat(contorno.BoundingRect.Height, contorno.BoundingRect.Width, MatType.CV_8UC1, new Scalar());
        Cv2.DrawContours(matTexturaAlfa, new[] { contorno.contorno }, 0, ProcesarRecuadros.ColEscalarBlanco,
        -1, LineTypes.AntiAlias, null, 0, -contorno.BoundingRect.TopLeft);

        var matTexturaColor = new Mat(matOriginal, contorno.BoundingRect);

        if (equalizarHistograma || ajusteTresh > 0f || equalizarHistogramaDeSat || otsu!=OtsuAlgo.Nop)
        {
            var convertMat = new Mat();
            Cv2.CvtColor(matTexturaColor, convertMat, ColorConversionCodes.BGR2HSV);
            var splits = convertMat.Split();

            if (equalizarHistograma || equalizarHistogramaDeSat)
            {
                var clahe = Cv2.CreateCLAHE();
                if (equalizarHistograma)
                {
                    Cv2.FastNlMeansDenoising(splits[2], splits[2]);
                    clahe.Apply(splits[2], splits[2]);
                }
                if (equalizarHistogramaDeSat)
                {
                    Cv2.FastNlMeansDenoising(splits[1], splits[1]);
                    clahe.Apply(splits[1], splits[1]);
                }
            }
            // Cv2.Threshold(splits[2],splits[1],ajusteTresh,255,ThresholdTypes.TozeroInv);
            if (otsu == OtsuAlgo.OtsuSaturacionYBrillo)
            {
            if (ajusteTresh > 0) Cv2.Threshold(splits[1], splits[1], ajusteTresh, 255, ThresholdTypes.Tozero);
                Cv2.Threshold(splits[2], splits[2], 127, 255, ThresholdTypes.Otsu);
                Cv2.Threshold(splits[1], splits[1], 127, 255, ThresholdTypes.Otsu);
            }
            else if (otsu == OtsuAlgo.OtsuFiltroDameColores) {
                // if (ajusteTresh > 0) Cv2.Threshold(splits[2], splits[1], ajusteTresh, 255, ThresholdTypes.Binary);
                // var resultadoColor = new Mat();
                Cv2.Min(splits[1],splits[2],splits[1]);//esto hace que las zonas oscuras no tengan saturacion mayor a su brillo
                // Cv2.Threshold(splits[], splits[2], 127, 255, ThresholdTypes.Otsu);
                Cv2.Threshold(splits[1], splits[1], 127, 255, ThresholdTypes.Otsu);
                splits[2] = splits[1];
                // Cv2.Min(splits[1],resultadoColor,splits[2]);
            }
            else if (otsu == OtsuAlgo.OtsuFiltroDameColoresYBlanco) {
                // if (ajusteTresh > 0) Cv2.Threshold(splits[2], splits[1], ajusteTresh, 255, ThresholdTypes.Binary);
                // var resultadoColor = new Mat();
                Cv2.Min(splits[1],splits[2],splits[1]);//esto hace que las zonas oscuras no tengan saturacion mayor a su brillo
                // Cv2.Threshold(splits[], splits[2], 127, 255, ThresholdTypes.Otsu);
                Cv2.Threshold(splits[1], splits[1], 127, 255, ThresholdTypes.Otsu);
                // splits[2] = new Mat(splits[2].Rows,splits[2].Cols,splits[2].Type(),255);
                splits[2].SetTo(255);
                // Cv2.Min(splits[1],resultadoColor,splits[2]);
            }
            else {
            if (ajusteTresh > 0) Cv2.Threshold(splits[1], splits[1], ajusteTresh, 255, ThresholdTypes.Tozero);
            }

            // clahe.Apply(splits[1],splits[1]);
            // clahe.Apply(splits[2],splits[2]);
            // Cv2. EqualizeHist(splits[2],splits[2]);
            Cv2.Merge(splits, matTexturaColor = convertMat);
            Cv2.CvtColor(matTexturaColor, matTexturaColor, ColorConversionCodes.HSV2BGR);
        }

        var textura = new Texture2D(matTexturaAlfa.Width, matTexturaAlfa.Height);
        textura.alphaIsTransparency = true;
        textura.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;

        Cv2.Merge(matTexturaColor.Split().Concat(new[] { matTexturaAlfa }).ToArray(), matTexturaAlfa);

        if (ajustarRotacion > 0) Cv2.Rotate(matTexturaAlfa, matTexturaAlfa, (RotateFlags)(ajustarRotacion - 1));
        textura = OCVUnity.MatToTexture(matTexturaAlfa, textura);

        return textura;
    }

#if UNITY_EDITOR
    UnityEngine.UI.RawImage rawImgPrueba;
    [CustomEditor(typeof(ExtraerSprites))]
    public class ExtraerSpritesEditor : Editor
    {
        bool mostrarAlfa = false;
        public override void OnInspectorGUI()
        {
            var coso = target as ExtraerSprites;
            EditorGUI.BeginChangeCheck();
            var rawImgPrueba = EditorGUILayout.ObjectField("Salida De Prueba", coso.rawImgPrueba, typeof(UnityEngine.UI.RawImage), true) as UnityEngine.UI.RawImage;
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(coso, "Coso");
                coso.rawImgPrueba = rawImgPrueba;
            }
            EditorGUI.BeginDisabledGroup(!coso.rawImgPrueba || coso.rawImgPrueba.gameObject.scene.rootCount == 0);
            {
                mostrarAlfa = EditorGUILayout.Toggle("Mostrar Alfa", mostrarAlfa);
                if (GUILayout.Button("Probar"))
                {
                    coso.Extraer();
                    var matdraw = coso.matRecuadro;
                    if (matdraw != null)
                    {
                        if (!mostrarAlfa)
                        {
                            matdraw = coso.procesarRecuadros.Recuadros[0].matRecuadroNormalizado.Clone();
                            //matdraw.SetTo(new Scalar(0));
                            for (int i = 0; i < coso.contornos.Count; i++)
                            {
                                Cv2.DrawContours(matdraw, coso.contornos.Select(c => c.contorno), i, Scalar.RandomColor(), 7);
                                Cv2.Polylines(matdraw, new[] { coso.contornos[i].BoundingRect.ToArray() }, true, Scalar.RandomColor(), 5);
                            }
                        }

                        var textura = new Texture2D(matdraw.Width, matdraw.Height);
                        textura.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
                        OCVUnity.MatToTexture(matdraw, textura);
                        coso.rawImgPrueba.texture = textura;
                        coso.rawImgPrueba.SetNativeSize();
                    }
                }
            }
            EditorGUI.EndDisabledGroup();
            DrawDefaultInspector();
        }
    }
#endif
}
