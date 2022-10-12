
// make IEnumerbale<IList<T>>
double[][] A = new double[][]{new double[] {1, 2, 3},
                            new double[] {6, 7, 9},
                            new double[] {2, 4, 6},
                            };
double[][] B = new double[][]{new double[] {0},
                            new double[] {0},
                            new double[] {0},
                            };
var m = new MatrixSolver<double>(A);
m.MakeReducedEchelon();
var freeParams = new double[m.NumFreeParams()];
freeParams[0] = 1;
var solved = m.SolveSystem(B, freeParams);
solved.ForEach(row => Console.WriteLine(string.Join(" ", row)));