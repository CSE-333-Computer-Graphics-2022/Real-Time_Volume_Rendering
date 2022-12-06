#version 330 core

in vec3 fColor;
in vec3 cameraPos;
in vec4 fragPos;
in vec3 ExtentMax;
in vec3 ExtentMin;

uniform float stepSize;
// uniform vec3 ExtentMax;
// uniform vec3 ExtentMin;

uniform sampler1D transferfun;
uniform sampler3D texture3d;

float screen_width = 640;
float screen_height = 640;

vec4 value;
float scalar;
vec4 dst = vec4(0, 0, 0, 0);
vec3 direction = vec3(0.0, 0.0, 0.0);
vec3 curren_pos, pos_max, pos_min;
float tmin_y, tmin_z, tmax_y, tmax_z, tmin, tmax;

vec3 up = vec3(0,1,0);
float aspect = screen_width/screen_height;
float focalHeight = 1.0; //Let's keep this fixed to 1.0
float focalDistance = focalHeight/(2.0 * tan(45 * 3.14/(180.0 * 2.0))); //More the fovy, close is focal plane
vec3 w = vec3(cameraPos - vec3(0,0,0));
vec3 w1 = normalize(w);
vec3 u = cross(up,w1);
vec3 u1 = normalize(u);
vec3 v = cross(w1,u1);
vec3 v1 = normalize(v);

// normalize(direction);


out vec4 outColor;

bool rayintersection(vec3 position, vec3 dir)
{
        bool sign0 = (dir.x < 0);
        bool sign1 = (dir.y < 0);
        bool sign2 = (dir.z < 0);

        vec3 bounds0 = ExtentMin;
        vec3 bounds1 = ExtentMax;

        vec3 invdir = 1/dir;

        if(invdir.x >= 0){
                tmin = (bounds0.x - position.x)*invdir.x;
                tmax = (bounds1.x - position.x)*invdir.x;
        }
        else{
                tmin = (bounds1.x - position.x)*invdir.x;
                tmax = (bounds0.x - position.x)*invdir.x;
        }

        if(invdir.y >= 0)
        {
                tmin_y = (bounds0.y - position.y)*invdir.y;
                tmax_y = (bounds1.y - position.y)*invdir.y;
        }
        else{
                tmin_y = (bounds1.y - position.y)*invdir.y;
                tmax_y = (bounds0.y - position.y)*invdir.y;
        }

        if((tmin > tmax_y) || (tmax_y > tmax)){
                return false;
        }
        if (tmin_y > tmin){
                tmin = tmin_y;
        }
        if (tmax_y < tmax){
                tmax = tmax_y;
        }

        if(invdir.z >= 0)
        {
                tmin_z = (bounds0.z - position.z)*invdir.z;
                tmax_z = (bounds1.z - position.z)*invdir.z;
        }
        else
        {
                tmin_z = (bounds1.z - position.z)*invdir.z; 
                tmax_z = (bounds0.z - position.z)*invdir.z;
        }
        
        if ((tmin > tmax_z) || (tmin_z > tmax)) 
                return false;
        if (tmin_z > tmin) 
                tmin = tmin_z; 
        if (tmax_z < tmax) 
                tmax = tmax_z; 
        return true; 
}

void main()
{
        vec3 position = vec3(fragPos);

        direction += -(w1)*focalDistance;
        float xw = aspect*(fragPos.x - screen_width/2.0 + 0.5)/screen_width;
        float yw = (fragPos.y - screen_height/2.0 + 0.5)/screen_height;
        direction += u1 * xw;
        direction += v1 * yw;

        position = position + direction*focalDistance;

        // direction = position - cameraPos;
        if(!rayintersection(position,direction)){
                outColor = vec4(0.0,0.0,0.0,0.0);
                return;
        }
        pos_min = position + tmin*direction;
        pos_max = position + tmax*direction;

        dst = vec4(0,0,0,0);
        curren_pos = pos_min;
        int tot_sample = 200;
        for(int i=0;i<tot_sample;i++){
                value = texture(texture3d, curren_pos/(ExtentMax-ExtentMin));
                scalar = value.a;
                vec4 src = texture(transferfun,scalar);

                dst = (1.0-dst.a)*src + dst;
                curren_pos = curren_pos + direction*stepSize;
                
                if(curren_pos.x<ExtentMin.x || curren_pos.y<ExtentMin.y || curren_pos.z<ExtentMin.z
                        || curren_pos.x>ExtentMax.x || curren_pos.y>ExtentMax.y || curren_pos.z>ExtentMax.z)
                        {
                                break;
                        }
                // if(i==tot_sample-1){
                //         tot_sample += 10;
                // }
        }
        // outColor = vec4(vec3(gl_FragCoord.z), 1.0)*vec4(1,0,0,0);
        outColor = dst;
        // outColor = vec4(fColor,1.0)
}
