using Sudoku.Shared;
using Microsoft.ML.Probabilistic.Algorithms;
using Microsoft.ML.Probabilistic.Distributions;
using Microsoft.ML.Probabilistic.Math;
using Microsoft.ML.Probabilistic.Models;
using Microsoft.ML.Probabilistic.Models.Attributes;
using Range = Microsoft.ML.Probabilistic.Models.Range;

namespace Sudoku.GraphProba;

public class EnhancedGraphProba : ISudokuSolver
{
    private static IterativeSudokuModel solverModel = new IterativeSudokuModel();

    public SudokuGrid Solve(SudokuGrid s)
    {
        return solverModel.SolveSudoku(s);
    }
}

public class IterativeSudokuModel
{
    public InferenceEngine InferenceEngine;
    private static List<int> CellDomain = Enumerable.Range(1, 9).ToList();
    private static List<int> CellIndices = Enumerable.Range(0, 81).ToList();

    public VariableArray<Dirichlet> CellsPrior;
    public VariableArray<Vector> ProbCells;
    public VariableArray<int> Cells;

    private const double EpsilonProba = 0.00000001;
    private static double FixedValueProba = 1.0 - ((CellDomain.Count - 1) * EpsilonProba);

    public IterativeSudokuModel()
    {
        // Initialize neighbor relationships
        InitializeNeighbours();

        Range valuesRange = new Range(CellDomain.Count).Named("valuesRange");
        Range cellsRange = new Range(CellIndices.Count).Named("cellsRange");

        CellsPrior = Variable.Array<Dirichlet>(cellsRange).Named("CellsPrior");
        ProbCells = Variable.Array<Vector>(cellsRange).Named("ProbCells");
        ProbCells[cellsRange] = Variable<Vector>.Random(CellsPrior[cellsRange]);
        ProbCells.SetValueRange(valuesRange);

        Dirichlet[] dirUnifArray =
            Enumerable.Repeat(Dirichlet.Uniform(CellDomain.Count), CellIndices.Count).ToArray();
        CellsPrior.ObservedValue = dirUnifArray;

        Cells = Variable.Array<int>(cellsRange);
        Cells[cellsRange] = Variable.Discrete(ProbCells[cellsRange]);

        // Apply Sudoku constraints
        foreach (var cellIndex in CellIndices)
        {
            if (cellIndex >= 0 && cellIndex < SudokuGrid.AllNeighbours.Length)
            {
                foreach (var neighbourCellIndex in SudokuGrid.AllNeighbours[cellIndex])
                {
                    int neighbourIndex = neighbourCellIndex.row * 9 + neighbourCellIndex.column;
                    if (neighbourIndex > cellIndex)
                    {
                        Variable.ConstrainFalse(Cells[cellIndex] == Cells[neighbourIndex]);
                    }
                }
            }
        }

        // Set inference algorithm
        IAlgorithm algo = new ExpectationPropagation();
        algo.DefaultNumberOfIterations = 50;
        InferenceEngine = new InferenceEngine(algo);
    }

    public SudokuGrid SolveSudoku(SudokuGrid s)
    {
        Dirichlet[] dirArray = Enumerable.Repeat(Dirichlet.Uniform(CellDomain.Count), CellIndices.Count).ToArray();

        foreach (var cellIndex in CellIndices)
        {
            if (s.Cells[Math.DivRem(cellIndex, 9, out int column), column] > 0)
            {
                Vector v = Vector.Constant(CellDomain.Count, EpsilonProba);
                v[s.Cells[Math.DivRem(cellIndex, 9, out int columnv), columnv] - 1] = FixedValueProba;
                dirArray[cellIndex] = Dirichlet.PointMass(v);
            }
        }

        CellsPrior.ObservedValue = dirArray;

        Dirichlet[] cellsProbsPosterior = InferenceEngine.Infer<Dirichlet[]>(ProbCells);

        foreach (var cellIndex in CellIndices)
        {
            if (s.Cells[Math.DivRem(cellIndex, 9, out int column), column] == 0)
            {
                var mode = cellsProbsPosterior[cellIndex].GetMode();
                var value = mode.IndexOf(mode.Max()) + 1;
                s.Cells[Math.DivRem(cellIndex, 9, out int columnv), columnv] = value;
            }
        }

        return s;
    }

    private static void InitializeNeighbours()
    {
        if (SudokuGrid.AllNeighbours == null || SudokuGrid.AllNeighbours.Length != 81)
        {
            SudokuGrid.AllNeighbours = new (int row, int column)[81][];

            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    int cellIndex = row * 9 + col;
                    List<(int row, int column)> neighbors = new List<(int row, int column)>();

                    // Add all cells in the same row
                    for (int c = 0; c < 9; c++)
                        if (c != col) neighbors.Add((row, c));

                    // Add all cells in the same column
                    for (int r = 0; r < 9; r++)
                        if (r != row) neighbors.Add((r, col));

                    // Add all cells in the same subgrid
                    int subgridRowStart = (row / 3) * 3;
                    int subgridColStart = (col / 3) * 3;
                    for (int r = subgridRowStart; r < subgridRowStart + 3; r++)
                    {
                        for (int c = subgridColStart; c < subgridColStart + 3; c++)
                        {
                            if (r != row || c != col) neighbors.Add((r, c));
                        }
                    }

                    SudokuGrid.AllNeighbours[cellIndex] = neighbors.ToArray();
                }
            }
        }
    }
}
