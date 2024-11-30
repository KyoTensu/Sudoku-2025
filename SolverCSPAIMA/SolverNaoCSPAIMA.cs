using Python.Runtime;
using Sudoku.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sudoku.SolverCSPAIMA
{
    public class SolverNaoCSPAIMA : PythonSolverBase
    {
        public override Shared.SudokuGrid Solve(Shared.SudokuGrid s)
        {
            using (Py.GIL())
            {

                using (PyModule scope = Py.CreateScope())
                {
                    // Injecter le script de conversion, ici un script qui pourrait être commun à tous les solveurs
                    AddNumpyConverterScript(scope);

                    // Convertir le tableau .NET en tableau NumPy
                    var pyCells = AsNumpyArray(s.Cells, scope);

                    // Créer une variable Python "instance" pour la grille de Sudoku
                    scope.Set("instance", pyCells);

                    // Charger et exécuter le script Python
                    string code = Resources.CSPAIMA_py;  // Le chemin ou contenu du script Python
                    Console.WriteLine("Exécution du code Python...");
                    scope.Exec(code);

                    // Vérifier si "solved_grid" existe avant de l'extraire
                    if (scope.Contains("solved_grid"))
                    {
                        Console.WriteLine("solved_grid trouvé dans le scope.");
                    }
                    else
                    {
                        Console.WriteLine("solved_grid non trouvé dans le scope.");
                    }

                    // Récupérer le résultat du script Python
                    PyObject result = scope.Get("solved_grid");

                    // Convertir le résultat NumPy en tableau .NET
                    var managedResult = AsManagedArray(scope, result);

                    // Retourner la grille de Sudoku avec les valeurs remplies
                    return new SudokuGrid() { Cells = managedResult };
                }
            }
        }

        protected override void InitializePythonComponents()
        {
            // Déclarer les modules Python nécessaires
            InstallPipModule("numpy");
            InstallPipModule("aima3");
            base.InitializePythonComponents();
        }
    }
}
