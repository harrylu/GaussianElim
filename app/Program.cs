
// make IEnumerbale<IList<T>>
/*
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
*/
double R1 = 3;
double R2 = 3;
double R3 = 4.5;
double R4 = 1.5;
double[][] A = new double[][]{new double[] {1, 1/R1, 0},
                            new double[] {0, (1/R1 + 1/R2 + 1/R3), -1/R3},
                            new double[] {0, -1/R3, 1/R4 + 1/R3},
                            };
double[][] B = new double[][]{new double[] {10/R1},
                            new double[] {10/R1},
                            new double[] {0},
                            };
var m = new MatrixSolver<double>(A);
m.MakeReducedEchelon();
Console.WriteLine(m);
//var freeParams = new double[m.NumFreeParams()];
//freeParams[0] = 1;
var solved = m.SolveSystem(B);
solved.ForEach(row => Console.WriteLine(string.Join(" ", row)));