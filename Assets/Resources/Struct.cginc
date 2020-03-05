//Change this particle to have 
//struct Particle {
//	float3 pos;
//    float3 vel;
//	float3 col;
//};

struct Particle {
    float3 position;
    float3 direction;
    float noise_offset;
    float speed;
};
