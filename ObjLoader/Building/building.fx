cbuffer vsBuffer
{
	matrix wvp;
};

cbuffer colorBuffer
{
	float4 faceColor;
	float4 outlineColor;
};

struct VsInput
{
	float4 pos : POSITION;
};

struct GsInput
{
	float4 pos : SV_POSITION;
};

struct PsInput
{
	float4 pos : SV_POSITION;
	float4 col : COLOR0;
};

GsInput Vs(VsInput input)
{
	GsInput res;
	res.pos = mul(input.pos, wvp);
	return res;
}

[maxvertexcount(3)]
void Gs(triangleadj GsInput input[6], inout TriangleStream<PsInput> outStream)
{
	PsInput triang;
	triang.col = faceColor;

	// Draw original triangle.

	triang.pos = input[0].pos;
	outStream.Append(triang);

	triang.pos = input[2].pos;
	outStream.Append(triang);
	
	triang.pos = input[4].pos;
	outStream.Append(triang);

	// Find outlines.

	//outStream.RestartStrip();
}

float4 Ps(PsInput input) : SV_TARGET
{
	return input.col;
}