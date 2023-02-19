#version 420
out vec4 fragColor;
in vec2 texCoord;
in vec3 normal;
uniform sampler2D texture0;
uniform vec3 lightDir;
void main()
{
    float diffuse = max(0, dot(normal, -lightDir));
    fragColor = texture(texture0, texCoord) * diffuse;
}