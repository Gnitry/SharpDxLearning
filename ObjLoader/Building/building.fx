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
	//res.pos = input.pos;
	return res;
}

void CreateVertex(inout TriangleStream<PsInput> outStream, float4 pos, bool isOutline)
{
	PsInput triang;
	triang.pos = pos;

	if (isOutline)
		triang.col = float4(1, 1, 1, 1);
	else
		triang.col = faceColor;

	outStream.Append(triang);
}

[maxvertexcount(12)]
void Gs(triangleadj GsInput inputs[6], inout TriangleStream<PsInput> outStream)
{
	float thickness = 0.1;

	CreateVertex(outStream, inputs[0].pos, false);
	CreateVertex(outStream, inputs[2].pos, false);
	CreateVertex(outStream, inputs[4].pos, false);
	outStream.RestartStrip();

	float4 pA = inputs[0].pos;
	float4 pB = inputs[2].pos;
	float4 pC = inputs[4].pos;
	float3 viewDirection = ((pA + pB + pC) / 3).xyz;
	viewDirection = -normalize(viewDirection);
	float3 faceNormal = normalize(cross((pB-pA).xyz, (pC-pA).xyz));
	float dotView = dot(faceNormal, viewDirection);
	bool isFrontFace = dotView > 0;
	
	if (!isFrontFace) return;

	for (uint i = 0; i < 6; i += 2) {
		uint nextI = (i + 2) % 6;
		pA = inputs[i].pos;
		pB = inputs[i + 1].pos;
		pC = inputs[nextI].pos;

		float3 viewDirection = ((pA + pB + pC) / 3).xyz;
		viewDirection = -normalize(viewDirection);
		faceNormal = normalize(cross((pB - pA).xyz, (pC - pA).xyz));
		dotView = dot(faceNormal, viewDirection);

		if (dotView <= 0) {
			float4 pos = inputs[i].pos;
			pos.x -= thickness / 2;
			pos.y -= thickness / 2;
			CreateVertex(outStream, pos, true);

			pos = inputs[nextI].pos;
			pos.x -= thickness / 2;
			pos.y += thickness / 2;
			CreateVertex(outStream, pos, true);

			pos.x += thickness;
			pos.y -= thickness;
			CreateVertex(outStream, pos, true);

			pos = inputs[i].pos;
			pos.x += thickness / 2;
			//CreateVertex(outStream, pos, true);

			outStream.RestartStrip();
		}
	}
}

float4 Ps(PsInput input) : SV_TARGET
{
	return input.col;
}