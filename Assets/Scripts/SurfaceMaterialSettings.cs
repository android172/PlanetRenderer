using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class SurfaceMaterialSettings : ScriptableObject {
    [System.Serializable]
    public struct SurfaceMaterial {
        public Texture2D mapDiffuse;
        public Texture2D mapNormal;
        public Texture2D mapOcclusion;
        [Min(0)]
        public float normalStrength;
        [Range(0, 1)]
        public float occlusionStrength;
        [Min(0)]
        public float scale;
        [Range(0, 1)]
        public float metallic;
        [Range(0, 1)]
        public float glossiness;
        public Color color;

        // Props
        public Texture2D MapDiffuse {
            get => (mapDiffuse == null) ? Texture2D.whiteTexture : mapDiffuse;
        }
        public Texture2D MapNormal {
            get => (mapNormal == null) ? Texture2D.normalTexture : mapNormal;
        }
        public Texture2D MapOcclusion {
            get => (mapOcclusion == null) ? Texture2D.whiteTexture : mapOcclusion;
        }

        public void set_settings(SurfaceMaterial other) {
            mapDiffuse = other.mapDiffuse;
            mapNormal = other.mapNormal;
            mapOcclusion = other.mapOcclusion;
            normalStrength = other.normalStrength;
            occlusionStrength = other.occlusionStrength;
            scale = other.scale;
            metallic = other.metallic;
            glossiness = other.glossiness;
            color = other.color;
        }

        // Methods
        public Texture2D get_diffuse_map_scaled(int max_ext) {
            return scale_texture(MapDiffuse, max_ext);
        }
        public Texture2D get_normal_map_scaled(int max_ext) {
            return scale_texture(MapNormal, max_ext);
        }
        public Texture2D get_ao_map_scaled(int max_ext) {
            return scale_texture(MapOcclusion, max_ext);
        }

        private static Texture2D scale_texture(in Texture2D texture, int dim) {
            // Create a new texture with the desired dimensions.
            Texture2D resized_texture = new Texture2D(dim, dim, TextureFormat.RGBA32, texture.mipmapCount > 1);

            // Use bilinear filtering for smoother resizing.
            Graphics.ConvertTexture(texture, 0, resized_texture, 0);
            return resized_texture;
        }
    }

    // Props
    private static readonly int biome_count = 16;
    public SurfaceMaterial[] biomeMaterial = new SurfaceMaterial[biome_count];

    private bool textures_info_updated = true;
    private int max_extent = 0;


    public void OnTextureInfoUpdated() {
        textures_info_updated = true;
    }

    public void set_settings(SurfaceMaterialSettings settings) {
        if (settings == null)
            throw new UnityException("Error in :: SurfaceMaterialSettings :: set_settings :: No settings passed.");
        for (int i = 0; i < biomeMaterial.Length; i++)
            biomeMaterial[i].set_settings(settings.biomeMaterial[i]);
        textures_info_updated = true;
    }

    public Texture2DArray get_diffuse_map() {
        // Create 2D array
        var diffuse_2D_array = create_2D_array();

        // Copy textures over
        for (int i = 0; i < biomeMaterial.Length; i++)
            Graphics.CopyTexture(
                biomeMaterial[i].get_diffuse_map_scaled(max_extent),
                0, 0, diffuse_2D_array, i, 0
            );

        return diffuse_2D_array;
    }
    public Texture2DArray get_normal_map() {
        // Create 2D array
        var normal_2D_array = create_2D_array();

        // Copy textures over
        for (int i = 0; i < biomeMaterial.Length; i++)
            Graphics.CopyTexture(
                biomeMaterial[i].get_normal_map_scaled(max_extent),
                0, 0, normal_2D_array, i, 0
            );

        return normal_2D_array;
    }
    public Texture2DArray get_occlusion_map() {
        // Create 2D array
        var ao_2D_array = create_2D_array();

        // Copy textures over
        for (int i = 0; i < biomeMaterial.Length; i++)
            Graphics.CopyTexture(
                biomeMaterial[i].get_ao_map_scaled(max_extent),
                0, 0, ao_2D_array, i, 0
            );

        return ao_2D_array;
    }

    public float[] get_metallic() {
        float[] metallic = new float[biomeMaterial.Length];
        for (int i = 0; i < biomeMaterial.Length; i++)
            metallic[i] = biomeMaterial[i].metallic;
        return metallic;
    }
    public float[] get_glossiness() {
        float[] glossiness = new float[biomeMaterial.Length];
        for (int i = 0; i < biomeMaterial.Length; i++)
            glossiness[i] = biomeMaterial[i].glossiness;
        return glossiness;
    }
    public float[] get_scale() {
        float[] scale = new float[biomeMaterial.Length];
        for (int i = 0; i < biomeMaterial.Length; i++)
            scale[i] = biomeMaterial[i].scale;
        return scale;
    }
    public float[] get_normal_strength() {
        float[] normal_strength = new float[biomeMaterial.Length];
        for (int i = 0; i < biomeMaterial.Length; i++)
            normal_strength[i] = biomeMaterial[i].normalStrength;
        return normal_strength;
    }
    public float[] get_occlusion_strength() {
        float[] occlusion_strength = new float[biomeMaterial.Length];
        for (int i = 0; i < biomeMaterial.Length; i++)
            occlusion_strength[i] = biomeMaterial[i].occlusionStrength;
        return occlusion_strength;
    }
    public Color[] get_colors() {
        Color[] colors = new Color[biomeMaterial.Length];
        for (int i = 0; i < biomeMaterial.Length; i++)
            colors[i] = biomeMaterial[i].color;
        return colors;
    }

    private Texture2DArray create_2D_array() {
        if (textures_info_updated) {
            compute_max_extent();
            compute_texture_format();
            // textures_info_updated = false;
        }
        return new Texture2DArray(max_extent, max_extent, biome_count, TextureFormat.RGBA32, false);
    }

    private void compute_max_extent() {
        max_extent = 0;
        void replace_max(int value) { if (value > max_extent) max_extent = value; }
        foreach (var biome_mat in biomeMaterial) {
            replace_max(biome_mat.MapDiffuse.width);
            replace_max(biome_mat.MapDiffuse.height);
            replace_max(biome_mat.MapNormal.width);
            replace_max(biome_mat.MapNormal.height);
            replace_max(biome_mat.MapOcclusion.width);
            replace_max(biome_mat.MapOcclusion.height);
        }
    }

    private void compute_texture_format() {
        // texture_format = biomeMaterial[0].MapDiffuse.format;
    }
}
