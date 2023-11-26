using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Rect = UnityEngine.Rect;
using OpenCvSharp;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu]
public class SpaceshipFinder : ScriptableObject
{
    //public Color _colPurpura = Color.HSVToRGB(250,100,42);//new Color(0.06666667f,0f,0.3960785f);
    //public Color _colAmarillo = Color.HSVToRGB(41,100,64);//new Color(0.6352941f,0.4313726f,0f);
    [SerializeField, Range(0, 255)]
    float _colPurpuraH = 133;
    [SerializeField, Range(0.5f, 60f)]
    float _umbralPurpura = 12f;
    [SerializeField, Range(1, 255)]
    float _umbralSatPurpura = 60f;

    [SerializeField, Range(0, 255)]
    float _colAmarilloH = 23;
    [SerializeField, Range(0.5f, 60f)]
    float _umbralAmarillo = 4f;
    [SerializeField, Range(1, 255)]
    float _umbralSatAmarillo = 135f;

    [SerializeField]
    int dilateCount = 2;
    [SerializeField]
    int erodeCount = 2;

    public enum TipoColor
    {
        HSV, HLS
    }

    [SerializeField]
    TipoColor _tipoColor = TipoColor.HSV;

    // [SerializeField, Range(1, 255)]
    // float _saturationThreshold = 127f;

    [SerializeField]
    ThresholdTypes _saturationThreshType = ThresholdTypes.Binary;

    public void ProcesarTextura(Texture2D tex2D, System.Action<Mat> onBlobsPurpuras = null, System.Action<Mat> onBlobsAmarillos = null)
    {
        using (Mat mat = OpenCvSharp.Unity.TextureToMat(tex2D))
        {
            ProcesarTextura(mat, onBlobsPurpuras, onBlobsAmarillos);
        }
    }

    public void ProcesarTextura(Mat mat, System.Action<Mat> onBlobsPurpuras = null, System.Action<Mat> onBlobsAmarillos = null)
    {
        using (Mat tempOutput = new Mat())
        using (Mat emptyMat = new Mat())
        using (Mat dst = new Mat())
        {
            Cv2.CvtColor(mat, dst, _tipoColor == TipoColor.HSV ? ColorConversionCodes.BGR2HSV : ColorConversionCodes.BGR2HLS);
            var splits = dst.Split();

            using (splits[0])
            using (splits[1])
            using (splits[2])
            {
                var hue = splits[0];
                var sat = _tipoColor == TipoColor.HSV ? splits[1] : splits[2];
                var brillo = _tipoColor == TipoColor.HSV ? splits[2] : splits[1];

                // Cv2.Threshold(sat, sat, _saturationThreshold, 255f, _saturationThreshType);

                using (Mat blobsPurpura = new Mat())
                using (Mat blobsAmarillos = new Mat())
                using (Mat tempSatThreshold = new Mat())
                {
                    var minMaxPurpura = new Vector2(_colPurpuraH - _umbralPurpura, _colPurpuraH + _umbralPurpura);
                    Cv2.InRange(hue, minMaxPurpura.x, minMaxPurpura.y, blobsPurpura);
                    Cv2.Threshold(sat, tempSatThreshold, _umbralSatPurpura, 255f, _saturationThreshType);
                    Cv2.BitwiseAnd(tempSatThreshold, blobsPurpura, blobsPurpura);

                    if (erodeCount > 0)
                        Cv2.Erode(blobsPurpura, blobsPurpura, emptyMat, null, erodeCount);
                    if (dilateCount > 0)
                        Cv2.Dilate(blobsPurpura, blobsPurpura, emptyMat, null, dilateCount);

                    var minMaxAmarillo = new Vector2(_colAmarilloH - _umbralAmarillo, _colAmarilloH + _umbralAmarillo);
                    Cv2.InRange(hue, minMaxAmarillo.x, minMaxAmarillo.y, blobsAmarillos);
                    Cv2.Threshold(sat, tempSatThreshold, _umbralSatAmarillo, 255f, _saturationThreshType);
                    Cv2.BitwiseAnd(tempSatThreshold, blobsAmarillos, blobsAmarillos);
                        
                    if (erodeCount > 0)
                        Cv2.Erode(blobsAmarillos, blobsAmarillos, emptyMat, null, erodeCount);
                    if (dilateCount > 0)
                        Cv2.Dilate(blobsAmarillos, blobsAmarillos, emptyMat, null, dilateCount);

                    Cv2.ConnectedComponents(blobsAmarillos, blobsAmarillos);


                    if (onBlobsPurpuras != null)
                        onBlobsPurpuras.Invoke(blobsPurpura);

                    if (onBlobsAmarillos != null)
                        onBlobsAmarillos.Invoke(blobsAmarillos);


                    //VerTexturaSola.Mostrar(OpenCvSharp.Unity.MatToTexture(laplacian), true, true);
                }
            }
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(SpaceshipFinder))]
    public class SpaceshipFinderEditor : Editor
    {
        Texture2D texturaInput;
        Texture2D texPurpura;
        Texture2D texAmarillo;
        SpaceshipFinder _finderTarget;

        private void OnEnable()
        {
            _finderTarget = (SpaceshipFinder)target;
        }

        private void OnDisable()
        {
            AssetDatabase.SaveAssets();

            if (texPurpura != null)
                DestroyImmediate(texPurpura);
            if (texAmarillo != null)
                DestroyImmediate(texAmarillo);
            texPurpura = null;
            texAmarillo = null;
        }

        private void OnBlobPurpura(Mat mat)
        {
            texPurpura = OpenCvSharp.Unity.MatToTexture(mat, texPurpura);
            texPurpura.name = "Purpura";
        }

        private void OnBlobAmarillo(Mat mat)
        {
            texAmarillo = OpenCvSharp.Unity.MatToTexture(mat, texAmarillo);
            texAmarillo.name = "Amarillo";
        }

        public override void OnInspectorGUI()
        {
            using (var changed = new EditorGUI.ChangeCheckScope())
            {
                texturaInput = EditorGUILayout.ObjectField("Input", texturaInput, typeof(Texture2D), allowSceneObjects: false) as Texture2D;
                EditorGUILayout.ObjectField("Purpura", texPurpura, typeof(Texture2D), allowSceneObjects: false);
                EditorGUILayout.ObjectField("Amarilla", texAmarillo, typeof(Texture2D), allowSceneObjects: false);

                DrawDefaultInspector();

                if (changed.changed)
                {
                    if (texturaInput != null)
                        _finderTarget.ProcesarTextura(texturaInput, OnBlobPurpura, OnBlobAmarillo);
                }
            }

            if (GUILayout.Button("Procesar"))
            {
                if (texturaInput != null)
                    _finderTarget.ProcesarTextura(texturaInput, OnBlobPurpura, OnBlobAmarillo);
            }
        }
    }
#endif
}
