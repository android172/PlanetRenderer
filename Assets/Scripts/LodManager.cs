using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class NodeCodeUtil {

    private static uint undilate(uint x) {
        x = (x | (x >> 1)) & 0x33333333;
        x = (x | (x >> 2)) & 0x0f0f0f0f;
        x = (x | (x >> 4)) & 0x00ff00ff;
        x = (x | (x >> 8)) & 0x0000ffff;
        return x & 0x0000ffff;
    }

    public static void decode(uint node_code, ref uint level, ref uint[] coords) {
        level = node_code & 0xf;
        coords[0] = undilate((node_code >> 4) & 0x05555555);
        coords[1] = undilate((node_code >> 5) & 0x05555555);
    }

    public static uint[] generate_children(uint node_code) {
        node_code = (++node_code & 0xf) | ((node_code & ~((uint)0xf)) << 2);
        
        uint[] ret = new uint[4];
        ret[0] = node_code;
        ret[1] = node_code | 0x10;
        ret[2] = node_code | 0x20;
        ret[3] = node_code | 0x30;

        return ret;
    }
}

public class LodQuadTree {

    // Perfectly spaced quadtree will have 3 * max_level + 1 nodes but theoretically we can have 4^max_level nodes
    // Since we are using uint32 for the nodes we can have a max lod level of 15.
    public static readonly int MAX_NUM_NODES = 3 * 15 + 1;
    public static readonly uint ROOT_NODE_CODE = 0;

    public LodQuadTree(uint node_code, Vector3 node_center, LodQuadTree parent) {
        this.node_code = node_code;
        this.node_center = node_center;
        this.parent = parent;
    }

    public List<LodQuadTree> GetAllChildren() {
        List<LodQuadTree> ret = new List<LodQuadTree>();
        
        if(children != null) {
            foreach(LodQuadTree child in children) {
                ret.Add(child);
                children.AddRange(child.GetAllChildren());
            }
        }

        return ret;
    }

    public List<uint> GetAllChildrenIndices() {
        List<uint> ret = new List<uint>();
        
        if(children != null) {
            foreach(LodQuadTree child in children) {
                ret.Add(child.node_code);
                children.AddRange(child.GetAllChildrenIndices());
            }
        }

        return ret;
    }

    public void Split(float sphere_radius, Vector3 face_center) {
        children = new LodQuadTree[4];
        uint[] children_codes = NodeCodeUtil.generate_children(node_code);

        for(int i = 0; i < 4; i++) {
            uint[] coords = new uint[2];
            uint level = 0;
            NodeCodeUtil.decode(children_codes[i], ref level, ref coords);

            // Get center on -1 to 1 grid
            float unit_cube_x = -1.0f + 2.0f * coords[0] / Mathf.Pow(2, level);
            float unit_cube_y = -1.0f + 2.0f * coords[1] / Mathf.Pow(2, level);

            // Get center on unit cube
            Vector3 child_center_unit_cube = new Vector3();
            if(face_center.x != 0.0f) {
                // Right or left face
                child_center_unit_cube = new Vector3(face_center.x, unit_cube_x, unit_cube_y);
            } else if(face_center.y != 0.0f) {
                // Top or bottom face
                child_center_unit_cube = new Vector3(unit_cube_x, face_center.y, unit_cube_y);
            } else if(face_center.z != 0.0f) {
                // Front or back face
                child_center_unit_cube = new Vector3(unit_cube_x, unit_cube_y, face_center.z);
            }

            // Get center on sphere
            Vector3 child_center = CubeSphereMesh.map_point_to_sphere(child_center_unit_cube).normalized * sphere_radius;

            children[i] = new LodQuadTree(children_codes[i], child_center, this);
        }
    }

    // Node centers are being stored as if they were on the unit sphere
    public Vector3 node_center;
    public uint node_code;
    public LodQuadTree[] children;
    public LodQuadTree parent;
}

// This struct is passed to the compute shader with a list of changes which need to be recomputed
public struct LodBufferLayoutChanges {
    public uint new_node_code;
    public uint offset;
    public int face_number; // 0-5 for 6 sides of cube
}

public class LodBufferLayout {

    public LodBufferLayout() {
        layout = new uint[LodQuadTree.MAX_NUM_NODES];
        layout[0] = LodQuadTree.ROOT_NODE_CODE;
        first_free_index = 1;
    }

    public List<LodBufferLayoutChanges> UpdateIndices(List<uint> new_indices, List<LodBufferLayoutChanges> changes, int face_number = 0) {
        // First free unused indices
        for(uint i = 0; i < layout.Length; i++) {
            if(!new_indices.Contains(layout[i]) && layout[i] != 0) {
                layout[i] = 0;

                changes.Add(new LodBufferLayoutChanges {
                    new_node_code = 0,
                    offset = i,
                    face_number = face_number
                });

                if(i < first_free_index)
                    first_free_index = i;
            }
        }

        // Then allocate new ones
        foreach(uint index in new_indices) {
            // In case we run out of space don't add more
            if(first_free_index >= layout.Length)
                break;

            if(!Array.Exists<uint>(layout, element => element == index)) {
                layout[first_free_index] = index;

                changes.Add(new LodBufferLayoutChanges {
                    new_node_code = index,
                    offset = first_free_index,
                    face_number = face_number
                });

                // Find next free index
                while(first_free_index < layout.Length) {
                    if(layout[first_free_index] == 0)
                        break;
                    first_free_index++;
                }
            }
        }

        return changes;
    }

    public uint[] layout;
    public uint first_free_index;
}

public class LodManager {

    public static readonly int NUM_QUAD_TREES = 6;
    public static readonly int LOD_DISTANCE_SCALE = 16;

    // THIS ORDER IS IMPORTANT! ALL VERTEX DATA IN THE BUFFERS
    // WILL BE ASSUMED TO BE IN THIS ORDER
    private static readonly Vector3[] node_centers = {
        new Vector3(0.0f, 0.0f, 1.0f),  // Front
        new Vector3(0.0f, 0.0f, -1.0f), // Back
        new Vector3(0.0f, 1.0f, 0.0f),  // Top
        new Vector3(0.0f, -1.0f, 0.0f), // Bottom
        new Vector3(1.0f, 0.0f, 0.0f),  // Right
        new Vector3(-1.0f, 0.0f, 0.0f)  // Left
    };

    public LodManager(ComputeShader compute_shader, uint index_count_per_instance, int vertex_count_per_instance) {
        lod_shader = compute_shader;
        lod_kernel_id = lod_shader.FindKernel("lod_kernel");

        this.vertex_count_per_instance = vertex_count_per_instance;
        
        // Setup quad trees and buffer layouts
        lod_quad_trees = new LodQuadTree[NUM_QUAD_TREES];
        lod_buffer_layouts = new LodBufferLayout[NUM_QUAD_TREES];

        for (int i = 0; i < NUM_QUAD_TREES; i++) {
            lod_quad_trees[i] = new LodQuadTree(LodQuadTree.ROOT_NODE_CODE, node_centers[i], null);
            lod_buffer_layouts[i] = new LodBufferLayout();
        }

        total_node_count = 6;

        // Lod layout changes buffer
        lod_buffer_layout_buffer_changes = new ComputeBuffer(LodQuadTree.MAX_NUM_NODES * 6, sizeof(uint) * 2 + sizeof(int));
        lod_shader.SetBuffer(lod_kernel_id, "lod_layout_changes", lod_buffer_layout_buffer_changes);

        // Lod layout buffer
        lod_layout_buffer = new ComputeBuffer(LodQuadTree.MAX_NUM_NODES * 6, sizeof(uint));
        lod_shader.SetBuffer(lod_kernel_id, "lod_layout", lod_layout_buffer);

        // Setup shader constants
        lod_shader.SetInt("index_count_per_instance", (int) index_count_per_instance);
        lod_shader.SetInt("MAX_NUM_NODES", LodQuadTree.MAX_NUM_NODES);
    }

    private float s(float z, float alpha) {
        return 2.0f * z * Mathf.Tan(alpha / 2.0f);
    }

    private void merge_quad_trees(Vector3 camera_pos, float camera_fov, float sphere_radius) {
        for(int i = 0; i < NUM_QUAD_TREES; i++) {
            Queue<LodQuadTree> queue = new Queue<LodQuadTree>();
            
            if(lod_quad_trees[i].children == null)
                continue;

            // Root node cannot be merged so we start from children
            foreach(LodQuadTree child in lod_quad_trees[i].children)
                queue.Enqueue(child);

            while(queue.Count > 0) {
                LodQuadTree node = queue.Dequeue();

                float node_size = Vector3.Distance(node.parent.node_center, node.node_center);

                // Check if node should be merged by distance 
                // TODO: include resolution of tile and maybe s()
                float distance_to_node_center = Vector3.Distance(camera_pos, node.node_center);
                if(node_size * LOD_DISTANCE_SCALE < distance_to_node_center) {
                    // merge
                    node.parent.children = null;

                    // update total node count in lod manager
                    total_node_count -= 3;
                
                }else {
                    // Add children to queue
                    if(node.children != null) {
                        foreach(LodQuadTree child in node.children)
                            queue.Enqueue(child);
                    }
                }
            }
        }
    }

    private void split_quad_trees(Vector3 camera_pos, float camera_fov, float sphere_radius) {
        for(int i = 0; i < NUM_QUAD_TREES; i++) {
            Queue<LodQuadTree> queue = new Queue<LodQuadTree>();
            queue.Enqueue(lod_quad_trees[i]);

            while(queue.Count > 0) {
                LodQuadTree node = queue.Dequeue();

                if(node.children != null) {
                    // Add children to queue
                    foreach(LodQuadTree child in node.children)
                        queue.Enqueue(child);
                    continue;
                }

                float node_size;
                if(node.parent == null) {
                    // 2PI r * arctan(sqrt(2)) / 2PI
                    node_size = 0.9553f * sphere_radius;
                } else {
                    node_size = Vector3.Distance(node.parent.node_center, node.node_center);
                }

                // Check if node should be split by distance 
                // TODO: include resolution of tile and maybe s()
                float distance_to_node_center = Vector3.Distance(camera_pos, node.node_center);
                if(node_size * LOD_DISTANCE_SCALE > distance_to_node_center) {
                    // split
                    node.Split(sphere_radius, node_centers[i]);

                    // update total node count in lod manager
                    total_node_count += 3;
                }
            }
        }
    }

    public void run_lod_kernels(Camera camera, Transform planet_transform, float sphere_radius) {

        // Camera position in planet coordinate space
        Vector3 camera_pos = planet_transform.InverseTransformPoint(camera.transform.position);

        // Split/merge quad trees
        // merge first then split to limit buffer fragmentation
        merge_quad_trees(camera_pos, camera.fieldOfView, sphere_radius);
        split_quad_trees(camera_pos, camera.fieldOfView, sphere_radius);

        // Update lod buffer layouts
        List<LodBufferLayoutChanges> changes = new List<LodBufferLayoutChanges>();
        for(int i = 0; i < NUM_QUAD_TREES; i++) {
            List<uint> indices = lod_quad_trees[i].GetAllChildrenIndices();
            lod_buffer_layouts[i].UpdateIndices(indices, changes, i);
        }

        if(changes.Count == 0)
            return;

        // Update compute layout changes buffer
        lod_buffer_layout_buffer_changes.SetData(changes);

        // Set constants
        lod_shader.SetFloat("sphere_radius", sphere_radius);
        lod_shader.SetInt("num_changes", changes.Count);

        // Dispatch
        uint thread_x, thread_y, thread_z;
        lod_shader.GetKernelThreadGroupSizes(lod_kernel_id, out thread_x, out thread_y, out thread_z);
        thread_x = (uint) Mathf.CeilToInt(changes.Count * vertex_count_per_instance / thread_x);
        lod_shader.Dispatch(lod_kernel_id, (int) thread_x, 1, 1);
    }

    private int total_node_count;
    public int get_node_count() { return total_node_count; }

    ComputeShader lod_shader;
    int lod_kernel_id;
    public int get_kernel_id() { return lod_kernel_id; }

    int vertex_count_per_instance;

    public LodQuadTree[] lod_quad_trees;

    public LodBufferLayout[] lod_buffer_layouts;
    
    // Need to be double buffered so compute shader can check for changes
    private ComputeBuffer lod_buffer_layout_buffer_changes;
    private ComputeBuffer lod_layout_buffer;
    public ComputeBuffer get_lod_layout_buffer() { return lod_layout_buffer; }
}
