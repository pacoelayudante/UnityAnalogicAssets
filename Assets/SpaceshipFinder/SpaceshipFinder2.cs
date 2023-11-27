using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Rect = UnityEngine.Rect;
using OpenCvSharp;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu]
public class SpaceshipFinder2 : ScriptableObject
{
    //public Color _colPurpura = Color.HSVToRGB(250,100,42);//new Color(0.06666667f,0f,0.3960785f);
    //public Color _colAmarillo = Color.HSVToRGB(41,100,64);//new Color(0.6352941f,0.4313726f,0f);
    public enum TipoColor
    {
        HSV, HLS
    }

    [SerializeField]
    Texture2D _inputTexture;

    [SerializeField]
    TipoColor _tipoColor = TipoColor.HSV;

    [SerializeField, Range(0, 255)]
    int _hue = 130;
    [SerializeField, Range(1, 60)]
    int _hueTolerance = 12;
    [SerializeField, MinMaxSlider(0, 255)]
    Vector2Int _saturacionValida = new Vector2Int(0, 255);
    [SerializeField, MinMaxSlider(0, 255)]
    Vector2Int _brilloValido = new Vector2Int(0, 255);

    // [SerializeField]
    // ThresholdTypes _saturationThreshType = ThresholdTypes.Binary;

    [SerializeField]
    int dilateCount = 2;
    [SerializeField]
    int erodeCount = 2;

    // [SerializeField, Range(1, 255)]
    // float _saturationThreshold = 127f;

    public struct Resultados
    {
        public System.Action<Mat> resultadoPrimerFiltro;
    }

    public void ProcesarTextura(Texture2D inputTex2D, Resultados resultados)
    {
        using (Mat inputMat = OpenCvSharp.Unity.TextureToMat(inputTex2D))
        {
            ProcesarTextura(inputMat, resultados);
        }
    }

    public void ProcesarTextura(Mat inputMat, Resultados resultados)
    {
        // using (Mat emptyMat = new Mat())
        using (Mat inputConverted = new Mat())
        using (Mat tempOutput = new Mat(inputMat.Rows, inputMat.Cols, inputMat.Type()))
        {
            Cv2.CvtColor(inputMat, inputConverted, _tipoColor == TipoColor.HSV ? ColorConversionCodes.BGR2HSV : ColorConversionCodes.BGR2HLS);

            int hueMin = _hue - _hueTolerance;
            int hueMax = _hue + _hueTolerance;

            // cuando HSV: es hue, saturacion y valor (brillo)
            // cuando HLS: es hue, brillo y saturacion
            var segundoComponente = _tipoColor == TipoColor.HSV ? _saturacionValida : _brilloValido;
            var tercerComponente = _tipoColor == TipoColor.HLS ? _saturacionValida : _brilloValido;

            var scalarMinimo = new Scalar(hueMin, segundoComponente[0], tercerComponente[0]);
            var scalarMaximo = new Scalar(hueMax, segundoComponente[1], tercerComponente[1]);
            using (Mat primerFiltro = new Mat())
            {
                Cv2.InRange(inputConverted, scalarMinimo, scalarMaximo, primerFiltro);
                tempOutput.SetTo(Scalar.DarkSlateGray);
                inputMat.CopyTo(tempOutput, primerFiltro);
                //inputMat.set Cv2(inputMat, primerFiltro, tempOutput,)
                resultados.resultadoPrimerFiltro?.Invoke(tempOutput);
            }
        }
    }

    private class MinMaxSlider : PropertyAttribute
    {
        public readonly float min;
        public readonly float max;
        public MinMaxSlider(float min, float max)
        {
            this.min = min;
            this.max = max;
        }
    }
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(MinMaxSlider))]
    private class MinMaxSliderDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.Vector2 && property.propertyType != SerializedPropertyType.Vector2Int)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            var valActual = property.propertyType == SerializedPropertyType.Vector2 ? property.vector2Value : property.vector2IntValue;
            float minActual = valActual[0];
            float maxActual = valActual[1];

            var minMaxSliderAtt = (MinMaxSlider)attribute;
            using (new EditorGUI.PropertyScope(position, label, property))
            {
                using (var change = new EditorGUI.ChangeCheckScope())
                {
                    EditorGUI.MinMaxSlider(position, label, ref minActual, ref maxActual, minMaxSliderAtt.min, minMaxSliderAtt.max);
                    if (change.changed)
                    {
                        if (property.propertyType == SerializedPropertyType.Vector2)
                            property.vector2Value = new Vector2(minActual, maxActual);
                        else //if (property.propertyType != SerializedPropertyType.Vector2Int)
                            property.vector2IntValue = new Vector2Int((int)minActual, (int)maxActual);
                    }
                }
            }
        }
    }

    [CustomEditor(typeof(SpaceshipFinder2))]
    public class SpaceshipFinderEditor : Editor
    {
        Texture2D texturaInput;
        Texture2D texPrimerFiltro;
        SpaceshipFinder2 _finderTarget;

        private void OnEnable()
        {
            _finderTarget = (SpaceshipFinder2)target;

            if (!texturaInput)
                texturaInput = _finderTarget._inputTexture;
        }

        private void OnDisable()
        {
            AssetDatabase.SaveAssets();

            if (texPrimerFiltro != null)
                DestroyImmediate(texPrimerFiltro);
            texPrimerFiltro = null;
        }

        private void OnPrimerResultado(Mat mat)
        {
            texPrimerFiltro = OpenCvSharp.Unity.MatToTexture(mat, texPrimerFiltro);
            texPrimerFiltro.name = "Primer Filtro";
        }

        public override void OnInspectorGUI()
        {
            using (var changed = new EditorGUI.ChangeCheckScope())
            {
                texturaInput = EditorGUILayout.ObjectField("Input", texturaInput, typeof(Texture2D), allowSceneObjects: false) as Texture2D;
                EditorGUILayout.ObjectField("Resultado", texPrimerFiltro, typeof(Texture2D), allowSceneObjects: false);

                DrawDefaultInspector();

                if (changed.changed)
                {
                    if (texturaInput != null)
                        _finderTarget.ProcesarTextura(texturaInput, new Resultados() { resultadoPrimerFiltro = OnPrimerResultado });
                }
            }
        }
    }
#endif
}
