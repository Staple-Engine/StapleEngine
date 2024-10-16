﻿using System;

namespace Staple.Editor;

[CustomPropertyDrawer(typeof(RangeAttribute))]
internal class RangePropertyDrawer : PropertyDrawer
{
    public override void OnGUI(string name, Func<object> getter, Action<object> setter, Func<Type, object> getAttribute)
    {
        var range = getAttribute(typeof(RangeAttribute)) as RangeAttribute;

        var value = getter();

        if(value is int i)
        {
            var newValue = EditorGUI.IntSlider(name, $"Range{name}Value", i, (int)range.minValue, (int)range.maxValue);

            if(newValue != i)
            {
                setter(newValue);
            }
        }
        else if (value is uint u)
        {
            var newValue = (uint)EditorGUI.IntSlider(name, $"Range{name}Value", (int)u, (int)range.minValue, (int)range.maxValue);

            if (newValue != u)
            {
                setter(newValue);
            }
        }
        else if (value is float f)
        {
            var newValue = EditorGUI.FloatSlider(name, $"Range{name}Value", f, (int)range.minValue, (int)range.maxValue);

            if (newValue != f)
            {
                setter(newValue);
            }
        }
    }
}
