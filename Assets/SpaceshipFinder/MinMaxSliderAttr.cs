using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

    public enum TipoHue
    {
        HSV, HLS
    }

public class MinMaxSlider : PropertyAttribute
{
    public readonly float min;
    public readonly float max;
    public MinMaxSlider(float min, float max)
    {
        this.min = min;
        this.max = max;
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
#endif
}