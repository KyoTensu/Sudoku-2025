using Sudoku.Shared;

namespace Sudoku.GraphProba;

public class GraphProba : ISudokuSolver
{
    public SudokuGrid Solve(SudokuGrid s)
    {
        // Step 1: Initialize a probability graph
        var probabilities = InitializeProbabilities(s);

        // Step 2: Solve using probability updates
        while (!IsSolved(s))
        {
            // Find the most probable value for each cell
            UpdateProbabilities(s, probabilities);

            // Fill cells with certain probabilities
            FillCertainValues(s, probabilities);
        }

        return s;
    }

    // Initializes probabilities for each cell (1-9 for empty cells)
    private Dictionary<(int row, int col), List<int>> InitializeProbabilities(SudokuGrid s)
    {
        var probabilities = new Dictionary<(int, int), List<int>>();

        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                if (s.Cells[row][col] == 0)
                {
                    // All numbers 1-9 are initially possible
                    probabilities[(row, col)] = Enumerable.Range(1, 9).ToList();
                }
            }
        }

        return probabilities;
    }

    // Updates probabilities based on current grid state
    private void UpdateProbabilities(SudokuGrid s, Dictionary<(int, int), List<int>> probabilities)
    {
        foreach (var cell in probabilities.Keys.ToList())
        {
            var (row, col) = cell;
            var possibleValues = probabilities[cell];

            // Remove numbers already in the row, column, or 3x3 box
            possibleValues.RemoveAll(n => IsInRow(s, row, n) || IsInCol(s, col, n) || IsInBox(s, row, col, n));

            // Update probabilities
            probabilities[cell] = possibleValues;
        }
    }

    // Fills cells that have only one possible value
    private void FillCertainValues(SudokuGrid s, Dictionary<(int, int), List<int>> probabilities)
    {
        foreach (var cell in probabilities.Keys.ToList())
        {
            var (row, col) = cell;
            var possibleValues = probabilities[cell];

            if (possibleValues.Count == 1)
            {
                // Assign the value to the grid
                s.Cells[row][col] = possibleValues[0];

                // Remove the cell from probabilities
                probabilities.Remove(cell);
            }
        }
    }

    // Checks if the Sudoku grid is solved
    private bool IsSolved(SudokuGrid s)
    {
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                if (s.Cells[row][col] == 0)
                {
                    return false;
                }
            }
        }
        return true;
    }

    // Helper functions to check the presence of a number in row, column, or box
    private bool IsInRow(SudokuGrid s, int row, int num)
    {
        return s.Cells[row].Contains(num);
    }

    private bool IsInCol(SudokuGrid s, int col, int num)
    {
        return s.Cells.Any(row => row[col] == num);
    }

    private bool IsInBox(SudokuGrid s, int row, int col, int num)
    {
        int boxRowStart = (row / 3) * 3;
        int boxColStart = (col / 3) * 3;

        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (s.Cells[boxRowStart + i][boxColStart + j] == num)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
