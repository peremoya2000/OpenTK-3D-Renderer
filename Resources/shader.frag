#version 420
const float ambWeight = .25;
const float diffWeight = 1-ambWeight;
const float specWeight = diffWeight;

out vec4 fragColor;
in vec2 texCoord;
in vec3 fragWorldPos;
in vec3 normal;

struct Material {
    vec3 ambientTint;
    vec3 diffuseTint;
    sampler2D mainTex; //Contains RGB for diffuse color and Alpha for Specularity
    float shininess; //Shininess is the power the specular light is raised to
};

struct Light {
    vec3 direction;
    vec3 color;
};

uniform Light light;
uniform Material material;
uniform vec3 viewPos;
void main()
{
    vec4 texData = texture(material.mainTex, texCoord);
    vec3 surfaceColor = texData.rgb;
    float specularMask = texData.a; 

    vec3 ambient = light.color * material.ambientTint * surfaceColor;

    float diffIntensity = max(dot(normal, light.direction), 0.0);
    vec3 diffuse = light.color * (diffIntensity * surfaceColor * material.diffuseTint);

    vec3 viewDir = normalize(viewPos - fragWorldPos);
    vec3 reflectDir = reflect(-light.direction, normal);
    float specIntensity = pow(max(dot(viewDir, reflectDir), 0.0), material.shininess);
    vec3 specular = light.color * (specIntensity * specularMask);

    vec3 result = ambient*ambWeight + diffuse*diffWeight + specular*specWeight;
    fragColor = vec4(result, 1.0);
}