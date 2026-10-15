// Blurs and slightly darkens everything drawn behind the dialogue overlay.
// It is applied to a full-rect ColorRect in StageDialogue: the shader reads
// the screen behind the rect (hint_screen_texture) and redraws it blurred,
// so the level map appears out of focus while a conversation runs.
shader_type canvas_item;

uniform sampler2D screen_tex : hint_screen_texture, filter_linear;

// How far the blur spreads (in scaled screen pixels). Higher = blurrier.
uniform float blur_strength : hint_range(0.0, 10.0) = 2.0;

// How much to darken the blurred image so dialogue text pops (0 = none).
uniform float darkness : hint_range(0.0, 1.0) = 0.35;

void fragment() {
    // 9-tap gaussian: center pixel plus 8 neighbours, weights sum to 1.
    vec2 px = SCREEN_PIXEL_SIZE * blur_strength;
    vec4 c = texture(screen_tex, SCREEN_UV) * 0.19648255;
    c += texture(screen_tex, SCREEN_UV + vec2( px.x,  0.0)) * 0.13961057;
    c += texture(screen_tex, SCREEN_UV + vec2(-px.x,  0.0)) * 0.13961057;
    c += texture(screen_tex, SCREEN_UV + vec2( 0.0,  px.y)) * 0.13961057;
    c += texture(screen_tex, SCREEN_UV + vec2( 0.0, -px.y)) * 0.13961057;
    c += texture(screen_tex, SCREEN_UV + px) * 0.08634874;
    c += texture(screen_tex, SCREEN_UV - px) * 0.08634874;
    c += texture(screen_tex, SCREEN_UV + vec2( px.x, -px.y)) * 0.08634874;
    c += texture(screen_tex, SCREEN_UV + vec2(-px.x,  px.y)) * 0.08634874;
    c.rgb = mix(c.rgb, vec3(0.0, 0.0, 0.02), darkness);
    COLOR = vec4(c.rgb, 1.0);
}
