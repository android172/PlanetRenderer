// Standard shader with triplanar mapping
// https://github.com/keijiro/StandardTriplanar

using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class RockyPlanetSurfaceInspector : ShaderGUI {
    static class Styles {
        static public readonly GUIContent albedo = new("Albedo", "Albedo (RGB)");
        static public readonly GUIContent normalMap = new("Normal Map", "Normal Map");
        static public readonly GUIContent occlusion = new("Occlusion", "Occlusion (G)");
        static public readonly GUIContent macro_var = new("Macro variation", "Macro variation");
    }

    public override void OnGUI(MaterialEditor editor, MaterialProperty[] props) {
        EditorGUI.BeginChangeCheck();

        var t_macro_var = FindProperty("_MacroVariation", props);
        editor.TexturePropertySingleLine(
            Styles.macro_var, t_macro_var,
            t_macro_var.textureValue ? FindProperty("_MacroVariation", props) : null
        );

        EditorGUI.EndChangeCheck();
    }
}