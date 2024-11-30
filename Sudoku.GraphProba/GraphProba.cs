using Sudoku.Shared;
using Microsoft.ML.Probabilistic.Algorithms;
using Microsoft.ML.Probabilistic.Distributions;
using Microsoft.ML.Probabilistic.Math;
using Microsoft.ML.Probabilistic.Models;
using Microsoft.ML.Probabilistic.Models.Attributes;
using Range = Microsoft.ML.Probabilistic.Models.Range;


namespace Sudoku.GraphProba;

public class GraphProba : ISudokuSolver
{
	private static RobustSudokuModel robustModel = new RobustSudokuModel();
	public SudokuGrid Solve(SudokuGrid s)
	{
		//var model = new NaiveSudokuModel();
		return robustModel.SolveSudoku(s);
	}
}

public class GraphNaiveProba : ISudokuSolver
{
	private static NaiveSudokuModel naiveModel = new NaiveSudokuModel();
	public SudokuGrid Solve(SudokuGrid s)
	{
		//var model = new NaiveSudokuModel();
		naiveModel.SolveSudoku(s);
		return s;
	}
}

/// <summary>
/// Ce deuxième modèle n'apporte pas encore tout à fait satisfaction mais apporte un réel progrès vis à vis du modèle naïf. J'ai rajouté des commentaires là où de nouveaux tests peuvent être entrepris.
/// le moteur d'inférence n'a plus qu'à compiler le modèle une seule fois, et il résout d'autres Sudokus faciles.
/// Il manque cependant de résoudre les Sudokus plus difficiles. Ceci dit, il trouve la bonne valeur pour une majorité de cellules,
/// et du coup on peut envisager de l'utiliser de façon itérative en ne conservant que les valeurs pour lesquelles les probabilités sont les meilleures et en réinjectant dans CellsPrior.ObservedValue comme dans l'exemple Cyclist d'Infer.Net,
/// plutôt que d'utiliser cellsPosterior.Point ou cellsProbsPosterior[cellIndex].GetMode() comme c'est le cas actuellement ce qui revient à ignorant les probabilités réelles et conserver toutes les valeurs du premier coup)
/// </summary>
public class RobustSudokuModel
{


	public InferenceEngine InferenceEngine;


	private static List<int> CellDomain = Enumerable.Range(1, 9).ToList();
	private static List<int> CellIndices = Enumerable.Range(0, 81).ToList();




	// Cf https://en.wikipedia.org/wiki/Categorical_distribution et https://en.wikipedia.org/wiki/Categorical_distribution#Bayesian_inference_using_conjugate_prior pour le choix des distributions
	// et le chapitre 6 de https://dotnet.github.io/infer/InferNet101.pdf pour l'implémentation dans Infer.Net


	public VariableArray<Dirichlet> CellsPrior;
	public VariableArray<Vector> ProbCells;
	public VariableArray<int> Cells;


	private const double EpsilonProba = 0.00000001;
	private static double FixedValueProba = 1.0 - ((CellDomain.Count - 1) * EpsilonProba);


	public RobustSudokuModel()
	{




		Range valuesRange = new Range(CellDomain.Count).Named("valuesRange");
		Range cellsRange = new Range(CellIndices.Count).Named("cellsRange");




		CellsPrior = Variable.Array<Dirichlet>(cellsRange).Named("CellsPrior");
		ProbCells = Variable.Array<Vector>(cellsRange).Named("ProbCells");
		ProbCells[cellsRange] = Variable<Vector>.Random(CellsPrior[cellsRange]);
		ProbCells.SetValueRange(valuesRange);




		// Initialisation des distribution a priori de façon uniforme (les valeurs sont équiprobables pour chaque cellule)


		Dirichlet[] dirUnifArray =
			Enumerable.Repeat(Dirichlet.Uniform(CellDomain.Count), CellIndices.Count).ToArray();
		CellsPrior.ObservedValue = dirUnifArray;


		Cells = Variable.Array<int>(cellsRange);
		Cells[cellsRange] = Variable.Discrete(ProbCells[cellsRange]);






		//Ajout des contraintes de Sudoku (all diff pour tous les voisinages)
		//Loic: cette boucle applique une contrainte de non égalité pour les cellules voisines de ligne, colonne et groupe
		foreach (var cellIndex in CellIndices)
		{
			int row = cellIndex / 9;
			int col = cellIndex % 9;
			foreach (var neighbourCellIndex in SudokuGrid.CellNeighbours[row][col])
			{
				int neighbourIndex = neighbourCellIndex.row * 9 + neighbourCellIndex.column;
				if (neighbourIndex > cellIndex)
				{
					Variable.ConstrainFalse(Cells[cellIndex] == Cells[neighbourIndex]);
				}
			}
		}





		//Todo: tester d'autres algo et paramétrages associés


		IAlgorithm algo = new ExpectationPropagation();
		//IAlgorithm algo = new GibbsSampling();
		//IAlgorithm algo = new VariationalMessagePassing();
		//IAlgorithm algo = new MaxProductBeliefPropagation();
		//les algos ont ete teste cependant la difference lors du lancement du projet n'est pas facilement visible
		algo.DefaultNumberOfIterations = 50;
		//algo.DefaultNumberOfIterations = 200;




		InferenceEngine = new InferenceEngine(algo);


		//InferenceEngine.OptimiseForVariables = new IVariable[] { Cells };


	}




	public virtual SudokuGrid SolveSudoku(SudokuGrid s)
	{
		Dirichlet[] dirArray = Enumerable.Repeat(Dirichlet.Uniform(CellDomain.Count), CellIndices.Count).ToArray();


		//On affecte les valeurs fournies par le masque à résoudre en affectant les distributions de probabilités initiales
		foreach (var cellIndex in CellIndices)
		{
			int row = cellIndex / 9;
			int col = cellIndex % 9;
			if (s.Cells[row, col] > 0)
			{


				//Vector v = Vector.Zero(CellDomain.Count);
				//v[s.Cellules[cellIndex] - 1] = 1.0;




				//Todo: Alternative: le fait de mettre une proba non nulle permet d'éviter l'erreur "zero probability" du Sudoku Easy-n°2, mais le Easy#3 n'est plus résolu
				//tentative de changer la probabilite pour solver le sudoku 3 infructueuse
				Vector v = Vector.Constant(CellDomain.Count, EpsilonProba);
				v[s.Cells[row, col] - 1] = FixedValueProba;


				dirArray[cellIndex] = Dirichlet.PointMass(v);
			}
		}


		CellsPrior.ObservedValue = dirArray;

		DoInference(dirArray, s);
		return s;
	}

	protected virtual void DoInference(Dirichlet[] dirArray, SudokuGrid s)
	{



		// Todo: tester en inférant sur d'autres variables aléatoire,
		// et/ou en ayant une approche itérative: On conserve uniquement les cellules dont les valeurs ont les meilleures probabilités 
		//et on réinjecte ces valeurs dans CellsPrior comme c'est également fait dans le projet neural nets. 
		//


		// IFunction draw_categorical(n)// where n is the number of samples to draw from the categorical distribution
		// {
		//
		// r = 1


		/* for (i=0; i<9; i++)
			for (j=0; j<9; j++)
				for (k=0; k<9; k++)
					ps[i][j][k] = probs[i][j][k].p; */




		//DistributionRefArray<Discrete, int> cellsPosterior = (DistributionRefArray<Discrete, int>)InferenceEngine.Infer(Cells);
		//var cellValues = cellsPosterior.Point.Select(i => i + 1).ToList();


		//Autre possibilité de variable d'inférence (bis)
		Dirichlet[] cellsProbsPosterior = InferenceEngine.Infer<Dirichlet[]>(ProbCells);


		foreach (var cellIndex in CellIndices)
		{
			var row = cellIndex / 9;
			int col = cellIndex % 9;
			if (s.Cells[row, col] == 0)
			{
				//s.Cellules[cellIndex] = cellValues[cellIndex];

				var mode = cellsProbsPosterior[cellIndex].GetMode();
				var value = mode.IndexOf(mode.Max()) + 1;
				s.Cells[row, col] = value;
			}
		}

	}


}






/// <summary>
/// Ce premier modèle est très faible: d'une part, il ne résout que quelques Sudokus faciles, d'autre part, le modèle est recompilé à chaque fois, ce qui prend beaucoup de temps
/// </summary>
public class NaiveSudokuModel
{


	private static List<int> CellDomain = Enumerable.Range(1, 9).ToList();
	private static List<int> CellIndices = Enumerable.Range(0, 81).ToList();




	public virtual void SolveSudoku(SudokuGrid s)
	{


		var algo = new ExpectationPropagation();
		var engine = new InferenceEngine(algo);


		//Implémentation naïve: une variable aléatoire entière par cellule
		var cells = new List<Variable<int>>(CellIndices.Count);


		foreach (var cellIndex in CellIndices)
		{
			//On initialise le vecteur de probabilités de façon uniforme pour les chiffres de 1 à 9
			var baseProbas = Enumerable.Repeat(1.0, CellDomain.Count).ToList();
			//Création et ajout de la variable aléatoire
			var cell = Variable.Discrete(baseProbas.ToArray());
			cells.Add(cell);


		}


		//Ajout des contraintes de Sudoku (all diff pour tous les voisinages)
		foreach (var cellIndex in CellIndices)
		{
			foreach (var neighbourCellIndex in SudokuGrid.CellNeighbours[cellIndex / 9][cellIndex % 9])
			{
				if ((neighbourCellIndex.row * 9 + neighbourCellIndex.column) > cellIndex)
				{
					Variable.ConstrainFalse(cells[cellIndex] == cells[(neighbourCellIndex.row * 9 + neighbourCellIndex.column)]);
				}
			}
		}



		//On affecte les valeurs fournies par le masque à résoudre comme variables observées
		foreach (var cellIndex in CellIndices)
		{
			int row = cellIndex / 9;
			int col = cellIndex % 9;
			if (s.Cells[row, col] > 0)
			{
				cells[cellIndex].ObservedValue = s.Cells[Math.DivRem(cellIndex, 9, out int columnv), columnv] - 1;
			}
		}


		foreach (var cellIndex in CellIndices)
		{
			int row = cellIndex / 9;
			int col = cellIndex % 9;
			if (s.Cells[row, col] == 0)
			{
				var result = (Discrete)engine.Infer(cells[cellIndex]);
				s.Cells[row, col] = result.Point + 1;
			}
		}


	}






}
