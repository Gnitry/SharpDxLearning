
float4 vs(float4 pos : POSITION) : SV_POSITION
{
	return pos;
}

float4 ps(float4 pos : SV_POSITION) : SV_TARGET
{
	return float4(1.0, 0.5, 0.2, 1.0);
}