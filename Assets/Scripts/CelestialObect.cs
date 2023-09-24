using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode()]
public abstract class CelestialObject : MonoBehaviour
{

    // Components
    public ShapeSettings shapeSettings;
    public SurfaceMaterialSettings surfaceMaterialSettings;

    [SerializeField, HideInInspector]
    protected MeshFilter mesh_filter;

    // Settings
    [HideInInspector]
    public SphereMeshGenerator.SphereType SphereType = SphereMeshGenerator.SphereType.Cube;
    [HideInInspector]
    public int resolution = 100;
    public Material material;

    // Vertex buffers
    private ComputeBuffer initial_pos_buffer;
    private ComputeBuffer position_buffer;
    private ComputeBuffer normal_buffer;
    private ComputeBuffer biome_buffer;
    private ComputeBuffer uv_buffer;

    // Camera shape control
    Transform main_camera_transform;
    private MainCameraShapeController camera_shape_controller;

    private void OnEnable()
    {
        if (mesh_filter == null) return;
        generate_mesh(); apply_noise();
        if (main_camera_transform != null)
            camera_shape_controller.transform_changed += OnCameraTransformChanged;
    }
    private void OnDisable()
    {
        release_buffers();
        if (main_camera_transform != null)
            camera_shape_controller.transform_changed -= OnCameraTransformChanged;
    }

    private void Update()
    {
        if (transform.hasChanged)
        {
            OnTransformChanged();
            transform.hasChanged = false;
        }
        if (!Application.IsPlaying(gameObject))
        {
            rebind_buffers();
            // set_surface_material_info();
        }
    }

    // Public Methods
    public void setup_camera_shape_control(Transform main_camera_transform, MainCameraShapeController camera_shape_controller)
    {
        this.main_camera_transform = main_camera_transform;
        this.camera_shape_controller = camera_shape_controller;
        camera_shape_controller.transform_changed += OnCameraTransformChanged;
    }

    [ContextMenu("initialize")]
    public void Initialize() { initialize_mesh_filter(); generate_mesh(); apply_noise(); }
    public void OnResolutionChanged() { generate_mesh(); apply_noise(); }
    public void OnShapeSettingsUpdated() { apply_noise(); }
    public void OnCameraTransformChanged() { update_view_based_culling(); }
    public void OnTransformChanged() { update_view_based_culling(); }
    public void OnSurfaceMaterialInfoChanged() { set_surface_material_info(); }

    // Private methods
    private void release_buffers()
    {
        initial_pos_buffer?.Release();
        position_buffer?.Release();
        normal_buffer?.Release();
        biome_buffer?.Release();
        uv_buffer?.Release();

        // initialized = false;
    }

    private void rebind_buffers()
    {
        material.SetBuffer("position_buffer", position_buffer);
        material.SetBuffer("normal_buffer", normal_buffer);
        material.SetBuffer("biome_buffer", biome_buffer);
        material.SetBuffer("uv_buffer", uv_buffer);
    }

    private void set_surface_material_info()
    {
        if (surfaceMaterialSettings == null) return;
        // Set textures
        material.SetTexture("_DiffuseMaps", surfaceMaterialSettings.get_diffuse_map());
        material.SetTexture("_NormalMaps", surfaceMaterialSettings.get_normal_map());
        material.SetTexture("_OcclusionMaps", surfaceMaterialSettings.get_occlusion_map());
        // Set surface texture settings
        material.SetFloatArray("_MapScale", surfaceMaterialSettings.get_scale());
        material.SetFloatArray("_NormalStrength", surfaceMaterialSettings.get_normal_strength());
        material.SetFloatArray("_OcclusionStrength", surfaceMaterialSettings.get_occlusion_strength());
        material.SetFloatArray("_Metallic", surfaceMaterialSettings.get_metallic());
        material.SetFloatArray("_Glossiness", surfaceMaterialSettings.get_glossiness());
        material.SetColorArray("_Color", surfaceMaterialSettings.get_colors());
    }

    private void initialize_mesh_filter()
    {
        // Delete all children meshes
        foreach (Transform child in transform)
            DestroyImmediate(child.gameObject);

        // Create mesh object (later maybe move this into generation, if we want to have more meshes per one celestial object)
        GameObject mesh_obj = new("Mesh");
        mesh_obj.transform.parent = transform;
        mesh_obj.transform.localPosition = Vector3.zero;
        mesh_obj.AddComponent<MeshRenderer>().sharedMaterial = material;

        mesh_filter = mesh_obj.AddComponent<MeshFilter>();
        mesh_filter.sharedMesh = new()
        {
            indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
        };

        transform.localPosition = Vector3.zero;
    }

    private void generate_mesh()
    {
        // Generate unit sphere
        SphereMeshGenerator.construct_mesh(mesh_filter.sharedMesh, (uint)resolution, SphereType);
        mesh_filter.sharedMesh.RecalculateNormals();

        // Get mesh data
        var positions = mesh_filter.sharedMesh.vertices;
        var normals = mesh_filter.sharedMesh.normals;
        var uvs = mesh_filter.sharedMesh.uv;
        var vertex_count = positions.Length;

        // Keep reference to old buffers
        var old_initial_pos_buffer = initial_pos_buffer;
        var old_position_buffer = position_buffer;
        var old_normal_buffer = normal_buffer;
        var old_biome_buffer = biome_buffer;
        var old_uv_buffer = uv_buffer;

        // Initialize buffers
        initial_pos_buffer = new ComputeBuffer(vertex_count, 3 * sizeof(float));
        position_buffer = new ComputeBuffer(vertex_count, 3 * sizeof(float));
        normal_buffer = new ComputeBuffer(vertex_count, 3 * sizeof(float));
        biome_buffer = new ComputeBuffer(vertex_count / 4 + 1, sizeof(uint));
        uv_buffer = new ComputeBuffer(vertex_count, 2 * sizeof(float));

        // Set initial buffer data
        initial_pos_buffer.SetData(positions, 0, 0, vertex_count);
        position_buffer.SetData(positions, 0, 0, vertex_count);
        normal_buffer.SetData(normals, 0, 0, vertex_count);
        uv_buffer.SetData(uvs, 0, 0, vertex_count);

        // Initialize shape noise settings
        shapeSettings.initialize(transform, initial_pos_buffer, position_buffer, normal_buffer, biome_buffer, vertex_count);

        // Initialize culling
        if (main_camera_transform != null)
            shapeSettings.setup_view_based_culling(main_camera_transform);

        // Set material buffers
        rebind_buffers();

        // Release old buffers if necessary
        old_initial_pos_buffer?.Release();
        old_position_buffer?.Release();
        old_normal_buffer?.Release();
        old_biome_buffer?.Release();
        old_uv_buffer?.Release();

        // Set surface material info
        set_surface_material_info();
    }

    private void apply_noise()
    {
        if (shapeSettings == null)
            throw new UnityException("Error in :: CelestialObject :: apply_noise :: Shape settings not set!");
        shapeSettings.apply_noise();
    }

    private void update_view_based_culling()
    {
        if (main_camera_transform == null) return;
        if (shapeSettings == null)
            throw new UnityException("Error in :: CelestialObject :: OnCameraTransformChanged :: Shape settings not set!");
        shapeSettings.update_view_based_culling();
    }
}
