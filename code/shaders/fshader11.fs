#version 330 core

in vec3 fColor;
in vec3 cameraPos;
in vec3 ExtentMax;
in vec4 fragPos;
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
vec3 curren_pos;

vec3 up = vec3(0,1,0);
float aspect = screen_width/screen_height;
float fov = 90;
float focalHeight = 1.0; //Let's keep this fixed to 1.0
float focalDistance = focalHeight/(2.0 * tan(90 * 3.14/(180.0 * 2.0))); //More the fovy, close is focal plane
vec3 w = normalize(vec3(cameraPos - vec3(0,0,0)));
vec3 u = normalize(cross(up,w));
vec3 v = normalize(cross(w,u));

float delta_t = 100;
float tentry;
float texit;
vec3 model_center = vec3(0, 0, 0);
float distance = length(model_center - cameraPos);
float radius = length(ExtentMax - ExtentMin)/2.0;
float tmin = distance - radius;
float tmax = distance + radius;

out vec4 outColor;

bool rayintersection(vec3 position, vec3 dir)
{
    float tymin, tymax, tzmin, tzmax;
    vec3 invdir = 1/dir;
    bvec3 sign = bvec3(invdir.x<0, invdir.y<0, invdir.z<0);

    if(invdir.x<0){
        tentry = (ExtentMax.x - position.x) / dir.x;
        texit = (ExtentMin.x - position.x) / dir.x; 
    }
    else{
        tentry = (ExtentMin.x - position.x) / dir.x;
        texit = (ExtentMax.x - position.x) / dir.x;
    }

    if(invdir.y<0){
        tymin = (ExtentMax.y - position.y) / dir.y;
        tymax = (ExtentMin.y - position.y) / dir.y; 
    }
    else{
        tymin = (ExtentMin.y - position.y) / dir.y;
        tymax = (ExtentMax.y - position.y) / dir.y;
    }
    
    if((tentry > tymax) || (tymin > texit)){
        return false;
    }
    
    if (tymin > tentry){
        tentry = tymin;
    }
    if (tymax < texit){
        texit = tymax;
    }

    if(invdir.z<0){
        tzmin = (ExtentMax.z - position.z) / dir.z;
        tzmax = (ExtentMin.z - position.z) / dir.z; 
    }
    else{
        tzmin = (ExtentMin.z - position.z) / dir.z;
        tzmax = (ExtentMax.z - position.z) / dir.z;
    }

    if(tzmin > tentry){
        tentry = tzmin;
    }
    if(tzmax < texit){
        texit = tzmax;
    }

    if(tentry > 0 && texit > 0 && tentry < texit){
        return true;
    }
    return false;
}

void main()
{
        vec3 position = cameraPos;
        float xw = aspect * (gl_FragCoord.x - screen_width/2.0 + 0.5) / screen_width;
        float yw = (gl_FragCoord.y - screen_height/2.0 + 0.5) / screen_height;
        float focalDistance = focalHeight/(2.0 * tan(90 * 3.14/(180.0 * 2.0))); //More the fovy, close is focal plane
        direction = normalize(u*xw + v*yw - focalDistance*w);

        if(!rayintersection(position,direction)){
                outColor = vec4(1.0,0.0,0.0,0.0);
                return;
        }

        dst = vec4(0,0,0,0);
        float sum = 0;
        int i = 0;
        float t = tentry;
        curren_pos = position + t*direction;
        float tnorm = (texit-tmin)/(tmax-tmin);
        float temp = length(vec3(fragPos) - cameraPos)/length(vec3(fragPos) + cameraPos);
        for(i=0;;i+=1){
            value = texture(texture3d, (curren_pos+((ExtentMax - ExtentMin)/2))/(ExtentMax-ExtentMin));
            sum += value.r;
            scalar = value.r;
            vec4 src = texture(transferfun,scalar);

            dst = (1.0-dst.a)*src + dst;

            t += delta_t;
            curren_pos = position + direction*t;
            // if(curren_pos.x<ExtentMin.x || curren_pos.y<ExtentMin.y || curren_pos.z<ExtentMin.z
            //         || curren_pos.x>ExtentMax.x || curren_pos.y>ExtentMax.y || curren_pos.z>ExtentMax.z)
            //         {
            //                 break;
            //         }
            if(t>texit){
                    break;
            }
            if(dst.a > 0.95){
                    break;
            }
        }
        sum /=i;
        outColor = dst;
        // outColor = vec4(sum, sum, sum, 1.0) + dst*0;
        // outColor = vec4(tnorm, tnorm, tnorm, 1.0) + dst*0;
        // outColor = vec4(temp, temp, temp, 1.0) + dst*0;
}