# pythonDotNet

[toc]

## Notes

- using MiniConda with python 3.13.5
- Visual Studio 2022
- .NET 8 console app
- install python.net using NuGet
- python installed using miniconda with various virtual environments created along the way
- lots of problems getting this to work with miniconda...finally got it to work based on
  * [Using Python.NET with Virtual Environments · pythonnet/pythonnet Wiki](https://github.com/pythonnet/pythonnet/wiki/Using-Python.NET-with-Virtual-Environments)

- can use either with "base" virtual environment or with user defined virtual environments without activating them...just point to the folder containing the virtual environment.
- ran into some problems getting the `PythonEngine.Shutdown();`to work (e.g. very slow to complete), but then it started working (?!?!?)

## TODO

- [x] get a C# project working
- [ ] evaluate the ease of loading and using python libraries in C#
  - pandas, polars and SQLAlchemy
  - charting library like Matplotlib/Seaborn/Plotly 
- [ ] look at how data exchange works between C# and python 
- [ ] performance. 
- [ ] look at the mechanisms required to load/use python libraries 
  - how to package the necessary libraries to run offline







## References

* [Python.NET | pythonnet](http://pythonnet.github.io/)

* [Writing and Running Python in .NET](https://www.youtube.com/watch?v=6N2oFh6YTTc) - Nick Chapsas tutorial

* [pythonnet/pythonnet](https://github.com/pythonnet/pythonnet)

  * [Python.NET documentation](https://pythonnet.github.io/pythonnet/)

* Tutorials
  * [Calling Python from C#: an introduction to PythonNET](https://somegenericdev.medium.com/calling-python-from-c-an-introduction-to-pythonnet-c3d45f7d5232)
  * [Pythonnet – A Simple Union of .NET Core and Python You’ll Love](https://www.codeproject.com/Articles/5352648/Pythonnet-A-Simple-Union-of-NET-Core-and-Python-Yo)
  * [Integrate Python with C# using Python.NET](https://www.luisllamas.es/en/csharp-pythonnet/)

  * [Intro to Pythonnet | Fundamentals - YouTube](https://www.youtube.com/watch?v=J7TETPbLw7c)