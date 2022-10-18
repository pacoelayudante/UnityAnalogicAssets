using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCvSharp;
using System.Diagnostics;

public class ORBTracking : MonoBehaviour
{
    public int _maxMatches = 50;

    public Texture2D _query2DTexture;
    // public Mat _srcMat;
    public Mat _queryDescriptors;

    public Texture2D _train2DTexture;
    // public Mat _dstMat;
    public Mat _trainDescriptors;

    public Texture2D _outMatchesTexture;
    public Texture2D _warpedTexture;

    [ContextMenu("Extract")]
    private void ExtractDescriptor()
    {
        Stopwatch stopWatch = new Stopwatch();
        stopWatch.Start();
        if (_query2DTexture == null || _train2DTexture == null)
            return;

        using var queryMat = OpenCvSharp.Unity.TextureToMat(_query2DTexture);
        using var trainMat = OpenCvSharp.Unity.TextureToMat(_train2DTexture);
        // if (_srcMat == null && _src2DTexture)
        //     _srcMat = OpenCvSharp.Unity.TextureToMat(_src2DTexture);

        // if (_dstMat == null && _dst2DTexture)
        //     _dstMat = OpenCvSharp.Unity.TextureToMat(_dst2DTexture);

        // if (_srcMat == null || _dstMat == null)
        //     return;

        Stopwatch stopWatchORB = new Stopwatch();
        stopWatchORB.Start();
        using var orb = ORB.Create(nFeatures: 500);

        if (_queryDescriptors == null)
            _queryDescriptors = new Mat();
        if (_trainDescriptors == null)
            _trainDescriptors = new Mat();

        orb.DetectAndCompute(queryMat, null, out KeyPoint[] queryKeyPoints, _queryDescriptors, false);
        orb.DetectAndCompute(trainMat, null, out KeyPoint[] trainKeyPoints, _trainDescriptors, false);
        stopWatchORB.Stop();
        var orbDuration = stopWatchORB.ElapsedMilliseconds;

        Stopwatch stopWatchMatcher = new Stopwatch();
        stopWatchMatcher.Start();
        using var bfmatcher = new BFMatcher(NormTypes.Hamming, crossCheck: true);
        var matches = bfmatcher.Match(_queryDescriptors, _trainDescriptors);
        System.Array.Sort(matches, (a, b) => a.Distance.CompareTo(b.Distance));

        /*using var indexParams = new OpenCvSharp.Flann.IndexParams();
        indexParams.SetAlgorithm(6);
        indexParams.SetInt("table_number",6);
        indexParams.SetInt("key_size",12);
        indexParams.SetInt("multi_probe_level",2);
        using var searchParams = new OpenCvSharp.Flann.SearchParams();

        using var flann = new FlannBasedMatcher(indexParams, searchParams);
        var matches = flann.KnnMatch(_queryDescriptors, _trainDescriptors, k:2);

        var goodMatches = new List<DMatch[]>();

        foreach (var matchGroup in matches)
        {
            if (matchGroup[0].Distance < .75f * matchGroup[1].Distance)
                goodMatches.Add(matchGroup);
            //         foreach (var match in matchGroup)
            // {
            //             match.Distance
            // }
        }*/
        stopWatchMatcher.Stop();
        var matcherDuration = stopWatchMatcher.ElapsedMilliseconds;

        if (matches.Length > _maxMatches)
            System.Array.Resize(ref matches, _maxMatches);

        using var outMatchImage = new Mat();
        Cv2.DrawMatches(queryMat, queryKeyPoints, trainMat, trainKeyPoints, matches, outMatchImage);

        if (_outMatchesTexture)
            GameObject.DestroyImmediate(_outMatchesTexture);

        _outMatchesTexture = OpenCvSharp.Unity.MatToTexture(outMatchImage);

        Stopwatch stopWatchWarper = new Stopwatch();
        stopWatchWarper.Start();
        var queryPts = new Point2d[matches.Length];
        var trainPts = new Point2d[matches.Length];
        int index = 0;
        foreach (var match in matches)
        {
            var queryPt = queryKeyPoints[match.QueryIdx].Pt;
            var trainPt = trainKeyPoints[match.TrainIdx].Pt;
            queryPts[index] = new Point2d(queryPt.X, queryPt.Y);
            trainPts[index++] = new Point2d(trainPt.X, trainPt.Y);
        }

        using var homography = Cv2.FindHomography(queryPts, trainPts, HomographyMethods.Ransac, 5.0d);
        using var warpedImg = new Mat();
        Cv2.WarpPerspective(queryMat, warpedImg, homography, trainMat.Size());

        stopWatchWarper.Stop();
        var warperDuration = stopWatchWarper.ElapsedMilliseconds;

        if (_warpedTexture)
            GameObject.DestroyImmediate(_warpedTexture);

        _warpedTexture = OpenCvSharp.Unity.MatToTexture(warpedImg);
        stopWatch.Stop();
        var fullDuration = stopWatch.ElapsedMilliseconds;

        UnityEngine.Debug.Log($"Full duration: {fullDuration}, orb duration {orbDuration}, match duration {matcherDuration}, homography duration {warperDuration}");
    }
}
