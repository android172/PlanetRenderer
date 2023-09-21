using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CelestialObjectGenerator : MonoBehaviour {

    public enum COType {
        Asteroid,
        Moon,
        RockyDryPlanet,
        RockyWetPlanet,
        GasPlanet,
        Star
    }

    public AsteroidShapeSettings defaultAsteroidShapeSettings;
    public RockyPlanetShapeSettings defaultMoonShapeSettings;
    public RockyPlanetShapeSettings defaultRockyPlanetDryShapeSettings;
    public RockyPlanetShapeSettings defaultRockyPlanetWetShapeSettings;
    public OceanShapeSettings defaultOceanShapeSettings;

    [SerializeField]
    Material surfaceMaterial;
    [SerializeField]
    Material oceanMaterial;
    [SerializeField]
    Material starMaterial;

    public string objectName = "Celestial Object";
    public COType objectType = COType.Asteroid;
    [Min(0.5f)]
    public float objectRadius = 1f;

    public SphereMeshGenerator.SphereType sphereType = SphereMeshGenerator.SphereType.Cube;
    public int sphereResolution = 100;

    public void generate_object() {
        // Generate object
        GameObject celestial_body = new(objectName);

        // Get main camera
        GameObject main_camera = GameObject.Find("Main Camera");
        var camera_shape_controller = main_camera.GetComponent<MainCameraShapeController>();

        // We are sure to have a surface, but...
        GameObject surface = new("surface");
        surface.transform.SetParent(celestial_body.transform);

        // If object is has no surface
        if (objectType == COType.Star) {
            // star script
            StarSphere starS = surface.AddComponent<StarSphere>();
            // resolution
            starS.SphereType = sphereType;
            starS.resolution = sphereResolution;
            // material
            starS.material = starMaterial;
            // radius
            starS.Radius = objectRadius;
            starS.OnRadiusUpdate();
            // tag
            starS.gameObject.tag = "StarSurface";
            // initialize
            starS.Initialize();

            // light
            GameObject light = Instantiate(Resources.Load<GameObject>("Starlight"));
            light.transform.SetParent(celestial_body.transform);
            light.name = objectName + " " + "Light";

            return;
        }
        if (objectType == COType.GasPlanet) {
            return;
        }

        // if object is solid
        // planet script
        Planet planetS = surface.AddComponent<Planet>();
        // resolution
        planetS.SphereType = sphereType;
        planetS.resolution = sphereResolution;
        // material
        planetS.material = new(surfaceMaterial);
        // Camera callback
        planetS.setup_camera_shape_control(main_camera.GetComponent<Camera>(), camera_shape_controller);
        // shape
        switch (objectType) {
            case COType.Asteroid:
                planetS.shapeSettings = ScriptableObject.CreateInstance<AsteroidShapeSettings>();
                planetS.shapeSettings.set_settings(defaultAsteroidShapeSettings);
                break;
            case COType.Moon:
                planetS.shapeSettings = ScriptableObject.CreateInstance<RockyPlanetShapeSettings>();
                planetS.shapeSettings.set_settings(defaultMoonShapeSettings);
                break;
            case COType.RockyDryPlanet:
                planetS.shapeSettings = ScriptableObject.CreateInstance<RockyPlanetShapeSettings>();
                planetS.shapeSettings.set_settings(defaultRockyPlanetDryShapeSettings);
                break;
            case COType.RockyWetPlanet:
                planetS.shapeSettings = ScriptableObject.CreateInstance<RockyPlanetShapeSettings>();
                planetS.shapeSettings.set_settings(defaultRockyPlanetWetShapeSettings);
                break;
        }
        planetS.shapeSettings.radius = objectRadius;
        planetS.shapeSettings.randomize_seed();
        // tag
        planetS.gameObject.tag = "Surface";
        // initialize
        planetS.Initialize();

        // if object has an ocean
        if (objectType == COType.RockyWetPlanet) {
            GameObject ocean = new("ocean");
            // ocean script
            OceanSphere oceanS = ocean.AddComponent<OceanSphere>();
            // resolution
            oceanS.SphereType = sphereType;
            oceanS.resolution = sphereResolution;
            // material
            oceanS.material = oceanMaterial;
            // shape
            oceanS.shapeSettings = ScriptableObject.CreateInstance<OceanShapeSettings>();
            oceanS.shapeSettings.set_settings(defaultOceanShapeSettings);
            oceanS.shapeSettings.radius = objectRadius;
            oceanS.shapeSettings.randomize_seed();
            // tag
            oceanS.gameObject.tag = "Ocean";
            // initialize
            oceanS.Initialize();

            // Set colors
            // oceanS.set_mesh_wave_color_mask(planetS.get_vertices(), 8);

            ocean.transform.SetParent(celestial_body.transform);
        }
    }
}
