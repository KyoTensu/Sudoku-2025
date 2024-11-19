using Sudoku.Shared;

namespace Sudoku.GraphProba;

public class GraphProba : ISudokuSolver
{
    public SudokuGrid Solve(SudokuGrid s)
    {
        SolveSudoku(s);
        return s;
    }

    private bool SolveSudoku(SudokuGrid grid)
    {
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                if (grid.Cells[row][col] == 0)
                {
                    for (int num = 1; num <= 9; num++)
                    {
                        if (IsSafe(grid, row, col, num))
                        {
                            grid.Cells[row][col] = num;

                            if (SolveSudoku(grid))
                            {
                                return true;
                            }

                            grid.Cells[row][col] = 0;
                        }
                    }
                    return false;
                }
            }
        }
        return true;
    }

    private bool IsSafe(SudokuGrid grid, int row, int col, int num)
    {
        for (int x = 0; x < 9; x++)
        {
            if (grid.Cells[row][x] == num || grid.Cells[x][col] == num)
            {
                return false;
            }
        }

        int startRow = row - row % 3, startCol = col - col % 3;
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (grid.Cells[i + startRow][j + startCol] == num)
                {
                    return false;
                }
            }
        }

        return true;
    }
}