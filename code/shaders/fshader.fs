#version 330 core

in vec3 fColor;
in vec3 cameraPos;
in vec3 ExtentMax;
in vec3 ExtentMin;
in mat4 inverse_viewproj;

uniform float stepSize;

uniform sampler1D transferfun;
uniform sampler3D texture3d;

float screen_width = 640;
float screen_height = 640;

vec4 value;
float scalar;
vec4 dst = vec4(0, 0, 0, 0);
vec3 direction;
vec3 curren_pos, pos_max, pos_min;
float tmin, tmax;

out vec4 outColor;

bool rayintersection(vec3 position, vec3 dir)
{
        vec3 tMin = (ExtentMin - position) / dir;
        vec3 tMax = (ExtentMax - position) / dir;
        vec3 t1 = min(tMin, tMax);
        vec3 t2 = max(tMin, tMax);
        float tmin = max(max(t1.x, t1.y), t1.z);
        float tmax = min(min(t2.x, t2.y), t2.z);
        if(tmin > tmax)
        {
                return false;
        }
        return true;
}

void main()
{
        vec4 ndc = vec4((gl_FragCoord.x/screen_width - 0.5)*2.0, (gl_FragCoord.y/screen_height - 0.5)*2.0, 
                                (gl_FragCoord.z - 0.5)*2.0, 1.0);
        // vec4 glposition = ndc/gl_FragCoord.w;
        // vec3 position = vec3(inverse_viewproj*glposition);
        vec4 glposition = inverse_viewproj*ndc;
        vec3 position = (glposition/glposition.w).xyz;

        direction = normalize(position - cameraPos);

        if(!rayintersection(position,direction)){
                outColor = vec4(1.0,0.0,0.0,1.0);
                return;
        }

        dst = vec4(0,0,0,0);
        curren_pos = position + tmin*direction;
        float sum = 0;
        int i = 0;
        float t = tmin;
        for(i=0;;i+=1){
                value = texture(texture3d, (curren_pos+((ExtentMax - ExtentMin)/2))/(ExtentMax-ExtentMin));
                sum += value.a;
                scalar = value.a;
                vec4 src = texture(transferfun,scalar);

                dst = (1.0-dst.a)*src + dst;

                t += stepSize;
                curren_pos = curren_pos + direction*t;
                
                if(curren_pos.x<ExtentMin.x || curren_pos.y<ExtentMin.y || curren_pos.z<ExtentMin.z
                        || curren_pos.x>ExtentMax.x || curren_pos.y>ExtentMax.y || curren_pos.z>ExtentMax.z)
                        {
                                break;
                        }
                if(t>tmax){
                        break;
                }
                if(dst.a > 0.95){
                        break;
                }
        }
        sum /=i;
        outColor = dst;
        // outColor = vec4(sum, sum, sum,1.0);
}
