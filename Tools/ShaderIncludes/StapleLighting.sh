#ifndef STAPLE_LIGHTING_GUARD
#define STAPLE_LIGHTING_GUARD

#include <bgfx_compute.sh>
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

uniform vec3 u_viewPos;

#define StapleLightDiffuse u_lightDiffuse
#define StapleLightSpecular u_lightSpecular
#define StapleLightTypePosition u_lightTypePosition
#define StapleLightSpotDirection u_lightSpotDirection

vec3 StapleLightNormal(vec3 normal, mat4 modelView)
{
	mat4 inverseMatrix = transpose(inverse(modelView));

	mat3 normalMatrix = mat3(inverseMatrix[0].xyz,
		inverseMatrix[1].xyz,
		inverseMatrix[2].xyz);
	
	return normalize(mul(normalMatrix, normal));
}

float StapleLightScaling(float dotProduct)
{
#if HALF_LAMBERT
	return pow(dotProduct * 0.5 + 0.5, 2.0);
#else
	return dotProduct;
#endif
}

float StapleLightDiffuseFactor(vec3 normal, vec3 lightDirection)
{
	return max(StapleLightScaling(dot(lightDirection, normal)), 0.0);
}

float StapleLightSpecularFactor(vec3 viewPosition, vec3 worldPosition, vec3 lightDirection, vec3 normal, float shininess)
{
	vec3 viewDirection = normalize(viewPosition - worldPosition);
	vec3 reflectDirection = reflect(-lightDirection, normal);
	
	return pow(max(StapleLightScaling(dot(viewDirection, reflectDirection)), 0.0), shininess);
}

vec3 StapleLightDirection(int index, vec3 worldPosition)
{
	vec4 typePosition = u_lightTypePosition[index];
	
	int lightType = int(typePosition.x);
	vec3 position = typePosition.yzw;

	if(lightType == 0) //Spot
	{
		//TODO
	
		return normalize(position - worldPosition);
	}
	else if(lightType == 1) //Directional
	{
		return normalize(position);
	}
	else if(lightType == 2) //Point
	{
		return normalize(position - worldPosition);
	}
	
	return normalize(position - worldPosition);
}

vec3 StapleProcessLights(int instanceID, vec3 viewPosition, vec3 worldPosition, vec3 normal)
{
	vec3 ambient = StapleLightAmbient.rgb;
	
	if(StapleLightCount == 0)
	{
		return ambient;
	}
	
	vec3 diffuse = vec3(0, 0, 0);
	vec3 specular = vec3(0, 0, 0);
	
	for(int i = 0; i < StapleLightCount; i++)
	{
		vec3 lightDirection = StapleLightDirection(i, worldPosition);
		
		float diffuseFactor = StapleLightDiffuseFactor(normal, lightDirection);
		
		diffuse += diffuseFactor * StapleLightDiffuse[i].rgb;
		
		float shininess = StapleLightSpecular[i].a;
		
		float specularFactor = StapleLightSpecularFactor(viewPosition, worldPosition, lightDirection, normal, shininess);
		
		specular += specularFactor * StapleLightDiffuse[i].rgb;
	}
	
	return ambient + diffuse; //+ specular;
}

vec3 StapleProcessLightsTangent(int instanceID, vec3 viewPosition, vec3 worldPosition, vec3 normal, mat3 tangentMatrix)
{
	vec3 ambient = StapleLightAmbient.rgb;
	
	if(StapleLightCount == 0)
	{
		return ambient;
	}
	
	vec3 diffuse = vec3(0, 0, 0);
	vec3 specular = vec3(0, 0, 0);
	
	for(int i = 0; i < StapleLightCount; i++)
	{
		vec3 lightDirection = StapleLightDirection(i, worldPosition);
		
		lightDirection = normalize(mul(lightDirection, tangentMatrix));
		
		float diffuseFactor = StapleLightDiffuseFactor(normal, lightDirection);
		
		diffuse += diffuseFactor * StapleLightDiffuse[i].rgb;
		
		float shininess = StapleLightSpecular[i].a;
		
		float specularFactor = StapleLightSpecularFactor(viewPosition, worldPosition, lightDirection, normal, shininess);
		
		specular += specularFactor * StapleLightDiffuse[i].rgb;
	}
	
	return ambient + diffuse; //+ specular;
}

#endif