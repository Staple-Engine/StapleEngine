#ifndef STAPLE_LIGHTING_GUARD
#define STAPLE_LIGHTING_GUARD

#include <bgfx_shader.sh>

#define STAPLE_MAX_LIGHTS 16

uniform vec4 u_lightAmbient;
uniform vec4 u_lightCount;

#define StapleLightAmbient u_lightAmbient
#define StapleLightCount int(u_lightCount.x)

uniform vec4 u_lightDiffuse[STAPLE_MAX_LIGHTS];
uniform vec4 u_lightSpecular[STAPLE_MAX_LIGHTS];
uniform vec4 u_lightTypePosition[STAPLE_MAX_LIGHTS];
uniform vec3 u_lightSpotDirection[STAPLE_MAX_LIGHTS];
uniform vec3 u_lightSpotValues[STAPLE_MAX_LIGHTS];

uniform mat3 u_normalMatrix;
uniform vec3 u_viewPos;

#define StapleLightDiffuse u_lightDiffuse
#define StapleLightSpecular u_lightSpecular
#define StapleLightTypePosition u_lightTypePosition
#define StapleLightSpotDirection u_lightSpotDirection

vec3 StapleLightNormal(vec3 normal)
{
	return normalize(mul(u_normalMatrix, normal));
}

float StapleLightScaling(float dotProduct)
{
#if HALF_LAMBERT
	return pow(dotProduct * 0.5 + 0.5, 2.0);
#else
	return dotProduct;
#endif
}

float StapleLightDiffuseFactor(vec3 normal, vec3 lightDir)
{
	return max(StapleLightScaling(dot(lightDir, normal)), 0.0);
}

float StapleLightSpecularFactor(vec3 viewPos, vec3 fragPos, vec3 lightDir, vec3 normal, float shininess)
{
	vec3 viewDir = normalize(viewPos - fragPos);
	vec3 reflectDir = reflect(-lightDir, normal);
	
	return pow(max(StapleLightScaling(dot(viewDir, reflectDir)), 0.0), shininess);
}

vec3 StapleLightDirection(int index, vec3 fragPos)
{
	vec4 typePosition = u_lightTypePosition[index];
	
	int lightType = int(typePosition.x);

	if(lightType == 0) //Spot
	{
		//TODO
	
		return normalize(typePosition.yzw - fragPos);
	}
	else if(lightType == 1) //Directional
	{
		return normalize(typePosition.yzw);
	}
	else if(lightType == 2) //Point
	{
		return normalize(typePosition.yzw - fragPos);
	}
	
	return normalize(typePosition.yzw - fragPos);
}

vec3 StapleProcessLights(vec3 viewPos, vec3 fragPos, vec3 normal)
{
	vec3 ambient = StapleLightAmbient.rgb;
	
	vec3 lightNormal = StapleLightNormal(normal);
	vec3 diffuse = vec3(0, 0, 0);
	vec3 specular = vec3(0, 0, 0);
	
	for(int i = 0; i < StapleLightCount; i++)
	{
		vec3 lightDir = StapleLightDirection(i, fragPos);
		
		float diffuseFactor = StapleLightDiffuseFactor(lightNormal, lightDir);
		
		diffuse += diffuseFactor * StapleLightDiffuse[i].rgb;
		
		float shininess = StapleLightSpecular[i].a;
		
		float specularFactor = StapleLightSpecularFactor(viewPos, fragPos, lightDir, lightNormal, shininess);
		
		specular += specularFactor * StapleLightDiffuse[i].rgb;
	}
	
	if(StapleLightCount == 0)
	{
		return ambient;
	}
	
	return ambient + diffuse; //+ specular;
}

#endif