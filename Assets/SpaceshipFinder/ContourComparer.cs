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
public class ContourComparer : ScriptableObject
{
    [SerializeField]
    private Texture2D _templateTexture;
    [SerializeField]
    private Texture2D _queryTexture;

    [SerializeField]
    private SpaceshipFinder2 _templatesPreprocessor;
    [SerializeField]
    private SpaceshipFinder2 _queryPreprocessor;

    [SerializeField]
    private ShapeMatchModes _shapeMatchModes = ShapeMatchModes.I1;
    [SerializeField]
    private double _shapeMatchParameter = 0d;

    [SerializeField, SpaceshipFinder2.MinMaxSlider(1, 120)]
    Vector2Int _labelBBoxSideLimits = new Vector2Int(1, 120);

    Point[][] _templatesContours;
    Point[][] _queryContours;

    Rect[] _templateBBox;
    Rect[] _queryBBox;

    Texture2D _templateResult;
    Texture2D _queryResult;

    Dictionary<Point[], List<Match>> _mapping = new();

    public class Match
    {
        public double similarity;
        public Rect rect;
        public float angle;
    }

    public void Compare(Texture2D templateTex2D, Texture2D queryTex2D)
    {
        if (templateTex2D == null || queryTex2D == null)
            return;

        using (Mat templateMat = OpenCvSharp.Unity.TextureToMat(templateTex2D))
        using (Mat queryMat = OpenCvSharp.Unity.TextureToMat(queryTex2D))
        {
            Compare(templateMat, queryMat);
        }
    }

    public void Compare(Mat templateMat, Mat queryMat)
    {
        if (templateMat == null || queryMat == null)
            return;

        _templatesPreprocessor.ProcesarTextura(templateMat,
        new SpaceshipFinder2.Resultados()
        {
            resultadoContornos = (contornos) =>
            {
                _templatesContours = contornos;
            },
            resultadoPrimerFiltroConExtras = (mat) =>
            {
                _templateResult = OpenCvSharp.Unity.MatToTexture(mat, _templateResult);
            }
        });

        _queryPreprocessor.ProcesarTextura(queryMat,
        new SpaceshipFinder2.Resultados()
        {
            resultadoContornos = (contornos) =>
            {
                _queryContours = contornos;
            },
            resultadoPrimerFiltroConExtras = (mat) =>
            {
                _queryResult = OpenCvSharp.Unity.MatToTexture(mat, _queryResult);
            }
        });

        _templateBBox = new Rect[_templatesContours.Length];
        for (int i = 0, count = _templatesContours.Length; i < count; i++)
        {
            var cvrect = Cv2.BoundingRect(_templatesContours[i]);
            _templateBBox[i] = new Rect(cvrect.Left, templateMat.Height - cvrect.Bottom, cvrect.Width, cvrect.Height);
        }

        _queryBBox = new Rect[_queryContours.Length];
        for (int i = 0, count = _queryContours.Length; i < count; i++)
        {
            var cvrect = Cv2.BoundingRect(_queryContours[i]);
            _queryBBox[i] = new Rect(cvrect.Left, queryMat.Height - cvrect.Bottom, cvrect.Width, cvrect.Height);
        }

        foreach (var templateCont in _templatesContours)
        {
            var matches = new List<Match>();
            _mapping.Add(templateCont, matches);
            foreach (var queryCont in _queryContours)
            {
                var cvrect = Cv2.BoundingRect(queryCont);


                if (cvrect.Width > _labelBBoxSideLimits[0] && cvrect.Width < _labelBBoxSideLimits[1]
                    && cvrect.Height > _labelBBoxSideLimits[0] && cvrect.Height < _labelBBoxSideLimits[1])
                {
                    double similarity = Cv2.MatchShapes(templateCont, queryCont, _shapeMatchModes);
                    var rect = new Rect(cvrect.Left, queryMat.Height - cvrect.Bottom, cvrect.Width, cvrect.Height);
                    var rotRect = Cv2.FitEllipse(queryCont);
                    matches.Add(new Match() { rect = rect, similarity = similarity, angle = rotRect.Angle });
                }
            }

            matches.Sort((matchA, matchB) => matchA.similarity.CompareTo(matchB.similarity));
        }

        // int ti = 0;
        // int qi = 0;
        // foreach(var template in _templatesContours)
        // {

        //     foreach (var query in _queryContours)
        //     {

        //        double similarity = Cv2.MatchShapes(template, query, _shapeMatchModes);
        //        Debug.Log($" for template {ti} and {qi++} similarity is: {similarity}");

        //     }

        //     ti++;

        // }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(ContourComparer))]
    public class ContourComparerEditor : Editor
    {
        List<Texture2D> _templatesTextures = new();
        List<Texture2D> _queryResults = new();
        Vector2 _scrollPos;

        void OnDisable()
        {
            foreach (var text2d in _templatesTextures)
                DestroyImmediate(text2d);
            foreach (var text2d in _queryResults)
                DestroyImmediate(text2d);

            _templatesTextures.Clear();
            _queryResults.Clear();
        }

        public override void OnInspectorGUI()
        {
            var comparer = (ContourComparer)target;
            DrawDefaultInspector();

            if (GUILayout.Button("Compare"))
                comparer.Compare(comparer._templateTexture, comparer._queryTexture);

            // var contours = comparer._templatesContours;
            // if (contours != null)
            // {
            //     for (int i = 0; i < contours.Length; i++)
            //     {
            //         while (i >= _templatesTextures.Count)
            //             _templatesTextures.Add(new Texture2D(2, 2));
            //         ExtractTexture(contours[i], comparer._templateTexture, _templatesTextures[i]);
            //     }
            // }

            if (comparer._templateBBox != null)
            {
                using (var scroll = new EditorGUILayout.ScrollViewScope(_scrollPos))
                {
                    _scrollPos = scroll.scrollPosition;
                    var textureSize = comparer._templateTexture.texelSize;
                    var queryTextureSize = comparer._queryTexture.texelSize;
                    //foreach (var bbox in comparer._templateBBox)
                    for (int i = 0, count = comparer._templateBBox.Length; i < count; i++)
                    {
                        var bbox = comparer._templateBBox[i];
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            var guirect = GUILayoutUtility.GetRect(bbox.width, bbox.height, GUILayout.ExpandWidth(false));
                            var t2d = comparer._templateResult ? comparer._templateResult : comparer._templateTexture;
                            GUI.DrawTextureWithTexCoords(guirect, t2d, new Rect(bbox.position * textureSize, bbox.size * textureSize));

                            // foreach (var bboxFound in comparer._queryBBox)
                            // {
                            //     guirect = GUILayoutUtility.GetRect(bboxFound.width, bboxFound.height, GUILayout.ExpandWidth(false));
                            //     GUI.DrawTextureWithTexCoords(guirect, comparer._queryTexture, new Rect(bboxFound.position * queryTextureSize, bboxFound.size * queryTextureSize));
                            // }
                            var templateContour = comparer._templatesContours?[i];
                            if (comparer._mapping == null || templateContour == null || comparer._queryResult == null)
                                continue;

                            foreach (var matches in comparer._mapping[templateContour])
                            {
                                using (new EditorGUILayout.VerticalScope())
                                {
                                    var bboxFound = matches.rect;
                                    GUILayout.Label(matches.similarity.ToString("0.00"));
                                    GUILayout.Label(matches.angle.ToString("0.00"));
                                    guirect = GUILayoutUtility.GetRect(bboxFound.width, bboxFound.height, GUILayout.ExpandWidth(false));
                                    GUI.DrawTextureWithTexCoords(guirect, comparer._queryResult, new Rect(bboxFound.position * queryTextureSize, bboxFound.size * queryTextureSize));
                                }
                            }

                        }
                    }
                }
            }
        }

        public void ExtractTexture(Point[] contour, Texture2D textureSource, Texture2D textureResult)
        {

        }
    }
#endif
}
