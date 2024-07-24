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

#define StapleLightDiffuse u_lightDiffuse
#define StapleLightSpecular u_lightSpecular
#define StapleLightTypePosition u_lightTypePosition
#define StapleLightSpotDirection u_lightSpotDirection

vec3 StapleLightNormal(vec3 normal)
{
	return normalize(mul(u_normalMatrix, normal));
}

float StapleLightDiffuseFactor(vec3 normal, vec3 lightDir)
{
	return max(dot(normal, lightDir), 0.0);
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

vec4 StapleProcessLights(vec3 fragPos, vec3 normal)
{
	vec4 ambient = StapleLightAmbient;
	
	vec3 lightNormal = StapleLightNormal(normal);
	vec4 diffuse = vec4(1, 1, 1, 1);
	
	for(int i = 0; i < StapleLightCount; i++)
	{
		vec3 lightDir = StapleLightDirection(i, fragPos);
		
		float diffuseFactor = StapleLightDiffuseFactor(lightNormal, lightDir);
		
		diffuse *= diffuseFactor * StapleLightDiffuse[i];
	}
	
	return ambient + diffuse;
}

#endif