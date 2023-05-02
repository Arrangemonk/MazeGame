
#version 330

// Input vertex attributes (from vertex shader)
in vec2 fragTexCoord;
in vec4 fragColor;
in vec3 fragNormal;
in vec3 fragPosition;
in vec4 fragTangent;

// Input uniform values
uniform sampler2D diffuse;     // diffuse texture
uniform sampler2D specular;    // normal texture
uniform sampler2D normalMap;    // normal texture
uniform vec4 colDiffuse;
uniform vec3 lightPos;          // light position
uniform mat4 matModel;          // pos, rotation and scaling of object
uniform mat4 matNormal;
uniform vec3 viewPos;           // eyes position


// Output fragment color
out vec4 finalColor;

void main()
{
    //textures

    vec3 texelColor = texture(diffuse, fragTexCoord).xyz;
    vec3 normalColor = texture(normalMap, fragTexCoord).xyz * 2.0 - 1.0;
    float specularIntensity = texture(specular, fragTexCoord).x;
    
    vec3 worldNormal = normalize(fragNormal * transpose(mat3(matNormal)));


    //normals
    vec3 tangent = normalize(matNormal * fragTangent).xyz;
    vec3 binormal = normalize(cross(worldNormal, tangent)).xyz;

    mat3 TBN = mat3(tangent, binormal, worldNormal);
    TBN = transpose(TBN);
    vec3 normal = normalize(normalColor*TBN); 

    //diffuse

    // find light source : L = Lightposition - surfacePosition
    vec3 lightDir = normalize(lightPos - fragPosition);

    // diffuse the light with the dot matrix :
    float shading = clamp(dot(normal, lightDir), 0.0, 1.0);
    vec3 diffuse = shading * texelColor;

    //spec

    
    vec3 viewDir = normalize(viewPos - fragPosition);
    vec3 reflectDir = reflect(-lightDir, normal);
    float spec = pow(clamp(dot(viewDir, reflectDir), 0.1, 1.0), 8);
    vec3 specular = specularIntensity * spec * texelColor.xyz; 

    //compositing
    
    finalColor = vec4(diffuse + specular, 1.0);

        float dist = length(viewPos - fragPosition);

    // these could be parameters...
    const vec4 fogColor = vec4(0.05, 0.1, 0.055, 1.0);
    const float fogDensity = 0.20;

    // Exponential fog
    float fogFactor = 1.0/exp((dist*fogDensity)*(dist*fogDensity));

    fogFactor = clamp(fogFactor, 0.0, 1.0);

    finalColor = mix(fogColor, finalColor, fogFactor);
}
