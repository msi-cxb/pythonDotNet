using Python.Runtime;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;

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

            var pathToVirtualEnvDll = $"{pathToVirtualEnv}\\python313.dll";

            var path = Environment.GetEnvironmentVariable("PATH").TrimEnd(';');
            path = string.IsNullOrEmpty(path) ? pathToVirtualEnv : path + ";" + pathToVirtualEnv;
            Environment.SetEnvironmentVariable("PATH", path, EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable("PYTHONHOME", pathToVirtualEnv, EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable("PYTHONPATH", $"{pathToVirtualEnv}\\Lib\\site-packages;{pathToVirtualEnv}\\Lib", EnvironmentVariableTarget.Process);

            Runtime.PythonDLL = pathToVirtualEnvDll;
            RuntimeData.FormatterType = typeof(NoopFormatter);

            PythonEngine.PythonHome = pathToVirtualEnv;
            PythonEngine.PythonPath = Environment.GetEnvironmentVariable("PYTHONPATH", EnvironmentVariableTarget.Process);

            PythonEngine.Initialize();
            var threadState = PythonEngine.BeginAllowThreads();

            Person person = new Person("John", "Smith");

            using (Py.GIL())
            {
                // basic hello world
                var pythonCodeString = $"print(\"Hello, World!\")";
                PythonEngine.Exec(pythonCodeString);

                // https://pythonnet.github.io/pythonnet/dotnet.html#passing-c-objects-to-the-python-engine
                // create a Python scope
                using (PyModule scope = Py.CreateScope())
                {
                    // convert the Person object to a PyObject
                    PyObject pyPerson = person.ToPython();

                    // create a Python variable "person"
                    scope.Set("person", pyPerson);

                    // the person object may now be used in Python
                    string code = "fullName = person.FirstName + ' ' + person.LastName";
                    scope.Exec(code);

                    scope.Exec($"print(fullName)");

                    // Nick Chapsas example (https://www.youtube.com/watch?v=6N2oFh6YTTc)

                    // https://stackoverflow.com/questions/78070739/cant-import-my-module-with-python-net-from-the-same-directory-as-the-project
                    string scriptDir = Directory.GetCurrentDirectory();
                    dynamic sys = Py.Import("sys");
                    sys.path.append(scriptDir);

                    dynamic example = Py.Import(@"example");
                    dynamic calculator = example.Calculator();
                    float result = calculator.add(1, 2);
                    Console.WriteLine(result);

                    // if numpy is not installed this will throw exception...
                    //dynamic numpy = Py.Import(@"numpy");

                    var numpyScript = """
                    import numpy as np
                    arr = np.array([1, 2, 3, 4, 5])
                    print(arr)
                    print(type(arr))
                    """;

                    Console.WriteLine($"-----------------\nnumpy script:");
                    PythonEngine.Exec(numpyScript);
                    Console.WriteLine($"-----------------");
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
    }
}

public class Person
{
    public Person(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
    }

    public string FirstName { get; set; }
    public string LastName { get; set; }
}

