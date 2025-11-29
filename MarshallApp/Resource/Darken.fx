sampler2D input : register(s0);

float Darkness : register(c0);

float4 main(float2 uv : TEXCOORD) : COLOR
{
    float4 color = tex2D(input, uv);
    color.rgb *= (1 - Darkness); 
    return color;
}

technique t0 
{
    pass p0 
    { 
        PixelShader = compile ps_2_0 main(); 
    }
}
