#include "GL/glew.h"
#include "GLFW/glfw3.h"
#include "diffusionterrain.h"
#include <iostream>
#include <fstream>
#include <stdexcept>

ScalarField2D ReadField(const std::string& path, int size) {
    ScalarField2D field(size,size);
    std::ifstream input(path,std::ios::binary);
    input.read(reinterpret_cast<char*>(&field[0]),size*size*sizeof(float));
    if(!input) throw std::runtime_error("Cannot read complete input "+path);
    return field;
}

int main(int argc,char** argv) {
    if(argc!=3) return 1;
    int size=std::stoi(argv[2]);
    std::string directory=argv[1];
    if(!glfwInit()) return 2;
    glfwWindowHint(GLFW_CONTEXT_VERSION_MAJOR,4);
    glfwWindowHint(GLFW_CONTEXT_VERSION_MINOR,5);
    glfwWindowHint(GLFW_VISIBLE,GLFW_FALSE);
    auto window=glfwCreateWindow(10,10,"Offline terrain solver",nullptr,nullptr);
    if(!window) return 3;
    glfwMakeContextCurrent(window);
    if(glewInit()!=GLEW_OK) return 4;
    std::cout<<"GPU "<<glGetString(GL_RENDERER)<<std::endl;
    {
        auto alpha=ReadField(directory+"/alpha.f32",size);
        auto altitude=ReadField(directory+"/altitude.f32",size);
        auto laplacian=ReadField(directory+"/laplacian.f32",size);
        SimpleGeometricMultigridFloat solver(alpha,altitude,laplacian);
        solver.InitGL();
        solver.Solve();
        auto result=solver.GetResult();
        auto error=glGetError();
        std::cout<<"GL_ERROR "<<error<<std::endl;
        if(error!=GL_NO_ERROR) return 5;
        std::ofstream output(directory+"/author_result.f32",std::ios::binary);
        output.write(reinterpret_cast<const char*>(&result[0]),size*size*sizeof(float));
        if(!output) return 6;
    }
    glfwDestroyWindow(window);
    glfwTerminate();
    return 0;
}
