#version 330
#define MAXITERATIONS 200
in vec2 fragTexCoord;
in vec4 fragColor;
in vec3 fragNormal;
in vec3 fragPosition;
in vec4 fragTangent;
uniform vec3 lightPos;
uniform vec3 viewPos;

uniform sampler2D diffuse;
uniform sampler2D specular;
uniform sampler2D heightMap;
uniform sampler2D normalMap;

uniform vec4 colDiffuse;
uniform mat4 matModel;
uniform mat4 matNormal;
out vec4 finalColor;

#define PARALAX_INTENSITY 0.015
#define PARALAX_QUALITY 16.0

vec2 parallax( in vec2 uv, in vec3 view )
{   
    float numLayers = PARALAX_QUALITY;
    float layerDepth = 1.0 / numLayers;
    vec2 p = view.xy  * PARALAX_INTENSITY / (2.0-view.z);
    vec2 deltaUVs = p / numLayers;
    float Texd = texture(heightMap,uv).r;
    float d = 0.0;
    int i = 0;
    while( d < Texd && i < PARALAX_QUALITY)
    {
        i++;
        uv -= deltaUVs;
        Texd = texture(heightMap,uv).r;
        d += layerDepth;  
    }

    vec2 lastUVs = uv + deltaUVs;
    
    float after = Texd - d;
    float before = texture(heightMap,uv).r - d + layerDepth;
    
    float w = after / (after - before);
    
    return mix( uv, lastUVs, w );
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
vec3 worldNormal = normalize(fragNormal * transpose(mat3(matNormal)));
vec3 tangent = normalize(matNormal * fragTangent).xyz;
vec3 binormal = normalize(cross(worldNormal, tangent)).xyz;
mat3 TBN = mat3(tangent, binormal, worldNormal);
vec2 UVs = parallax(fragTexCoord,viewDir*TBN);
TBN = transpose(TBN);

vec4 texel = texture(diffuse, UVs);
vec3 texelColor = texel.xyz;
vec3 normalColor = texture(normalMap, UVs).xyz * 2.0 - 1.0;
vec3 specularColor = texture(specular, UVs).xyz;

vec3 normal = normalize(normalColor*TBN);

vec3 lightDir = normalize(lightPos - fragPosition);

float shading = clamp(dot(normal, lightDir), 0.0, 1.0);// * (0.8/clamp(distance(fragPosition,lightPos),1.0,0.0));
vec3 diffuse = shading * texelColor;

vec3 reflectDir = reflect(-lightDir, normal);
float spec = pow(clamp(dot(viewDir, reflectDir), 0.1, 0.8), 8);
vec3 specular = specularColor * spec;
finalColor = vec4(diffuse + specular, 1.0);

//if(fragPosition.y < -0.2){
//    finalColor = vec4(0,0.2,0.6,1.0);
//}
float dist = length(viewPos - fragPosition);
//const vec4 fogColor = vec4(0.05, 0.1, 0.055, 1.0);
const vec4 fogColor = vec4(0.0,0.0,0.0, 1.0);
const float fogDensity = 0.3;
float fogFactor = 1.0/exp((dist*fogDensity)*(dist*fogDensity));
fogFactor = clamp(fogFactor, 0.0, 1.0);
float n = clamp(noise(fragTexCoord,20.0) * 0.2,0,0.2);
finalColor = mix(fogColor, finalColor, fogFactor);

}