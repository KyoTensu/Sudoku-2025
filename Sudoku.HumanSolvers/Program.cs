using Sudoku.Shared;
namespace Sudoku.HumanSolvers
{
public class HumanSolvers : ISudokuSolver
{
		/// <summary>
		/// Solves the given Sudoku grid using human techniques.
		/// </summary>
		/// <param name="s">The Sudoku grid to be solved.</param>
		/// <returns>
		/// The solved Sudoku grid.
		/// </returns>
		public SudokuGrid Solve(SudokuGrid s)
		{
			bool progress;
			do
			{
				progress = ApplyTechniques(s);

				// If no techniques work but the puzzle isn't solved, it’s unsolvable with logic
				if (!progress && !IsSolved(s))
				{
					throw new InvalidOperationException("This puzzle requires guessing or advanced techniques not supported by this solver.");
				}

			} while (!IsSolved(s));

			return s;
		}

		/// <summary>
		/// Applies human solving techniques to the grid.
		/// </summary>
		/// <param name="s">The Sudoku grid.</param>
		/// <returns>True if progress was made, false otherwise.</returns>
		private bool ApplyTechniques(SudokuGrid s)
		{
			// Try each technique in order of simplicity
			if (ApplyNakedSingles(s)) return true;
			if (ApplyHiddenSingles(s)) return true;
			if (ApplyNakedPairs(s)) return true;
			if (ApplyHiddenPairs(s)) return true;
			if (ApplyPointingPairs(s)) return true;
			if (ApplyBoxLineReduction(s)) return true;
			if (ApplyLockedCandidates(s)) return true;
			if (ApplyYWing(s)) return true;
            if (ApplyXYZWing(s)) return true;
			
			// Add more techniques here as needed (e.g., Naked Pairs, Hidden Pairs, etc.)

			return false; // No progress made
		}

		/// <summary>
		/// Checks if the grid is solved.
		/// </summary>
		private bool IsSolved(SudokuGrid s)
		{
			for (int row = 0; row < 9; row++)
			{
				for (int col = 0; col < 9; col++)
				{
					if (s.Cells[row, col] == 0) return false; // Unsigned cell means it's unsolved
				}
			}
			return true;
		}

		/// <summary>
		/// Identifies and solves cells using the Naked Singles technique.
		/// </summary>
		/// <param name="s">The Sudoku grid.</param>
		/// <returns>True if any cell was solved, false otherwise.</returns>
		private bool ApplyNakedSingles(SudokuGrid s)
		{
			bool progress = false;

			for (int row = 0; row < 9; row++)
			{
				for (int col = 0; col < 9; col++)
				{
					if (s.Cells[row, col] == 0) // Only consider empty cells
					{
						var candidates = GetCandidates(s, row, col);
						if (candidates.Count() == 1)
						{
							// Naked single found
							s.Cells[row, col] = candidates.First();
							progress = true;
						}
					}
				}
			}

			return progress;
		}

		/// <summary>
		/// Identifies and solves cells using the Hidden Singles technique.
		/// </summary>
		/// <param name="s">The Sudoku grid.</param>
		/// <returns>True if any cell was solved, false otherwise.</returns>
		private bool ApplyHiddenSingles(SudokuGrid s)
		{
			bool progress = false;

			for (int region = 0; region < 27; region++) // 9 rows + 9 columns + 9 blocks
			{
				var emptyCells = GetCellsInRegion(s, region)
					.Where(cell => s.Cells[cell.row, cell.col] == 0)
					.ToList();

				var candidatesMap = new Dictionary<int, List<(int row, int col)>>();

				// Map candidates to their positions in the region
				foreach (var (row, col) in emptyCells)
				{
					foreach (var candidate in GetCandidates(s, row, col))
					{
						if (!candidatesMap.ContainsKey(candidate))
						{
							candidatesMap[candidate] = new List<(int, int)>();
						}
						candidatesMap[candidate].Add((row, col));
					}
				}

				// Look for Hidden Singles (candidates appearing exactly once in the region)
				foreach (var kvp in candidatesMap)
				{
					if (kvp.Value.Count == 1)
					{
						var (row, col) = kvp.Value.First();
						s.Cells[row, col] = kvp.Key;
						progress = true;
					}
				}
			}

			return progress;
		}
		
		/// <summary>
/// Identifies and solves cells using the Naked Pairs technique.
/// </summary>
/// <param name="s">The Sudoku grid.</param>
/// <returns>True if any cell was solved, false otherwise.</returns>
private bool ApplyNakedPairs(SudokuGrid s)
{
    bool progress = false;

    // Check rows, columns, and blocks
    for (int region = 0; region < 27; region++) // 9 rows + 9 columns + 9 blocks
    {
        var emptyCells = GetCellsInRegion(s, region)
            .Where(cell => s.Cells[cell.row, cell.col] == 0)
            .ToList();

        var candidatesMap = new Dictionary<string, List<(int row, int col)>>();

        // Map pairs of candidates to their positions in the region
        foreach (var (row, col) in emptyCells)
        {
            var candidates = GetCandidates(s, row, col).OrderBy(c => c).ToList();
            if (candidates.Count == 2)
            {
                var candidatePair = string.Join(",", candidates);
                if (!candidatesMap.ContainsKey(candidatePair))
                {
                    candidatesMap[candidatePair] = new List<(int, int)>();
                }
                candidatesMap[candidatePair].Add((row, col));
            }
        }

        // Look for Naked Pairs
        foreach (var kvp in candidatesMap)
        {
            if (kvp.Value.Count == 2)
            {
                // Eliminate this pair from other cells in the region
                var pair = kvp.Value;
                foreach (var (row, col) in GetCellsInRegion(s, region).Where(c => !pair.Contains(c)))
                {
                    var currentCandidates = GetCandidates(s, row, col).ToList();
                    foreach (var candidate in currentCandidates)
                    {
                        if (kvp.Key.Contains(candidate.ToString()))
                        {
                            // Remove candidate from the list of candidates for this cell
                            s.Cells[row, col] = 0;
                            progress = true;
                        }
                    }
                }
            }
        }
    }

    return progress;
}

		/// <summary>
/// Identifies and solves cells using the Pointing Pairs technique.
/// </summary>
/// <param name="s">The Sudoku grid.</param>
/// <returns>True if any cell was solved, false otherwise.</returns>
private bool ApplyPointingPairs(SudokuGrid s)
{
    bool progress = false;

    // Check all 9 blocks
    for (int block = 0; block < 9; block++)
    {
        var blockCells = GetCellsInRegion(s, block);
        var candidateMap = new Dictionary<int, List<(int row, int col)>>();

        // Collect all candidates in the block
        foreach (var (row, col) in blockCells)
        {
            if (s.Cells[row, col] == 0) // Empty cell
            {
                var candidates = GetCandidates(s, row, col);
                foreach (var candidate in candidates)
                {
                    if (!candidateMap.ContainsKey(candidate))
                    {
                        candidateMap[candidate] = new List<(int, int)>();
                    }
                    candidateMap[candidate].Add((row, col));
                }
            }
        }

        // For each candidate, check if it appears in only one row or column of the block
        foreach (var kvp in candidateMap)
        {
            var candidate = kvp.Key;
            var positions = kvp.Value;

            // If the candidate appears only once in a row or column, eliminate it from that row/column outside the block
            if (positions.Select(pos => pos.row).Distinct().Count() == 1) // Appears in only one row
            {
                var row = positions.First().row;
                for (int col = 0; col < 9; col++)
                {
                    if (s.Cells[row, col] == 0 && GetCandidates(s, row, col).Contains(candidate))
                    {
                        s.Cells[row, col] = 0;
                        progress = true;
                    }
                }
            }

            if (positions.Select(pos => pos.col).Distinct().Count() == 1) // Appears in only one column
            {
                var col = positions.First().col;
                for (int row = 0; row < 9; row++)
                {
                    if (s.Cells[row, col] == 0 && GetCandidates(s, row, col).Contains(candidate))
                    {
                        s.Cells[row, col] = 0;
                        progress = true;
                    }
                }
            }
        }
    }

    return progress;
}

		
		/// <summary>
/// Identifies and solves cells using the Hidden Pairs technique.
/// </summary>
/// <param name="s">The Sudoku grid.</param>
/// <returns>True if any cell was solved, false otherwise.</returns>
private bool ApplyHiddenPairs(SudokuGrid s)
{
    bool progress = false;

    // Check rows, columns, and blocks
    for (int region = 0; region < 27; region++) // 9 rows + 9 columns + 9 blocks
    {
        var emptyCells = GetCellsInRegion(s, region)
            .Where(cell => s.Cells[cell.row, cell.col] == 0)
            .ToList();

        var candidatesMap = new Dictionary<string, List<(int row, int col)>>();

        // Map pairs of candidates to their positions in the region
        foreach (var (row, col) in emptyCells)
        {
            var candidates = GetCandidates(s, row, col).OrderBy(c => c).ToList();
            foreach (var candidate in candidates)
            {
                if (!candidatesMap.ContainsKey(candidate.ToString()))
                {
                    candidatesMap[candidate.ToString()] = new List<(int, int)>();
                }
                candidatesMap[candidate.ToString()].Add((row, col));
            }
        }

        // Look for Hidden Pairs
        foreach (var kvp in candidatesMap)
        {
            if (kvp.Value.Count == 2)
            {
                // Eliminate this pair from other cells in the region
                var pair = kvp.Value;
                foreach (var (row, col) in GetCellsInRegion(s, region).Where(c => !pair.Contains(c)))
                {
                    var currentCandidates = GetCandidates(s, row, col).ToList();
                    foreach (var candidate in currentCandidates)
                    {
                        if (kvp.Key.Contains(candidate.ToString()))
                        {
                            // Remove candidate from the list of candidates for this cell
                            s.Cells[row, col] = 0;
                            progress = true;
                        }
                    }
                }
            }
        }
    }

    return progress;
}

		/// <summary>
/// Identifies and solves cells using the Box-Line Reduction technique.
/// </summary>
/// <param name="s">The Sudoku grid.</param>
/// <returns>True if any cell was solved, false otherwise.</returns>
private bool ApplyBoxLineReduction(SudokuGrid s)
{
    bool progress = false;

    // Check all 9 blocks
    for (int block = 0; block < 9; block++)
    {
        var blockCells = GetCellsInRegion(s, block).ToList();
        
        // Collect candidates for each row and column in the block
        var rowCandidates = new Dictionary<int, List<int>>();
        var colCandidates = new Dictionary<int, List<int>>();

        foreach (var (row, col) in blockCells)
        {
            if (s.Cells[row, col] == 0) // Only consider empty cells
            {
                var candidates = GetCandidates(s, row, col);
                foreach (var candidate in candidates)
                {
                    if (!rowCandidates.ContainsKey(row))
                    {
                        rowCandidates[row] = new List<int>();
                    }
                    rowCandidates[row].Add(candidate);

                    if (!colCandidates.ContainsKey(col))
                    {
                        colCandidates[col] = new List<int>();
                    }
                    colCandidates[col].Add(candidate);
                }
            }
        }

        // Apply Box-Line Reduction for rows
        foreach (var kvp in rowCandidates)
        {
            var row = kvp.Key;
            var rowCandidatesList = kvp.Value.Distinct().ToList();

            // If a candidate appears only in this row within the block, eliminate it from the same row outside the block
            foreach (var candidate in rowCandidatesList)
            {
                if (blockCells.Count(cell => cell.row == row && GetCandidates(s, cell.row, cell.col).Contains(candidate)) == 1)
                {
                    // Eliminate candidate from the row outside the block
                    for (int col = 0; col < 9; col++)
                    {
                        if (col / 3 != block % 3 && s.Cells[row, col] == 0 && GetCandidates(s, row, col).Contains(candidate))
                        {
                            s.Cells[row, col] = 0;
                            progress = true;
                        }
                    }
                }
            }
        }

        // Apply Box-Line Reduction for columns
        foreach (var kvp in colCandidates)
        {
            var col = kvp.Key;
            var colCandidatesList = kvp.Value.Distinct().ToList();

            // If a candidate appears only in this column within the block, eliminate it from the same column outside the block
            foreach (var candidate in colCandidatesList)
            {
                if (blockCells.Count(cell => cell.col == col && GetCandidates(s, cell.row, cell.col).Contains(candidate)) == 1)
                {
                    // Eliminate candidate from the column outside the block
                    for (int row = 0; row < 9; row++)
                    {
                        if (row / 3 != block / 3 && s.Cells[row, col] == 0 && GetCandidates(s, row, col).Contains(candidate))
                        {
                            s.Cells[row, col] = 0;
                            progress = true;
                        }
                    }
                }
            }
        }
    }

    return progress;
}
		/// <summary>
/// Identifies and solves cells using the Locked Candidates technique.
/// </summary>
/// <param name="s">The Sudoku grid.</param>
/// <returns>True if any cell was solved, false otherwise.</returns>
private bool ApplyLockedCandidates(SudokuGrid s)
{
    bool progress = false;

    // Check all 9 boxes (3x3 blocks)
    for (int block = 0; block < 9; block++)
    {
        var blockCells = GetCellsInRegion(s, block).ToList();

        // Collect candidates for each row and column in the block
        var rowCandidates = new Dictionary<int, List<int>>();
        var colCandidates = new Dictionary<int, List<int>>();

        foreach (var (row, col) in blockCells)
        {
            if (s.Cells[row, col] == 0) // Only consider empty cells
            {
                var candidates = GetCandidates(s, row, col);
                foreach (var candidate in candidates)
                {
                    if (!rowCandidates.ContainsKey(row))
                    {
                        rowCandidates[row] = new List<int>();
                    }
                    rowCandidates[row].Add(candidate);

                    if (!colCandidates.ContainsKey(col))
                    {
                        colCandidates[col] = new List<int>();
                    }
                    colCandidates[col].Add(candidate);
                }
            }
        }

        // Apply Locked Candidates for rows
        foreach (var kvp in rowCandidates)
        {
            var row = kvp.Key;
            var rowCandidatesList = kvp.Value.Distinct().ToList();

            // If a candidate appears only in this row within the block, eliminate it from other rows in the same block
            foreach (var candidate in rowCandidatesList)
            {
                var candidateInRow = blockCells.Count(cell => cell.row == row && GetCandidates(s, cell.row, cell.col).Contains(candidate));
                if (candidateInRow == 1)
                {
                    // Eliminate this candidate from the other rows in the block
                    foreach (var (r, c) in blockCells.Where(cell => cell.row != row))
                    {
                        if (GetCandidates(s, r, c).Contains(candidate))
                        {
                            s.Cells[r, c] = 0;
                            progress = true;
                        }
                    }
                }
            }
        }

        // Apply Locked Candidates for columns
        foreach (var kvp in colCandidates)
        {
            var col = kvp.Key;
            var colCandidatesList = kvp.Value.Distinct().ToList();

            // If a candidate appears only in this column within the block, eliminate it from other columns in the same block
            foreach (var candidate in colCandidatesList)
            {
                var candidateInCol = blockCells.Count(cell => cell.col == col && GetCandidates(s, cell.row, cell.col).Contains(candidate));
                if (candidateInCol == 1)
                {
                    // Eliminate this candidate from the other columns in the block
                    foreach (var (r, c) in blockCells.Where(cell => cell.col != col))
                    {
                        if (GetCandidates(s, r, c).Contains(candidate))
                        {
                            s.Cells[r, c] = 0;
                            progress = true;
                        }
                    }
                }
            }
        }
    }

    return progress;
}

		/// <summary>
/// Identifies and solves cells using the Y-Wing technique.
/// </summary>
/// <param name="s">The Sudoku grid.</param>
/// <returns>True if any cell was solved, false otherwise.</returns>
private bool ApplyYWing(SudokuGrid s)
{
    bool progress = false;

    // Find all possible Y-Wing patterns in the grid
    for (int row1 = 0; row1 < 9; row1++)
    {
        for (int col1 = 0; col1 < 9; col1++)
        {
            if (s.Cells[row1, col1] != 0)
                continue; // Skip already filled cells

            var candidates1 = GetCandidates(s, row1, col1);
            if (candidates1.Count() != 2) 
                continue; // Y-Wing needs two candidates in the first cell

            foreach (var candidate in candidates1)
            {
                // Find potential Y-Wing patterns
                var yWingCells = FindYWingCells(s, row1, col1, candidate);
                foreach (var (row2, col2) in yWingCells)
                {
                    var candidates2 = GetCandidates(s, row2, col2);
                    if (candidates2.Contains(candidate))
                    {
                        // Now, check for the third cell forming the Y-wing
                        var yWingThirdCell = FindThirdYWingCell(s, row1, col1, row2, col2, candidate);
                        if (yWingThirdCell != null)
                        {
                            var (row3, col3) = yWingThirdCell.Value;
                            var candidates3 = GetCandidates(s, row3, col3);

                            // If this is a valid Y-Wing pattern, eliminate the candidate from other cells in its row/column
                            if (candidates3.Contains(candidate))
                            {
                                // Eliminate the candidate from other cells in the affected row and column
                                progress |= EliminateYWingCandidate(s, candidate, row1, col1, row2, col2, row3, col3);
                            }
                        }
                    }
                }
            }
        }
    }

    return progress;
}
		private bool ApplyXYZWing(SudokuGrid s)
{
    bool progress = false;

    // Iterate over all possible XYZ-Wing combinations.
    for (int region = 0; region < 27; region++) // 9 rows + 9 columns + 9 blocks
    {
        var emptyCells = GetCellsInRegion(s, region)
            .Where(cell => s.Cells[cell.row, cell.col] == 0)  // Empty cells
            .ToList();

        // Map possible candidates for each empty cell in this region
        var candidateMap = new Dictionary<(int row, int col), List<int>>();

        foreach (var (row, col) in emptyCells)
        {
            candidateMap[(row, col)] = GetCandidates(s, row, col).ToList();
                
            
        }

        // Look for XYZ-Wing patterns in the region
        foreach (var x in candidateMap)
        {
            foreach (var y in candidateMap)
            {
                foreach (var z in candidateMap)
                {
                    if (x.Key == y.Key || y.Key == z.Key || x.Key == z.Key) continue;

                    // Check if X, Y, Z form a valid XYZ-Wing (X shares candidate with Y, Y shares with Z)
                    var commonCandidateXY = x.Value.Intersect(y.Value).FirstOrDefault();
                    var commonCandidateYZ = y.Value.Intersect(z.Value).FirstOrDefault();

                    // If a valid XYZ-Wing is found (common candidates between XY and YZ)
                    if (commonCandidateXY != 0 && commonCandidateYZ != 0 && commonCandidateXY == commonCandidateYZ)
                    {
                        int commonCandidate = commonCandidateXY;

                        // Eliminate the common candidate from other cells in the same row, column, or block
                        var cellsToEliminate = GetCellsInRegion(s, region)
                            .Where(cell => s.Cells[cell.row, cell.col] == 0 && 
                                           (cell.row == x.Key.row || cell.col == x.Key.col || 
                                            cell.row == z.Key.row || cell.col == z.Key.col))
                            .ToList();

                        foreach (var (row, col) in cellsToEliminate)
                        {
                            if (candidateMap[(row, col)].Contains(commonCandidate))
                            {
                                candidateMap[(row, col)].Remove(commonCandidate);
                                s.Cells[row, col] = 0; // Eliminate this candidate
                                progress = true;
                            }
                        }
                    }
                }
            }
        }
    }

    return progress;
}


/// <summary>
/// Finds possible Y-Wing cells (i.e., cells that share a candidate value).
/// </summary>
private List<(int row, int col)> FindYWingCells(SudokuGrid s, int row1, int col1, int candidate)
{
    List<(int row, int col)> yWingCells = new List<(int row, int col)>();

    for (int row2 = 0; row2 < 9; row2++)
    {
        for (int col2 = 0; col2 < 9; col2++)
        {
            if (s.Cells[row2, col2] != 0 || (row1 == row2 && col1 == col2))
                continue; // Skip filled cells and the first cell itself

            var candidates2 = GetCandidates(s, row2, col2);
            if (candidates2.Contains(candidate) && candidates2.Count() == 2)
            {
                yWingCells.Add((row2, col2));
            }
        }
    }

    return yWingCells;
}

/// <summary>
/// Finds the third cell for a Y-Wing pattern given two cells and a common candidate.
/// </summary>
private (int row, int col)? FindThirdYWingCell(SudokuGrid s, int row1, int col1, int row2, int col2, int candidate)
{
    var commonCandidates1 = GetCandidates(s, row1, col1).Where(c => GetCandidates(s, row2, col2).Contains(c)).ToList();

    if (commonCandidates1.Count != 1) 
        return null; // The first and second cells should have one common candidate

    int thirdCandidate = commonCandidates1.First();

    // Look for the third cell where the candidate can be eliminated from
    for (int row3 = 0; row3 < 9; row3++)
    {
        for (int col3 = 0; col3 < 9; col3++)
        {
            if (s.Cells[row3, col3] != 0 || (row1 == row3 && col1 == col3) || (row2 == row3 && col2 == col3))
                continue;

            var candidates3 = GetCandidates(s, row3, col3);
            if (candidates3.Contains(thirdCandidate))
            {
                return (row3, col3);
            }
        }
    }

    return null;
}

/// <summary>
/// Eliminates the candidate from other cells in the affected row and column based on the Y-Wing pattern.
/// </summary>
private bool EliminateYWingCandidate(SudokuGrid s, int candidate, int row1, int col1, int row2, int col2, int row3, int col3)
{
    bool progress = false;

    // Eliminate the candidate from other cells in the rows and columns of the Y-Wing
    for (int r = 0; r < 9; r++)
    {
        for (int c = 0; c < 9; c++)
        {
            if ((r == row1 || r == row2 || r == row3) && (c == col1 || c == col2 || c == col3) && s.Cells[r, c] == 0)
            {
                var candidates = GetCandidates(s, r, c);
                if (candidates.Contains(candidate))
                {
                    s.Cells[r, c] = 0;
                    progress = true;
                }
            }
        }
    }

    return progress;
}


		

private readonly int[] candidatesArray = new int[9];

private int[] GetCandidates(SudokuGrid s, int row, int col)
{
    int index = 0;

    for (int val = 1; val <= 9; val++)
    {
        if (IsValid(s, row, col, val))
        {
            candidatesArray[index++] = val;
        }
    }

    return candidatesArray.Take(index).ToArray();
}
        
        

		/// <summary>
		/// Checks if a value is valid in the given cell.
		/// </summary>
		private bool IsValid(SudokuGrid s, int row, int col, int val)
		{
			// Check row
			for (int c = 0; c < 9; c++)
				if (s.Cells[row, c] == val) return false;

			// Check column
			for (int r = 0; r < 9; r++)
				if (s.Cells[r, col] == val) return false;

			// Check 3x3 block
			int blockRow = (row / 3) * 3;
			int blockCol = (col / 3) * 3;
			for (int r = 0; r < 3; r++)
			{
				for (int c = 0; c < 3; c++)
				{
					if (s.Cells[blockRow + r, blockCol + c] == val) return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Gets all cells in a specific region (row, column, or block).
		/// </summary>
		private IEnumerable<(int row, int col)> GetCellsInRegion(SudokuGrid s, int region)
		{
			if (region < 9) // Row
			{
				for (int col = 0; col < 9; col++)
				{
					yield return (region, col);
				}
			}
			else if (region < 18) // Column
			{
				int col = region - 9;
				for (int row = 0; row < 9; row++)
				{
					yield return (row, col);
				}
			}
			else // Block
			{
				int block = region - 18;
				int blockRow = (block / 3) * 3;
				int blockCol = (block % 3) * 3;
				for (int r = 0; r < 3; r++)
				{
					for (int c = 0; c < 3; c++)
					{
						yield return (blockRow + r, blockCol + c);
					}
				}
			}
		}
	}
}

