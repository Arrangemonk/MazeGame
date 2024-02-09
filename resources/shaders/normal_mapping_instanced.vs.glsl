#version 330
in vec3 vertexPosition;
in vec2 vertexTexCoord;
in vec3 vertexNormal;
in vec4 vertexColor;
in vec4 vertexTangent;
in mat4 instanceTransform;
uniform mat4 mvp;
uniform mat4 matModel;
uniform mat4 matNormal;
out vec2 fragTexCoord;
out vec4 fragColor;
out vec3 fragPosition;
out vec4 fragTangent;
out vec3 fragNormal;


void main()
{
    // Compute MVP for current instance
    mat4 mvpi = mvp*instanceTransform;
    fragTexCoord = vertexTexCoord;
    fragColor = vertexColor;
    fragNormal = normalize(vec3(matNormal*vec4(vertexNormal, 1.0)));
    fragTangent = normalize(matNormal * vertexTangent);
    gl_Position = mvpi*vec4(vertexPosition, 1.0);
    fragPosition = (instanceTransform*vec4(vertexPosition, 1.0)).xyz;
}