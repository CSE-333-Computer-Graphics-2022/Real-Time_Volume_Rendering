#include "utils.h"

#define  GLM_FORCE_RADIANS
#define  GLM_ENABLE_EXPERIMENTAL

#include <glm/gtc/type_ptr.hpp>
#include <glm/glm.hpp>
#include <glm/gtc/matrix_transform.hpp>
#include <glm/gtx/string_cast.hpp>

#include<cmath>

#include<iostream>
#include <unistd.h>
#include <stdio.h>

//Globals
int screen_width = 640, screen_height=640;
GLint vModel_uniform, vView_uniform, vProjection_uniform;
GLint vColor_uniform, vCam_uniform;
glm::mat4 modelT, viewT, projectionT;//The model, view and projection transformations

double oldX, oldY, currentX, currentY;
bool isDragging=false;

void createBoundingbox(unsigned int &, unsigned int &);
void setupModelTransformation(unsigned int &);
void setupViewTransformation(unsigned int &);
void setupProjectionTransformation(unsigned int &);
glm::vec3 getTrackBallVector(double x, double y);
bool load_volume(const char* filename);
void setUniforms(unsigned int &);
GLfloat* createTransferfun(int width, int height);
float x_size = 256;
float y_size = 256;
float z_size = 256;
float step_size = 0.001;
const int vol_size = x_size*y_size*z_size;
GLubyte* volume = new GLubyte[vol_size];
GLfloat *tf = new GLfloat[256*4];
glm::vec4 camposition = glm::vec4(0.0, 0.0, 500.0, 1.0);
GLuint VAO, transferfun, texture3d;

int main(int, char**)
{
    // Setup window
    GLFWwindow *window = setupWindow(screen_width, screen_height);
    ImGuiIO& io = ImGui::GetIO(); // Create IO object

    ImVec4 clearColor = ImVec4(1.0f, 1.0f, 1.0f, 1.00f);
    const char *filepath = "../data/bonzai_volume.raw";
    if(!load_volume(filepath))                                      // Reading the Volume
    {
        std::cout<<"Volume not loaded succesfully"<<std::endl;
        printf("Current working dir: %s\n", get_current_dir_name());
        return 0;
    }

    tf = createTransferfun(x_size, y_size);                     // Creating transfer function

    unsigned int shaderProgram = createProgram("./shaders/vshader.vs", "./shaders/fshader.fs");

    glUseProgram(shaderProgram);

    glGenTextures(1,&texture3d);
    glActiveTexture(GL_TEXTURE0);
    glBindTexture(GL_TEXTURE_3D, texture3d);
    glTexParameteri(GL_TEXTURE_3D, GL_TEXTURE_WRAP_S, GL_CLAMP);
    glTexParameteri(GL_TEXTURE_3D, GL_TEXTURE_WRAP_T, GL_CLAMP);
    glTexParameteri(GL_TEXTURE_3D, GL_TEXTURE_WRAP_R, GL_CLAMP);
    glTexParameteri(GL_TEXTURE_3D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
    glTexParameteri(GL_TEXTURE_3D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
    glTexImage3D(GL_TEXTURE_3D,0,GL_INTENSITY,x_size,y_size,z_size,0,GL_LUMINANCE,GL_UNSIGNED_BYTE,volume);
    delete [] volume;

    
    glGenTextures(1, &transferfun);
    glActiveTexture(GL_TEXTURE1);
    glBindTexture(GL_TEXTURE_1D, transferfun);
    glTexParameteri(GL_TEXTURE_1D, GL_TEXTURE_WRAP_S, GL_REPEAT);
    glTexParameteri(GL_TEXTURE_1D, GL_TEXTURE_WRAP_T, GL_REPEAT);
    glTexParameteri(GL_TEXTURE_1D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
    glTexParameteri(GL_TEXTURE_1D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
    glTexImage1D(GL_TEXTURE_1D,0,GL_RGBA,256,0,GL_RGBA,GL_FLOAT,tf);
    
    glGenVertexArrays(1, &VAO);

    glUseProgram(shaderProgram);
    setupModelTransformation(shaderProgram);
    setupViewTransformation(shaderProgram);
    setupProjectionTransformation(shaderProgram);

    setUniforms(shaderProgram);

    createBoundingbox(shaderProgram, VAO);

    oldX = oldY = currentX = currentY = 0.0;
    int prevLeftButtonState = GLFW_RELEASE;
    glEnable(GL_DEPTH_TEST);

    while (!glfwWindowShouldClose(window))
    {
        glfwPollEvents();

        // Get current mouse position
        int leftButtonState = glfwGetMouseButton(window,GLFW_MOUSE_BUTTON_LEFT);
        double x,y;
        glfwGetCursorPos(window,&x,&y);
        if(leftButtonState == GLFW_PRESS && prevLeftButtonState == GLFW_RELEASE){
            isDragging = true;
            currentX = oldX = x;
            currentY = oldY = y;
        }
        else if(leftButtonState == GLFW_PRESS && prevLeftButtonState == GLFW_PRESS){
            currentX = x;
            currentY = y;
        }
        else if(leftButtonState == GLFW_RELEASE && prevLeftButtonState == GLFW_PRESS){
            isDragging = false;
        }

        if (ImGui::IsKeyDown(ImGui::GetKeyIndex(ImGuiKey_UpArrow))) {
          if(io.KeyShift)
			{	
				camposition.z = camposition.z+2;
				setupViewTransformation(shaderProgram);
			}
          else 
		  	{
				camposition.z = camposition.z-2;
				setupViewTransformation(shaderProgram);
			}
        }

        // Rotate based on mouse drag movementsetupViewTransformation(shaderProgram);
        prevLeftButtonState = leftButtonState;
        if(isDragging && (currentX !=oldX || currentY != oldY))
        {
            glm::vec3 va = getTrackBallVector(oldX, oldY);
            glm::vec3 vb = getTrackBallVector(currentX, currentY);

            float angle = acos(std::min(1.0f, glm::dot(va,vb)));
            glm::vec3 axis_in_camera_coord = glm::cross(va, vb);
            glm::mat3 camera2object = glm::inverse(glm::mat3(viewT*modelT));
            glm::vec3 axis_in_object_coord = camera2object * axis_in_camera_coord;
            modelT = glm::rotate(modelT, angle, axis_in_object_coord);
            glUniformMatrix4fv(vModel_uniform, 1, GL_FALSE, glm::value_ptr(modelT));

            oldX = currentX;
            oldY = currentY;
        }

        // Start the Dear ImGui frame
        ImGui_ImplOpenGL3_NewFrame();
        ImGui_ImplGlfw_NewFrame();
        ImGui::NewFrame();

        glUseProgram(shaderProgram);

        {
            ImGui::Begin("Information");                          
            ImGui::Text("%.3f ms/frame (%.1f FPS)", 1000.0f / ImGui::GetIO().Framerate, ImGui::GetIO().Framerate);
            ImGui::End();
        }

        // Rendering
        ImGui::Render();
        int display_w, display_h;
        glfwGetFramebufferSize(window, &display_w, &display_h);
        glViewport(0, 0, display_w, display_h);
        glClearColor(clearColor.x, clearColor.y, clearColor.z, clearColor.w);
        glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);

        glBindVertexArray(VAO); 
        
        glDrawArrays(GL_TRIANGLES, 0, 36);

        ImGui_ImplOpenGL3_RenderDrawData(ImGui::GetDrawData());

        glfwSwapBuffers(window);

    }

    // Cleanup
    cleanup(window);

    return 0;
}

bool load_volume(const char* filename)
{
    FILE *file = fopen(filename,"rb");
    if(NULL == file)
    {
        return false;
    }
    fread(volume,sizeof(GLubyte),vol_size,file);
    std::cout<<"Reached Here";
    fclose(file);
    return true;
}

GLfloat* createTransferfun(int width, int height)
{
    for(int i=0; i<256; i++) {
        if(i>=0 && i<=10){
            tf[i*4] = 1.0;
            tf[i*4 + 1] = 0.0;
            tf[i*4 + 2] = 0.0;
            tf[i*4 + 3] = 0.5;
        }
        if(i>=80 && i<=150){
            tf[i*4] = 0.0;
            tf[i*4 + 1] = 1.0;
            tf[i*4 + 2] = 0.0;
            tf[i*4 + 3] = 0.5;
        }
        if(i>=200 && i<=256){
            tf[i*4] = 0.0;
            tf[i*4 + 1] = 0.0;
            tf[i*4 + 2] = 1.0;
            tf[i*4 + 3] = 0.5;
        }
    }
    return tf;
}

void createBoundingbox(unsigned int &program, unsigned int &cube_VAO)
{
    glUseProgram(program);

    //Bind shader variables
    int vVertex_attrib = glGetAttribLocation(program, "vVertex");
    if(vVertex_attrib == -1) {
        fprintf(stderr, "Could not bind location: vVertex\n");
        exit(0);
    }

    //Cube data
    // GLfloat cube_vertices[] = {
    //     x_size-1, y_size-1, -z_size+1, 0, y_size-1, 0-z_size+1,
    //     0, 0, -z_size+1, x_size-1, 0, -z_size+1,
    //     x_size, y_size-1, 0, 0, y_size-1, 0,
    //     0, 0, 0, x_size-1, 0, 0
    // };
    GLfloat a = x_size/2;
    GLfloat b = y_size/2;
    GLfloat c = z_size/2;
    GLfloat cube_vertices[] = {
        a, b, -c, -a, b, -c,
        -a, -b, -c, a, -b, -c,
        a, b, c, -a, b, c,
        -a, -b, c, a, -b, c
    };
    // GLfloat cube_vertices[] = {10, 10, -10, -10, 10, -10, -10, -10, -10, 10, -10, -10, //Front
    //                10, 10, 10, -10, 10, 10, -10, -10, 10, 10, -10, 10}; //Back
    GLushort cube_indices[] = {
                0, 1, 2, 0, 2, 3, //Front
                4, 7, 5, 5, 7, 6, //Back
                1, 6, 2, 1, 5, 6, //Left
                0, 3, 4, 4, 7, 3, //Right
                0, 4, 1, 4, 5, 1, //Top
                2, 6, 3, 3, 6, 7 //Bottom
                };

    //Generate VAO object
    glGenVertexArrays(1, &cube_VAO);
    glBindVertexArray(cube_VAO);

    //Create VBOs for the VAO
    //Position information (data + format)
    int nVertices = (6*2)*3; //(6 faces) * (2 triangles each) * (3 vertices each)
    GLfloat *expanded_vertices = new GLfloat[nVertices*3];
    for(int i=0; i<nVertices; i++) {
        expanded_vertices[i*3] = cube_vertices[cube_indices[i]*3];
        expanded_vertices[i*3 + 1] = cube_vertices[cube_indices[i]*3+1];
        expanded_vertices[i*3 + 2] = cube_vertices[cube_indices[i]*3+2];
    }
    GLuint vertex_VBO;
    glGenBuffers(1, &vertex_VBO);
    glBindBuffer(GL_ARRAY_BUFFER, vertex_VBO);
    glBufferData(GL_ARRAY_BUFFER, nVertices*3*sizeof(GLfloat), expanded_vertices, GL_STATIC_DRAW);
    glEnableVertexAttribArray(vVertex_attrib);
    glVertexAttribPointer(vVertex_attrib, 3, GL_FLOAT, GL_FALSE, 0, 0);
    delete []expanded_vertices;

    glBindBuffer(GL_ARRAY_BUFFER, 0);
    glBindVertexArray(0); //Unbind the VAO to disable changes outside this function.
}

void setupModelTransformation(unsigned int &program)
{
    //Modelling transformations (Model -> World coordinates)
    // modelT = glm::translate(glm::mat4(1.0f), glm::vec3(-x_size/2, -y_size/2, -z_size/2));//Model coordinates are the world coordinates
    modelT = glm::translate(glm::mat4(1.0f), glm::vec3(0, 0, 0));//Model coordinates are the world coordinates

    //Pass on the modelling matrix to the vertex shader
    glUseProgram(program);
    vModel_uniform = glGetUniformLocation(program, "vModel");
    if(vModel_uniform == -1){
        fprintf(stderr, "Could not bind location: vModel\n");
        exit(0);
    }
    glUniformMatrix4fv(vModel_uniform, 1, GL_FALSE, glm::value_ptr(modelT));
}


void setupViewTransformation(unsigned int &program)
{
    //Viewing transformations (World -> Camera coordinates
    viewT = glm::lookAt(glm::vec3(camposition), glm::vec3(0.0, 0.0, 0.0), glm::vec3(0.0, 1.0, 0.0));

    //Pass-on the viewing matrix to the vertex shader
    glUseProgram(program);
    vView_uniform = glGetUniformLocation(program, "vView");
    if(vView_uniform == -1){
        fprintf(stderr, "Could not bind location: vView\n");
        exit(0);
    }
    glUniformMatrix4fv(vView_uniform, 1, GL_FALSE, glm::value_ptr(viewT));

	vCam_uniform = glGetUniformLocation(program, "camPosition");
	if(vCam_uniform == -1){
		fprintf(stderr, "Could not bind location: camPosition\n");
		exit(0);
	}
	glUniform3fv(vCam_uniform, 1, glm::value_ptr(glm::vec3(camposition)));
}

void setupProjectionTransformation(unsigned int &program)
{
    //Projection transformation
    projectionT = glm::perspective(45.0f, (GLfloat)screen_width/(GLfloat)screen_height, 0.1f, 600.0f);

    //Pass on the projection matrix to the vertex shader
    glUseProgram(program);
    vProjection_uniform = glGetUniformLocation(program, "vProjection");
    if(vProjection_uniform == -1){
        fprintf(stderr, "Could not bind location: vProjection\n");
        exit(0);
    }
    glUniformMatrix4fv(vProjection_uniform, 1, GL_FALSE, glm::value_ptr(projectionT));
}

glm::vec3 getTrackBallVector(double x, double y)
{
	glm::vec3 p = glm::vec3(2.0*x/screen_width - 1.0, 2.0*y/screen_height - 1.0, 0.0); //Normalize to [-1, +1]
	p.y = -p.y; //Invert Y since screen coordinate and OpenGL coordinates have different Y directions.

	float mag2 = p.x*p.x + p.y*p.y;
	if(mag2 <= 1.0f)
		p.z = sqrtf(1.0f - mag2);
	else
		p = glm::normalize(p); //Nearest point, close to the sides of the trackball
	return p;
}

void setUniforms(unsigned int &program)
{
    glUseProgram(program);

    GLuint vstep_size = glGetUniformLocation(program, "stepsize");
    if(vstep_size == -1){
        fprintf(stderr, "Could not bind location: vstep_size\n");
        exit(0);
    }
    glUniform1f(vstep_size, step_size);

    GLuint vExtentMin = glGetUniformLocation(program, "extentMin");
    if(vExtentMin == -1){
        fprintf(stderr, "Could not bind location: vExtentMin\n");
        exit(0);
    }
    glUniform3f(vExtentMin, -x_size/2, -y_size/2, -z_size/2);

    GLuint vExtentMax = glGetUniformLocation(program, "extentMax");
    if(vExtentMax == -1){
        fprintf(stderr, "Could not bind location: vExtentMax\n");
        exit(0);
    }
    glUniform3f(vExtentMax, x_size/2, y_size/2, z_size/2);

    GLuint tex1 = glGetUniformLocation(program,"texture3d");
    if(tex1 == -1){
        fprintf(stderr, "Could not bind location: texture3d\n");
        exit(0);
    }
    else{
    unsigned int VAO;
    glGenVertexArrays(1, &VAO);
        glActiveTexture(GL_TEXTURE0);
        glBindTexture(GL_TEXTURE_3D, texture3d);
        glUniform1i(tex1, 0);
    }

    GLuint tex2 = glGetUniformLocation(program,"transferfun");
    if(tex2 == -1){
        fprintf(stderr, "Could not bind location: transferfun\n");
        exit(0);
    }
    else{
        glActiveTexture(GL_TEXTURE1);
        glBindTexture(GL_TEXTURE_1D, transferfun);
        glUniform1i(tex2, 1);
    }
}