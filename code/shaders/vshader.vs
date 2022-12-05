#version 330 core

layout (location = 0) in vec3 vVertex;

uniform mat4 vModel;
uniform mat4 vView;
uniform mat4 vProjection;
vec3 vColor = vec3(1.0, 0.0, 0.0);
uniform vec3 camPosition;
//uniform float stepsize;
//uniform vec3 extentMin;
//uniform vec3 extentMax;

out vec3 fColor;
out vec3 cameraPos;
//out float stepSize;
//out vec3 ExtentMax;
//out vec3 ExtentMin;
out mat4 inv_view_proj;

void main() {
	gl_Position = vProjection * vView * vModel * vec4(vVertex, 1.0);
	//ExtentMax = extentMax;
	//ExtentMin = extentMin;
	cameraPos = vec3(inverse(vView*vModel)*vec4(camPosition, 1.0));
	//stepSize = stepsize;
	inv_view_proj = inverse(vView*vModel);
	fColor = vColor; //Interpolate color
}
