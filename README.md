# ElegantXML-Crestron

This set of modules enables reading XML configuration files into Crestron Simpl Windows projects, using Simpl#. When I began this project, I had already been using a Simpl+ version of this code that did much the same thing. While it worked fine for small projects, when I started working on some projects with hundreds of attributes coming from the XML, the loading and saving times took a steep dive, taking several minutes to load and save the files.

When I first ran performance tests with this code, I had about 500 attributes I was loading and saving, randomly distributed between Digital, Analog, Signed Analog, and Serial types. The file load and save times were in the realm of 5 seconds or so, running on a CP3 processor.

More in-depth documentation can be found in the help files for each module.

[Elegant XML - Manager](https://github.com/ProfessorAire/ElegantXML-Crestron/blob/master/Docs/Elegant%20XML%20-%20Manager%20v1.0.pdf)  
[Elegant XML - Analog Values](https://github.com/ProfessorAire/ElegantXML-Crestron/blob/master/Docs/Elegant%20XML%20-%20Analog%20Values%20v1.0.pdf)  
[Elegant XML - Signed Analog Values](https://github.com/ProfessorAire/ElegantXML-Crestron/blob/master/Docs/Elegant%20XML%20-%20Signed%20Analog%20Values%20v1.0.pdf)  
[Elegant XML - Serial Values](https://github.com/ProfessorAire/ElegantXML-Crestron/blob/master/Docs/Elegant%20XML%20-%20Serial%20Values%20v1.0.pdf)  
[Elegant XML - Digital Values](https://github.com/ProfessorAire/ElegantXML-Crestron/blob/master/Docs/Elegant%20XML%20-%20Digital%20Values%20v1.0.pdf)
[Elegant XML - Analog Property Interlock](https://github.com/ProfessorAire/ElegantXML-Crestron/blob/v1.4/Docs/Elegant%20XML%20-%20Analog%20Property%20Interlock.pdf)
[Elegant XML - Serial Property Interlock](https://github.com/ProfessorAire/ElegantXML-Crestron/blob/master/Docs/Elegant%20XML%20-%20Serial%20Property%20Interlock.pdf)
[Elegant XML - Signed Analog Property Interlock](https://github.com/ProfessorAire/ElegantXML-Crestron/blob/master/Docs/Elegant%20XML%20-%20Signed%20Analog%20Property%20Interlock.pdf)
