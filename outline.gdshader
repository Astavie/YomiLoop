shader_type canvas_item;

uniform vec3 line_colour: source_color = vec3(1.0);
uniform int line_thickness: hint_range(0, 10) = 0;
uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_nearest;

void fragment() {
    vec2 pixel_size = vec2(1.0 / 480.0, 1.0 / 270.0);
	vec2 size = pixel_size * float(line_thickness);
	float line_alpha = 0.0;
	for (float i = -size.x; i <= size.x; i += pixel_size.x) {
		for (float j = -size.y; j <= size.y; j += pixel_size.y) {
			line_alpha += texture(screen_texture, SCREEN_UV + vec2(i, j)).a;
		}
	}
	vec4 colour = texture(screen_texture, SCREEN_UV);
	vec4 outline = vec4(line_colour, min(line_alpha, 1.0));
	COLOR *= mix(outline, colour, colour.a);
}
