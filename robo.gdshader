shader_type canvas_item;
render_mode unshaded;

uniform vec3 line_colour: source_color = vec3(1.0);
uniform int line_thickness: hint_range(0, 10) = 0;

uniform float aberration = 0.0;
uniform vec2 redDir = vec2(1, 0);
uniform vec2 greenDir = vec2(0, 1);
uniform vec2 blueDir = vec2(-1, 0);

uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_nearest;

vec2 rotate(vec2 uv, float angle)
{
	mat2 rotation = mat2(vec2(sin(angle), -cos(angle)),
						vec2(cos(angle), sin(angle)));
	return uv * rotation;
}

vec4 outline_color(vec2 screen_uv) {
    vec2 pixel_size = vec2(1.0 / 480.0, 1.0 / 270.0);
	vec2 size = pixel_size * float(line_thickness);
	float line_alpha = 0.0;
	for (float i = -size.x; i <= size.x; i += pixel_size.x) {
		for (float j = -size.y; j <= size.y; j += pixel_size.y) {
			line_alpha += texture(screen_texture, screen_uv + vec2(i, j)).a;
		}
	}
	vec4 colour = texture(screen_texture, screen_uv);
	vec4 outline = vec4(line_colour, min(line_alpha, 1.0));
	return outline;
}

void fragment() {
	vec2 redDirRot = rotate(redDir, TIME * 2.0);
	vec2 greenDirRot = rotate(greenDir, -TIME * 3.0);
	vec2 blueDirRot = rotate(blueDir, TIME * 5.0);

	vec2 redUv = SCREEN_UV + aberration * redDirRot / vec2(640, 360);
	vec2 greenUv = SCREEN_UV + aberration * greenDirRot / vec2(640, 360);
	vec2 blueUv = SCREEN_UV + aberration * blueDirRot / vec2(640, 360);

	vec4 red = textureLod(screen_texture, redUv, 0.0);
	vec4 green = textureLod(screen_texture, greenUv, 0.0);
	vec4 blue = textureLod(screen_texture, blueUv, 0.0);

    if (red.a > 0.0001) {
        red.rgb /= red.a;
    }
    if (green.a > 0.0001) {
        green.rgb /= green.a;
    }
    if (blue.a > 0.0001) {
        blue.rgb /= blue.a;
    }
	float alpha = (red.a + blue.a + green.a) / 3.0;

    vec4 colour = texture(screen_texture, SCREEN_UV);
    if (line_thickness > 0) {
        vec4 outline = outline_color(SCREEN_UV);
   	    COLOR *= mix(outline, vec4(red.r, green.g, blue.b, alpha), colour.a);
    } else {
        COLOR *= vec4(red.r, green.g, blue.b, alpha);
    }
}
