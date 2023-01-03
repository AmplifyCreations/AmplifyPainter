# Tablet Library

How to compile:
1. In visual studio installer select modify
2. Add the "Desktop development with C++" environment. 
3. In the optional components add one of the windows SDKs (latest should work) if not selected already
4. Let it download and install.
5. Open up visual studio project file, all should be ready to compile

Notes on compilation:
- The only differences between debug and release are the added debug messages for debug and perfomance compilation improvements for release
- When commiting remember to always compile both x86 and x64 versions
- When compiling unity editor should be closed in order for the proper dlls to copy to the correct folder or else this steps fails in the compilation
