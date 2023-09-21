using UnityEngine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.VisualScripting.Dependencies.NCalc;

public class CubeSphereMesh {
    public static SphereMesh construct_mesh(uint resolution) {
        Stopwatch sw = new();

        sw.Start();
        var cube_sphere = construct_cube(resolution);
        map_to_unit_sphere(cube_sphere);
        sw.Stop();

        // Elapsed = 00:00:03.0389022 // for side
        // Elapsed = 00:00:02.9811514 // side for
        // Elapsed = 00:00:00.3820955 // Limited (side for)
        // UnityEngine.Debug.Log("Elapsed = " + sw.Elapsed);

        return cube_sphere;
    }

    // Private functions
    private static SphereMesh construct_cube(uint resolution) {
        // 6 sides - 12 edges (coz of the overlap of sides) + 8 corners (coz of the overlap of edges)
        uint vertex_count = 6 * resolution * resolution - 12 * resolution + 8;
        // one side has 2 * (res - 1) triangles, 6 sides total
        uint triangle_count = 6 * 2 * (resolution - 1) * (resolution - 1);

        // Initialize arrays
        Vector3[] vertices = new Vector3[vertex_count];
        int[] indices = new int[3 * triangle_count];

        uint v_index = 0;
        uint i_index = 0;

        // Construct sides
        Func<float, float, Vector3>[] sides = {
            (a, b) => { return new( 1,  a,  b); },
            (a, b) => { return new(-1,  b,  a); },
            (a, b) => { return new( b,  1,  a); },
            (a, b) => { return new( a, -1,  b); },
            (a, b) => { return new( a,  b,  1); },
            (a, b) => { return new( b,  a, -1); }
        };

        // Keep track of unique vertices; Only used for edges
        Dictionary<Vector3, int> unique_edge_vertices = new();


        // Add all vertices & indices
        int[,] side_indices = new int[resolution, resolution];
        foreach (var side in sides) {
            // Iterate and add all vertices
            for (int i = 0; i < resolution; i++) {
                float x = 2.0f * i / (resolution - 1) - 1.0f;
                for (int j = 0; j < resolution; j++) {
                    float y = 2.0f * j / (resolution - 1) - 1.0f;

                    // Compute vertex at this position
                    Vector3 vertex = side(x, y);
                    int index = (int) v_index;

                    // If this is an edge vertex it could possible be a duplicate from some other side
                    if (i * j * (resolution - 1 - i) * (resolution - 1 - j) == 0) {
                        // If Vertex already included just return it
                        if (unique_edge_vertices.ContainsKey(vertex))
                            index = unique_edge_vertices[vertex];
                        // New vertex, so add it to the list of unique vertices
                        else {
                            vertices[v_index++] = vertex;
                            unique_edge_vertices.Add(vertex, index);
                        }
                    }
                    // Otherwise just add it
                    else vertices[v_index++] = vertex;

                    // Add vertex index at this position
                    side_indices[i, j] = index;
                }
            }

            // Callback for adding triangles in index list, per quad
            // (i, j) -> quad corner
            // (i + p, j + q) -> opposite quad corner
            void add_quad(int i, int j, int p, int q) {
                // Get quad
                int i1 = side_indices[i + 0, j + 0]; // Bottom left
                int i2 = side_indices[i + 0, j + q]; // Bottom right
                int i3 = side_indices[i + p, j + 0]; // Top left
                int i4 = side_indices[i + p, j + q]; // Top right

                // This makes sure that triangle are oriented CCW
                if (p * q < 0)
                    (i3, i2) = (i2, i3);

                // Add indices
                indices[i_index++] = i1;
                indices[i_index++] = i3;
                indices[i_index++] = i4;
                indices[i_index++] = i2;
                indices[i_index++] = i1;
                indices[i_index++] = i4;
            }

            // Iterate and add all indices
            // We start from corners and move slowly to the middle
            uint half_point = (resolution % 2 == 1) ? resolution / 2 : resolution / 2 - 1;
            for (int i = 0; i < half_point; i++) {
                int inv_i = (int) resolution - i - 1;
                for (int j = 0; j < half_point; j++) {
                    int inv_j = (int) resolution - j - 1;
                    add_quad(i, j, 1, 1);
                    add_quad(i, inv_j, 1, -1);
                    add_quad(inv_i, j, -1, 1);
                    add_quad(inv_i, inv_j, -1, -1);
                }
            }

            // If resolution is even (odd quad count per side) we need additional code for enclosing central hole (1 quad wide)
            if (resolution % 2 == 0) {
                int h = (int) half_point;
                int inv_h = h + 1;
                for (int k = 0; k < half_point; k++) {
                    int inv_k = (int) resolution - k - 1;
                    add_quad(h, k, 1, 1);
                    add_quad(k, h, 1, 1);
                    add_quad(inv_h, inv_k, -1, -1);
                    add_quad(inv_k, inv_h, -1, -1);
                }
                add_quad(h, h, 1, 1);
            }
        }

        return new(indices, vertices);
    }

    public static Vector3 map_point_to_sphere(Vector3 point) {
        var x_sq = point.x * point.x;
        var y_sq = point.y * point.y;
        var z_sq = point.z * point.z;

        point.x *= Mathf.Sqrt(1.0f - y_sq / 2.0f - z_sq / 2.0f + y_sq * z_sq / 3.0f);
        point.y *= Mathf.Sqrt(1.0f - z_sq / 2.0f - x_sq / 2.0f + z_sq * x_sq / 3.0f);
        point.z *= Mathf.Sqrt(1.0f - x_sq / 2.0f - y_sq / 2.0f + x_sq * y_sq / 3.0f);
        
        return point;
    }

    private static void map_to_unit_sphere(SphereMesh sphere) {
        for (int i = 0; i < sphere.vertices.Length; i++) {
            var v = sphere.vertices[i];

            var x_sq = v.x * v.x;
            var y_sq = v.y * v.y;
            var z_sq = v.z * v.z;

            sphere.vertices[i].x *= Mathf.Sqrt(1.0f - y_sq / 2.0f - z_sq / 2.0f + y_sq * z_sq / 3.0f);
            sphere.vertices[i].y *= Mathf.Sqrt(1.0f - z_sq / 2.0f - x_sq / 2.0f + z_sq * x_sq / 3.0f);
            sphere.vertices[i].z *= Mathf.Sqrt(1.0f - x_sq / 2.0f - y_sq / 2.0f + x_sq * y_sq / 3.0f);

            sphere.vertices[i].Normalize();
        }
    }
}
