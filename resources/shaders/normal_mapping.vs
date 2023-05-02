#version 330

// Input vertex attributes
in vec3 vertexPosition;
in vec2 vertexTexCoord;
in vec3 vertexNormal;
in vec4 vertexColor;
in vec4 vertexTangent;

// Input uniform values
uniform mat4 mvp;
uniform mat4 matModel;
uniform mat4 matNormal;

// Output vertex attributes (to fragment shader)
out vec2 fragTexCoord;
out vec4 fragColor;
out vec3 fragPosition;
out vec4 fragTangent;
out vec3 fragNormal;
// out vec3 fragBiTangent;


// NOTE: Add here your custom variables

void main()
{
    fragTexCoord = vertexTexCoord;
    fragColor = vertexColor;
    fragNormal = vertexNormal;
   // fragPosition = vertexPosition * matModel
    fragTangent = vertexTangent;


    //Calculate final vertex position
    gl_Position = mvp*vec4(vertexPosition, 1.0);


    fragPosition = (matModel * vec4(vertexPosition, 1)).xyz;


}

