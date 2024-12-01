using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Keras;
using Keras.Models;
using Numpy;
using Python.Runtime;
using Sudoku.Shared;

namespace NeuralNetworkSolverCSharp
{
	public class NeuralNetworkSolverCSharp:PythonSolverBase
	{
		
        private static string modelPath = GetFullPath(@".\Models\sudoku.model");
		private static BaseModel model;

        //static NeuralNetworkSolverCSharp()
        //{
        //    try
        //    {
        //        // Initialize Python runtime
        //        //InitializePythonRuntime();

        //        // Log the model path
        //        Console.WriteLine($"Model path: {modelPath}");

        //        // Check if the file exists
        //        if (!File.Exists(modelPath))
        //        {
        //            throw new FileNotFoundException($"Model file not found at path: {modelPath}");
        //        }

        //        // Load the model
        //        model = NeuralNetHelper.LoadModel(modelPath);
        //    }
        //    catch (Exception ex)
        //    {
        //        // Log the exception details
        //        Console.WriteLine($"Error initializing model: {ex.Message}");
        //        throw;
        //    }
        //}

        public override SudokuGrid Solve(SudokuGrid s)
        {
            return NeuralNetHelper.SolveSudoku(s, model);
        }

		protected override void InitializePythonComponents()
		{
			//declare your pip packages here
			InstallPipModule("numpy");
			InstallPipModule("tensorflow");
			base.InitializePythonComponents();

			// Load the model
			model = NeuralNetHelper.LoadModel(modelPath);
		}

		private static string GetFullPath(string relativePath)
        {
            return Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, relativePath));
        }

        //private static void InitializePythonRuntime()
        //{
        //    // Set the Python home directory
        //    string pythonHome = @"C:\Users\eliol\anaconda3\envs\sudoku_env";
        //    Environment.SetEnvironmentVariable("PYTHONHOME", pythonHome, EnvironmentVariableTarget.Process);

        //    // Set the Python DLL path
        //    string pythonDll = @"C:\Users\eliol\anaconda3\envs\sudoku_env\python38.dll";
        //    Runtime.PythonDLL = pythonDll;

        //    // Set the Python path to include the site-packages directory
        //    string pythonPath = @"C:\Users\eliol\anaconda3\envs\sudoku_env\Lib;C:\Users\eliol\anaconda3\envs\sudoku_env\DLLs;C:\Users\eliol\anaconda3\envs\sudoku_env\Lib\site-packages";
        //    Environment.SetEnvironmentVariable("PYTHONPATH", pythonPath, EnvironmentVariableTarget.Process);

        //    // Log the Python configuration
        //    Console.WriteLine($"PYTHONHOME: {Environment.GetEnvironmentVariable("PYTHONHOME", EnvironmentVariableTarget.Process)}");
        //    Console.WriteLine($"PYTHONPATH: {Environment.GetEnvironmentVariable("PYTHONPATH", EnvironmentVariableTarget.Process)}");
        //    Console.WriteLine($"Python DLL: {Runtime.PythonDLL}");
			
			     //       // Set Python home and path for PythonEngine
        //    PythonEngine.PythonHome = pythonHome;
        //    PythonEngine.PythonPath = pythonPath;

        //    // Initialize the Python engine
        //    PythonEngine.Initialize();
        //}
    }
	public class NeuralNetHelper
	{

		static NeuralNetHelper()
		{
			//PythonEngine.PythonHome = @"C:\Users\eliol\anaconda3";
			Setup.UseTfKeras();
		}

		public static BaseModel LoadModel(string strpath)
		{
			return BaseModel.LoadModel(strpath);
		}

		public static NDarray GetFeatures(SudokuGrid objSudoku)
		{
			return Normalize(np.array(objSudoku.Cells).reshape(9, 9));
		}

		public static NDarray Normalize(NDarray features)
		{
			return (features / 9) - 0.5;
		}

		public static NDarray DeNormalize(NDarray features)
		{
			return (features + 0.5) * 9;
		}



		public static SudokuGrid SolveSudoku(SudokuGrid s, BaseModel model)
		{
			var features = GetFeatures(s);
			while (true)
			{
				var output = model.Predict(features.reshape(1, 9, 9, 1));
				output = output.squeeze();
                output = output.reshape(9, 9, 9);

                
				var prediction = np.argmax(output, axis: 2).reshape(9, 9) + 1;
				var proba = np.around(np.max(output, axis: new[] { 2 }).reshape(9, 9), 2);

				features = DeNormalize(features);
				var mask = features.@equals(0);
				if (((int)mask.sum()) == 0)
				{
					break;
				}

				var probNew = proba * mask;
				var ind = (int)np.argmax(probNew);
				var (x, y) = (ind / 9, ind % 9);
				var val = prediction[x][y];
				features[x][y] = val;
				features = Normalize(features);

			}

            string sudokuString = ConvertToSudokuString(features);
            Console.WriteLine("Sudoku string: " + sudokuString);

            return SudokuGrid.ReadSudoku(sudokuString);
        }

        private static string ConvertToSudokuString(NDarray features)
        {
            StringBuilder sb = new StringBuilder();
            double[] flatArray = features.GetData<double>();
            double[,] array = new double[9, 9];
            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    array[i, j] = flatArray[i * 9 + j];
                }
            }

            for (int i = 0; i < array.GetLength(0); i++)
            {
                for (int j = 0; j < array.GetLength(1); j++)
                {
                    int value = (int)array[i, j];
                    sb.Append(value == 0 ? '0' : value.ToString()[0]);
                }
            }

            return sb.ToString();
        }
	}
}
