Shader "Unlit/SplatShader"
{
    Properties
    {
        _FlashlightAngle ("Flashlight Angle", Float) = 45.0
        _ScannerPos ("Scanner Position", Float) = -1000.0
        _ScannerWidth ("Scanner Width", Float) = 0.5
        _ScannerActive ("Scanner Active", Float) = 0.0
        _FlashlightFalloffTex ("Flashlight Falloff Texture", 2D) = "white" {}
        _ScaleFactor ("Scale Factor", Float) = 1.0
    }
    SubShader
    {
        Tags { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent" 
            "IgnoreProjector" = "True"
        }
        LOD 100
        
        ZWrite Off
        ZTest Less
        Cull Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 centerPos : TEXCOORD1;
                float3 color : TEXCOORD2;
                float opacity : TEXCOORD3;     
                float3 cov0_3 : TEXCOORD5;
                float3 cov3_6 : TEXCOORD6;   
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 color : TEXCOORD0;
                float opacity : TEXCOORD1;
                float2 vpositionScaled : TEXCOORD4;
                float3 worldCorner : TEXCOORD5;
            };
            
            float3x3 create_mat3(float3 c0, float3 c1, float3 c2)
            {
                return float3x3(c0, c1, c2);
            }

            float4x4 _FlashlightWorldToLocal;
            float _FlashlightAngle;
            float _FlashlightRange;
            float _ScannerPos;
            float _ScannerWidth;
            float _ScannerActive;
            sampler2D _FlashlightFalloffTex;
            float _ScaleFactor;
            v2f vert (appdata v)
            {
                // the last column of UNITY_MATRIX_M (i.e., UNITY_MATRIX_M[0].w, UNITY_MATRIX_M[1].w, UNITY_MATRIX_M[2].w) contains the position of the object (directly without sign-flips, even for the third component)
                // now: in the neutral/origin case: UNITY_MATRIX_M = Identity

                // UNITY_MATRIX_V[row_id].{x,y,z,w} where w is the fourth column
                // in rotation zero case, then last column is the sign-flipped camera position
                // UNITY_MATRIX_V has [2].z at -1, because shaders use OpenGL convention
                // https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Camera-worldToCameraMatrix.html
                
                // artificially set to 1 to obtain numeric equivalance at the input level
                // In Unity, the camera space flips the z-axis to obtain OpenGL convention
                // UNITY_MATRIX_V[2].z = 1;
                 
                // UNITY_MATRIX_P[1].y == -2.4142134 // up-down flipped as indicated by _ProjectionParams.x == -1, see https://docs.unity3d.com/Manual/SL-PlatformDifferences.html
                // abs(UNITY_MATRIX_P[2].z - 0.001001) < 0.000001 // reverse near/far                 
                // In Unity, the m11 of the projection matrix is flipped to obtain OpenGL up-down convention 
                // UNITY_MATRIX_P[1].y = UNITY_MATRIX_P[1].y * -1; // thereby achieving: UNITY_MATRIX_P[1].y == 2.4142134
                // UNITY_MATRIX_P[2].z = -1.002002;
                // UNITY_MATRIX_P[2].w = -0.2002002;

                float width = _ScreenParams.x;
                float height = _ScreenParams.y;

                // Calculate FOV from projection matrix
                float fovy = 2.0 * degrees(atan(1.0 / unity_CameraProjection._m11));
                float tanfovy = tan(radians(fovy) * 0.5);
                float tanfovx = tanfovy * (width / height);
                float focalX = 0.5 * width / tanfovx;
                float focalY = 0.5 * height / tanfovy;

                v2f o;

                o.vertex = float4(200.0, 200.0, 200.0, 1.0);

                float3 scaledCenterPos = v.centerPos * _ScaleFactor;
                float4 worldCenter = mul(UNITY_MATRIX_M, float4(scaledCenterPos, 1.0));
                
                float4 viewCenter = mul(UNITY_MATRIX_V, worldCenter);
                float4 clipCenter = mul(UNITY_MATRIX_P, viewCenter);
                float3 ndcCenter = clipCenter.xyz / clipCenter.w;
                if (_ProjectionParams.x < 0) ndcCenter.y = - ndcCenter.y;

                o.vertex = float4(ndcCenter.xyz, 0.0) + float4(v.vertex.xyz * 0.01, 1.0);
                o.vpositionScaled = sqrt(8.0) * v.vertex.xy;
                o.color = GammaToLinearSpace(v.color);
                o.opacity = v.opacity;

                // (idx 30242, unsorted, position convention-converted, rotation, not convention-converted to establish numeric equivalence)

                // ---              
             
                float clip = 1.2 * clipCenter.w;
                if (clipCenter.z < -clip || 
                    clipCenter.x < -clip || 
                    clipCenter.x > clip || 
                    clipCenter.y < -clip || 
                    clipCenter.y > clip)
                {
                    
                    o.color = float4(1.0, 1.0, 0.0, 1.0);
                    return o;
                }

                // if(
                //     width == 1280.0 && height == 720.0 &&
                //     abs(fovy - 45.0) <= 0.01 &&
                //     abs(tanfovy - 0.41421) <= 0.0001 &&
                //     abs(tanfovx - 0.73637) <= 0.0001 &&
                //     abs(focalX - 869.0) <= 1.0 &&
                //     abs(focalY - 869.0) <= 1.0 &&
                //     UNITY_MATRIX_M[0][0] == 1.0 && UNITY_MATRIX_M[0][1] == 0.0 && UNITY_MATRIX_M[0][2] == 0.0 && UNITY_MATRIX_M[0][3] == 0.0 &&
                //     UNITY_MATRIX_M[1][0] == 0.0 && UNITY_MATRIX_M[1][1] == 1.0 && UNITY_MATRIX_M[1][2] == 0.0 && UNITY_MATRIX_M[1][3] == 0.0 &&
                //     UNITY_MATRIX_M[2][0] == 0.0 && UNITY_MATRIX_M[2][1] == 0.0 && UNITY_MATRIX_M[2][2] == 1.0 && UNITY_MATRIX_M[2][3] == 0.0 &&
                //     UNITY_MATRIX_M[3][0] == 0.0 && UNITY_MATRIX_M[3][1] == 0.0 && UNITY_MATRIX_M[3][2] == 0.0 && UNITY_MATRIX_M[3][3] == 1.0 &&
                //     UNITY_MATRIX_V[0][0] == 1.0 && UNITY_MATRIX_V[0][1] == 0.0 && UNITY_MATRIX_V[0][2] == 0.0 && UNITY_MATRIX_V[0][3] == 0.0 &&
                //     UNITY_MATRIX_V[1][0] == 0.0 && UNITY_MATRIX_V[1][1] == 1.0 && UNITY_MATRIX_V[1][2] == 0.0 && UNITY_MATRIX_V[1][3] == 0.0 &&
                //     UNITY_MATRIX_V[2][0] == 0.0 && UNITY_MATRIX_V[2][1] == 0.0 && UNITY_MATRIX_V[2][2] == 1.0 && UNITY_MATRIX_V[2][3] == 0.0 && // OVERRRIDDEN!!!!
                //     UNITY_MATRIX_V[3][0] == 0.0 && UNITY_MATRIX_V[3][1] == 0.0 && UNITY_MATRIX_V[3][2] == 0.0 && UNITY_MATRIX_V[3][3] == 1.0 &&
                //     abs(UNITY_MATRIX_P[0][0] - 1.3579952) <= 0.0001 && UNITY_MATRIX_P[0][1] == 0.0 && UNITY_MATRIX_P[0][2] == 0.0 && UNITY_MATRIX_P[0][3] == 0.0 &&
                //     UNITY_MATRIX_P[1][0] == 0.0 && abs(UNITY_MATRIX_P[1][1] - 2.4142134) <= 0.0001 && UNITY_MATRIX_P[1][2] == 0.0 && UNITY_MATRIX_P[1][3] == 0.0 &&  // OVERRRIDDEN!!!!
                //     UNITY_MATRIX_P[2][0] == 0.0 && UNITY_MATRIX_P[2][1] == 0.0 && UNITY_MATRIX_P[2][2] == -1.002002 && UNITY_MATRIX_P[2][3] == -0.2002002 && // OVERRRIDDEN!!!!
                //     UNITY_MATRIX_P[3][0] == 0.0 && UNITY_MATRIX_P[3][1] == 0.0 && UNITY_MATRIX_P[3][2] == -1.0 && UNITY_MATRIX_P[3][3] == 0.0 &&
                //     abs(v.centerPos.x - -0.78123) <= 0.0001 &&
                //     abs(v.centerPos.y - 0.65588) <= 0.0001 &&
                //     abs(v.centerPos.z - -3.1099) <= 0.0001 &&
                //     abs(viewCenter.x - -0.78123814) <= 0.0001 &&
                //     abs(viewCenter.y - 0.6558889) <= 0.0001 &&
                //     abs(viewCenter.z - -3.1099024) <= 0.0001 &&
                //     abs(clipCenter.x - -1.060917588580011) <= 0.0001 &&
                //     abs(clipCenter.y - 1.5834558777899705) <= 0.0001 &&
                //     abs(clipCenter.z - 2.9159282306306307) <= 0.0001 &&
                //     abs(ndcCenter.x - -0.34114176334923274) <= 0.0001 &&
                //     abs(ndcCenter.y - 0.5091657788971031) <= 0.0001 &&
                //     abs(ndcCenter.z - 0.9376269270156615) <= 0.0001 &&
                //     abs(clipCenter.w - 3.1099024) <= 0.0001 &&
                //     abs(v.cov0_3.x - 0.01403388) <= 0.0001 && abs(v.cov0_3.y - 0.00045061) <= 0.0001 && abs(v.cov0_3.z - 0.00589865) <= 0.0001 &&
                //     abs(v.cov3_6.x - 0.0034403) <= 0.0001 && abs(v.cov3_6.y - 0.00178671) <= 0.0001 && abs(v.cov3_6.z - 0.00323099) <= 0.0001
                // ) {
                    // o.color = float4(0.0, 1.0, 0.0, 1.0);
                // } else {
                    // o.vertex = float4(0.0, 0.0, 0.001, 0.0) + float4(v.vertex.xyz * 0.1, 1.0);
                    // o.color = float4(1.0, 0.0, 0.0, 1.0);
                    // return o;
                // }
                
                
                
                // return o;
                // Calculate world position of the splat center
      


                if (_ScannerActive > 0.5) {
                    float distanceFromScanner = abs(v.centerPos.x - _ScannerPos);
                    
                    if (distanceFromScanner > _ScannerWidth) {
                        o.vertex = float4(0, 0, 0, 0);
                        o.color = float4(0, 0, 0, 0);
                        o.opacity = 0;
                        o.vpositionScaled = float2(0, 0);
                        return o;
                    }
                }
                float3x3 Vrk = create_mat3(
                    float3(v.cov0_3.x, v.cov0_3.y, v.cov0_3.z) * (_ScaleFactor * _ScaleFactor),
                    float3(v.cov0_3.y, v.cov3_6.x, v.cov3_6.y) * (_ScaleFactor * _ScaleFactor),
                    float3(v.cov0_3.z, v.cov3_6.y, v.cov3_6.z) * (_ScaleFactor * _ScaleFactor)
                );

                float s = 1.0 / (viewCenter.z * viewCenter.z);

                float3x3 J = transpose(create_mat3(
                    float3(focalX / viewCenter.z, 0, -(focalX * viewCenter.x) * s),
                    float3(0, focalY / viewCenter.z, -(focalY * viewCenter.y) * s),
                    float3(0, 0, 0)
                ));

                float3x3 W = transpose((float3x3)mul(UNITY_MATRIX_V, UNITY_MATRIX_M));
                float3x3 T = mul(W, J);

                float3x3 cov2Dm = mul(mul(transpose(T), Vrk), T);

                // cov2Dm[0][0] > 1096.006 && cov2Dm[0][0] < 1096.009
                // if(
                //     abs(s - 0.1033966) < 0.0001 &&
                //     abs(focalX - 869.0) <= 1.0 &&
                //     abs(focalY - 869.0) <= 1.0 &&                    
                //     abs(viewCenter.x - -0.78123814) <= 0.0001 &&
                //     abs(viewCenter.y - 0.6558889) <= 0.0001 &&
                //     abs(viewCenter.z - -3.1099024) <= 0.0001 &&
                //     abs(cov2Dm[0][0] - 880.5369464) < 0.1 &&
                //     abs(cov2Dm[1][1] - 338.78086604) < 0.1 &&
                //     abs(cov2Dm[0][1] - 83.9313776) < 0.1
                // ) {
                    // o.color = float4(0.0, 1.0, 0.0, 1.0);
                // } else {
                    // o.color = float4(1.0, 0.0, 0.0, 1.0);
                // } 
                // o.vertex = float4(0.0, 0.0, 0.001, 0.0) + float4(v.vertex.xyz * 0.1, 1.0);
                // return o;

                cov2Dm[0][0] += 0.3;
                cov2Dm[1][1] += 0.3;

                float a = cov2Dm[0][0];
                float d = cov2Dm[1][1];
                float b = cov2Dm[0][1];
                float D = a * d - b * b;
                float trace = a + d;
                float traceOver2 = 0.5 * trace;
                float term2 = sqrt(max(0.1, traceOver2 * traceOver2 - D));
                float eigenValue1 = traceOver2 + term2;
                float eigenValue2 = traceOver2 - term2;

                if (eigenValue2 <= 0.0) {
                    o.color = float4(0.0, 1.0, 1.0, 1.0);
                    o.opacity = 0;
                    return o;
                 } 

                float2 eigenVector1 = normalize(float2(b, eigenValue1 - a));
                float2 eigenVector2 = float2(eigenVector1.y, -eigenVector1.x);

                float sqrt8 = sqrt(8.0);
                float2 basisVector1 = eigenVector1 * min(sqrt8 * sqrt(eigenValue1), 1024.0);
                float2 basisVector2 = eigenVector2 * min(sqrt8 * sqrt(eigenValue2), 1024.0);
                
                // STANDARD NDC COMPUTATION
                // float2 basisViewport = float2(1.0 / width, 1.0 / height);
                // float2 ndcOffset = float2(v.vertex.x * basisVector1 + v.vertex.y * basisVector2) * basisViewport * 2.0;
                // o.vertex = float4(ndcCenter.xy + ndcOffset, ndcCenter.z, 1.0);


                float2 pixelBasis1 = basisVector1 * (1.0 / focalX);
                float2 pixelBasis2 = basisVector2 * (1.0 / focalY);

                float3 worldBasis1 = mul((float3x3)UNITY_MATRIX_I_V, float3(pixelBasis1, 0.0)) * viewCenter.z;
                float3 worldBasis2 = mul((float3x3)UNITY_MATRIX_I_V, float3(pixelBasis2, 0.0)) * viewCenter.z;

                float3 worldCorner = worldCenter.xyz + v.vertex.x * worldBasis1 + v.vertex.y * worldBasis2;

                float4 clipPos = mul(UNITY_MATRIX_VP, float4(worldCorner, 1.0));
                o.vertex = clipPos / clipPos.w;

                o.worldCorner = worldCorner;


                if (_FlashlightAngle != 0.0)
                {
                    float4 flashlightSpacePos = float4(0.0, 0.0, 0.0, 0.0);
                    // flashlightSpacePos = mul(_FlashlightWorldToLocal, float4(worldCorner, 1.0)); // discards one of the quad-triangle, leading to hard edges
                    flashlightSpacePos = mul(_FlashlightWorldToLocal, float4(worldCenter.xyz, 1.0));

                    float distanceToLight = length(flashlightSpacePos.xyz);
                    float angleFromCenter = degrees(acos(flashlightSpacePos.z / distanceToLight));
                    
                    if (angleFromCenter > _FlashlightAngle * 0.5) {
                        o.vertex = float4(0, 0, 0, 0);
                        o.color = float4(0, 0, 0, 0);
                        o.opacity = 0;
                        o.vpositionScaled = float2(0, 0);
                        return o;
                    }
                }

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float A = dot(i.vpositionScaled, i.vpositionScaled);
                if (A > 8.0) discard;
                
                float flashlightOpacity = 1.0;
                if (_FlashlightAngle != 0.0)
                {
                    float4 flashlightSpacePos = mul(_FlashlightWorldToLocal, float4(i.worldCorner, 1.0));
                    float distanceToLight = length(flashlightSpacePos.xyz);
                    float angleFromCenter = degrees(acos(flashlightSpacePos.z / distanceToLight));
                    float normalizedAngle = saturate(angleFromCenter / (_FlashlightAngle * 0.5));
                    flashlightOpacity = tex2D(_FlashlightFalloffTex, float2(normalizedAngle, 0)).r;
                }

                float baseOpacity = i.opacity * flashlightOpacity;
                float opacity = exp(-0.5 * A) * baseOpacity;
                
                return fixed4(i.color * opacity, opacity);
            }
            ENDCG
        }
    }
}
    