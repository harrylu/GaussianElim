using System.Linq;
using System.Collections.Generic;
using System.Text;
using System;
using UnityEngine;

//all rows below last pivot are zero rows?

// DEMO:
//double[][] A = new double[][]{new double[] {1, 2, 3},
//                            new double[] {6, 7, 9},
//                            new double[] {2, 4, 6},
//                            };
//double[][] B = new double[][]{new double[] {0},
//                            new double[] {0},
//                            new double[] {0},
 //                           };
//var m = new MatrixSolver<double>(A);
//m.MakeReducedEchelon();
//var freeParams = new double[m.NumFreeParams()];
//freeParams[0] = 1;
//var solved = m.SolveSystem(B, freeParams); //returns list of lists of T
public class MatrixSolver<T> where T : IComparable, IEquatable<T>
{
    private List<Row> rows;
    private int rowsA;
    private int colsA;

    private List<bool> columnsIsPivot; //stores True if column is pivot, otherwise false
    private int numPivots;
    private int numFree;
    private List<int> columnsDeterminerIdx; //stores the row_idx of the pivot or the first zero value's row_idx in the free column
    // technically, doesnt necessarily store first zero; stores where col was discovered to be free
    private bool isReducedEchelon;

    private List<Action< List<Row> >> rowOperationQueue;

    public MatrixSolver(IEnumerable<IList<T>> A)
    {
        this.rows = MakeRows(A);
        this.rowsA = rows.Count();
        if (rowsA == 0) {this.colsA = 0;} else {colsA = rows[0].GetValues().Count();}
    
        columnsIsPivot = new List<bool>();
        columnsDeterminerIdx = new List<int>();
        rowOperationQueue = new List<Action<List<Row>>>();
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        foreach (Row row in rows)
        {
            sb.AppendLine(row.ToString());
        }
        return sb.ToString();
    }

    List<Row> MakeRows(IEnumerable<IList<T>> enumerable)
    {
        var rows = new List<Row>();
        foreach (IList<T> li in enumerable)
        {
            rows.Add(new Row(li));
        }
        return rows;
    }

    private void SwapRows(List<Row> rows, int index1, int index2)
    {
        Row tmp = rows[index1];
        rows[index1] = rows[index2];
        rows[index2] = tmp;
    }

    private IEnumerable<(int, Row)> GetRowsAbove(List<Row> rows, int row_index)
    //returns tuples of (row_idx, row object)
    {
        return Enumerable.Range(0, row_index).Zip(rows.GetRange(0, row_index), (x, y) => (x, y));
    }

    private IEnumerable<(int, Row)> GetRowsBelow(List<Row>rows, int row_index)
    //returns tuples of (row_idx, row object)
    {
        row_index += 1;
        int count = rows.Count() - row_index;
        return Enumerable.Range(row_index, count).Zip(rows.GetRange(row_index, count), (x, y) => (x, y));
    }

    private int FindNonzeroColBelow(int row_index, int col_idx)
    // returns the row index of a row below the specified row
    //that has a nonzero value at the specified column in its values
    // -1 if not found
    {
        foreach (var (i, row) in this.GetRowsBelow(this.rows, row_index))
        {
            if (!row[col_idx].Equals(default(T)))
            {
                return i;
            }
        }
        return -1;
    }

    private void ReduceAbovePivots()
    {
        for (int col_idx = 0; col_idx < this.columnsIsPivot.Count(); col_idx++)
        {
            if (!columnsIsPivot[col_idx]) continue;
            int row_idx = this.columnsDeterminerIdx[col_idx];   //row_idx of pivot
            Row row = rows[row_idx];    //row of the pivot
            foreach (var (i, upper_row) in this.GetRowsAbove(this.rows, row_idx))
            {
                // we want to make col_idx of the upper row 0
                // we know that this row's val at col_idx is 1
                // so we do upper_row - this_row * upper_row.val
                T scale = upper_row[col_idx];
                upper_row.SubtractRow(row, scale);
                rowOperationQueue.Add(SubtractRowOperation(i, row_idx, scale));
            }
        }
    }
    public void MakeReducedEchelon()
    {
        if (isReducedEchelon) return;
        int row_idx = 0;
        int col_idx = 0;
        while (row_idx < rowsA && col_idx < colsA)
        {
            Row row = this.rows[row_idx];
            T val = row[col_idx]; // value at current row_idx, col_idx
            if (val.Equals(default(T)))
            {
                int swap_row_idx = this.FindNonzeroColBelow(row_idx, col_idx);
                if (swap_row_idx == -1)
                {
                    // this column is a free param, stay on this row but move onto next col
                    this.columnsIsPivot.Add(false);
                    this.columnsDeterminerIdx.Add(row_idx);
                    numFree++;
                    col_idx += 1;
                    continue;
                }
                // swap rows, stay on this row and col
                this.SwapRows(this.rows, row_idx, swap_row_idx);
                rowOperationQueue.Add(SwapOperation(row_idx, swap_row_idx));
                continue;
            }
            // we are on a pivot
            this.columnsIsPivot.Add(true);
            this.columnsDeterminerIdx.Add(row_idx);
            numPivots++;
            //scale pivot to 1 (along with rest of row)
            T one = (T)Convert.ChangeType(1, typeof(T));
            if (!val.Equals(one))
            {
                dynamic scale = (dynamic)one/val;
                row.Scale(scale);
                row.SetIndex(col_idx, one); // to avoid 0.99999 precision errors
                rowOperationQueue.Add(ScaleOperation(row_idx, scale));
            }
            
            //make pivot column below pivot 0
            foreach (var (i, lower_row) in this.GetRowsBelow(this.rows, row_idx))
            {
                // we want to make col_idx of the lower row 0
                // we know that this row's val at col_idx is 1
                // so we do lower_row - this_row * lower_row.val
                T scale = lower_row[col_idx];
                lower_row.SubtractRow(row, scale);
                rowOperationQueue.Add(SubtractRowOperation(i, row_idx, scale));
            }
            row_idx += 1;
            col_idx += 1;
        }

        // if not done with columns, rest are free
        row_idx -= 1;
        for (int i = 0; i < this.colsA - col_idx; i++)
        {
            this.columnsIsPivot.Add(false);
            this.columnsDeterminerIdx.Add(row_idx);
            numFree++;
        }

        ReduceAbovePivots();
        isReducedEchelon = true;
    }

    public int NumFreeParams()
    {
        if (!isReducedEchelon)
        {
            this.MakeReducedEchelon();
        }

        return this.numFree;
    }

    public List<List<T>> SolveSystem(IEnumerable<IList<T>> B)
    {
        return SolveSystem(B, null!);
    }

    public List<List<T>> SolveSystem(IEnumerable<IList<T>> B, IEnumerable<T> substituteFreeParams)
    /// Solves Ax = B for x
    {
        if (!isReducedEchelon)
        {
            this.MakeReducedEchelon();
        }

        List<Row> B_rows = MakeRows(B);
        foreach (Action<List<Row>> action in rowOperationQueue)
        {
            action(B_rows);
        }

        var ret = new List<List<T>>();
        if (numFree > 0)
        {
            IEnumerator<T> freeParams = substituteFreeParams.GetEnumerator();
            for (int col_idx = 0; col_idx < columnsIsPivot.Count(); col_idx++)
            {
                if (!columnsIsPivot[col_idx])
                {
                    freeParams.MoveNext();
                    T param = freeParams.Current;
                    int row_idx = columnsDeterminerIdx[col_idx];
                    // backsub
                    foreach(var (above_row_idx, row) in GetRowsAbove(this.rows, row_idx+1)) //might be problems with this
                    {
                        
                        B_rows[above_row_idx].Subtract(Convert.ChangeType((dynamic)param * row[col_idx], typeof(T)));
                    }
                    ret.Add(Enumerable.Repeat(param,B_rows[0].GetValues().Count()).ToList()); // make list with param duplicated
                }
                else
                {
                    // will fill in at the end
                    ret.Add(null!);
                }
            }
            freeParams.Dispose();
        }

        for (int col_idx = 0; col_idx < columnsIsPivot.Count(); col_idx++)
        {
            if (ret[col_idx] == null)
            {
                int row_idx = columnsDeterminerIdx[col_idx];    // pivot row_idx
                ret[col_idx] = B_rows[row_idx].GetValues().ToList();
            }
        }
        return ret;
    }

    class Row
    {
        public IList<T> values;
        public T this[int index]
        {
            get {return this.values[index];} 
            set {this.values[index] = value;}
        }

        public Row(IList<T> values)
        {
            this.values = values;
        }

        public IList<T> GetValues()
        {
            return this.values;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach(T e in this.GetValues())
            {
                string eStr = e.ToString()!;
                if (eStr is null) eStr = "";
                if (eStr.Length > 7) eStr = eStr.Substring(0, 7);
                sb.Append(eStr + "\t");
            }
            return sb.ToString();
        }

        public void SetIndex(int i, T val)
        {
            this[i] = val;
        }
        public void SubtractRow(Row other, T scale)
        // elementwise subtraction: this row - other row * scale
        {
            foreach ( (int i, T e) in other.GetValues().Select((item, key) => new KeyValuePair<int, T>(key, item)))
            {
                this[i] -= (dynamic)e * scale;
            }
        }

        public void Subtract(T toSubtract)
        {
            for (int i = 0; i < this.GetValues().Count(); i++)
            {
                this[i] -= (dynamic)toSubtract;
            }
        }

        public void Scale(T factor)
        {
            for (int i = 0; i < this.GetValues().Count(); i++)
            {
                this[i] *= (dynamic)factor;
            }
        }

        public int FindPivot(int start=0)
        ///<summary>
        /// Returns index of pivot column (first idx in values with nonzero value)
        ///</summary>
        /// param start is where to start the search
        ///<returns> returns int index of pivot column (-1 if not found) </returns>
        {
            for (int i = start; i < this.GetValues().Count(); i++)
            {
                if (!this[i].Equals(default(T))) return i;
            }
            return -1;
        }
    }

    Action<List<Row>> SubtractRowOperation(int this_idx, int other_idx, T scale)
    {
        return delegate(List<Row> rows)
        {
            rows[this_idx].SubtractRow(rows[other_idx], scale);
        };
    }
    Action<List<Row>> ScaleOperation(int this_idx, T scale)
    {
        return delegate(List<Row> rows)
        {
            rows[this_idx].Scale(scale);
        };
    }
    Action<List<Row>> SwapOperation(int index1, int index2)
    {
        return delegate(List<Row> rows)
        {
            this.SwapRows(rows, index1, index2);
        };
    }
}