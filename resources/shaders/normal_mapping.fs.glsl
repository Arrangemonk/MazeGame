#version 330
in vec2 fragTexCoord;
in vec4 fragColor;
in vec3 fragNormal;
in vec3 fragPosition;
in vec4 fragTangent;
in mat3 TBN;

uniform vec3 lightPos;
uniform vec3 viewPos;
uniform float time;

uniform sampler2D diffuse;
uniform sampler2D specular;
uniform sampler2D heightMap;
uniform sampler2D normalMap;

uniform vec4 colDiffuse;
uniform mat4 matModel;
uniform mat4 matNormal;
uniform vec4 fogColor;
uniform vec2 resolution;

out vec4 finalColor;

#define PARALAX_INTENSITY 0.01

#define PARALAX_QUALITY 256.0

vec2 parallax(
    vec2 v_uv,
    vec3 v_viewDir_TS,
    sampler2D heightMap)
{
    vec3 viewDir = normalize(v_viewDir_TS);
    vec2 v_totalDisplacement = viewDir.xy * PARALAX_INTENSITY;
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

float noise(vec2 pos, float evolve) {
    
    // Loop the evolution (over a very long period of time).
    float e = fract((evolve*0.01));
    
    // Coordinates
    float cx  = pos.x*e;
    float cy  = pos.y*e;
    
    // Generate a "random" black or white value
    return fract(23.0*fract(2.0/fract(fract(cx*2.4/cy*23.0+pow(abs(cy/22.4),3.3))*fract(cx*evolve/pow(abs(cy),0.050)))));
}


void main()
{
vec3 viewDir = normalize(viewPos - fragPosition);
//vec2 UVs = parallax(fragTexCoord,viewDir * TBN, heightMap);
vec2 UVs = fragTexCoord;

vec4 texel = texture(diffuse, UVs);
vec3 texelColor = texel.xyz;
vec3 normalColor = texture(normalMap, UVs).xyz * 2.0 - 1.0;
vec3 specularColor = texture(specular, UVs).xyz;

vec3 normal = normalize(normalColor*TBN);
vec3 lightDir = normalize(lightPos - fragPosition);

float shading = clamp(dot(normal, lightDir), 0.0, 1.0);// * (0.8/clamp(distance(fragPosition,lightPos),1.0,0.0));
vec3 diffuse = shading * texelColor;

vec3 reflectDir = reflect(-lightDir, normal);
vec3 reflectDir2 = reflect(lightDir, normal);
float spec = pow(clamp(dot(viewDir, reflectDir), 0.1, 0.8), 8);
float spec2 = pow(clamp(dot(viewDir, reflectDir2), 0.1, 0.8), 8);
vec3 specular = specularColor * (spec + spec2);
finalColor = vec4(diffuse + specular, 1.0);

float dist = length(viewPos - fragPosition);
const float fogDensity = 0.3;
float fogFactor = 1.0/exp((dist*fogDensity)*(dist*fogDensity));
fogFactor = clamp(fogFactor, 0.0, 1.0);

vec2 uv = fragTexCoord / resolution;

//float f = noise(uv * 4.0,time);
//finalColor -= clamp(f *.2, 0, 0.2);
//vec3 nc = specular;
//finalColor = vec4(nc,1.0);
finalColor = mix(fogColor, finalColor, fogFactor);

}