#version 330
in vec2 fragTexCoord;
in vec4 fragColor;
in vec3 fragNormal;
in vec3 fragPosition;
in vec4 fragTangent;
uniform sampler2D diffuse;
uniform sampler2D specular;
uniform sampler2D normalMap;
uniform vec4 colDiffuse;
uniform vec3 lightPos;
uniform mat4 matModel;
uniform mat4 matNormal;
uniform vec3 viewPos;
out vec4 finalColor;
void main()
{
vec4 texel = texture(diffuse, fragTexCoord);
vec3 texelColor = texel.xyz;
vec3 normalColor = texture(normalMap, fragTexCoord).xyz * 2.0 - 1.0;
float specularIntensity = texture(specular, fragTexCoord).x;
vec3 worldNormal = normalize(fragNormal * transpose(mat3(matNormal)));
vec3 tangent = normalize(matNormal * fragTangent).xyz;
vec3 binormal = normalize(cross(worldNormal, tangent)).xyz;
mat3 TBN = mat3(tangent, binormal, worldNormal);
TBN = transpose(TBN);
vec3 normal = normalize(normalColor*TBN);
vec3 lightDir = normalize(lightPos - fragPosition);
float shading = clamp(dot(normal, lightDir), 0.0, 1.0);
vec3 diffuse = shading * texelColor;
vec3 viewDir = normalize(viewPos - fragPosition);
vec3 reflectDir = reflect(-lightDir, normal);
float spec = pow(clamp(dot(viewDir, reflectDir), 0.1, 1.0), 8);
vec3 specular = specularIntensity * spec * texelColor.xyz;
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
finalColor = mix(fogColor, finalColor, fogFactor);

}