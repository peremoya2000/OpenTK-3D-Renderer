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
    sampler2D mainTex; //Contains RGB for diffuse color and Alpha for Specularity
    float shininess; //Shininess is the power the specular light is raised to
};

struct Light {
    vec4 vector;
    vec3 color;
    float intensity;
    float radius;
};

uniform Light[MAX_LIGHTS] lights;
uniform Material material;
uniform vec3 viewPos;


vec3 shadeFragment(vec4 matData, vec3 lightDir, vec3 lightColor)
{
    vec3 surfaceColor = matData.rgb;
    float specularMask = matData.a; 

    vec3 ambient = lightColor * material.ambientTint * surfaceColor;

    float diffIntensity = max(dot(normal, lightDir), 0.0);
    vec3 diffuse = lightColor * (diffIntensity * surfaceColor * material.diffuseTint);

    vec3 viewDir = normalize(viewPos - fragWorldPos);
    vec3 reflectDir = reflect(-lightDir, normal);
    float specIntensity = pow(max(dot(viewDir, reflectDir), 0.0), material.shininess);
    vec3 specular = lightColor * (specIntensity * specularMask);

    return ambient*ambWeight + diffuse*diffWeight + specular*specWeight;
}

vec3 directionalLight(Light light, vec4 texData)
{
    vec3 shadingColor = shadeFragment(texData, light.vector.xyz, light.color);
    if(light.intensity>0)
    {
        return shadingColor*light.intensity;
    }
    else
    {
        return shadingColor;
    }
}

float pow2(float x)
{
    return x*x;
}

vec3 pointLight(Light light, vec4 texData)
{
    vec3 lightVec = fragWorldPos-light.vector.xyz;
    vec3 lightDir = normalize(lightVec);
    vec3 shadingColor = shadeFragment(texData, lightDir, light.color);

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
    vec4 texData = texture(material.mainTex, texCoord);
    vec3 result;

    for(int i = 0; i < MAX_LIGHTS; ++i)
    {
        if(lights[i].vector.w==0)
        {
            result+=directionalLight(lights[i], texData);
        }
        else
        {
            result+=pointLight(lights[i], texData);
        }
    }

    fragColor = vec4(result, 1.0);
}