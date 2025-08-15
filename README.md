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

## Example

```c#
using Python.Runtime;

namespace pythonDotNet
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                // miniconda "base" virtual environment (the default conda virtual environment)
                // var pathToVirtualEnv = @"C:\Users\charlie\miniconda3";

                // miniconda "python313" virtual environment with no extra python modules loaded
                // var pathToVirtualEnv = @"C:\Users\charlie\miniconda3\envs\python313";

                // miniconda "numpy" virtual environment with numpy python module loaded
                // var pathToVirtualEnv = @"C:\Users\charlie\miniconda3\envs\numpy";

                // use the local python that has been copied to the applicaton directory
                var pathToVirtualEnv = @".\Python";

                var pathToVirtualEnvDll = $"{pathToVirtualEnv}\\python313.dll";

                var path = Environment.GetEnvironmentVariable("PATH").TrimEnd(';');
                path = string.IsNullOrEmpty(path) ? pathToVirtualEnv : path + ";" + pathToVirtualEnv;
                Environment.SetEnvironmentVariable("PATH", path, EnvironmentVariableTarget.Process);
                Environment.SetEnvironmentVariable("PYTHONHOME", pathToVirtualEnv, EnvironmentVariableTarget.Process);
                Environment.SetEnvironmentVariable("PYTHONPATH", $"{pathToVirtualEnv}\\Lib\\site-packages;{pathToVirtualEnv}\\Lib;{pathToVirtualEnv}\\DLLs;{pathToVirtualEnv}\\Library\\lib", EnvironmentVariableTarget.Process);
                
                // this is only required when using matplotlib...but even with this I could not get it to work....
                Environment.SetEnvironmentVariable("QT_PLUGIN_PATH", $"{pathToVirtualEnv}\\Library\\lib", EnvironmentVariableTarget.Process);

                Runtime.PythonDLL = pathToVirtualEnvDll;
                RuntimeData.FormatterType = typeof(NoopFormatter);

                PythonEngine.PythonHome = pathToVirtualEnv;
                PythonEngine.PythonPath = Environment.GetEnvironmentVariable("PYTHONPATH", EnvironmentVariableTarget.Process);

                PythonEngine.Initialize();
                var threadState = PythonEngine.BeginAllowThreads();

                // obtain the GIL for thread safety
                using (Py.GIL())
                {
                    //////////////////////////////////////////////////
                    // run basic hello world script from C# string
                    string pythonCodeString = """print("basic python hello, world!")""";
                    PythonEngine.Exec(pythonCodeString);

                    //////////////////////////////////////////////////
                    // use a scope, set/get some variables
                    // https://pythonnet.github.io/pythonnet/dotnet.html#passing-c-objects-to-the-python-engine
                    using (PyModule scope = Py.CreateScope())
                    {
                        // create person object
                        Person person = new Person("John", "Smith", 21);

                        // convert the Person object to a PyObject
                        PyObject pyPerson = person.ToPython();

                        // create a Python variable "person" using C# PyObject
                        scope.Set("person", pyPerson);

                        // the person python object may now be used in Python
                        scope.Exec("""fullName = person.FirstName + ' ' + person.LastName""");
                        scope.Exec("""print("print fullName from python: ",fullName)""");

                        // get the scope variables and get the value of fullName
                        var vars = scope.Variables();
                        Console.WriteLine($"print fullName from C#: {vars["fullName"]}");
                    }

                    //////////////////////////////////////////////////
                    // need to update sys.path with the path 
                    // to this executable if scripts are stored with exe
                    // this is required for a couple of the following examples
                    // https://stackoverflow.com/questions/78070739/cant-import-my-module-with-python-net-from-the-same-directory-as-the-project
                    string scriptDir = Directory.GetCurrentDirectory();
                    dynamic sys = Py.Import("sys");
                    sys.path.append(scriptDir);

                    //////////////////////////////////////////////////
                    // use C# class in python script
                    // https://somegenericdev.medium.com/calling-python-from-c-an-introduction-to-pythonnet-c3d45f7d5232
                    // requires sys.path update
                    // also requires that this code lives in a namespace named after the project "pythonDotNet"

                    // Use C# classes in Python
                    pythonCodeString = """
                        import clr
                        clr.AddReference("pythonDotNet")
                        from pythonDotNet import MyClass
                        csharpVariable=MyClass.MyVar
                        csharpVariable=csharpVariable + " and this part was added by Python"
                        print(csharpVariable)
                        csharpMethodStr = MyClass.getMyVar(" and this was added by getMyVar()")
                        print(csharpMethodStr)
                        """;
                    PythonEngine.Exec(pythonCodeString);

                    // import any class, including C#’s System classes like Debug
                    pythonCodeString = """
                        import clr
                        clr.AddReference("System")
                        from System.Diagnostics import Debug
                        Debug.WriteLine("*******************\n\n\nthis line was printed by Python in the Debug window!\n\n\n********************")
                        """;
                    PythonEngine.Exec(pythonCodeString);

                    // Use C# types from Python (in this case DateTime)
                    pythonCodeString = """
                        import clr
                        clr.AddReference("System")
                        from System import DateTime
                        mydate=DateTime(2010, 5, 11)
                        print(mydate)
                        print(type(mydate))
                        print("printing var mydate generated from a C# type in python: " + DateTime(2010, 5, 11).ToString())
                        """;
                    PythonEngine.Exec(pythonCodeString);

                    // pass C# object "someone" to python, modify it, and return value as "hisAge" from Python to C#
                    Person someone = new Person("John", "Doe", 21);
                    object age = RunPythonCodeAndReturn(
                        """
                        someone.Age=someone.Age+3
                        hisAge=someone.Age
                        """, 
                        someone, 
                        "someone", 
                        "hisAge");
                    Console.WriteLine("from RunPythonCodeAndReturn() someone.age returned as hisAge is " + age.ToString());

                    //////////////////////////////////////////////////
                    // Nick Chapsas example (https://www.youtube.com/watch?v=6N2oFh6YTTc)
                    // read example.py and call methods from C#
                    // requires sys.path update
                    dynamic example = Py.Import("example");
                    example.hello_world();
                    dynamic calculator = example.Calculator();
                    float result = calculator.add(1, 2);
                    Console.WriteLine($"from example.py calculator, add result is {result} {result.GetType()}");

                    // if numpy is not installed in the virtual env then this will throw exception...
                    dynamic np = Py.Import("numpy");
                    Console.WriteLine($"using numpy, cos(2pi) = {np.cos(np.pi *2)}");

                    dynamic sin = np.sin;
                    Console.WriteLine($"using numpy, sin(5) = {sin(5)}");

                    double c = (double)(np.cos(5) + np.sin(5));
                    Console.WriteLine($"using numpy, np.cos(5) + np.sin(5) = {c}");

                    dynamic a = np.array(new List<float> { 1, 2, 3 });
                    Console.WriteLine($"using numpy, np.array dtype is {a.dtype}");

                    dynamic b = np.array(new List<float> { 6, 5, 4 }, dtype: np.int32);
                    Console.WriteLine($"using numpy, np.array dtype is {b.dtype}");

                    Console.WriteLine($"using numpy, a is  {a} b is {b}");
                    Console.WriteLine($"using numpy, a * b is  {a * b}");

                    //////////////////////////////////////////////////
                    // https://www.codeproject.com/Articles/5352648/Pythonnet-A-Simple-Union-of-NET-Core-and-Python-Yo

                    // define a python function and use it in C#
                    // note dynamic instead of PyModule
                    using (dynamic scope = Py.CreateScope())
                    {
                        int firstInt = 1;
                        int secondInt = 2;
                        scope.Exec("def add(a, b): return a + b");
                        var sum = scope.add(firstInt, secondInt);
                        Console.WriteLine($"a python function used in C# --> add({firstInt},{secondInt}): {sum}");
                    }

                    // generate a python list in python and assign to C# object
                    using (PyModule scope = Py.CreateScope())
                    {
                        scope.Exec("number_list = [1, 2, 3, 4, 5]");
                        var pythonListObj = scope.Eval("number_list");
                        var csharpListObj = pythonListObj.As<int[]>();
                        Console.WriteLine("The numbers in C# from python list 'number_list' are:");
                        foreach (var value in csharpListObj)
                        {
                            Console.WriteLine($"  {value} {value.GetType()}");
                        }
                    }

                    //////////////////////////////////////////////////
                    // run a python script that imports numpy
                    // if numpy is not installed in the virtual env then this will throw exception...
                    var numpyScript = """
                    import numpy as np
                    arr = np.array([1, 2, 3, 4, 5])
                    print(arr)
                    print(type(arr))
                    """;

                    Console.WriteLine($"-----------------\nnumpy script:");
                    PythonEngine.Exec(numpyScript);
                    Console.WriteLine($"-----------------\n");

                    //////////////////////////////////////////////////
                    // example using matplotlib
                    Console.WriteLine($"-----------------\nmatplotlibExample1");
                    matplotlibExample1();
                    Console.WriteLine($"-----------------\n");

                    Console.WriteLine($"-----------------\nmatplotlibExample2");
                    matplotlibExample2();
                    Console.WriteLine($"-----------------\n");

                }
                Console.WriteLine($"done.");
                PythonEngine.EndAllowThreads(threadState);

            }
            catch (Python.Runtime.PythonException pe)
            {
                Console.WriteLine(pe.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                Console.WriteLine("Shutdown start");
                var start = DateTime.Now;
                PythonEngine.Shutdown();
                var diff = DateTime.Now - start;
                Console.WriteLine($"Shutdown stop {diff.TotalSeconds}");
            }

            // pause so we can see the output from the debug.writeline()
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            Console.WriteLine("Key pressed! Program continues.");
        }

        public static object RunPythonCodeAndReturn(string pycode, object parameter, string parameterName, string returnedVariableName)
        {
            object returnedVariable = new object();

            using (PyModule scope = Py.CreateScope())
            {
                scope.Set(parameterName, parameter.ToPython());
                scope.Exec(pycode);
                returnedVariable = scope.Get<object>(returnedVariableName);
            }
            
            return returnedVariable;
        }

        public static void matplotlibExample1()
        {
            dynamic np = Py.Import("numpy");
            dynamic plt = Py.Import("matplotlib.pyplot");
            dynamic mpl = Py.Import("matplotlib");
            mpl.use("TKAgg");

            // Generate some data
            dynamic x = np.linspace(0, 10, 100);
            dynamic y = np.sin(x);

            // Create plot
            plt.plot(x, y);
            plt.title("Sine Wave");
            plt.xlabel("X-axis");
            plt.ylabel("Y-axis");

            // Save plot to a file 
            plt.savefig("sine_wave1.png");

            // note that this blocks, and I cannot get block=False to work
            // plt.show(block: false);
            plt.show();

            plt.close();
        }

        public static void matplotlibExample2()
        {
            // note that this method when run after matplotlibExample1 inherits the python env with variables etc.

            using (PyModule scope = Py.CreateScope())
            {
                dynamic np = scope.Import("numpy");
                dynamic plt = scope.Import("matplotlib.pyplot");
                dynamic x = np.array(new double[] { 1.0, 2.0, 3.0, 4.0, 5.0, 6.0 , 7.0, 8.0});
                dynamic y = np.sin(x);
                scope.Set("x", x);
                scope.Set("y", y);
                plt.plot(x, y);
                plt.title("Sine Wave");
                plt.xlabel("X-axis");
                plt.ylabel("Y-axis");
                plt.savefig("sine_wave2.png");
                plt.show();
                plt.close();
            }
        }
    }

    public class Person
    {
        public Person(string firstName, string lastName, int age)
        {
            FirstName = firstName;
            LastName = lastName;
            Age = age;
        }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int Age { get; set; }
    }

    public class MyClass
    {
        public static string MyVar = "this is a C# variable";

        public static string getMyVar(string s) { return  MyVar + s; }
    }

}



```



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
