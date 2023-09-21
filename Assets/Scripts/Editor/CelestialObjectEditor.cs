using UnityEngine;
using UnityEditor;
[CustomEditor(typeof(CelestialObject))]
public class CelestialObjectEditor : Editor {

    protected CelestialObject CO;
    SerializedObject CO_serialized;

    // sub-editors
    Editor shape_editor;

    private void OnEnable() {
        CO = (CelestialObject) target;
        CO_serialized = new SerializedObject(CO);
    }

    protected void draw_settings_editor(ref Editor editor, Object settings, System.Action on_resolution_updated, System.Action on_settings_updated) {
        if (settings == null) return;

        int min_resolution = 1;
        int max_resolution = 10;
        switch (CO.SphereType) {
            case SphereMeshGenerator.SphereType.Spiral:
                min_resolution = 20;
                max_resolution = 1000000;
                break;
            case SphereMeshGenerator.SphereType.Cube:
                min_resolution = 2;
                max_resolution = 1000;
                break;
            case SphereMeshGenerator.SphereType.Tile:
                min_resolution = 2;
                max_resolution = 50;
                break;
        }

        SerializedProperty resolution_property = CO_serialized.FindProperty("resolution");
        EditorGUILayout.IntSlider(resolution_property, min_resolution, max_resolution);

        if (resolution_property.intValue != CO.resolution) {
            CO.resolution = resolution_property.intValue;
            on_resolution_updated?.Invoke();
        }

        using (var check = new EditorGUI.ChangeCheckScope()) {
            EditorGUILayout.InspectorTitlebar(true, settings);
            CreateCachedEditor(settings, null, ref editor);
            editor.OnInspectorGUI();

            if (check.changed) {
                on_settings_updated?.Invoke();
            }
        }

        CO_serialized.ApplyModifiedProperties();
    }

    public override void OnInspectorGUI() {
        using (var check = new EditorGUI.ChangeCheckScope()) {
            base.OnInspectorGUI();
        }

        if (CO == null)
            CO = (CelestialObject) target;
        CO_serialized ??= new SerializedObject(CO);

        draw_settings_editor(ref shape_editor, CO.shapeSettings, CO.OnResolutionChanged, CO.OnShapeSettingsUpdated);
    }
}
