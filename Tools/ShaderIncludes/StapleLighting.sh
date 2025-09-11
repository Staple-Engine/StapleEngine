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
	mat4 inverseMatrix = inverse(transpose(modelView));
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

vec3 StapleProcessLights(int instanceID, vec3 viewPos, vec3 fragPos, vec3 normal)
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
		vec3 lightDir = StapleLightDirection(i, fragPos);
		
		float diffuseFactor = StapleLightDiffuseFactor(normal, lightDir);
		
		diffuse += diffuseFactor * StapleLightDiffuse[i].rgb;
		
		float shininess = StapleLightSpecular[i].a;
		
		float specularFactor = StapleLightSpecularFactor(viewPos, fragPos, lightDir, normal, shininess);
		
		specular += specularFactor * StapleLightDiffuse[i].rgb;
	}
	
	return ambient + diffuse; //+ specular;
}

vec3 StapleProcessLightsTangent(int instanceID, vec3 viewPos, vec3 fragPos, vec3 normal, mat3 tangentMatrix)
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
		vec3 lightDir = StapleLightDirection(i, fragPos);
		
		lightDir = normalize(mul(lightDir, tangentMatrix));
		
		float diffuseFactor = StapleLightDiffuseFactor(normal, lightDir);
		
		diffuse += diffuseFactor * StapleLightDiffuse[i].rgb;
		
		float shininess = StapleLightSpecular[i].a;
		
		float specularFactor = StapleLightSpecularFactor(viewPos, fragPos, lightDir, normal, shininess);
		
		specular += specularFactor * StapleLightDiffuse[i].rgb;
	}
	
	return ambient + diffuse; //+ specular;
}

#endif