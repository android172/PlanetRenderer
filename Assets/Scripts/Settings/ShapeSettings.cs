using UnityEngine;

public class ShapeSettings : ScriptableObject {
    [System.Serializable]
    public class NoiseSettings {
        public bool enable;
        [Range(1, 10)]
        public int numberOfLayers = 1;
        [Min(0f)]
        public float amplitudeFading;
        [Min(0f)]
        public float baseFrequency = 1f;
        [Min(0f)]
        public float frequencyMultiplier;
        public float strength = 1f;
        public float baseHeight;
        public Vector3 seed;

        private static readonly System.Random r = new();

        public NoiseSettings(NoiseSettings settings) {
            enable = settings.enable;
            numberOfLayers = settings.numberOfLayers;
            amplitudeFading = settings.amplitudeFading;
            baseFrequency = settings.baseFrequency;
            frequencyMultiplier = settings.frequencyMultiplier;
            strength = settings.strength;
            baseHeight = settings.baseHeight;
            seed.x = settings.seed.x;
            seed.y = settings.seed.y;
            seed.z = settings.seed.z;
        }

        public virtual float[] get_noise() {
            return new float[] {
                numberOfLayers,
                amplitudeFading,
                baseFrequency,
                frequencyMultiplier,
                strength,
                baseHeight,
                seed.x,
                seed.y,
                seed.z
            };
        }

        public virtual Vector2 get_noise_range() {
            return enable ? new(baseHeight - strength, baseHeight + strength) : new();
        }

        public void randomize_seed() {
            seed.x = rand_to_float(r.NextDouble(), r.Next(15));
            seed.y = rand_to_float(r.NextDouble(), r.Next(15));
            seed.z = rand_to_float(r.NextDouble(), r.Next(15));
        }
        private float rand_to_float(double mantissa, int exponent) {
            double mn = 2.0 * mantissa - 1.0;
            double ex = System.Math.Pow(2.0, exponent);
            return (float) (mn * ex);
        }
    }

    [System.Serializable]
    public class MNoiseSettings : NoiseSettings {
        public float power = 1f;
        [Range(0, 1)]
        public float gain = 1f;

        public MNoiseSettings(MNoiseSettings settings) : base(settings) {
            power = settings.power;
            gain = settings.gain;
        }

        public override float[] get_noise() {
            return new float[] {
                numberOfLayers,
                amplitudeFading,
                baseFrequency,
                frequencyMultiplier,
                strength,
                baseHeight,
                seed.x,
                seed.y,
                seed.z,
                power,
                gain
            };
        }

        public override Vector2 get_noise_range() {
            return enable ? new(baseHeight, baseHeight + strength) : new();
        }
    }

    [System.Serializable]
    public class UMNoiseSettings : MNoiseSettings {
        public float offset;

        public UMNoiseSettings(UMNoiseSettings settings) : base(settings) {
            offset = settings.offset;
        }

        public override float[] get_noise() {
            return new float[] {
                numberOfLayers,
                amplitudeFading,
                baseFrequency,
                frequencyMultiplier,
                strength,
                baseHeight,
                seed.x,
                seed.y,
                seed.z,
                power,
                gain,
                offset
            };
        }
    }

    [System.Serializable]
    public class CraterNoiseSettings : NoiseSettings {
        [Range(0, 1)]
        public float depth = 0.5f;
        [Min(1f)]
        public float radius = 2f;
        [Min(1)]
        public int slope = 12;
        [Min(0f)]
        public float centralElevationHeight;
        [Min(0f)]
        public float centralElevationWidth;
        [Min(0f)]
        public float outsideSlope;
        public float jitter;

        public CraterNoiseSettings(CraterNoiseSettings settings) : base(settings) {
            depth = settings.depth;
            radius = settings.radius;
            slope = settings.slope;
            centralElevationHeight = settings.centralElevationHeight;
            centralElevationWidth = settings.centralElevationWidth;
            outsideSlope = settings.outsideSlope;
            jitter = settings.jitter;
        }

        public override float[] get_noise() {
            return new float[] {
                numberOfLayers,
                depth,
                amplitudeFading,
                baseFrequency,
                frequencyMultiplier,
                radius,
                slope,
                centralElevationHeight,
                centralElevationWidth,
                outsideSlope,
                jitter,
                strength,
                baseHeight,
                seed.x,
                seed.y,
                seed.z
            };
        }

        public override Vector2 get_noise_range() {
            return enable ? new(baseHeight - strength * depth, baseHeight) : new();
        }
    }

    // Shape settings
    [Min(0.5f)]
    public float radius = 1f;

    // Noise shape compute
    public ComputeShader shapeComputeShader;
    private int shader_kernel_id;
    private uint thread_x;
    private uint thread_y;
    private uint thread_z;

    // Noise compute buffers
    private ComputeBuffer initial_position_buffer;
    private ComputeBuffer position_buffer;
    private ComputeBuffer normal_buffer;
    private ComputeBuffer biome_buffer;
    private int vertex_count;

    // Outside context
    protected bool view_based_culling = false;
    protected Transform camera_t;
    protected Transform shape_t;
    private float[] last_shape_limits = new float[] { 0, 0, 0, 0 };

    public virtual void set_settings(ShapeSettings settings) {
        shapeComputeShader = settings.shapeComputeShader;
        radius = settings.radius;
    }
    public virtual void randomize_seed() { }

    public virtual void initialize(
        Transform shape_transform,
        ComputeBuffer initial_position_buffer,
        ComputeBuffer position_buffer,
        ComputeBuffer normal_buffer,
        ComputeBuffer biome_buffer,
        int vertex_count
    ) {
        if (shapeComputeShader == null)
            throw new UnityException("Error in :: ShapeSettings :: initialize :: Compute shader not set.");
        if (initial_position_buffer.count < position_buffer.count)
            throw new UnityException("Error in :: ShapeSettings :: initialize :: Initial position buffer incompatible with the current one.");

        // Needs reference to parent transform
        shape_t = shape_transform;

        // Here we will setup compute shader
        // First we need to find kernel
        shader_kernel_id = shapeComputeShader.FindKernel("compute_shape");

        // Compute required thread count
        shapeComputeShader.GetKernelThreadGroupSizes(shader_kernel_id, out thread_x, out thread_y, out thread_z);
        thread_x = (uint) Mathf.CeilToInt(1.0f * vertex_count / thread_x);

        // Set buffers
        this.initial_position_buffer = initial_position_buffer;
        this.position_buffer = position_buffer;
        this.normal_buffer = normal_buffer;
        this.biome_buffer = biome_buffer;
        this.vertex_count = vertex_count;

        // Set number of vertices
        shapeComputeShader.SetInt("num_of_vertices", vertex_count);
    }

    public void setup_view_based_culling(Transform camera_transform) {
        view_based_culling = true;
        camera_t = camera_transform;
    }

    public void update_view_based_culling() {
        // Set noise settings
        set_noise_settings(true);

        // run
        shapeComputeShader.Dispatch(shader_kernel_id, (int) thread_x, (int) thread_y, (int) thread_z);
    }

    private void set_core_noise_settings() {
        // Set radius
        shape_t.localScale = new(radius, radius, radius);

        // Set buffers
        shapeComputeShader.SetBuffer(shader_kernel_id, "vertices", initial_position_buffer);
        shapeComputeShader.SetBuffer(shader_kernel_id, "out_vertices", position_buffer);
        shapeComputeShader.SetBuffer(shader_kernel_id, "normals", normal_buffer);
        shapeComputeShader.SetBuffer(shader_kernel_id, "biomes", biome_buffer);

        // Set number of vertices
        shapeComputeShader.SetInt("num_of_vertices", vertex_count);
    }
    protected virtual void set_additional_noise_settings() { }

    private void set_noise_settings(bool is_position_update = false) {
        // Check validity
        if (shapeComputeShader == null)
            throw new UnityException("Error in :: ShapeSettings :: apply_noise :: Compute shader not set.");

        // Set noise settings
        set_core_noise_settings();
        set_additional_noise_settings();

        // Set culling if enabled
        set_culling_info(is_position_update);
    }

    public void apply_noise() {
        // Set noise settings
        set_noise_settings();

        // run
        shapeComputeShader.Dispatch(shader_kernel_id, (int) thread_x, (int) thread_y, (int) thread_z);
    }


    protected virtual Vector2 noise_range() {
        return new Vector2(radius, radius);
    }

    private void set_culling_info(bool upload_last_shape_limits) {
        if (!view_based_culling) {
            // Render everything
            shapeComputeShader.SetFloats("shape_limits", new float[] { 0, 0, 0, 0 });
            shapeComputeShader.SetFloats("old_shape_limits", new float[] { 0, 0, 0, 1 });
            return;
        }

        // Compute noise range
        var total_noise_range = noise_range();
        var min_r = total_noise_range.x; // Radius of the smaller sphere
        var max_r = total_noise_range.y; // Radius of the larger sphere
        if (max_r < min_r)
            throw new UnityException(
            "Error in :: ShapeSettings :: set_culling_info :: " +
            "Maximum computed noise smaller then the minimum one/");

        // Check if we are inside
        var to_camera = camera_t.position - shape_t.position;
        var camera_dir = to_camera.normalized;
        var camera_dist = to_camera.magnitude;
        if (camera_dist < min_r) {
            // Render nothing
            shapeComputeShader.SetFloats("shape_limits", new float[] { 0, 0, 0, 1 });
            shapeComputeShader.SetFloats("old_shape_limits", new float[] { 0, 0, 0, 0 });
            return;
        }

        // Compute maximum render-able angle
        var to_circle_dir = compute_camera_to_circle_dir(camera_t.position, -to_camera, min_r);
        var max_render_angle = compute_dot_product_limit(camera_t.position, to_circle_dir, shape_t.position, max_r);

        // Send last shape limit info to compute shader
        if (upload_last_shape_limits)
            shapeComputeShader.SetFloats("old_shape_limits", last_shape_limits);
        else
            shapeComputeShader.SetFloats("old_shape_limits", new float[] { 0, 0, 0, 1 });

        // Send this info to compute shader
        last_shape_limits = new float[] { camera_dir.x, camera_dir.y, camera_dir.z, max_render_angle };
        shapeComputeShader.SetFloats("shape_limits", last_shape_limits);
    }

    private Vector3 compute_camera_to_circle_dir(Vector3 camera_position, Vector3 to_center, float sphere_r) {
        var dist_to_center = to_center.magnitude;
        var sq_dist_to_center = to_center.sqrMagnitude;

        // Compute distance from camera to tangent circle
        var dist_to_circle = Mathf.Sqrt(sq_dist_to_center - sphere_r * sphere_r);

        // Compute distance from camera to tangent circle center
        var dist_to_circle_center = dist_to_circle * dist_to_circle / dist_to_center;

        // Compute tangent circle radius
        var circle_r = dist_to_circle * sphere_r / dist_to_center;

        // Compute a vector colinear with the circle plane
        var to_center_dir = to_center.normalized;
        var other_vec = (Mathf.Abs(to_center_dir.x) > 0.9) ? new Vector3(0, 1, 0) : new Vector3(1, 0, 0);
        var circle_v = Vector3.Cross(to_center_dir, other_vec).normalized;

        // Get coords of a point on a tangent circle
        var circle_point = camera_position + dist_to_circle_center * to_center_dir + circle_r * circle_v;

        // Compute direction vector
        return (circle_point - camera_position).normalized;
    }

    private float compute_dot_product_limit(Vector3 line_org, Vector3 line_dir, Vector3 sphere_center, float sphere_r) {
        var center_to_org = line_org - sphere_center;
        var center_to_org_dir = center_to_org.normalized;

        // Solve equation for intersection
        // Calculate coefficients for the quadratic equation
        var a = Vector3.Dot(line_dir, line_dir);
        var b = 2 * Vector3.Dot(line_dir, center_to_org);
        var c = Vector3.Dot(center_to_org, center_to_org) - sphere_r * sphere_r;

        // Calculate discriminant of the quadratic equation
        var d = b * b - 4 * a * c;
        if (d < -1.0e-6)
            throw new UnityException(
                "Error in :: ShapeSettings :: compute_camera_to_larger_sphere_intersection_dist :: " +
                "Something is wrong with the function implementation. Impossible outcome.");
        if (d < 0) d = 0;

        // Compute solutions
        var t1 = (-b + Mathf.Sqrt(d)) / (2 * a);
        var t2 = (-b - Mathf.Sqrt(d)) / (2 * a);
        var t = Mathf.Max(t1, t2);

        // Find intersection point
        var intersection_p = line_org + line_dir * t;

        // Find center to intersection direction
        var center_to_int_dir = (intersection_p - sphere_center).normalized;

        return Vector3.Dot(center_to_int_dir, center_to_org_dir);
    }
}
