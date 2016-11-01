float4 PS(float4 pos : SV_POSITION) : SV_TARGET
{
	return float4(1.0, 0.5, 0.2, 1.0);
}

technique10 {
	pass pass0 {
		SetPixelShader( CompileShader(px_4_0, PS()) );
	};
};