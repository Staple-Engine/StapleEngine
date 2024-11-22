Type Compute

Begin Parameters

WOBuffer<vec4> myBuffer : 0

End Parameters

Begin Compute

#define GROUP_SIZE 512

NUM_THREADS(GROUP_SIZE, 1, 1)
void main()
{
	myBuffer[gl_GlobalInvocationID.x] = vec4(1.0, 2.0, 3.0, 4.0);
}

End Compute
