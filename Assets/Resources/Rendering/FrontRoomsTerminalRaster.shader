// FrontRooms office terminal: the VGA text-mode raster (game visual, 2026-10-30; spec research/computer_ui/41 section 3.1).
// One offscreen pass, run only when the terminal's screen changes (a key, the clock's minute): it turns the 80 x 25
// (glyph, attribute) cells into the 720 x 400 text-mode picture at 2x (1440 x 800), whole pixels only, into the
// terminal's RenderTexture; the caller regenerates its mips. The CRT tube (FrontRooms/ScreenVideo) then draws that
// texture on the desk monitor like any picture, stretching 720 x 400 to the 4:3 tube as the monitor did.
//   _TermCells   80 x 25 RGBA8 (linear): r = CP437 glyph, g = attribute (bg << 4 | fg); memory row = screen row (top first)
//   _TermFont    128 x 256 R8: the 8 x 16 face in CP437 order, memory row = glyph row (top first)
//   _TermPalette the 16 text colours, linear (the target is sRGB: the store encodes them back to the exact bytes)
// A cell is 9 x 16: the 9th column is blank except for CP437 0xC0-0xDF, which repeat their 8th column (VGA).
// No keywords, no time, no blink. FrontRoomsTerminalRaster.RasterCpu is the same function on the CPU (the fallback and
// the harness's pixel check).
Shader "Hidden/FrontRooms/TerminalRaster"
{
    SubShader
    {
        Pass
        {
            Name "TerminalRaster"
            ZTest Always ZWrite Off Cull Off Blend Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.5
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

            TEXTURE2D(_TermCells);
            TEXTURE2D(_TermFont);
            float4 _TermPalette[16];

            struct Attributes { uint vertexID : SV_VertexID; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings Vert(Attributes input)
            {
                Varyings o;
                o.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                o.uv = GetFullScreenTriangleTexCoord(input.vertexID);
                return o;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                // the picture pixel, counted from the top-left (Unity texture space: v = 1 is the top row)
                float px = min(floor(input.uv.x * 1440.0), 1439.0);
                float py = min(floor((1.0 - input.uv.y) * 800.0), 799.0);
                float col = floor(px / 18.0);
                float row = floor(py / 32.0);
                float cx = floor((px - col * 18.0) * 0.5);      // 0..8 within the 9-dot cell
                float cy = floor((py - row * 32.0) * 0.5);      // 0..15
                float4 cell = LOAD_TEXTURE2D(_TermCells, uint2((uint)col, (uint)row));
                float glyph = floor(cell.r * 255.0 + 0.5);
                float attr = floor(cell.g * 255.0 + 0.5);
                float gx = cx;
                float use = 1.0;
                if (cx > 7.5)
                {
                    gx = 7.0;
                    use = (glyph >= 192.0 && glyph <= 223.0) ? 1.0 : 0.0;
                }
                float gcol = glyph - 16.0 * floor(glyph / 16.0);
                float grow = floor(glyph / 16.0);
                float ink = LOAD_TEXTURE2D(_TermFont, uint2((uint)(gcol * 8.0 + gx), (uint)(grow * 16.0 + cy))).r;
                float fgI = attr - 16.0 * floor(attr / 16.0);
                float bgI = floor(attr / 16.0);
                float on = step(0.5, ink) * use;
                float3 c = on > 0.5 ? _TermPalette[(int)fgI].rgb : _TermPalette[(int)bgI].rgb;
                return float4(c, 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
