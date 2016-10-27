float4 vs(float4 pos : POSITION) : SV_POSITION
{
	return pos;
}

float4 ps(float4 pos: SV_POSITION) : SV_TARGET
{
	return float4(0x60 / 255.0, 0x7d / 255.0, 0x8b / 255.0, 1.0);
}