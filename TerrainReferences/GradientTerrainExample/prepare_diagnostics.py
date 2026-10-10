from pathlib import Path

root = Path(__file__).resolve().parent
upstream = root / 'upstream/eric-guerin-gradient-terrains-68da08b'
source = (upstream / 'code/src/main.cpp').read_text()
source = source.replace('glewInit();', '''glewInit();
    std::cout << "GL_VENDOR " << glGetString(GL_VENDOR) << std::endl;
    std::cout << "GL_RENDERER " << glGetString(GL_RENDERER) << std::endl;
    std::cout << "GL_VERSION " << glGetString(GL_VERSION) << std::endl;''')
for statement, label in [('diffusion.InitGL();', 'InitGL'), ('diffusion.Solve();', 'Solve')]:
    source = source.replace(statement, statement + f'''
    {{ GLenum e = glGetError(); std::cout << "GL_ERROR_{label} " << e << std::endl; if(e) return 2; }}''')
source = source.replace('result.SavePGM("../results/result.pgm");', '''
    { GLenum e = glGetError(); std::cout << "GL_ERROR_GetResult " << e << std::endl; if(e) return 2; }
    std::ofstream raw("../results/result_diagnostic.f32", std::ios::binary);
    for (int row=0; row<result.SizeY(); ++row)
        for (int column=0; column<result.SizeX(); ++column) {
            float value=result.Get(row,column);
            raw.write(reinterpret_cast<const char*>(&value),sizeof(value));
        }
    raw.close();
    result.SavePGM("../results/result_diagnostic.pgm");''')
(root / 'diagnostic_main.cpp').write_text(source)
