#version 330

#define MAX_LIGHTS              4
#define LIGHT_DIRECTIONAL       0
#define LIGHT_POINT             1
#define PI 3.141592653589793238
#define FLT_MAX 3.402823466e+38

struct Light {
    int enabled;
    int type;
    vec3 position;
    vec3 target;
    vec4 color;
    float intensity;
    mat4 lightVP;
    sampler2D shadowMap;
};

// Input vertex attributes (from vertex shader)
in vec3 fragPosition;
in vec2 fragTexCoord;
in vec4 fragColor;
in vec3 fragNormal;
in vec3 fragTangent;
in vec4 shadowPos;
in mat3 TBN;

// Output fragment color
out vec4 finalColor;

// Input uniform values
uniform int numOfLights;
uniform samplerCube skyMap;
uniform sampler2D albedoMap;
uniform sampler2D mraMap;
uniform sampler2D normalMap;
uniform sampler2D emissiveMap;



// Input lighting values
uniform Light lights[MAX_LIGHTS];
uniform vec3 viewPos;

uniform float ambient;

uniform int shadowMapResolution;

#define PARALAX_QUALITY 64.0

vec2 parallax(
    vec2 v_uv,
    vec3 v_viewDir_TS,
    sampler2D heightMap)
{
    float parallaxDepth = albedoColor.w;
    if (parallaxDepth == 0.0)
    {
        return v_uv;
    }
    vec3 viewDir = normalize(v_viewDir_TS);
    vec2 v_totalDisplacement = viewDir.xy * parallaxDepth;
    vec2 v_stepSize = v_totalDisplacement / PARALAX_QUALITY;
    float f_depthSliceSize = 1.0 / PARALAX_QUALITY;
    vec2 v_currentUV = v_uv;
    float f_currentDepth = 1.0;
    float f_currentHeight = 0.0;

    for (int i = 0; i < PARALAX_QUALITY; ++i)
    {
        f_currentDepth -= f_depthSliceSize;
        v_currentUV += v_stepSize;
        f_currentHeight = texture(heightMap, v_currentUV).r;
        if (f_currentHeight > f_currentDepth)
        {
            break;
        }
    }
    vec2 v_prevUV = v_currentUV - v_stepSize;
    float f_prevHeight = texture(heightMap, v_prevUV).r;
    float f_prevDepth = f_currentDepth + f_depthSliceSize;
    float f_deltaH = f_currentHeight - f_prevHeight;
    float f_deltaD = f_currentDepth - f_prevDepth;
    float t = (f_currentDepth - f_currentHeight) / (f_deltaH - f_deltaD);

    return v_currentUV - v_stepSize * t;
}

// Reflectivity in range 0.0 to 1.0
// NOTE: Reflectivity is increased when surface view at larger angle
vec3 SchlickFresnel(float hDotV, vec3 refl)
{
    return refl + (1.0 - refl) * pow(1.0 - hDotV, 5.0);
}

float GgxDistribution(float nDotH, float roughness)
{
    float a = roughness * roughness * roughness * roughness;
    float d = nDotH * nDotH * (a - 1.0) + 1.0;
    d = PI * d * d;
    return a / max(d, 0.0000001);
}

float GeomSmith(float nDotV, float nDotL, float roughness)
{
    float r = roughness + 1.0;
    float k = r * r / 8.0;
    float ik = 1.0 - k;
    float ggx1 = nDotV / (nDotV * ik + k);
    float ggx2 = nDotL / (nDotL * ik + k);
    return ggx1 * ggx2;
}

float border(float value, float bc, float x)
{
    return mix(value, bc, max(0.0, sign(x) * sign(x - 1.0)));
}
float borderx2(float value, float bc, vec2 uv)
{
    return border(border(value, bc, uv.x), bc, uv.y);
}

vec2 latlong(vec3 normal)
{
    return vec2(atan(normal.z, normal.x) * 0.5 / PI + 0.5, acos(normal.y) / PI);
}


vec3 ComputePBR()
{
    vec3 V = normalize(viewPos - fragPosition);
    vec2 UV = parallax(fragTexCoord, -V * TBN, albedoMap);
    //vec2 UV = fragTexCoord;

    vec3 albedo = texture(albedoMap, UV).rgb;
    albedo = vec3(albedoColor.x * albedo.x, albedoColor.y * albedo.y, albedoColor.z * albedo.z);

    float metallic = clamp(metallicValue, 0.0, 1.0);
    float roughness = clamp(roughnessValue, 0.0, 1.0);

    vec4 mra = texture(mraMap, UV);
    metallic = clamp(mra.b + metallicValue, 0.04, 1.0);
    roughness = clamp(mra.g + roughnessValue, 0.04, 1.0);
    float ao = (mra.r);

    vec3 N = texture(normalMap, UV).rgb;
    N = N * 2.0 - 1.0;
    N = normalize(N * TBN);

    vec3 emissive = texture(emissiveMap, UV).rgb * emissivePower;

    // if dia-electric use base reflectivity of 0.04 otherwise ut is a metal use albedo as base reflectivity
    vec3 baseRefl = mix(vec3(0.04), albedo.rgb, metallic);
    vec3 lightAccum = vec3(0.0);  // Acumulate lighting lum

    for (int i = 0; i < numOfLights; i++)
    {
        vec3 L = normalize(lights[i].position - fragPosition);      // Compute light vector
        vec3 H = normalize(V + L);                                  // Compute halfway bisecting vector
        float dist = length(lights[i].position - fragPosition);     // Compute distance to light
        float attenuation = 1.0 / (dist * dist * 0.23);                   // Compute attenuation
        vec3 radiance = lights[i].color.rgb * lights[i].intensity * attenuation; // Compute input radiance, light energy comming in

        // Cook-Torrance BRDF distribution function
        float nDotV = max(dot(N, V), 0.0000001);
        float nDotL = max(dot(N, L), 0.0000001);
        float hDotV = max(dot(H, V), 0.0);
        float nDotH = max(dot(N, H), 0.0);
        float D = GgxDistribution(nDotH, roughness);    // Larger the more micro-facets aligned to H
        float G = GeomSmith(nDotV, nDotL, roughness);   // Smaller the more micro-facets shadow
        vec3 F = SchlickFresnel(hDotV, baseRefl);       // Fresnel proportion of specular reflectance

        vec3 spec = (D * G * F) / (4.0 * nDotV * nDotL);

        // Difuse and spec light can't be above 1.0
        // kD = 1.0 - kS  diffuse component is equal 1.0 - spec comonent
        vec3 kD = vec3(1.0) - F;

        // Mult kD by the inverse of metallnes, only non-metals should have diffuse light
        kD *= 1.0 - metallic;

        vec4 fragPosLightSpace = lights[i].lightVP * vec4(fragPosition, 1);
        float fragmentDepthUnaltered = fragPosLightSpace.z;
        fragPosLightSpace.xyz /= fragPosLightSpace.w; // Perform the perspective division
        fragPosLightSpace.xyz = (fragPosLightSpace.xyz + 1.0) / 2.0; // Transform from [-1, 1] range to [0, 1] range
        vec2 sampleCoords = fragPosLightSpace.xy;
        float curDepth = fragPosLightSpace.z;

        float bias = max(0.0002 * (1.0 - dot(N, L)), 0.00002) + 0.00001;
        float shadowCounter = 0.0;
        const float numSamples = 9.0;

        // PCF (percentage-closer filtering) algorithm:
        // Instead of testing if just one point is closer to the current point,
        // we test the surrounding points as well
        // This blurs shadow edges, hiding aliasing artifacts
        vec2 texelSize = vec2(1.0 / float(shadowMapResolution));
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                vec2 finalcoords = sampleCoords + texelSize * max(1.0, dist) * vec2(x, y);
                float sampleDepth = borderx2(texture(lights[i].shadowMap, finalcoords).r, FLT_MAX, finalcoords);
                shadowCounter += float(0.0 < fragmentDepthUnaltered && sampleDepth < curDepth - bias);
            }
        }


        lightAccum += mix(((kD * albedo.rgb / PI + spec) * radiance * nDotL) * lights[i].enabled, vec3(0, 0, 0), shadowCounter / numSamples); // Angle of light has impact on result
    }
    vec3 ambientreflection = textureLod(skyMap, reflect(-V, N), roughness * 12.0).xyz;
    ambientreflection = mix(vec3(0.4) * ambientreflection, ambientreflection, 0.5 * (metallic + roughness));
    ambientreflection = pow(ambientreflection, vec3(2.2));
    vec3 ambientFinal = ambientreflection * ambient * ao;
    return ambientFinal + lightAccum * ao + emissive;
}

void main()
{
    vec3 color = ComputePBR();

    // HDR tonemapping
    color = pow(color, color + vec3(1.0));

    // Gamma correction
    color = pow(color, vec3(1.0 / 2.2));

    finalColor = vec4(color, 1.0);
}