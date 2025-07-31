shader_type canvas_item;
render_mode unshaded;

uniform float abberation = 0.0;
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

void fragment() {
	vec2 redDirRot = rotate(redDir, TIME * 2.0);
	vec2 greenDirRot = rotate(greenDir, -TIME * 3.0);
	vec2 blueDirRot = rotate(blueDir, TIME * 5.0);
	
	vec2 redUv = SCREEN_UV + abberation * redDirRot / vec2(640, 360);
	vec2 greenUv = SCREEN_UV + abberation * greenDirRot / vec2(640, 360);
	vec2 blueUv = SCREEN_UV + abberation * blueDirRot / vec2(640, 360);
	
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

    COLOR *= vec4(red.r, green.g, blue.b, alpha);
}
