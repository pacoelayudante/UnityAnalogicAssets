using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCvSharp;
using UnityCV = OpenCvSharp.Unity;
using Rect = UnityEngine.Rect;
using System.Linq;

public class PrimerFiltro
{
    [System.Serializable]
    public class Config
    {
        public bool showDebugPreviews;
        public int debugID = -1;
        public int umbralSaturacion = 30;
        public int umbralBrillo = 140;
        public Vector2Int limitesRosa = new Vector2Int(135, 165), limitesVerde = new Vector2Int(35, 85);

        public Vector2Int threshBlackShipBlobs = new Vector2Int(80, 15);

        public ContourApproximationModes contourApproximationModes = ContourApproximationModes.ApproxTC89L1;

        public float maxDistEntreColores = 25f;
        public Vector2 tamDeColorPastilla = new Vector2(6, 32);
        public int radioBlobPastillas = 55;

    }

    Config _config;
    Texture2D _textura;
    Mat _mat;
    bool _disposeBaseMat;

    public Texture2D debugPreviewT2D;
    static Scalar _negroColor = new Scalar(0, 0, 0);
    static Scalar _rosaColor = new Scalar(255, 0, 255);
    static Scalar _verdeColor = new Scalar(0, 255, 0);

    public GrafoDeContornos _grafoRosa, _grafoVerde, _grafoNaves;
    public List<PastillaBicolor> _pastillas = new List<PastillaBicolor>();

    public List<BlobNave> _naves = new List<BlobNave>();
    public List<BlobSalidas> _salidas = new List<BlobSalidas>();
    public List<BlobAccionDisparo> _disparos = new List<BlobAccionDisparo>();

    Mat _matGray, _matHSV;
    Mat _matSatMask, _matValueMask;
    Mat _matRosaMask, _matVerdeMask, _matRosaVerdeMask;

    Mat _matThreshOne, _matThreshTwo;
    Mat _kernelRect2x2, _kernelRect3x3, _kernelCircle3;

    Mat _matHue, _matSat, _matVal;

    List<Mat> _disposearEsto = new List<Mat>();

    public PrimerFiltro(Config config, Texture2D textura) : this(config, textura, null) { }
    public PrimerFiltro(Config config, Texture2D textura, Mat mat)
    {
        _mat = mat;
        _textura = textura;
        _config = config;
        _disposearEsto.AddRange(new[]{
            _matGray = new Mat(),
            _matHSV = new Mat(),
            _matSatMask = new Mat(),
            _matValueMask = new Mat(),
            _matRosaMask = new Mat(),
            _matVerdeMask = new Mat(),
            _matRosaVerdeMask = new Mat(),
            _matThreshOne = new Mat(),
            _matThreshTwo = new Mat(),
            _kernelRect2x2 = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(2, 2)),
            _kernelRect3x3 = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3, 3)),
            _kernelCircle3 = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(3, 3)),
        });
    }

    public void Procesar()
    {
        if (_mat == null)
        {
            _disposeBaseMat = true;
            _mat = UnityCV.TextureToMat(_textura);
        }

        Cv2.CvtColor(_mat, _matGray, ColorConversionCodes.BGR2GRAY);
        Cv2.CvtColor(_mat, _matHSV, ColorConversionCodes.BGR2HSV);

        Disposear(_matHue, _matSat, _matVal);
        Cv2.Split(_matHSV, out Mat[] canales);
        (_matHue, _matSat, _matVal) = (canales[0], canales[1], canales[2]);

        Cv2.Threshold(_matSat, _matSatMask, _config.umbralSaturacion, 255f, ThresholdTypes.Binary);
        Cv2.Threshold(_matVal, _matValueMask, _config.umbralBrillo, 255f, ThresholdTypes.Binary);

        Cv2.InRange(_matHue, _config.limitesRosa[0], _config.limitesRosa[1], _matRosaMask);
        Cv2.InRange(_matHue, _config.limitesVerde[0], _config.limitesVerde[1], _matVerdeMask);

        Cv2.BitwiseAnd(_matSatMask, _matValueMask, _matSatMask);
        Cv2.BitwiseAnd(_matSatMask, _matRosaMask, _matRosaMask);
        Cv2.BitwiseAnd(_matSatMask, _matVerdeMask, _matVerdeMask);

        Cv2.BitwiseOr(_matRosaMask, _matVerdeMask, _matRosaVerdeMask);

        // Cv2.Merge( new []{ _matRosaMask, _matVerdeMask, _matSat}, _matHSV);
        Cv2.Merge(new[] { _matSat, _matSat, _matSat }, _matHSV);
        Cv2.FindContours(_matRosaMask, out Point[][] contornos, out HierarchyIndex[] jerarquia, RetrievalModes.CComp, _config.contourApproximationModes);
        _grafoRosa = GrafoDeContornos.Crear(contornos, _matRosaMask.Size(), jerarquia);
        Cv2.Polylines(_matHSV, contornos, true, new Scalar(0, 255, 255), 1, LineTypes.AntiAlias);

        Cv2.FindContours(_matVerdeMask, out Point[][] contornos2, out HierarchyIndex[] jerarquia2, RetrievalModes.CComp, _config.contourApproximationModes);
        _grafoVerde = GrafoDeContornos.Crear(contornos2, _matVerdeMask.Size(), jerarquia2);
        Cv2.Polylines(_matHSV, contornos2, true, new Scalar(255, 0, 255), 1, LineTypes.AntiAlias);

        EmparejarContornos(_grafoVerde._primerNivel, _grafoRosa._primerNivel, _config.maxDistEntreColores, _config.tamDeColorPastilla);
        //{
        //using (Mat mixSatBri = new Mat(), blackMask = new Mat())
        //  {
        //  primero 40 despues 15? mas o menos
        Cv2.Threshold(_matGray, _matThreshOne, _config.threshBlackShipBlobs[0], 255, ThresholdTypes.Binary);
        Cv2.Absdiff(_matSat, _matVal, _matThreshTwo);
        Cv2.BitwiseAnd(_matThreshOne, _matThreshTwo, _matThreshTwo);
        Cv2.Threshold(_matThreshTwo, _matThreshTwo, _config.threshBlackShipBlobs[1], 255, ThresholdTypes.BinaryInv);

        //Cv2.BitwiseOr(_matThreshTwo, _matRosaVerdeMask, _matThreshTwo);


        _matThreshOne.SetTo(new Scalar(0));
        foreach (var past in _pastillas)
        {
            Cv2.DrawContours(_matThreshTwo, new[] { past._verde._contornoOpenCV, past._rosa._contornoOpenCV }, -1, new Scalar(255), -1);

            Cv2.Circle(_matThreshOne, past.CentroCV, _config.radioBlobPastillas, new Scalar(255), -1);

            var pV = new Point2f(past._verde.BBox.center.x, past._verde.BBox.center.y);
            var pR = new Point2f(past._rosa.BBox.center.x, past._rosa.BBox.center.y);
            Cv2.Line(_matHSV, pV, pR, new Scalar(0, 255, 0));

            pV = new Point2f(past._verde.BBox.min.x, past._verde.BBox.min.y);
            pR = new Point2f(past._verde.BBox.max.x, past._verde.BBox.max.y);
            Cv2.Rectangle(_matHSV, pV, pR, new Scalar(0, 255, 255));
            pV = new Point2f(past._rosa.BBox.min.x, past._rosa.BBox.min.y);
            pR = new Point2f(past._rosa.BBox.max.x, past._rosa.BBox.max.y);
            Cv2.Rectangle(_matHSV, pV, pR, new Scalar(255, 255, 0));
        }

        _naves.Clear();
        Cv2.BitwiseAnd(_matThreshOne, _matThreshTwo, _matThreshOne);
        Cv2.FindContours(_matThreshOne, out Point[][] contornos3, out HierarchyIndex[] jerarquia3, RetrievalModes.CComp, _config.contourApproximationModes);
        _grafoNaves = GrafoDeContornos.Crear(contornos3, _matThreshOne.Size(), jerarquia3);

        Cv2.CvtColor(_matGray, _matGray, ColorConversionCodes.GRAY2BGR);
        _mat.CopyTo(_matGray);

        foreach (var cont in _grafoNaves._primerNivel)
        {

            var nuevaNave = new BlobNave(cont, _pastillas);
            if (nuevaNave.Nivel > 0)
            {
                _naves.Add(nuevaNave);

                //Cv2.Polylines(_matGray, new []{cont._contornoOpenCV}, true, new Scalar(255, 255, 0), 2, LineTypes.AntiAlias);
                Cv2.Ellipse(_matGray, cont.EllipseCV.Center, new Size(cont.EllipseCV.Size.Width / 2f, cont.EllipseCV.Size.Height / 2f), cont.EllipseCV.Angle, 0f, 360f, new Scalar(255, 255, 0), 2, LineTypes.AntiAlias);

                var largo = Mathf.Max(cont.EllipseCV.Size.Width, cont.EllipseCV.Size.Height) / 3f;
                Cv2.ArrowedLine(_matGray, cont.CentroBBoxCV, cont.CentroBBoxCV + (Point)(nuevaNave.DireccionCV * largo), new Scalar(255, 255, 0), 1);
            }
        }

        foreach (var cont in _grafoRosa._primerNivel)
        {
            if (_pastillas.Any((past) => past._rosa == cont))
                continue;

            if (cont.BBox.width < _config.tamDeColorPastilla[1] && cont.BBox.height < _config.tamDeColorPastilla[1])
                continue;

            var nuevaSalida = new BlobSalidas(cont, _matValueMask);
            if (nuevaSalida.Cantidad > 0)
            {
                _salidas.Add(nuevaSalida);

                float minDist = float.PositiveInfinity;
                BlobNave naveCerca = null;
                foreach (var nave in _naves)
                {
                    var dist = Vector2.Distance(cont.CentroBBox, nave.CentroBBox);
                    if (dist < minDist)
                    {
                        naveCerca = nave;
                        minDist = dist;
                    }
                }

                naveCerca.Add(nuevaSalida);

                foreach (var v2 in nuevaSalida._salidasEstimados)
                {
                    var punto = new Point(v2.x, v2.y);
                    Cv2.Circle(_matGray, punto, 4, new Scalar(255, 0, 255), 2, LineTypes.AntiAlias);
                }
            }
        }

        foreach (var cont in _grafoVerde._primerNivel)
        {
            if (_pastillas.Any((past) => past._verde == cont))
                continue;

            if (cont.BBox.width < _config.tamDeColorPastilla[1] && cont.BBox.height < _config.tamDeColorPastilla[1])
                continue;

            // foreach (var cont2 in cont._contenidos)
            // {
            //     Cv2.FillConvexPoly(_matGray, cont2._contornoOpenCV, new Scalar(0, 255, 255));
            // }

            var nuevoDisparo = new BlobAccionDisparo(cont, _matRosaMask, _matValueMask);
            if (nuevoDisparo.Cantidad > 0)
            {
                _disparos.Add(nuevoDisparo);

                int idx = 0;
                foreach (var v2 in nuevoDisparo._disparosEstimados)
                {
                    var punto = new Point(v2.x, v2.y);
                    Cv2.Circle(_matGray, punto, 4, new Scalar(0, 255, 255), 2, LineTypes.AntiAlias);
                    Cv2.PutText(_matGray, (++idx).ToString(), punto, HersheyFonts.HersheyPlain, 1f, new Scalar(0, 0, 0));
                }
            }
        }

        foreach (var nave in _naves)
        {
            int idx = 0;
            foreach (var pt in nave._salidasBabor)
            {
                if (idx < _disparos[0].Cantidad) {
                    var tiro = _disparos[0]._disparosEstimados[idx];
                    var dir = (pt-tiro)*1000;
                    Cv2.ArrowedLine(_matGray, new Point(tiro.x,tiro.y), new Point(dir.x,dir.y), new Scalar(255,0,0), 1);
                }

                var point = new Point(pt.x, pt.y);
                Cv2.PutText(_matGray, (++idx).ToString(), point, HersheyFonts.HersheyPlain, 1f, new Scalar(255, 190, 255));
            }
            idx = 0;
            foreach (var pt in nave._salidasEstribor)
            {
                if (idx < _disparos[0].Cantidad) {
                    var tiro = _disparos[0]._disparosEstimados[idx];
                    var dir = (pt-tiro)*1000;
                    Cv2.ArrowedLine(_matGray, new Point(tiro.x,tiro.y), new Point(dir.x,dir.y), new Scalar(255,0,0), 1);
                }
                
                var point = new Point(pt.x, pt.y);
                Cv2.PutText(_matGray, (++idx).ToString(), point, HersheyFonts.HersheyPlain, 1f, new Scalar(255, 190, 255));
            }
        }

        if (_config.showDebugPreviews)
        {
            if (debugPreviewT2D)
                Object.DestroyImmediate(debugPreviewT2D);

            if (_config.debugID < 0)
                debugPreviewT2D = UnityCV.MatToTexture(_matGray);
            else
                debugPreviewT2D = UnityCV.MatToTexture(_disposearEsto[_config.debugID % _disposearEsto.Count]);
        }
    }

    void EmparejarContornos(List<GrafoDeContornos.Contorno> gVerde, List<GrafoDeContornos.Contorno> gRosa, float maxD, Vector2 tamBBox)
    {
        var parejasPosibles = new List<PastillaBicolor>();
        for (int iv = 0, countV = gVerde.Count; iv < countV; iv++)
        {
            var verde = gVerde[iv];
            if (verde.BBox.width < tamBBox[0] || verde.BBox.height < tamBBox[0]
            || verde.BBox.width > tamBBox[1] || verde.BBox.height > tamBBox[1])
                continue;

            for (int ir = 0, countR = gRosa.Count; ir < countR; ir++)
            {
                var rosa = gRosa[ir];
                if (rosa.BBox.width < tamBBox[0] || rosa.BBox.height < tamBBox[0]
                || rosa.BBox.width > tamBBox[1] || rosa.BBox.height > tamBBox[1])
                    continue;

                var d = Vector2.Distance(verde.BBox.center, rosa.BBox.center);
                if (maxD <= 0f || d <= maxD)
                    parejasPosibles.Add(new PastillaBicolor(verde, rosa, d));
            }
        }
        parejasPosibles.Sort((a, b) => a._distancia.CompareTo(b._distancia));

        var puntosVerdesEncontrados = new List<GrafoDeContornos.Contorno>();
        var puntosRosaEncontrados = new List<GrafoDeContornos.Contorno>();

        _pastillas.Clear();

        for (int i = 0, count = parejasPosibles.Count; i < count; i++)
        {
            var verde = parejasPosibles[i]._verde;
            var rosa = parejasPosibles[i]._rosa;
            if (!puntosRosaEncontrados.Contains(rosa) && !puntosVerdesEncontrados.Contains(verde))
            {
                _pastillas.Add(parejasPosibles[i]);
                puntosVerdesEncontrados.Add(verde);
                puntosRosaEncontrados.Add(rosa);
            }
        }
    }

    void CargarMapa(bool grupoSecundario, Dictionary<GrafoDeContornos.Contorno, Dictionary<GrafoDeContornos.Contorno, float>> mapa, GrafoDeContornos.Contorno actual, List<GrafoDeContornos.Contorno> otroGrupo)
    {
        for (int i = otroGrupo.Count - 1; i >= 0; i--)
        {
            var primerKey = grupoSecundario ? otroGrupo[i] : actual;
            var segundaKey = grupoSecundario ? actual : otroGrupo[i];

            if (!mapa.ContainsKey(primerKey))
                mapa.Add(primerKey, new Dictionary<GrafoDeContornos.Contorno, float>());

            if (!mapa[primerKey].ContainsKey(segundaKey))
                mapa[primerKey].Add(segundaKey, Vector2.Distance(primerKey.BBox.center, segundaKey.BBox.center));
        }
    }

    ~PrimerFiltro()
    {
        if (_disposeBaseMat && _mat != null && !_mat.IsDisposed)
            _mat.Dispose();

        Disposear(_matHue, _matSat, _matVal);
        Disposear(_disposearEsto.ToArray());

        if (debugPreviewT2D)
            Object.DestroyImmediate(debugPreviewT2D);
    }

    void Disposear(params DisposableObject[] disposear)
    {
        foreach (var disp in disposear)
        {
            if (disp != null && !disp.IsDisposed)
                disp.Dispose();
        }
    }
}
