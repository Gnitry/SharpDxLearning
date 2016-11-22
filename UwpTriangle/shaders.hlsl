float4x4 worldViewProj;

float4 vs(float4 pos : POSITION) : SV_POSITION
{
	return mul(pos, worldViewProj);
}

float4 ps(float4 pos: SV_POSITION) : SV_TARGET
{
	return float4(0.5, 0.5, 0.5, 0.5);
}