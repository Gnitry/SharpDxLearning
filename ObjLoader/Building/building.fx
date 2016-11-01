cbuffer vsBuffer
{
	matrix wvp;
}

struct VsInput {
	float4 pos : POSITION;
};

struct PsInput {
	float4 pos : SV_POSITION;
	float3 normal : NORMAL0;
};

PsInput Vs(VsInput input)
{
	PsInput res;
    res.pos = mul(input.pos, wvp);
    return res;
}

[maxvertexcount(3)]
void Gs(triangle PsInput input[3], inout TriangleStream<PsInput> outStream)
{
	outStream.Append(input[0]);
	outStream.Append(input[1]);
	outStream.Append(input[2]);
}

float4 Ps(PsInput input) : SV_TARGET
{
	//return float4(0x2D / 255.0, 0x45 / 255.0, 0x64 / 255.0, 1.0);
	return float4(1.0, 1.0, 1.0, 0.8);
}