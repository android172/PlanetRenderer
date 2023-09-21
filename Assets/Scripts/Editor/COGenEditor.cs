using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CelestialObjectGenerator))]
public class COGenEditor : Editor {
    CelestialObjectGenerator COG;
    SerializedObject COG_serialized;

    private void OnEnable() {
        COG = (CelestialObjectGenerator) target;
        COG_serialized = new SerializedObject(COG);
    }

    public override void OnInspectorGUI() {
        // Default noise settings
        EditorGUILayout.LabelField("Default Noise Settings");
        switch (COG.objectType) {
            case CelestialObjectGenerator.COType.Asteroid:
                EditorGUILayout.PropertyField(COG_serialized.FindProperty("defaultAsteroidShapeSettings"), true);
                break;
            case CelestialObjectGenerator.COType.Moon:
                EditorGUILayout.PropertyField(COG_serialized.FindProperty("defaultMoonShapeSettings"), true);
                break;
            case CelestialObjectGenerator.COType.RockyDryPlanet:
                EditorGUILayout.PropertyField(COG_serialized.FindProperty("defaultRockyPlanetDryShapeSettings"), true);
                break;
            case CelestialObjectGenerator.COType.RockyWetPlanet:
                EditorGUILayout.PropertyField(COG_serialized.FindProperty("defaultRockyPlanetWetShapeSettings"), true);
                EditorGUILayout.PropertyField(COG_serialized.FindProperty("defaultOceanShapeSettings"), true);
                break;
        }

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Sphere generation");
        EditorGUILayout.PropertyField(COG_serialized.FindProperty("sphereType"));

        int min_resolution = 1;
        int max_resolution = 10;
        switch (COG.sphereType) {
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
        if (COG.sphereResolution < min_resolution) COG.sphereResolution = min_resolution;
        if (COG.sphereResolution > max_resolution) COG.sphereResolution = max_resolution;
        EditorGUILayout.IntSlider(COG_serialized.FindProperty("sphereResolution"), min_resolution, max_resolution);

        EditorGUILayout.PropertyField(COG_serialized.FindProperty("objectRadius"), new("Radius"), true);

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Materials Used");
        if (COG.objectType == CelestialObjectGenerator.COType.GasPlanet) {
        } else if (COG.objectType == CelestialObjectGenerator.COType.Star) {
            EditorGUILayout.PropertyField(COG_serialized.FindProperty("starMaterial"), true);
        } else {
            EditorGUILayout.PropertyField(COG_serialized.FindProperty("surfaceMaterial"), true);
            if (COG.objectType == CelestialObjectGenerator.COType.RockyWetPlanet)
                EditorGUILayout.PropertyField(COG_serialized.FindProperty("oceanMaterial"), true);
        }

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Object Settings");

        EditorGUILayout.PropertyField(COG_serialized.FindProperty("objectName"), true);
        EditorGUILayout.PropertyField(COG_serialized.FindProperty("objectType"), true);

        if (GUILayout.Button("Generate")) {
            COG.generate_object();
        }

        COG_serialized.ApplyModifiedProperties();
    }
}
