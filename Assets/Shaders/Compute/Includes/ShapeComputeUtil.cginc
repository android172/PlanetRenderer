// ///////////////// //
// Noise computation //
// ///////////////// //
struct NoiseValues {
  float continent_mask;
  float ocean_mask;
  float flatness;
  float c_base;
  float ridge_1;
  float ridge_2;
  float crater;
  float bumps;
};

NoiseValues compute_noise(float3 position);
float compute_height(NoiseValues noise);

// ////////////////// //
// Normal computation //
// ////////////////// //
float3 rotate_vector(float3 v, float3 ax, float angle);
float3 compute_triangle_normal(float3 up, float3 p1, float3 p2, float3 p3);
float3 compute_neighbour(float3 up, float3 ax, float angle);

float3 compute_normal(float3 up, float3 p0, float diff_angle) {
  // Compute rotation axis
  float3 other_vec = (abs(up.x) > 0.9) ? float3(0, 1, 0) : float3(1, 0, 0);
  float3 rot_axis_1 = normalize(cross(up, other_vec));
  float3 rot_axis_2 = normalize(cross(up, rot_axis_1));

  // Copmute neighbouring points
  float3 p1 = compute_neighbour(up, rot_axis_1, diff_angle);
  float3 p2 = compute_neighbour(up, rot_axis_1, -diff_angle);
  float3 p3 = compute_neighbour(up, rot_axis_2, diff_angle);
  float3 p4 = compute_neighbour(up, rot_axis_2, -diff_angle);

  // Compute triange normals
  float3 n1 = compute_triangle_normal(up, p0, p1, p3);
  float3 n2 = compute_triangle_normal(up, p0, p1, p4);
  float3 n3 = compute_triangle_normal(up, p0, p2, p3);
  float3 n4 = compute_triangle_normal(up, p0, p2, p4);

  // Compute surface normal at postiion p0
  float3 n = (n1 + n2 + n3 + n4) / 4.0;

  return n;
}

float3 compute_neighbour(float3 up, float3 ax, float angle) {
  float3 position = rotate_vector(up, ax, angle);
  NoiseValues noise = compute_noise(position);
  float height = compute_height(noise);
  return position * height;
}

float3 rotate_vector(float3 v, float3 ax, float angle) {
  float cos_theta = cos(angle);
  float sin_theta = sin(angle);
  float inv_cos_theta = 1.0 - cos_theta;

  // Create rotation matrix
  float3x3 rotationMatrix =
      float3x3(cos_theta + ax.x * ax.x * inv_cos_theta,
               ax.x * ax.y * inv_cos_theta - ax.z * sin_theta,
               ax.x * ax.z * inv_cos_theta + ax.y * sin_theta,
               ax.y * ax.x * inv_cos_theta + ax.z * sin_theta,
               cos_theta + ax.y * ax.y * inv_cos_theta,
               ax.y * ax.z * inv_cos_theta - ax.x * sin_theta,
               ax.z * ax.x * inv_cos_theta - ax.y * sin_theta,
               ax.z * ax.y * inv_cos_theta + ax.x * sin_theta,
               cos_theta + ax.z * ax.z * inv_cos_theta);

  return mul(rotationMatrix, v);
}

float3 compute_triangle_normal(float3 up, float3 p1, float3 p2, float3 p3) {
  float3 p12 = p2 - p1;
  float3 p13 = p3 - p1;
  float3 n = normalize(cross(p12, p13));
  return (dot(up, n) > 0) ? n : -n;
}