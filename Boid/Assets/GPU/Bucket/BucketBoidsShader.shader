﻿Shader "Custom/BoidsShader" {
    Properties {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
    }
    SubShader {
        Pass {

            Tags {"LightMode" = "ForwardBase"}
            
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"
            
            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
            #pragma target 4.5

            struct Boid 
            {
                float3 velocity;
                float3 position;
                int id;//通常のBoidShaderとの違いはここだけ
            };
            
            sampler2D _MainTex;
            float3 _Scale;
            fixed4 _Color;

        #if SHADER_TARGET >= 45
            StructuredBuffer<Boid> _BoidBuffer;
        #endif

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 normal : NORMAL;
                float2 uv : TEXCOORD0;
                SHADOW_COORDS(2)
                fixed3 diff : COLOR0;
                fixed3 ambient : COLOR1;
                
            };
            
            //アフィン変換の行列を作成する
            float4x4 transform(float3 scale, float3 angles, float3 position)
            {
                float4x4 mat = 0;
                
                //scaleを適応
                mat._11_22_33_44 = float4(scale, 1);
                
                //angleを適応
                float sh, ch, cb, sb, ca, sa;
                sincos(angles.y, sh, ch);
                sincos(angles.z, sa, ca);
                sincos(angles.x, sb, cb);  
                //Ry * Rx * Rzの順で回転行列を掛ける
                fixed4x4 rot = fixed4x4(
                     ch * ca + sh * sb * sa, -ch * sa + sh * sb * ca, sh * cb, 0,
                                    cb * sa,                 cb * ca,     -sb, 0,
                    -sh * ca + ch * sb * sa,  sh * sa + ch * sb * ca, ch * cb, 0,
                                          0,                      0,       0, 1
                );
                mat = mul(rot, mat);
                
                //positionの適応
                mat._14_24_34 = position;  
                
                return mat;
            }

            v2f vert (appdata_full v, uint instanceID : SV_InstanceID)
            {
            #if SHADER_TARGET >= 45
                Boid b = _BoidBuffer[instanceID];
                float3 vel = b.velocity;
                float3 pos = b.position;
            #else
                float3 vel = 0;
                float3 pos = 0;
            #endif

                float rotX = -asin(vel.y/length(vel.xyz) + 1e-8);//0除算防止
                float rotY = atan2(vel.x, vel.z);
                
                float4x4 object2world = transform(
                    _Scale,
                    float3(rotX, rotY, 0),
                    pos
                );
                
                //以下からはチュートリアル通りにライティングを行う
                v2f o;
                o.pos = UnityObjectToClipPos(mul(object2world, v.vertex));
                o.normal = normalize(mul(object2world, v.normal));
                o.uv = v.texcoord;
                half nl = max(0, dot(o.normal, _WorldSpaceLightPos0.xyz));
                o.diff = nl * _LightColor0.rgb;
                o.ambient = ShadeSH9(half4(o.normal.xyz, 1));
                TRANSFER_SHADOW(o)
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed shadow = SHADOW_ATTENUATION(i);
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                fixed3 lighting = i.diff * shadow + i.ambient;
                col.rgb *= lighting;
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }

            ENDCG
        }
        
        UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
    }
}
