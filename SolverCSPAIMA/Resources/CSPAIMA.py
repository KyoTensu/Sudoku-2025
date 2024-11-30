import numpy as np
from aima3.logic import *
from aima3.csp import *

solved_grid = np.empty((9, 9), dtype=int)
# Fonction de validation des contraintes
def sudoku_constraint(A, a, B, b):
    return a != b  # Les valeurs des voisins doivent être différentes

# Résolution du Sudoku en utilisant CSP
def solve_sudoku_csp(grid):    
    # Afficher la grille d'entrée pour vérifier qu'elle est bien reçue
    print("Grille initiale:")
    print(np.array(grid))  # Afficher la grille sous forme de tableau NumPy

    variables = [(i, j) for i in range(9) for j in range(9)]
    domains = {(i, j): [grid[i][j]] if grid[i][j] != 0 else list(range(1, 10)) for i in range(9) for j in range(9)}

    neighbors = {}
    for i in range(9):
        for j in range(9):
            neighbors[(i, j)] = set()
            for x in range(9):
                if x != i:
                    neighbors[(i, j)].add((x, j))  # Voisin dans la même colonne
                if x != j:
                    neighbors[(i, j)].add((i, x))  # Voisin dans la même ligne
            start_row, start_col = 3 * (i // 3), 3 * (j // 3)
            for r in range(start_row, start_row + 3):
                for c in range(start_col, start_col + 3):
                    if (r, c) != (i, j):
                        neighbors[(i, j)].add((r, c))

    # Créer un CSP avec les variables, domaines et voisins
    csp = CSP(variables, domains, neighbors, sudoku_constraint)
    
    # Effectuer la recherche avec backtracking et AC3 pour l'inférence
    solution = backtracking_search(csp, select_unassigned_variable=mrv, order_domain_values=unordered_domain_values, inference=forward_checking)
    
    # Si la solution est correcte, créer la grille résolue
    solved_grid = np.zeros((9, 9), dtype=int)
    for (i, j), value in solution.items():
        solved_grid[i, j] = value

    # Afficher la grille résolue pour s'assurer que 'solved_grid' est bien défini
   
    return solved_grid

solved_grid = solve_sudoku_csp(instance)
