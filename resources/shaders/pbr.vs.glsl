#version 330

// Input vertex attributes
in vec3 vertexPosition;
in vec2 vertexTexCoord;
in vec3 vertexNormal;
in vec3 vertexTangent;
in vec4 vertexColor;

// Input uniform values
uniform mat4 mvp;
uniform mat4 matModel;
uniform mat4 matNormal;
uniform vec3 lightPos;
uniform vec4 difColor;

// Output vertex attributes (to fragment shader)
out vec3 fragPosition;
out vec2 fragTexCoord;
out vec4 fragColor;
out vec3 fragNormal;
out vec3 fragTangent;
out mat3 TBN;

const float normalOffset = 0.1;

void main()
{
    vec3 vertexBinormal = cross(vertexNormal, vertexTangent);

    mat3 normalMatrix = transpose(inverse(mat3(matModel)));

    fragPosition = vec3(matModel * vec4(vertexPosition, 1.0f));

    fragTexCoord = vertexTexCoord;
    fragNormal = normalize(normalMatrix * vertexNormal);
    fragTangent = normalize(normalMatrix * vertexTangent);
    vec3 fragBinormal = normalize(normalMatrix * vertexBinormal);

    fragTangent = normalize(fragTangent - dot(fragTangent, fragNormal) * fragNormal);
    fragBinormal = cross(fragNormal, fragTangent);

    TBN = transpose(mat3(fragTangent, fragBinormal, fragNormal));

    gl_Position = mvp * vec4(vertexPosition, 1.0);
}