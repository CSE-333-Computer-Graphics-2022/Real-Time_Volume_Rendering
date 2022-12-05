#version 330 core

in vec3 fColor;
in vec3 cameraPos;
uniform float stepSize;
uniform vec3 ExtentMax;
uniform vec3 ExtentMin;
in mat4 inv_view_proj;

uniform sampler1D transferfun;
uniform sampler3D texture3d;

vec4 value;
float scalar;
vec4 dst = vec4(0, 0, 0, 0);
vec3 direction;
vec3 curren_pos, pos_max, pos_min;
int screen_width = 640;
int screen_height=640;
float tmin_y, tmin_z, tmax_y, tmax_z, tmin, tmax;

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
        // tmin = (bounds[sign0].x - position.x)*invdir.x;
        // tmax = (bounds[1-sign0].x - position.x)*invdir.x;
        // tmin_y = (bounds[sign1].y - position.y)*invdir.y;
        // tmax_y = (bounds[1-sign1].y - position.y)*invdir.y;

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
        // tmin_z = (bounds[sign2].z - position.z)*invdir.z; 
        // tmax_z = (bounds[1-sign2].z - position.z)*invdir.z;
        
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
        vec4 ndc = vec4((gl_FragCoord.x/screen_width - 0.5)*2.0,(gl_FragCoord.y/screen_height - 0.5)*2.0,(gl_FragCoord.z - 0.5)*2.0,1.0);
        vec4 clip = inv_view_proj*ndc;
        vec3 position = (clip/clip.w).xyz;

        direction = position - cameraPos;
        if(!rayintersection(position,direction)){
                outColor = vec4(0,0,0,1);
                return;
        }
        pos_min = position + tmin*direction;
        pos_max = position + tmax*direction;

        dst = vec4(0,1,0,0);
        curren_pos = pos_min;
        for(int i=0;i<200;i++){
                value = texture(texture3d, curren_pos);
                scalar = value.a;
                vec4 src = texture(transferfun,scalar);

                dst = (1.0-dst.a)*src + dst;
                curren_pos = curren_pos + direction*stepSize;
                

                if(curren_pos.x<ExtentMin.x || curren_pos.y<ExtentMin.y || curren_pos.z<ExtentMin.z
                        || curren_pos.x>ExtentMax.x || curren_pos.y>ExtentMax.y || curren_pos.z>ExtentMax.z)
                        {
                                break;
                        }
                // vec3 temp1 = sign(position - ExtentMin);
                // vec3 temp2 = sign(ExtentMax - position);
                // float inside = dot(temp1,temp2);
                // if(inside<3.0){
                //         dst = vec4(1,0,0,0);
                //         break;
                // }
        }
        // outColor = vec4(vec3(gl_FragCoord.z), 1.0)*vec4(1,0,0,0);
        outColor = vec4(fColor,1.0);
        // outColor = value*vec4(fColor, 1.0)*src;
        // outColor = normalize(src+vec4(fColor, 1.0))/2;
        // vec3 s = (normalize(fColor+cameraPos+stepSize))/2;
         // outColor = vec4(fColor, 1.0);//cameraPos;
        // outColor = vec4(s, 1.0);
        // outColor = dst*vec4(fColor, 1.0);
}

// vec3 worldpos(float depth){
//         float z = depth*2.0 - 1.0;
//         vec4 clipspacepos = vec4()
// }

// void main(void) {
//         // for(int i=0;i<200;i++){
//         //         value = texture(texture3d, position);
//         //         scalar = value.a;
//         //         vec4 src = texture(transferfun,scalar);

//         //         dst = (1.0-dst.a)*src + dst;
//         //         position = position + direction*stepSize;

//         //         vec3 temp1 = sign(position - ExtentMin);
//         //         vec3 temp2 = sign(ExtentMax - position);
//         //         float inside = dot(temp1,temp2);
//         //         if(inside<3.0){
//         //                 break;
//         //         }
//         // }
//         outColor = vec4(vec3(gl_FragCoord.z), 1.0)*vec4(fColor, 1.0);
//         // outColor = value*vec4(fColor, 1.0)*src;
//         // outColor = normalize(src+vec4(fColor, 1.0))/2;
//         // vec3 s = (normalize(fColor+cameraPos+stepSize))/2;
//         // outColor = vec4(fColor, 1.0);//cameraPos;
//         // outColor = vec4(s, 1.0);
//         // outColor = dst*vec4(fColor, 1.0);
// }
