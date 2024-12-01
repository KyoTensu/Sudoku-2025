using Python.Runtime;
using Sudoku.Shared;

namespace Sudoku.ResolutionNeuronne
{
    public class SolveurNeuronne : ISudokuSolver
    {
        public SudokuGrid Solve(SudokuGrid s)
        {
            return s;
        }
    }

    public class SolveurNeuronnePython : PythonSolverBase
    {
        public override Shared.SudokuGrid Solve(Shared.SudokuGrid s)
        {
            InitializePythonComponents();

            using (Py.GIL())
            {
                using (PyModule scope = Py.CreateScope())
                {
					
                    // Ajouter le script de conversion NumPy
                    AddNumpyConverterScript(scope);
					
                    // Convertir la grille de Sudoku en objet Python
                    // Convertissez le tableau .NET en tableau NumPy
					var pyCells = AsNumpyArray(s.Cells, scope);

					// create a Python variable "instance"
					scope.Set("instance", pyCells);


                    // Lire et exécuter le script Python
                    string code = System.IO.File.ReadAllText("SolveurNeuronnePython.py");
                    scope.Exec(code);

                    PyObject result = scope.Get("result");

					Console.WriteLine(result);




					// Convertissez le résultat NumPy en tableau .NET
					var managedResult = AsManagedArray(scope, result);

					return new SudokuGrid() { Cells = managedResult };
                }
            }
        }

        protected override void InitializePythonComponents()
        {
	        //declare your pip packages here
	        InstallPipModule("numpy");
	        InstallPipModule("tensorflow");
			base.InitializePythonComponents();
        }

	}
}