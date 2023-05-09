#version 330
in vec3 vertexPosition;
in vec2 vertexTexCoord;
in vec3 vertexNormal;
in vec4 vertexColor;
in vec4 vertexTangent;
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
fragTexCoord = vertexTexCoord;
fragColor = vertexColor;
fragNormal = vertexNormal;
fragTangent = vertexTangent;
gl_Position = mvp*vec4(vertexPosition, 1.0);
fragPosition = (matModel * vec4(vertexPosition, 1)).xyz;
}