import numpy as np
import tensorflow as tf
from tensorflow.keras.models import load_model
from timeit import default_timer
import copy


def preprocess_sudoku(grid):
    """
    Prépare une grille de Sudoku donnée sous forme de liste 2D pour le modèle.
    """
    quiz_array = np.array(grid).reshape(9, 9, 1)
    quiz_array = quiz_array / 9 - 0.5  # Normalisation
    return quiz_array

def predict_sudoku(model, quiz_array):
    """
    Prédit la solution d'un Sudoku avec le modèle.
    """
    quiz_array = np.expand_dims(quiz_array, axis=0)
    
    predictions = model.predict(quiz_array)
    
    predicted_sudoku = np.argmax(predictions, axis=-1).reshape(9, 9) + 1
    
    return predicted_sudoku

def is_valid_sudoku(sudoku):
    """
    Vérifie si un Sudoku est valide.
    """
    def is_valid_block(block):
        return sorted(block) == list(range(1, 10))
    
    for row in sudoku:
        if not is_valid_block(list(row)):
            return False
    
    for col in sudoku.T:
        if not is_valid_block(list(col)):
            return False
    
    for i in range(0, 9, 3):
        for j in range(0, 9, 3):
            block = sudoku[i:i+3, j:j+3].flatten()
            if not is_valid_block(list(block)):
                return False
    
    return True

if 'instance' not in locals():
    instance = np.array([
        [5, 2, 0, 0, 0, 6, 0, 0, 0],
        [0, 0, 0, 0, 7, 0, 1, 0, 0],
        [3, 0, 0, 0, 0, 0, 0, 0, 0],
        [0, 0, 0, 4, 0, 0, 8, 0, 0],
        [6, 0, 0, 0, 0, 0, 0, 5, 0],
        [0, 0, 0, 0, 0, 0, 0, 0, 0],
        [0, 4, 1, 8, 0, 0, 0, 0, 0],
        [0, 0, 0, 0, 3, 0, 0, 2, 0],
        [0, 0, 8, 7, 0, 0, 0, 0, 0]], dtype=int)

model = load_model('C:/Users/etien/Downloads/sudoku_solver_final.h5')

start = default_timer()
quiz_array = preprocess_sudoku(instance)

resultt = predict_sudoku(model, quiz_array)

result = np.array(resultt, dtype=np.int64)


if is_valid_sudoku(resultt):
    print("Le Sudoku prédit est valide.")
else:
    print("Le Sudoku prédit n'est pas valide.")


if all(all(cell != 0 for cell in row) for row in result):
    print("Sudoku résolu par le modèle CNN :")
    for row in result:
        print(row)
else:
    print("Aucune solution trouvée.")
execution = default_timer() - start
print("Le temps de résolution est de : ", execution * 1000, " ms")
