using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using OpenCvSharp;

public class GrafoDeContornos
{
    public List<Contorno> _todos = new List<Contorno>();
    public List<Contorno> _primerNivel = new List<Contorno>();
    public Vector2 _tam;

    public int Count => _todos.Count;
    public int PrimerNivelCount => _primerNivel.Count;

    public static GrafoDeContornos Crear(Point[][] contornosOCV, Size size, HierarchyIndex[] jerarquia)
    {
        var nuevoGrafo = new GrafoDeContornos();
        nuevoGrafo._tam = new Vector2(size.Width, size.Height);

        for (int i = 0, count = jerarquia.Length; i < count; i++)
        {
            nuevoGrafo._todos.Add(new Contorno(nuevoGrafo, contornosOCV[i], jerarquia[i]));
            if (jerarquia[i].Parent < 0)
                nuevoGrafo._primerNivel.Add(nuevoGrafo._todos[i]);
        }
        return nuevoGrafo;
    }
    /*
        public Contorno this[int indice] {
            get => _primerNivel[ indice ];
        }*/

    public class Contorno
    {
        GrafoDeContornos _grafo;

        public Point[] _contornoOpenCV;
        HierarchyIndex _hierarhyIndex;

        Contorno _contenedor;
        public List<Vector2> _contornoUnity;
        public List<Vector2> _contornoUnityNormalized;

        public List<Contorno> _contenidos = new List<Contorno>();
        public int Count => _contenidos.Count;

        public int PointCount => _contornoOpenCV.Length;

        public Vector2 Centroide
        {
            get
            {
                CacheMoments();
                return _cachedCentroide;
            }
        }

        public UnityEngine.Rect BBox
        {
            get
            {
                CacheRects();
                return _cacheBBox;
            }
        }
        public UnityEngine.Rect BBoxNormalized
        {
            get
            {
                CacheRects();
                return _cacheBBoxNorm;
            }
        }
        public OpenCvSharp.Rect BBoxCV
        {
            get
            {
                CacheRects();
                return _cacheBBoxCV;
            }
        }

        public RotatedRect EllipseCV
        {
            get {
                CacheEllipse();
                return _cachedEllipseCV;
            }
        }

        public Vector2 CentroBBox => BBox.center;
        public Point CentroBBoxCV => BBoxCV.Center;

        public Moments Moments
        {
            get
            {
                CacheMoments();
                return _cachedMoments;
            }
        }

        public Vector2 Tam => _grafo._tam;

        bool _bboxNeedsCache = true, _ellipseNeedsCache = true;
        RotatedRect _cachedEllipseCV;
        OpenCvSharp.Rect _cacheBBoxCV;
        UnityEngine.Rect _cacheBBox, _cacheBBoxNorm;

        Vector2 _cachedCentroide;
        Moments _cachedMoments;

        float _cacheArea;
        float _cachePerimetro;
        List<Vector2> _cacheConvexHull;

        public Contorno(GrafoDeContornos grafo, Point[] contorno, HierarchyIndex hierarhyIndex)
        {
            _grafo = grafo;
            _contornoOpenCV = contorno;
            _hierarhyIndex = hierarhyIndex;

            _contornoUnity = _contornoOpenCV.Select(p => new Vector2(p.X, p.Y)).ToList();
            _contornoUnityNormalized = _contornoOpenCV.Select(p => new Vector2(p.X / Tam.x, p.Y / Tam.y)).ToList();

            if (hierarhyIndex.Parent >= 0) SetParent(grafo._todos[hierarhyIndex.Parent]);
        }

        private void CacheRects()
        {
            if (_bboxNeedsCache)
            {
                _cacheBBoxCV = Cv2.BoundingRect(_contornoOpenCV);
                _cacheBBox = new UnityEngine.Rect(_cacheBBoxCV.X, _cacheBBoxCV.Y, _cacheBBoxCV.Width, _cacheBBoxCV.Height);
                _cacheBBoxNorm = new UnityEngine.Rect(_cacheBBoxCV.X / Tam.x, _cacheBBoxCV.Y / Tam.y, _cacheBBoxCV.Width / Tam.x, _cacheBBoxCV.Height / Tam.y);
                _bboxNeedsCache = false;
            }
        }

        private void CacheEllipse()
        {
            if (_ellipseNeedsCache)
            {
                _cachedEllipseCV = Cv2.FitEllipse(_contornoOpenCV);
            }
        }

        private void CacheMoments()
        {
            if (_cachedMoments == null)
                _cachedMoments = Cv2.Moments(_contornoOpenCV);

            _cachedCentroide = new Vector2((float)(_cachedMoments.M10 / _cachedMoments.M00), (float)(_cachedMoments.M01 / _cachedMoments.M00));
        }

        public bool Contains(Contorno contenido) => _contenidos.Contains(contenido);

        public bool PointInside(Vector2 point) => Cv2.PointPolygonTest(_contornoOpenCV, new Point2f(point.x, point.y), false) >= 0d;

        void SetParent(Contorno contenedor)
        {
            if (_contenedor != null)
                _contenedor.RemoveChild(this);

            contenedor.AddChild(this);
        }
        void AddChild(Contorno contenido)
        {
            Debug.Assert(contenido._contenedor == null);

            if (!Contains(contenido))
                _contenidos.Add(contenido);
            contenido._contenedor = this;
        }
        void RemoveChild(Contorno contenido)
        {
            _contenidos.Remove(contenido);
            if (contenido._contenedor == this)
                contenido._contenedor = null;
        }
    }
}
