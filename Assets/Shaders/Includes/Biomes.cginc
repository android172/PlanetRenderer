#define BIOME_FLATLANDS 0x00
#define BIOME_MOUNTAINS 0x01
#define BIOME_U_MOUNTAINS 0x02
#define BIOME_UNDERWATER 0x03
#define BIOME_DEEP_WATERS 0x04
#define BIOME_BEACH 0x05
#define BIOME_CRATER 0x06
#define BIOME_CRATER_RIDGE 0x07
#define BIOME_ISLAND 0x08
#define BIOME_PEAKS 0x09
#define BIOME_VALLEY 0x0a
#define BIOME_MAX 0x0b

void apply_biome(inout RWStructuredBuffer<uint> biomes, uint vertex,
                 uint biome) {
  uint biome_i = vertex / 4;
  uint biome_o = vertex % 4;

  switch (biome_o) {
  case 0:
    biomes[biome_i] &= 0xffffff00;
    biomes[biome_i] |= biome;
    break;
  case 1:
    biome <<= 8 * 1;
    biomes[biome_i] &= 0xffff00ff;
    biomes[biome_i] |= biome;
    break;
  case 2:
    biome <<= 8 * 2;
    biomes[biome_i] &= 0xff00ffff;
    biomes[biome_i] |= biome;
    break;
  case 3:
    biome <<= 8 * 3;
    biomes[biome_i] &= 0x00ffffff;
    biomes[biome_i] |= biome;
    break;
  }
}

uint get_biome(uniform StructuredBuffer<uint> biomes, uint vertex) {
  uint biome_i = vertex / 4;
  uint biome_o = vertex % 4;

  uint biome = biomes[biome_i];
  switch (biome_o) {
  case 0:
    break;
  case 1:
    biome >>= 8 * 1;
    break;
  case 2:
    biome >>= 8 * 2;
    break;
  case 3:
    biome >>= 8 * 3;
    break;
  }

  biome &= 0x000000ff;
  return biome;
}