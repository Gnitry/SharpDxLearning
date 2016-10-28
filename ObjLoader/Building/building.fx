cbuffer vsBuffer
{
	matrix wvp;
}

float4 vs(float4 pos : POSITION) : SV_POSITION
{
	return mul(pos, wvp);
}

float4 ps(float4 pos: SV_POSITION) : SV_TARGET
{
	//return float4(0x2D / 255.0, 0x45 / 255.0, 0x64 / 255.0, 1.0);
	return float4(1.0, 1.0, 1.0, 0.3);
}