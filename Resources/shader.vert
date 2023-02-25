#version 420
layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec3 aNormal;
layout(location = 2) in vec2 aTexCoord;

out vec2 texCoord;
out vec3 fragWorldPos;
out vec3 normal;
uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;
uniform mat3 normalRot;

void main(void)
{
    normal = aNormal * normalRot;
    texCoord = aTexCoord;
    vec4 worldPos = vec4(aPosition, 1.0) * model;
    fragWorldPos = worldPos.xyz;
    gl_Position = worldPos * view * projection;
}