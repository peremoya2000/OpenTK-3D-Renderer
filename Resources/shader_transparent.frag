#version 420
#define MAX_LIGHTS 16
const float ambWeight = .25;
const float diffWeight = 1-ambWeight;
const float specWeight = diffWeight;
const float lightDecayIndex = 4;

out vec4 fragColor;
in vec2 texCoord;
in vec3 fragWorldPos;
in vec3 normal;

struct Material {
    vec3 ambientTint;
    vec3 diffuseTint;
    sampler2D mainTex; //Contains RGB for diffuse color and Alpha for opacity
    float shininess; //Shininess is the power the specular light is raised to
    float opacity;
};

struct Light {
    vec4 vector;
    vec3 color;
    float intensity;
    float radius;
};

uniform Light[MAX_LIGHTS] lights;
uniform float lightCount;
uniform Material material;
uniform vec3 viewPos;


vec3 shadeFragment(vec3 albedo, vec3 lightDir, vec3 lightColor)
{
    vec3 ambient = lightColor * material.ambientTint * albedo;

    float diffIntensity = max(dot(-normal, lightDir), 0.0);
    vec3 diffuse = lightColor * (diffIntensity * albedo * material.diffuseTint);

    vec3 viewDir = normalize(fragWorldPos-viewPos);
    vec3 reflectDir = reflect(-lightDir, normal);
    float specIntensity = pow(max(dot(viewDir, reflectDir), 0.0), material.shininess);
    vec3 specular = lightColor * specIntensity;

    return ambient*ambWeight + diffuse*diffWeight + specular*specWeight;
}

vec3 directionalLight(Light light, vec3 albedo)
{
    vec3 shadingColor = shadeFragment(albedo, light.vector.xyz, light.color);
    return shadingColor*max(0,light.intensity);
}

float pow2(float x)
{
    return x*x;
}

vec3 pointLight(Light light, vec3 albedo)
{
    vec3 lightVec = fragWorldPos-light.vector.xyz;
    vec3 lightDir = normalize(lightVec);
    vec3 shadingColor = shadeFragment(albedo, lightDir, light.color);

    float normalizedDist = length(lightVec)/light.radius;
    float attenuationFactor;
    if(normalizedDist<1)
    {
        attenuationFactor = light.intensity * pow2(1-pow2(normalizedDist)) / (1+lightDecayIndex*normalizedDist);
    }
    else
    {
        attenuationFactor = 0;
    }

    return shadingColor * attenuationFactor;
}

void main()
{
    vec3 albedo = texture(material.mainTex, texCoord).xyz;
    vec3 result = vec3(0.0);

    for(int i = 0; i < min(MAX_LIGHTS, lightCount); ++i)
    {
        if(lights[i].vector.w==0)
        {
            result+=directionalLight(lights[i], albedo);
        }
        else
        {
            result+=pointLight(lights[i], albedo);
        }
    }

    fragColor = vec4(result, material.opacity);
}