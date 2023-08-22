using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class CelestialObject : MonoBehaviour {

    // Components
    public ShapeSettings shapeSettings;
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
    private ComputeBuffer uv_buffer;

    bool initialized = false;

#if UNITY_EDITOR
    public void OnEditorEnable() {
        if(!initialized)
            generate();
    }

    public void OnEditorDisable() {
        release_buffers();
    }
#endif

    ~CelestialObject() {
        release_buffers();
    }

    void OnEnable() {
       generate();
    }

    void OnDisable() {
        release_buffers();
    }

    // Public Methods
    public void generate() {
        
        // Delete all children meshes
        foreach (Transform child in transform) {
            DestroyImmediate(child.gameObject);
        }

        generate_mesh();
        apply_noise();

        initialized = true;
    }

    public void OnResolutionChanged() {
        if(!initialized) {
            Debug.Log("Not initialized!");
            return;
        }

        generate_mesh();
        apply_noise();
    }

    public void OnShapeSettingsUpdated() {
        if(!initialized) {
            Debug.Log("Not initialized!");
            return;
        }

        apply_noise();
    }

    public Vector3[] get_vertices() {
        return mesh_filter.sharedMesh.vertices;
    }

    // Private methods
    private void release_buffers() {
        initial_pos_buffer?.Release();
        position_buffer?.Release();
        normal_buffer?.Release();
        uv_buffer?.Release();

        initialized = false;
    }

    private void initialize_mesh_filter() {
        // Create mesh object (later maybe move this into generation, if we want to have more meshes per one celestial object)
        GameObject mesh_obj = new("Mesh");
        mesh_obj.transform.parent = transform;
        mesh_obj.transform.localPosition = Vector3.zero;
        mesh_obj.AddComponent<MeshRenderer>().sharedMaterial = material;

        mesh_filter = mesh_obj.AddComponent<MeshFilter>();
        mesh_filter.sharedMesh = new() {
            indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
        };

        transform.localPosition = Vector3.zero;
    }

    private void generate_mesh() {
        // Initialize mesh filter
        initialize_mesh_filter();

        // Generate unit sphere
        SphereMeshGenerator.construct_mesh(mesh_filter.sharedMesh, (uint) resolution, SphereType);
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
        var old_uv_buffer = uv_buffer;

        // Initialize buffers
        initial_pos_buffer = new ComputeBuffer(vertex_count, 3 * sizeof(float));
        position_buffer = new ComputeBuffer(vertex_count, 3 * sizeof(float));
        normal_buffer = new ComputeBuffer(vertex_count, 3 * sizeof(float));
        uv_buffer = new ComputeBuffer(vertex_count, 2 * sizeof(float));

        // Set initial buffer data
        initial_pos_buffer.SetData(positions, 0, 0, vertex_count);
        position_buffer.SetData(positions, 0, 0, vertex_count);
        normal_buffer.SetData(normals, 0, 0, vertex_count);
        uv_buffer.SetData(uvs, 0, 0, vertex_count);

        // Initialize shape noise settings
        shapeSettings.initialize(initial_pos_buffer, position_buffer, normal_buffer, vertex_count);

        // Set material buffers
        material.SetBuffer("position_buffer", position_buffer);
        material.SetBuffer("normal_buffer", normal_buffer);
        material.SetBuffer("uv_buffer", uv_buffer);

        // Release old buffers if necessary
        old_initial_pos_buffer?.Release();
        old_position_buffer?.Release();
        old_normal_buffer?.Release();
        old_uv_buffer?.Release();
    }

    private void apply_noise() {
        if (shapeSettings == null) {
            Debug.Log("Shape settings not set!");
            return;
        }

        shapeSettings.apply_noise();
        // MeshF.sharedMesh.RecalculateNormals();
    }
}
