using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Rect = UnityEngine.Rect;
using CvRect = OpenCvSharp.Rect;
using OpenCvSharp;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Contour
{
    public int _level = 0;
    public Point[] _shape;
    public List<Contour> childs;
    public Contour previousSibling;
    public Contour nextSibling;
    public Contour parent;
}

public class ContoursTree
{
    public readonly List<Contour> allContours;
    public readonly Dictionary<int, List<Contour>> contoursByLevel;
    public readonly List<Contour> topLevelContours;

    public ContoursTree(Point[][] points, HierarchyIndex[] hierarchy, RetrievalModes retrievalMode)
    {

    }
}

[CreateAssetMenu]
public class ContourFinderFromBinary : ScriptableObject
{
    [SerializeField]
    private RetrievalModes _retrievalModes = RetrievalModes.Tree;
    [SerializeField]
    private ContourApproximationModes _contourApproximationModes = ContourApproximationModes.ApproxTC89KCOS;

    public ContoursTree Texture2D(Texture2D textureInput)
        => Process(OpenCvSharp.Unity.TextureToMat(textureInput));

    public ContoursTree Process(Mat binaryMatInput)
    {
        Cv2.FindContours(binaryMatInput, out Point[][] points, out HierarchyIndex[] hierarchy, _retrievalModes, ContourApproximationModes.ApproxTC89KCOS);
        return new ContoursTree(points, hierarchy, _retrievalModes);
    }
}
