#version 330 core

layout (location = 0) in vec3 vVertex;
//layout (location = 1) in vec3 vVertexL;

uniform mat4 vModel;
uniform mat4 vView;
uniform mat4 vProjection;
vec3 vColor = vec3(1.0, 0.0, 0.0);
uniform vec3 camPosition;
uniform vec3 extentmin;
uniform vec3 extentmax;

out vec3 fColor;
out vec3 cameraPos;
out vec4 fragPos;
out vec3 ExtentMax;
out vec3 ExtentMin;

void main() {
	gl_Position = vProjection * vView * vModel * vec4(vVertex, 1.0);
	//gl_Position = vProjection * vView * vModel * vec4(vVertexL, 1.0);
	cameraPos = vec3(inverse(vView*vModel)*vec4(camPosition, 1.0));
	fragPos = vModel*vec4(vVertex,1.0);
	ExtentMax = vec3(inverse(vModel)*vec4(extentmax,1.0));
	ExtentMin = vec3(inverse(vModel)*vec4(extentmin,1.0));
	//ExtentMax = extentmax;
	//ExtentMin = extentmin;
	fColor = vColor; //Interpolate color
}
