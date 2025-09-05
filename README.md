# pythonDotNet

[toc]

## TODO

- [x] get a C# project working
- [ ] evaluate the ease of loading and using python libraries in C#
  - [x] numpy
  - [x] pandas
  - [ ] polars
  - [ ] SQLAlchemy
  - [ ] DuckDB
  - [ ] Matplotlib
    - [x] generate plots and save to file
    - [x] use show() to display on screen
    - [ ] Why does show() not work with Qt? (currently only works with Tk)
  - [ ] Seaborn
  - [ ] Plotly
- [x] evaluate ability of building C# dll with methods that can be imported and used in python
- [x] look at how data exchange works between C# and python 
  - [x] going back and forth (C#/Python) with simple data types (e.g. list, array)
  - [x] C# datatable to python pandas dataframe
  - [x] python pandas dataframe to C# datatable
- [ ] performance
  - [ ] more research/testing of best methods to get/release python GIL so that it does not affect rest of application
  - [ ] current examples use very small test dataset...need to test with larger datasets to look at performance
- [x] look at the mechanisms required to load/use python libraries 
  - [x] how to package the necessary libraries to run offline
- [ ] Deployment
  - [x] how to deploy the python environment with the C# application
  - [ ] test framework-dependent install on vanilla VM without Visual Studio with conda virtual environment

## Configuration

- using MiniConda with python 3.13.5
- Visual Studio 2022 17.14.11 with .NET 8 console app project
- install python.net using NuGet (version 3.0.5)
- using conda virtual environment named 'numpy' with numpy, pandas, pandasnet installed
- https://github.com/msi-cxb/pythonDotNet

## Notes

- lots of problems getting this to work with miniconda...finally got it to work based on [Using Python.NET with Virtual Environments](https://github.com/pythonnet/pythonnet/wiki/Using-Python.NET-with-Virtual-Environments)
  - I think most issues had to do with using virtual environments 
  - now that I've figured it out, it is working well...virtual environments does add some additional complexity

- can use either with conda "base" virtual environment or with user defined virtual environments without activating them...just point to the folder containing the virtual environment (below is for windows):
  - `base` is in C:\Users\[username]\miniconda3
  - `[virtual environment]` is in C:\Users\[username]\miniconda3\envs\\[virtual environment name]

- ran into some problems getting the `PythonEngine.Shutdown();`to work (e.g. very slow...minutes...to complete), but then it started working (?!?!?). Now typical shutdown time is 2-3 seconds.
- deployment
  - build a `conda env` with the necessary dependencies
    - you can [and probably should] use environment.yml file with `conda env` for reproducibility
    - tested with python 3.13.5, numpy, matplotlib, pandas, pandasnet

  - you can use robocopy command  to the Visual Studio post build event
    - `(robocopy "[path to conda env]" "$(TargetDir)Python" /MIR /NFL /NDL) ^& IF %ERRORLEVEL% LEQ 7 SET ERRORLEVEL=0`
    - Note that including this in build can take a long time (minutes) as there are lots of files even with most basic python env. 
    - May be best to do this step manually and/or include application setting to point to python folder location (example code does this). 

  - publish
    - `self-contained` publish mode does not work
      - build succeeds but exe does not run

    - `Framework-dependent` publish mode does work
      - the publish result will most likely need to have C# redistributable installed or similar
        - need to test this on vanilla VM without Visual Studio installed
      
    - conda virtual environment `Python` directory  is not copied to publish folder...need to copy manually

- example C# project https://github.com/msi-cxb/pythonDotNet
  - includes
    - basic python examples
    - using external DLL
    - pandas specific
    - pandas dataframe to C# datatable (via pandasnet)
    - C# datatable to python pandas dataframe 

  - example output below


## C# DataTable <---> Python Pandas DataFrame

Bottom line is that some code is necessary to convert between C# DataTable and Python Pandas DataFrame is required. There is no automatic conversion. However, it is not that difficult. 

### Python Pandas DataFrame to C# DataTable

Python code that sets up a Pandas DataFrame:

``` python
import clr
import pandasnet
import pandas as pd
from datetime import datetime

x = pd.DataFrame({
    'A': [1, 2, 3],
    'B': [1.21, 1.22, 1.23],
    'C': ['foo', 'bar', 'other'],
    'D': [datetime(2021, 1, 21,12,0,1), datetime(2021, 1, 22,12,0,2), datetime(2021, 1, 23,12,0,3)]
})

# this will work in C# by itself as long as 'import pandasnet' is included
print("DataFrame in python")
print(x)
```

C# source code that converts Python Pandas DataFrame `x` to C# DataTable. Note that pandasnet Python package (see the `Related Projects` section in [References](#References)) provides the mapping of the Pandas DataFrame to a C# Dictionary<string, Array>, which is then converted to a C# DataTable via ConvertToDataTable() method. The data types from the Pandas DataFrame are maintained when converting to C# DataTable (at least in theory).

``` c#
// assign a C# variable to the Pandas DataFrame
Dictionary<string, Array> df = scope.Eval("x");
using DataTable dt1 = ConvertToDataTable(df);

public static DataTable ConvertToDataTable(Dictionary<string, Array> dict)
{
    DataTable dataTable = null;
    using (dataTable = new DataTable())
    {
        if (dict.Count > 0)
        {
            // keys are column names
            var headers = dict.Keys;
            var values = dict.Values;

            // create columns with names and types from dict
            foreach(var i in dict)
            {
                dataTable.Columns.Add(i.Key, i.Value.GetType().GetElementType());
            }

            // i is the row number
            for (int row = 0; row < dict.Values.Max(item => item.Length); row++)
            {
                DataRow dataRow = dataTable.NewRow();
                object[] array = new object[dict.Count];

                // j is the column number
                for (int col = 0; col < dict.Count; col++)
                {
                    KeyValuePair<string, Array> p = dict.ElementAt(col);
                    dataRow[col] = p.Value.GetValue(row);
                }
                dataTable.Rows.Add(dataRow);
            }
        }
    }
    return dataTable;
}

```



### C# DataTable to Python Pandas DataFrame

In the C# code below, `dt` is a C# DataTable. PythonNet is then used to create a python object from the DataTable, and then the python script is executed that uses the DataTable python object to create a DataFrame.

``` C#
using (dynamic scope = Py.CreateScope())
{
    PyObject pyDt = dt.ToPython();

    scope.Set("dt", pyDt);

    Console.WriteLine($"-----------------\npython script:\n-----------------");
    scope.Exec(pyScript);
}

```

Below is Python script code that converts C# DataTable to Python Pandas DataFrame.

``` python
print("Convert C# datatable to Pandas DataFrame")

# Extract column names
columns = [col.ColumnName for col in dt.Columns]

# Extract row data
data = []
for row in dt.Rows:
    row_data = [row[col.ColumnName] for col in dt.Columns]
    data.append(row_data)

# Create a Pandas DataFrame
df = pd.DataFrame(data, columns=columns)

print("")
print("DataFrame Info()")
print(df.info())
print("")
print(df)
```



## Test Program Example Output 

```
*** PYTHONTZPATH
PATH C:\Program Files\Parallels\Parallels Tools\Applications;C:\WINDOWS\system32;C:\WINDOWS;C:\WINDOWS\System32\Wbem;C:\WINDOWS\System32\WindowsPowerShell\v1.0\;C:\WINDOWS\System32\OpenSSH\;C:\Program Files\TortoiseSVN\bin;C:\Program Files\TortoiseGit\bin;C:\Program Files\Git\cmd;C:\Program Files\GitHub CLI\;C:\Program Files (x86)\Windows Kits\10\Windows Performance Toolkit\;C:\Program Files\dotnet\;C:\Users\charlie\AppData\Local\Microsoft\WindowsApps;M:\tools\bin;C:\Users\charlie\AppData\Local\Programs\Microsoft VS Code\bin;C:\Users\charlie\.dotnet\tools;M:\tools\_alias;M:\tools\bin\vim91;C:\Users\charlie\miniconda3\envs\numpy
PYTHONHOME C:\Users\charlie\miniconda3\envs\numpy
PYTHONPATH C:\Users\charlie\miniconda3\envs\numpy\Lib\site-packages;C:\Users\charlie\miniconda3\envs\numpy\Lib;C:\Users\charlie\miniconda3\envs\numpy\DLLs;C:\Users\charlie\miniconda3\envs\numpy\Library\lib
TZPATH C:\Users\charlie\miniconda3\envs\numpy\share\zoneinfo
PYTHONTZPATH C:\Users\charlie\miniconda3\envs\numpy\share\zoneinfo
starting...

---------------------------
basic python examples
---------------------------

basic python hello, world!
-----------------
numpy script:
[1 2 3 4 5]
<class 'numpy.ndarray'>
-----------------

print fullName from python:  John Smith
print fullName from C#: John Smith
this is a C# variable and this part was added by Python
this is a C# variable and this was added by getMyVar()
5/11/2010 12:00:00 AM
<class 'System.DateTime'>
printing var mydate generated from a C# type in python: 5/11/2010 12:00:00 AM
from RunPythonCodeAndReturn() someone.age returned as hisAge is 24
Hello World from example.py!
from example.py calculator, add result is 3 System.Single
using numpy, cos(2pi) = 1.0
using numpy, sin(5) = -0.9589242746631385
using numpy, np.cos(5) + np.sin(5) = -0.6752620891999122
using numpy, np.array dtype is float64
using numpy, np.array dtype is int32
using numpy, a is  [1. 2. 3.] b is [6 5 4]
using numpy, a * b is  [ 6. 10. 12.]
a python function used in C# --> add(1,2): 3
The numbers in C# from python list 'number_list' are:
  1 System.Int32
  2 System.Int32
  3 System.Int32
  4 System.Int32
  5 System.Int32
-----------------
matplotlibExample1
-----------------

-----------------
matplotlibExample2
-----------------


---------------------------
using an external DLL
---------------------------

-----------------
script:
['C:\\Users\\charlie\\miniconda3\\envs\\numpy\\Lib\\site-packages', 'C:\\Users\\charlie\\miniconda3\\envs\\numpy\\Lib', 'C:\\Users\\charlie\\miniconda3\\envs\\numpy\\DLLs', 'C:\\Users\\charlie\\miniconda3\\envs\\numpy\\Library\\lib', 'C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App\\8.0.19\\', 'H:\\pythonDotNet\\bin\\Debug\\net8.0', 'H:\\PythonDotNetDLL\\bin\\Debug\\net8.0']
2
-1

---------------------------
pandas specific test code
---------------------------

-----------------
numpy/pandas script:
C:\Users\charlie\miniconda3\envs\numpy\Lib\site-packages\pandas\_libs\tslibs\__init__.py:40: InvalidTZPathWarning: Invalid paths specified in PYTHONTZPATH environment variable. Paths should be absolute but found the following relative paths:
    .\share\zoneinfo
  from pandas._libs.tslibs.conversion import localize_pydatetime
TZPATH .\share\zoneinfo
None
[[0 0]
 [0 1]
 [1 0]
 [1 1]]
<class 'numpy.ndarray'>
[1 0 1 0]
<class 'numpy.ndarray'>
     StateAttributes  reward
row1          [0, 0]       1
row2          [0, 1]       0
row3          [1, 0]       1
row4          [1, 1]       0
<class 'pandas.core.frame.DataFrame'>
------------------
np_array_data_Obj
{np_array_data_Obj}

{np_array_data_Obj.GetType()}
------------------
np_array_reward_Obj
{np_array_reward_Obj}

{np_array_reward_Obj.GetType()}
------------------
pd_dataframe
{pd_dataframe}

{pd_dataframe.GetType()}
----- [0 0]
>>> 0
>>> 0
-----
----- [0 1]
>>> 0
>>> 1
-----
----- [1 0]
>>> 1
>>> 0
-----
----- [1 1]
>>> 1
>>> 1
-----
---------
head(2)
     StateAttributes  reward
row1          [0, 0]       1
row2          [0, 1]       0
---------
tail(2)
     StateAttributes  reward
row3          [1, 0]       1
row4          [1, 1]       0
---------
iloc[0]
StateAttributes    [0, 0]
reward                  1
Name: row1, dtype: object
-----------------


---------------------------
pandas dataframe to C# datatable (via pandasnet)
---------------------------

-----------------
python script:
DataFrame in python
   A     B      C                   D
0  1  1.21    foo 2021-01-21 12:00:01
1  2  1.22    bar 2021-01-22 12:00:02
2  3  1.23  other 2021-01-23 12:00:03
BasicDataFrame in python
   A     B      C                   D
0  1  1.21    foo 2021-01-21 12:00:01
1  2  1.22    bar 2021-01-22 12:00:02
2  3  1.23  other 2021-01-23 12:00:03
-----------------
C#:
--------------------
Python DataFrame to C# DataTable
--------------------

--------------------
DataTable (num rows 3 columns 4)
--------------------
DumpDataTable
---------------------------------------
A,B,C,D,
1 [System.Int64],1.21 [System.Double],foo [System.String],1/21/2021 12:00:01 PM [System.DateTime],
2 [System.Int64],1.22 [System.Double],bar [System.String],1/22/2021 12:00:02 PM [System.DateTime],
3 [System.Int64],1.23 [System.Double],other [System.String],1/23/2021 12:00:03 PM [System.DateTime],

--------------------
--------------------
Python BasicDataFrame to C# DataTable
--------------------

--------------------
BasicDataFrame DataTable (rows 3 columns 4)
--------------------
DumpDataTable
---------------------------------------
A,B,C,D,
1 [System.Int64],1.21 [System.Double],foo [System.String],1/21/2021 12:00:01 PM [System.DateTime],
2 [System.Int64],1.22 [System.Double],bar [System.String],1/22/2021 12:00:02 PM [System.DateTime],
3 [System.Int64],1.23 [System.Double],other [System.String],1/23/2021 12:00:03 PM [System.DateTime],

--------------------

---------------------------
C# datatable to python
---------------------------

-----------------
DataTable
-----------------
DumpDataTable
---------------------------------------
A,B,C,D,
1 [System.Int64],1.21 [System.Double],foo [System.String],1/21/2021 12:00:01 PM [System.DateTime],
2 [System.Int64],1.22 [System.Double],bar [System.String],1/22/2021 12:00:02 PM [System.DateTime],
3 [System.Int64],1.23 [System.Double],other [System.String],1/23/2021 12:00:03 PM [System.DateTime],

-----------------
python script:
-----------------
Convert C# datatable to Pandas DataFrame

DataFrame Info()
<class 'pandas.core.frame.DataFrame'>
RangeIndex: 3 entries, 0 to 2
Data columns (total 4 columns):
 #   Column  Non-Null Count  Dtype
---  ------  --------------  -----
 0   A       3 non-null      int64
 1   B       3 non-null      float64
 2   C       3 non-null      object
 3   D       3 non-null      object
dtypes: float64(1), int64(1), object(2)
memory usage: 228.0+ bytes
None

   A     B      C                      D
0  1  1.21    foo  1/21/2021 12:00:01 PM
1  2  1.22    bar  1/22/2021 12:00:02 PM
2  3  1.23  other  1/23/2021 12:00:03 PM
done.
Shutdown start
Shutdown stop 3.8166418
Press any key to continue...
```




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
