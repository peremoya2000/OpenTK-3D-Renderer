#version 420
out vec4 fragColor;
in vec2 texCoord;
in vec3 fragWorldPos;
in vec3 normal;
uniform sampler2D texture0;
uniform vec3 lightDir;
uniform vec3 viewPos;
void main()
{
    float diffuse = max(0, dot(normal, -lightDir));
    vec3 viewDir = normalize(viewPos - fragWorldPos);
    vec3 reflectDir = reflect(lightDir, normal);
    float specular = pow(max(dot(viewDir, reflectDir), 0.0), 32);
    fragColor = texture(texture0, texCoord) * diffuse + .4 * specular;
}