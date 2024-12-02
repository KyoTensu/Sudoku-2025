# Solveur de Sudoku

Ce projet inclut un solveur probabiliste de Sudoku basé sur le framework Microsoft Infer.NET.
Le solveur propose deux modèles pour résoudre des puzzles Sudoku, chacun utilisant des techniques d'inférence bayésienne pour déduire les valeurs des cases tout en respectant les règles du Sudoku.

---

## **1. Vue d'ensemble**

### **Objectif**
Le code implémente deux modèles probabilistes :
- **Modèle robuste (`RobustSudokuModel`)** : Un modèle probabiliste avancé capable de résoudre une plus large gamme de puzzles Sudoku, utilisant une inférence itérative pour améliorer la précision.
- **Modèle naïf (`NaiveSudokuModel`)** : Un modèle plus simple avec des contraintes de base mais des capacités limitées, principalement destiné aux puzzles faciles.

Les deux modèles respectent l'ensemble des règles du Sudoku (les lignes, colonnes et sous-grilles 3x3 doivent contenir des chiffres uniques de 1 à 9) et utilisent des contraintes probabilistes pour inférer les valeurs manquantes.

### **Caractéristiques**
- Inférence bayésienne pour calculer les valeurs probables de chaque case.
- Prise en charge du raffinement itératif pour gérer les puzzles plus complexes.
- Compatibilité avec les algorithmes d'Infer.NET, notamment Expectation Propagation, Gibbs Sampling et Variational Message Passing.

---

## **2. Structure du code**

### **2.1. Classes**

#### **`GraphProba` et `GraphNaiveProba`**
- Ces classes sont des wrappers implémentant l'interface `ISudokuSolver`.
- Elles utilisent respectivement les modèles `RobustSudokuModel` et `NaiveSudokuModel` pour résoudre des puzzles Sudoku.

#### **`RobustSudokuModel`**
- Un modèle probabiliste robuste.
- Caractéristiques :
  - Utilise des distributions de type `Dirichlet` pour les probabilités des cases.
  - Applique des contraintes de Sudoku pour garantir l'unicité dans les lignes, colonnes et sous-grilles.
  - Met à jour les probabilités de manière itérative pour affiner la solution.

#### **`NaiveSudokuModel`**
- Un modèle plus simple et moins efficace.
- Caractéristiques :
  - Considère chaque case comme une variable indépendante.
  - Résout les puzzles sans utiliser de mises à jour itératives ni de raffinement des probabilités.

---

### **2.2. Fonctionnalités principales**

#### **Inférence bayésienne**
- Les modèles utilisent des distributions probabilistes (`Dirichlet` et `Discrete`) pour représenter la probabilité de chaque valeur possible pour une case de Sudoku.
- Les algorithmes d'inférence comme Expectation Propagation optimisent ces probabilités pour trouver les valeurs les plus probables.

#### **Contraintes**
- Implémentation de `ConstrainFalse` dans Infer.NET pour garantir l'unicité entre les cases voisines (lignes, colonnes, sous-grilles).

#### **Raffinement itératif (Modèle robuste)**
- Exploite les mises à jour itératives :
  - Les valeurs avec une forte probabilité sont fixées.
  - Les probabilités restantes sont recalculées en fonction des contraintes et des observations initiales.

---

## **3. Utilisation**

### **`Solve(SudokuGrid s)`**
- Méthode implémentée dans `GraphProba` et `GraphNaiveProba` :
  - **Entrée** : Un objet `SudokuGrid` contenant les valeurs initiales.
  - **Sortie** : Le `SudokuGrid` résolu.

### **Exemple**
```csharp
var solver = new GraphProba();
SudokuGrid puzzle = /* Initialisez votre puzzle Sudoku */;
SudokuGrid solution = solver.Solve(puzzle);
```

---

## **4. Limitations**
- Le **Modèle naïf** est coûteux en calcul et limité aux puzzles simples.
- Le **Modèle robuste** :
  - A des difficultés avec les puzzles très complexes.
  - S'appuie sur un raffinement itératif qui peut ne pas converger pour toutes les configurations.

---

## **5. Améliorations futures**
- Tester d'autres algorithmes d'inférence et configurations de paramètres.
- Améliorer les méthodes itératives en donnant la priorité aux cases avec les probabilités les plus élevées.
- Explorer l'intégration avec des modèles de réseaux neuronaux pour fournir de meilleurs a priori pour les puzzles difficiles.

---

## **6. Références**
- Documentation Infer.NET : [https://dotnet.github.io/infer/InferNet101.pdf](https://dotnet.github.io/infer/InferNet101.pdf)
- Distribution multinomiale : [Wikipedia](https://en.wikipedia.org/wiki/Multinomial_distribution)
- Distribution catégorielle : [Wikipedia](https://en.wikipedia.org/wiki/Categorical_distribution)
- Prior conjugué : [Wikipedia](https://en.wikipedia.org/wiki/Conjugate_prior)
- Distribution de Dirichlet : [Wikipedia](https://en.wikipedia.org/wiki/Dirichlet_distribution)
- Distribution Dirichlet-multinomiale : [Wikipedia](https://en.wikipedia.org/wiki/Dirichlet-multinomial_distribution)
- Contraintes de Sudoku : [Règles du Sudoku](https://fr.wikipedia.org/wiki/Sudoku)
