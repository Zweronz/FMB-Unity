Shader "FMB/Transparent Diffuse" {
    Properties {
        _MainTex ("Base (RGB)", 2D) = "white" {}
    }
    SubShader {
        Cull Off
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
        LOD 150
    
    CGPROGRAM
    #pragma surface surf Lambert alpha:fade noforwardadd
    
    sampler2D _MainTex;
    
    struct Input {
        float2 uv_MainTex;
        float4 color : COLOR;
    };


    
    void surf (Input IN, inout SurfaceOutput o) {
        fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * IN.color;
        o.Albedo = c.rgb;
        o.Alpha = c.a;
    }
    ENDCG
    }
    
    Fallback "Legacy Shaders/VertexLit"
    }