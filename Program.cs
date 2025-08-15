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
                var pathToVirtualEnv = @"C:\Users\charlie\miniconda3\envs\numpy";

                // conda virtual environment copied to folder called "Python" in applicaton directory
                // note that this folder can be quite large depending on installed modules (e.g. numpy, pandas, etc.)
                // var pathToVirtualEnv = @".\Python";

                Console.WriteLine($"*** PYTHONTZPATH {Environment.GetEnvironmentVariable("PYTHONTZPATH")}");
                var fullPathToVirtualEnv = Path.GetFullPath(pathToVirtualEnv);

                var pathToVirtualEnvDll = $"{fullPathToVirtualEnv}\\python313.dll";

                var path = Environment.GetEnvironmentVariable("PATH").TrimEnd(';');
                path = string.IsNullOrEmpty(path) ? fullPathToVirtualEnv : path + ";" + fullPathToVirtualEnv;
                Environment.SetEnvironmentVariable("PATH", path, EnvironmentVariableTarget.Process);
                Environment.SetEnvironmentVariable("PYTHONHOME", fullPathToVirtualEnv, EnvironmentVariableTarget.Process);
                Environment.SetEnvironmentVariable("PYTHONPATH", $"{fullPathToVirtualEnv}\\Lib\\site-packages;{fullPathToVirtualEnv}\\Lib;{fullPathToVirtualEnv}\\DLLs;{fullPathToVirtualEnv}\\Library\\lib", EnvironmentVariableTarget.Process);

                // required by pandas?!?!?
                Environment.SetEnvironmentVariable("TZPATH", $"{fullPathToVirtualEnv}\\share\\zoneinfo", EnvironmentVariableTarget.Process);
                Environment.SetEnvironmentVariable("PYTHONTZPATH", $"{fullPathToVirtualEnv}\\share\\zoneinfo", EnvironmentVariableTarget.Process);

                Console.WriteLine($"PATH {Environment.GetEnvironmentVariable("PATH")}");
                Console.WriteLine($"PYTHONHOME {Environment.GetEnvironmentVariable("PYTHONHOME")}");
                Console.WriteLine($"PYTHONPATH {Environment.GetEnvironmentVariable("PYTHONPATH")}");
                Console.WriteLine($"TZPATH {Environment.GetEnvironmentVariable("TZPATH")}");
                Console.WriteLine($"PYTHONTZPATH {Environment.GetEnvironmentVariable("PYTHONTZPATH")}");

                // this is only required when using matplotlib...but even with this I could not get Qt to work....
                Environment.SetEnvironmentVariable("QT_PLUGIN_PATH", $"{fullPathToVirtualEnv}\\Library\\lib", EnvironmentVariableTarget.Process);

                Runtime.PythonDLL = pathToVirtualEnvDll;
                RuntimeData.FormatterType = typeof(NoopFormatter);
                PythonEngine.PythonHome = fullPathToVirtualEnv;
                PythonEngine.PythonPath = Environment.GetEnvironmentVariable("PYTHONPATH", EnvironmentVariableTarget.Process);
                PythonEngine.Initialize();
                var threadState = PythonEngine.BeginAllowThreads();
                Console.WriteLine($"starting...");

                if (false)
                {
                    // obtain the GIL for thread safety
                    using (Py.GIL())
                    {
                        //////////////////////////////////////////////////
                        // run basic hello world script from C# string
                        string pythonCodeString = """print("basic python hello, world!")""";
                        PythonEngine.Exec(pythonCodeString);
                    }
                    using (Py.GIL())
                    {
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
                    }
                    using (Py.GIL())
                    {
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
                    }
                    using (Py.GIL())
                    {
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
                        string pythonCodeString = """
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
                    }
                    using (Py.GIL())
                    {
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
                        Console.WriteLine($"using numpy, cos(2pi) = {np.cos(np.pi * 2)}");

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
                    }
                    using (Py.GIL())
                    {
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
                    }
                    using (Py.GIL())
                    {
                        //////////////////////////////////////////////////
                        // example using matplotlib
                        Console.WriteLine($"-----------------\nmatplotlibExample1");
                        matplotlibExample1();
                        Console.WriteLine($"-----------------\n");
                    }
                    using (Py.GIL())
                    {
                        Console.WriteLine($"-----------------\nmatplotlibExample2");
                        matplotlibExample2();
                        Console.WriteLine($"-----------------\n");
                    }
                }

                if (true)
                {
                    using (Py.GIL())
                    {
                        //////////////////////////////////////////////////
                        // numpy and pandas
                        // note that it does throw this error which I can't seem to fix...but it otherwise works
                        // InvalidTZPathWarning: Invalid paths specified in PYTHONTZPATH environment variable.
                        //      Paths should be absolute but found the following relative paths:
                        //          .\share\zoneinfo
                        // if numpy/pandas is not installed in the virtual env then this will throw exception...
                        var numpyScript = """
                        import os
                        import sysconfig
                        import numpy as np
                        import pandas as pd
                        print("TZPATH " + sysconfig.get_config_var("TZPATH"))
                        print(os.environ.get("PYTHONTZPATH"))
                        data = np.array([[0, 0], [0, 1] , [1, 0] , [1, 1]])
                        print(data)
                        print(type(data))
                        reward = np.array([1,0,1,0])
                        print(reward)
                        print(type(reward))
                        dataset = pd.DataFrame()
                        dataset['StateAttributes'] = data.tolist()
                        dataset['reward'] = reward.tolist()
                        print(dataset)
                        print(type(dataset))
                        """;
                        Console.WriteLine($"-----------------\nnumpy script:");
                        PythonEngine.Exec(numpyScript);
                        Console.WriteLine($"-----------------\n");
                    }
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

            // Qt not working, but Tk does...once set here it will be used from here on out
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


