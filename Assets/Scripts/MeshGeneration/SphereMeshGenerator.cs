using UnityEngine;

public class SphereMesh {
    public int[] indices;
    public Vector3[] vertices;

    public SphereMesh() {
        vertices = null;
        indices = null;
    }
    public SphereMesh(int[] indices, Vector3[] vertices) {
        this.indices = indices;
        this.vertices = vertices;
    }
}

public static class SphereMeshGenerator {
    //  Sphere type
    public enum SphereType {
        Spiral,
        Cube,
        Tile
    }

    // Flat tile mesh for instanced rendering with quad trees with resolution NxN and unit side lengths, centered around the origin
    private static SphereMesh construct_tile(uint resolution) {
        SphereMesh tile_mesh = new SphereMesh();

        // Initialize arrays
        uint vertex_count = resolution * resolution;
        uint triangle_count = 2 * (resolution - 1) * (resolution - 1);
        tile_mesh.vertices = new Vector3[vertex_count];
        tile_mesh.indices = new int[3 * triangle_count];

        // Add vertices
        float resolution_float = resolution;
        for(int j = 0; j < resolution; j++) {
            for(int i = 0; i < resolution; i++) {
                float x_pos = i - (resolution_float - 1) / 2.0f;
                float z_pos = j - (resolution_float - 1) / 2.0f;
                tile_mesh.vertices[j * resolution + i] = new Vector3(x_pos, 0, z_pos);
            }
        }

        // Add indices
        int resolution_int = (int) resolution;
        for(int j = 0; j < resolution - 1; j++) {
            for(int i = 0; i < resolution - 1; i++) {
                int index = 6 * (j * (resolution_int - 1) + i);
                tile_mesh.indices[index + 0] = j * resolution_int + i;
                tile_mesh.indices[index + 1] = (j + 1) * resolution_int + i;
                tile_mesh.indices[index + 2] = j * resolution_int + i + 1;
                tile_mesh.indices[index + 3] = j * resolution_int + i + 1;
                tile_mesh.indices[index + 4] = (j + 1) * resolution_int + i;
                tile_mesh.indices[index + 5] = (j + 1) * resolution_int + i + 1;
            }
        }
        
        return tile_mesh;
    }


    // Provided functionality

    /// <summary>
    /// Construct unity sphere with given resolution.
    /// </summary>
    /// <param name="target_mesh">Mesh in which to store vertex and index data generated</param>
    /// <param name="resolution">Resolution of the unit sphere.</param>
    /// <param name="type">Type of sphere being generated. Determines generation algorithm</param>
    /// <exception cref="System.Exception">If generation algorithm for the given type is unimplemented.</exception>
    public static void construct_mesh(Mesh target_mesh, uint resolution, SphereType type = SphereType.Cube) {
        // Construct unit sphere
        SphereMesh unit_sphere;
        switch (type) {
            case SphereType.Spiral:
                unit_sphere = SpiralSphereMesh.construct_mesh(resolution);
                break;
            case SphereType.Cube:
                unit_sphere = CubeSphereMesh.construct_mesh(resolution);
                break;
            case SphereType.Tile:
                unit_sphere = construct_tile(resolution);
                break;
            default:
                throw new System.Exception("Unimplemented.");
        }

        // Transform into mesh
        target_mesh.Clear();
        target_mesh.vertices = unit_sphere.vertices;
        target_mesh.triangles = unit_sphere.indices;
        target_mesh.SetUVs(0, unit_sphere.vertices);
    }
}
