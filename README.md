# pythonDotNet

[toc]

## TODO

- [x] get a C# project working
- [ ] evaluate the ease of loading and using python libraries in C#
  - [x] numpy
  - [ ] pandas
  - [ ] polars
  - [ ] SQLAlchemy
  - [x] Matplotlib
    - [ ] generate plots and save to file
    - [ ] use show() to display on screen
  - [ ] Seaborn
  - [ ] Plotly 
- [ ] look at how data exchange works between C# and python 
  - [ ] going back and forth (C#/Python) with simple data types (e.g. list, array)
  - [ ] dataframe between C# and python

- [ ] performance
  - [ ] more research/testing of best methods to get/release python GIL

- [x] look at the mechanisms required to load/use python libraries 
  - [x] how to package the necessary libraries to run offline
- [ ] Deployment
  - [x] how to deploy the python environment with the C# application
  - [ ] test framework-dependent install on vanilla VM without Visual Studio

## Configuration

- using MiniConda with python 3.13.5
- Visual Studio 2022 17.14.11 with .NET 8 console app project
- install python.net using NuGet (version 3.0.5)

## Notes

- lots of problems getting this to work with miniconda...finally got it to work based on
  * [Using Python.NET with Virtual Environments · pythonnet/pythonnet Wiki](https://github.com/pythonnet/pythonnet/wiki/Using-Python.NET-with-Virtual-Environments)
- can use either with conda "base" virtual environment or with user defined virtual environments without activating them...just point to the folder containing the virtual environment.
  - `base` is in C:\Users\[username]\miniconda3
  - `[virtual environment]` is in C:\Users\[username]\miniconda3\envs\\[virtual environment name]

- ran into some problems getting the `PythonEngine.Shutdown();`to work (e.g. very slow...minutes...to complete), but then it started working (?!?!?). Now typical shutdown time is 2-3 seconds.
- deployment
  - build a `conda env` with the necessary dependencies
    - you can use environment.yml file with `conda env` for reproducibility
    - tested with python 3.13.5, dumpy, and matplotlib

  - add robocopy command  to the Visual Studio post build event
    - `(robocopy "[path to conda env]" "$(TargetDir)Python" /MIR /NFL /NDL) ^& IF %ERRORLEVEL% LEQ 7 SET ERRORLEVEL=0`
    - Note that this can take a long time (minutes) as there are lots of files even with most basic python env. May be best to do this step manually and/or include application setting to point to python folder location. 
    
  - publish
    - `self-contained` publish mode does not work
      - build succeeds but exe does not run
  
    - `Framework-dependent` publish mode does work
      - the publish result will most likely need to have C# redistributable installed or similar
        - need to test this on vanilla VM without Visual Studio installed
      
    - conda virtual environment `Python` directory  is not copied to publish folder...need to copy manually


## FAQ

- Q: When using PythonNet in which process does the Python interpreter run?

  - A: It runs in the same process as the CLR/C#/.NET and runtime of your application. Because of this, running python code with Python.NET can block the application. 

  * [Threading · pythonnet/pythonnet Wiki](https://github.com/pythonnet/pythonnet/wiki/Threading)

    * When calling Python functions, the caller must hold the GIL. Otherwise, you'll likely experience crashes with AccessViolationException or data races, that corrupt memory.

      When executing .NET code, consider releasing the GIL to let Python run other threads. Otherwise, you might experience deadlocks or starvation.

- Q: when I create a plot in C# using Python.NET, can I get a binary stream instead of saving to a file?

  - A: these links might help

    * [python matplotlib get binary stream of chart image - Google Search](https://www.google.com/search?client=safari&rls=en&q=python+matplotlib+get+binary+stream+of+chart+image&ie=UTF-8&oe=UTF-8)

    * [python - Get Binary Image Data From a MatPlotLib Canvas?](https://stackoverflow.com/questions/12144877/get-binary-image-data-from-a-matplotlib-canvas)

    * [python - Matplotlib to smtplib](https://stackoverflow.com/questions/18766060/matplotlib-to-smtplib/21862555#21862555)

    ```python
    from matplotlib import pyplot as plt
    from io import BytesIO
    
    plt.plot([],[])
    data_obj = BytesIO()
    plt.savefig(data_obj, format='png')
    
    # write to file as demo; you can do what you want with the data
    with open('myplot.png', 'wb') as f:
        f.write(data_obj.getvalue())
    ```

## Performance

I've not run any specific performance tests...assuming we would stick to C# if performance is necessary. 

* [Performance: pythonnet more than 400x slower than identical C# · Issue #694 · pythonnet/pythonnet](https://github.com/pythonnet/pythonnet/issues/694)
* https://github.com/QuantConnect/Lean/issues/2026#issuecomment-486752032
  * Python is now about ~20% from C# and on-par with Cython native speeds.

* [How To Run Python Code Concurrently In C# Using Python.NET | Diogo Tosta Silva](https://tostasilva.com/posts/how-to-work-with-concurrency-in-pythonnet-using-c-sharp/)

### From Google AI Overview:

Python.NET allows for integration between Python and the .NET Common Language Runtime (CLR), enabling Python code to interact with .NET assemblies and embed Python within .NET applications. 

Performance Considerations: 

- **Overhead of Interoperability:** There is an inherent overhead when calling between Python and .NET through Python.NET. This overhead is more pronounced with frequent, small calls and less significant when dealing with computationally intensive tasks or large data operations handled by optimized libraries like NumPy or TensorFlow within Python. 

- **Data Marshalling:** Transferring large or complex data structures between Python and .NET incurs a cost due to data marshalling, the process of converting data types between the two environments. 

- **Use Case Dependency:** 

  The impact of Python.NET's performance overhead is highly dependent on the specific use case: 

  - **High-frequency, small calls:** Performance can be noticeably affected. 
  - **Large, computationally intensive operations:** The overhead becomes a smaller fraction of the overall execution time. 
  - **Leveraging optimized Python libraries:** When operations are offloaded to efficient Python libraries (e.g., NumPy for array operations, PyTorch/TensorFlow for GPU computations), the interop overhead is often negligible compared to the execution time of the underlying operations. 

- **Shutdown Slowness:** In certain scenarios, particularly when significant memory has been allocated on the C# side, the PythonEngine.Shutdown() or Runtime.Shutdown() calls can be slow due to garbage collection processes. 

Optimization Strategies: 

- **Minimize Cross-Language Calls:** Reduce the frequency of calls between Python and .NET, especially within tight loops. 
- **Batch Operations:** Modify Python functions to accept and process larger batches of data at once, reducing the number of individual interop calls. 
- **Utilize Optimized Libraries:** Leverage highly optimized Python libraries for computationally intensive tasks to minimize the impact of the interop overhead. 
- **Consider Alternatives:** For scenarios where performance is critical and Python.NET proves to be a bottleneck, alternative approaches like microservices or child processes for running Python code might be considered. 

*AI responses may include mistakes.*

## References

* [Python.NET | pythonnet](http://pythonnet.github.io/) - home page for Python.NET

* [Writing and Running Python in .NET](https://www.youtube.com/watch?v=6N2oFh6YTTc) - Nick Chapsas tutorial that started me down this path

* [pythonnet/pythonnet](https://github.com/pythonnet/pythonnet) - GitHub repo for Python.NET

  * [Python.NET documentation](https://pythonnet.github.io/pythonnet/) - which is just ok

* Tutorials
  * [Calling Python from C#: an introduction to PythonNET](https://somegenericdev.medium.com/calling-python-from-c-an-introduction-to-pythonnet-c3d45f7d5232)
  * [Pythonnet – A Simple Union of .NET Core and Python You’ll Love](https://www.codeproject.com/Articles/5352648/Pythonnet-A-Simple-Union-of-NET-Core-and-Python-Yo)
  * [Integrate Python with C# using Python.NET](https://www.luisllamas.es/en/csharp-pythonnet/)

  * [Pythonnet – A Simple Union of .NET Core and Python You’ll Love - CodeProject](https://www.codeproject.com/Articles/5352648/Pythonnet-A-Simple-Union-of-NET-Core-and-Python-Yo)
  * [Calling Python from C#: an introduction to PythonNET | by somegenericdev | Medium](https://somegenericdev.medium.com/calling-python-from-c-an-introduction-to-pythonnet-c3d45f7d5232)
  
  * [Intro to Pythonnet](https://www.youtube.com/watch?v=gFO12dJLBGI&list=PLcFcktZ0wnNnz07eWc7N5ao1dyiXoV-ib&index=1) - series of 11 tutorial videos on Python.NET
  
  * [Python and IronPython scripting and debugging - AlterNET Software](https://www.alternetsoft.com/blog/python-net-iron-python-scripting)

- Deployment hints
  - [Conda Python environments are standalone and can be deployed with your application](https://github.com/pythonnet/pythonnet/issues/463#issuecomment-302818208) discusses that conda `virtual environment` is self-contained (a folder of files ) and portable and can be included with your C# app using robocopy.  
- Github Sample Code
  - [First steps with Python.NET](https://www.libreautomate.com/forum/showthread.php?tid=7484) has Pynet.cs class and discusses the BinaryFormatter issue
  - https://github.com/yagweb/pythonnetLab
  - https://github.com/olonok69/LLM_Notebooks/blob/main/microsoft/csharp/pythonNet/Program.cs
- Related Projects
  * [fdieulle/pandasnet](https://github.com/fdieulle/pandasnet)  - pandasnet is a python package build on top of pythonnet. It provides additional data conversions for pandas, numpy and datetime.
  
  * [SciSharp/NumSharp: High Performance Computation for N-D Tensors in .NET, similar API to NumPy.](https://github.com/SciSharp/NumSharp)
