Shader "Huatuo/HuatuoLogoEffect"
{
    Properties
    {
        _MainTex("MainTex",2D)="white"{}
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Transparent" 
            "Queue"="Transparent" 
            "IgnoreProjector"="True"
        }
        LOD 200
        pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            sampler2D _MainTex;
            float4 _MainTex_ST;

            struct appdata
            {
                float4 vertex:POSITION;
                float2 texcoord:TEXCOORD0;
            };

            struct v2f
            {
                float4 pos:POSITION;
                float2 uv:TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord,_MainTex);
                return o;
            }
                        
            float4 frag(v2f o):SV_Target
            {
                float2 c = (tex2D(_MainTex,o.uv.xy+float2(0,_Time.x)).gb+tex2D(_MainTex,o.uv.xy+float2(_Time.x,0)).gb)-1;
                float2 ruv = o.uv.xy + c.xy * 0.01;
                return tex2D(_MainTex,ruv);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"    
}
