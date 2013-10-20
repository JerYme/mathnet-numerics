﻿// <copyright file="MklLinearAlgebraProvider.Double.cs" company="Math.NET">
// Math.NET Numerics, part of the Math.NET Project
// http://numerics.mathdotnet.com
// http://github.com/mathnet/mathnet-numerics
// http://mathnetnumerics.codeplex.com
//
// Copyright (c) 2009-2013 Math.NET
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
// </copyright>

#if NATIVEMKL

using MathNet.Numerics.LinearAlgebra.Factorization;
using MathNet.Numerics.Properties;
using System;
using System.Numerics;
using System.Security;

namespace MathNet.Numerics.Providers.LinearAlgebra.Mkl
{
    /// <summary>
    /// Intel's Math Kernel Library (MKL) linear algebra provider.
    /// </summary>
    public partial class MklLinearAlgebraProvider : ManagedLinearAlgebraProvider
    {
        /// <summary>
        /// Computes the requested <see cref="Norm"/> of the matrix.
        /// </summary>
        /// <param name="norm">The type of norm to compute.</param>
        /// <param name="rows">The number of rows in the matrix.</param>
        /// <param name="columns">The number of columns in the matrix.</param>
        /// <param name="matrix">The matrix to compute the norm from.</param>
        /// <returns>
        /// The requested <see cref="Norm"/> of the matrix.
        /// </returns>
        [SecuritySafeCritical]
        public override double MatrixNorm(Norm norm, int rows, int columns, double[] matrix)
        {
            if (matrix == null)
            {
                throw new ArgumentNullException("matrix");
            }

            if (rows <= 0)
            {
                throw new ArgumentException(Resources.ArgumentMustBePositive, "rows");
            }

            if (columns <= 0)
            {
                throw new ArgumentException(Resources.ArgumentMustBePositive, "columns");
            }

            if (matrix.Length < rows * columns)
            {
                throw new ArgumentException(string.Format(Resources.ArrayTooSmall, rows * columns), "matrix");
            }

            var work = new double[rows];
            return SafeNativeMethods.d_matrix_norm((byte)norm, rows, columns, matrix, work);
        }

        /// <summary>
        /// Computes the dot product of x and y.
        /// </summary>
        /// <param name="x">The vector x.</param>
        /// <param name="y">The vector y.</param>
        /// <returns>The dot product of x and y.</returns>
        /// <remarks>This is equivalent to the DOT BLAS routine.</remarks>
        [SecuritySafeCritical]
        public override double DotProduct(double[] x, double[] y)
        {
            if (y == null)
            {
                throw new ArgumentNullException("y");
            }

            if (x == null)
            {
                throw new ArgumentNullException("x");
            }

            if (x.Length != y.Length)
            {
                throw new ArgumentException(Resources.ArgumentArraysSameLength);
            }

            return SafeNativeMethods.d_dot_product(x.Length, x, y);
        }

        /// <summary>
        /// Adds a scaled vector to another: <c>result = y + alpha*x</c>.
        /// </summary>
        /// <param name="y">The vector to update.</param>
        /// <param name="alpha">The value to scale <paramref name="x"/> by.</param>
        /// <param name="x">The vector to add to <paramref name="y"/>.</param>
        /// <param name="result">The result of the addition.</param>
        /// <remarks>This is similar to the AXPY BLAS routine.</remarks>
        [SecuritySafeCritical]
        public override void AddVectorToScaledVector(double[] y, double alpha, double[] x, double[] result)
        {
            if (y == null)
            {
                throw new ArgumentNullException("y");
            }

            if (x == null)
            {
                throw new ArgumentNullException("x");
            }

            if (y.Length != x.Length)
            {
                throw new ArgumentException(Resources.ArgumentVectorsSameLength);
            }

            if (!ReferenceEquals(y, result))
            {
                Array.Copy(y, 0, result, 0, y.Length);
            }

            if (alpha == 0.0)
            {
                return;
            }

            SafeNativeMethods.d_axpy(y.Length, alpha, x, result);
        }

        /// <summary>
        /// Scales an array. Can be used to scale a vector and a matrix.
        /// </summary>
        /// <param name="alpha">The scalar.</param>
        /// <param name="x">The values to scale.</param>
        /// <param name="result">This result of the scaling.</param>
        /// <remarks>This is similar to the SCAL BLAS routine.</remarks>
        [SecuritySafeCritical]
        public override void ScaleArray(double alpha, double[] x, double[] result)
        {
            if (x == null)
            {
                throw new ArgumentNullException("x");
            }

            if (!ReferenceEquals(x, result))
            {
                Array.Copy(x, 0, result, 0, x.Length);
            }

            if (alpha == 1.0)
            {
                return;
            }

            SafeNativeMethods.d_scale(x.Length, alpha, result);
        }

        /// <summary>
        /// Multiples two matrices. <c>result = x * y</c>
        /// </summary>
        /// <param name="x">The x matrix.</param>
        /// <param name="rowsX">The number of rows in the x matrix.</param>
        /// <param name="columnsX">The number of columns in the x matrix.</param>
        /// <param name="y">The y matrix.</param>
        /// <param name="rowsY">The number of rows in the y matrix.</param>
        /// <param name="columnsY">The number of columns in the y matrix.</param>
        /// <param name="result">Where to store the result of the multiplication.</param>
        /// <remarks>This is a simplified version of the BLAS GEMM routine with alpha
        /// set to 1.0 and beta set to 0.0, and x and y are not transposed.</remarks>
        public override void MatrixMultiply(double[] x, int rowsX, int columnsX, double[] y, int rowsY, int columnsY, double[] result)
        {
            MatrixMultiplyWithUpdate(Transpose.DontTranspose, Transpose.DontTranspose, 1.0, x, rowsX, columnsX, y, rowsY, columnsY, 0.0, result);
        }

        /// <summary>
        /// Multiplies two matrices and updates another with the result. <c>c = alpha*op(a)*op(b) + beta*c</c>
        /// </summary>
        /// <param name="transposeA">How to transpose the <paramref name="a"/> matrix.</param>
        /// <param name="transposeB">How to transpose the <paramref name="b"/> matrix.</param>
        /// <param name="alpha">The value to scale <paramref name="a"/> matrix.</param>
        /// <param name="a">The a matrix.</param>
        /// <param name="rowsA">The number of rows in the <paramref name="a"/> matrix.</param>
        /// <param name="columnsA">The number of columns in the <paramref name="a"/> matrix.</param>
        /// <param name="b">The b matrix</param>
        /// <param name="rowsB">The number of rows in the <paramref name="b"/> matrix.</param>
        /// <param name="columnsB">The number of columns in the <paramref name="b"/> matrix.</param>
        /// <param name="beta">The value to scale the <paramref name="c"/> matrix.</param>
        /// <param name="c">The c matrix.</param>
        [SecuritySafeCritical]
        public override void MatrixMultiplyWithUpdate(Transpose transposeA, Transpose transposeB, double alpha, double[] a, int rowsA, int columnsA, double[] b, int rowsB, int columnsB, double beta, double[] c)
        {
            if (a == null)
            {
                throw new ArgumentNullException("a");
            }

            if (b == null)
            {
                throw new ArgumentNullException("b");
            }

            if (c == null)
            {
                throw new ArgumentNullException("c");
            }

            var m = transposeA == Transpose.DontTranspose ? rowsA : columnsA;
            var n = transposeB == Transpose.DontTranspose ? columnsB : rowsB;
            var k = transposeA == Transpose.DontTranspose ? columnsA : rowsA;
            var l = transposeB == Transpose.DontTranspose ? rowsB : columnsB;

            if (c.Length != m*n)
            {
                throw new ArgumentException(Resources.ArgumentMatrixDimensions);
            }

            if (k != l)
            {
                throw new ArgumentException(Resources.ArgumentMatrixDimensions);
            }

            SafeNativeMethods.d_matrix_multiply(transposeA, transposeB, m, n, k, alpha, a, b, beta, c);
        }

        /// <summary>
        /// Computes the LUP factorization of A. P*A = L*U.
        /// </summary>
        /// <param name="data">An <paramref name="order"/> by <paramref name="order"/> matrix. The matrix is overwritten with the
        /// the LU factorization on exit. The lower triangular factor L is stored in under the diagonal of <paramref name="data"/> (the diagonal is always 1.0
        /// for the L factor). The upper triangular factor U is stored on and above the diagonal of <paramref name="data"/>.</param>
        /// <param name="order">The order of the square matrix <paramref name="data"/>.</param>
        /// <param name="ipiv">On exit, it contains the pivot indices. The size of the array must be <paramref name="order"/>.</param>
        /// <remarks>This is equivalent to the GETRF LAPACK routine.</remarks>
        [SecuritySafeCritical]
        public override void LUFactor(double[] data, int order, int[] ipiv)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            if (ipiv == null)
            {
                throw new ArgumentNullException("ipiv");
            }

            if (data.Length != order*order)
            {
                throw new ArgumentException(Resources.ArgumentArraysSameLength, "data");
            }

            if (ipiv.Length != order)
            {
                throw new ArgumentException(Resources.ArgumentArraysSameLength, "ipiv");
            }

            SafeNativeMethods.d_lu_factor(order, data, ipiv);
        }

        /// <summary>
        /// Computes the inverse of matrix using LU factorization.
        /// </summary>
        /// <param name="a">The N by N matrix to invert. Contains the inverse On exit.</param>
        /// <param name="order">The order of the square matrix <paramref name="a"/>.</param>
        /// <remarks>This is equivalent to the GETRF and GETRI LAPACK routines.</remarks>
        [SecuritySafeCritical]
        public override void LUInverse(double[] a, int order)
        {
            if (a == null)
            {
                throw new ArgumentNullException("a");
            }

            if (a.Length != order*order)
            {
                throw new ArgumentException(Resources.ArgumentArraysSameLength, "a");
            }

            var work = new double[order];
            SafeNativeMethods.d_lu_inverse(order, a, work, work.Length);
        }

        /// <summary>
        /// Computes the inverse of a previously factored matrix.
        /// </summary>
        /// <param name="a">The LU factored N by N matrix.  Contains the inverse On exit.</param>
        /// <param name="order">The order of the square matrix <paramref name="a"/>.</param>
        /// <param name="ipiv">The pivot indices of <paramref name="a"/>.</param>
        /// <remarks>This is equivalent to the GETRI LAPACK routine.</remarks>
        [SecuritySafeCritical]
        public override void LUInverseFactored(double[] a, int order, int[] ipiv)
        {
            if (a == null)
            {
                throw new ArgumentNullException("a");
            }

            if (ipiv == null)
            {
                throw new ArgumentNullException("ipiv");
            }

            if (a.Length != order*order)
            {
                throw new ArgumentException(Resources.ArgumentArraysSameLength, "a");
            }

            if (ipiv.Length != order)
            {
                throw new ArgumentException(Resources.ArgumentArraysSameLength, "ipiv");
            }

            var work = new double[order];
            SafeNativeMethods.d_lu_inverse_factored(order, a, ipiv, work, order);
        }

        /// <summary>
        /// Computes the inverse of matrix using LU factorization.
        /// </summary>
        /// <param name="a">The N by N matrix to invert. Contains the inverse On exit.</param>
        /// <param name="order">The order of the square matrix <paramref name="a"/>.</param>
        /// <param name="work">The work array. The array must have a length of at least N,
        /// but should be N*blocksize. The blocksize is machine dependent. On exit, work[0] contains the optimal
        /// work size value.</param>
        /// <remarks>This is equivalent to the GETRF and GETRI LAPACK routines.</remarks>
        [SecuritySafeCritical]
        public override void LUInverse(double[] a, int order, double[] work)
        {
            if (a == null)
            {
                throw new ArgumentNullException("a");
            }

            if (a.Length != order*order)
            {
                throw new ArgumentException(Resources.ArgumentArraysSameLength, "a");
            }

            if (work == null)
            {
                throw new ArgumentNullException("work");
            }

            if (work.Length < order)
            {
                throw new ArgumentException(Resources.WorkArrayTooSmall, "work");
            }

            SafeNativeMethods.d_lu_inverse(order, a, work, work.Length);
        }

        /// <summary>
        /// Computes the inverse of a previously factored matrix.
        /// </summary>
        /// <param name="a">The LU factored N by N matrix.  Contains the inverse On exit.</param>
        /// <param name="order">The order of the square matrix <paramref name="a"/>.</param>
        /// <param name="ipiv">The pivot indices of <paramref name="a"/>.</param>
        /// <param name="work">The work array. The array must have a length of at least N,
        /// but should be N*blocksize. The blocksize is machine dependent. On exit, work[0] contains the optimal
        /// work size value.</param>
        /// <remarks>This is equivalent to the GETRI LAPACK routine.</remarks>
        [SecuritySafeCritical]
        public override void LUInverseFactored(double[] a, int order, int[] ipiv, double[] work)
        {
            if (a == null)
            {
                throw new ArgumentNullException("a");
            }

            if (ipiv == null)
            {
                throw new ArgumentNullException("ipiv");
            }

            if (a.Length != order*order)
            {
                throw new ArgumentException(Resources.ArgumentArraysSameLength, "a");
            }

            if (ipiv.Length != order)
            {
                throw new ArgumentException(Resources.ArgumentArraysSameLength, "ipiv");
            }

            if (work == null)
            {
                throw new ArgumentNullException("work");
            }

            if (work.Length < order)
            {
                throw new ArgumentException(Resources.WorkArrayTooSmall, "work");
            }

            SafeNativeMethods.d_lu_inverse_factored(order, a, ipiv, work, order);
        }

        /// <summary>
        /// Solves A*X=B for X using LU factorization.
        /// </summary>
        /// <param name="columnsOfB">The number of columns of B.</param>
        /// <param name="a">The square matrix A.</param>
        /// <param name="order">The order of the square matrix <paramref name="a"/>.</param>
        /// <param name="b">On entry the B matrix; on exit the X matrix.</param>
        /// <remarks>This is equivalent to the GETRF and GETRS LAPACK routines.</remarks>
        [SecuritySafeCritical]
        public override void LUSolve(int columnsOfB, double[] a, int order, double[] b)
        {
            if (a == null)
            {
                throw new ArgumentNullException("a");
            }

            if (a.Length != order*order)
            {
                throw new ArgumentException(Resources.ArgumentArraysSameLength, "a");
            }

            if (b.Length != columnsOfB*order)
            {
                throw new ArgumentException(Resources.ArgumentArraysSameLength, "b");
            }

            if (ReferenceEquals(a, b))
            {
                throw new ArgumentException(Resources.ArgumentReferenceDifferent);
            }

            SafeNativeMethods.d_lu_solve(order, columnsOfB, a, b);
        }

        /// <summary>
        /// Solves A*X=B for X using a previously factored A matrix.
        /// </summary>
        /// <param name="columnsOfB">The number of columns of B.</param>
        /// <param name="a">The factored A matrix.</param>
        /// <param name="order">The order of the square matrix <paramref name="a"/>.</param>
        /// <param name="ipiv">The pivot indices of <paramref name="a"/>.</param>
        /// <param name="b">On entry the B matrix; on exit the X matrix.</param>
        /// <remarks>This is equivalent to the GETRS LAPACK routine.</remarks>
        [SecuritySafeCritical]
        public override void LUSolveFactored(int columnsOfB, double[] a, int order, int[] ipiv, double[] b)
        {
            if (a == null)
            {
                throw new ArgumentNullException("a");
            }

            if (ipiv == null)
            {
                throw new ArgumentNullException("ipiv");
            }

            if (a.Length != order*order)
            {
                throw new ArgumentException(Resources.ArgumentArraysSameLength, "a");
            }

            if (ipiv.Length != order)
            {
                throw new ArgumentException(Resources.ArgumentArraysSameLength, "ipiv");
            }

            if (b.Length != columnsOfB*order)
            {
                throw new ArgumentException(Resources.ArgumentArraysSameLength, "b");
            }

            if (ReferenceEquals(a, b))
            {
                throw new ArgumentException(Resources.ArgumentReferenceDifferent);
            }

            SafeNativeMethods.d_lu_solve_factored(order, columnsOfB, a, ipiv, b);
        }

        /// <summary>
        /// Computes the Cholesky factorization of A.
        /// </summary>
        /// <param name="a">On entry, a square, positive definite matrix. On exit, the matrix is overwritten with the
        /// the Cholesky factorization.</param>
        /// <param name="order">The number of rows or columns in the matrix.</param>
        /// <remarks>This is equivalent to the POTRF LAPACK routine.</remarks>
        [SecuritySafeCritical]
        public override void CholeskyFactor(double[] a, int order)
        {
            if (a == null)
            {
                throw new ArgumentNullException("a");
            }

            if (order < 1)
            {
                throw new ArgumentException(Resources.ArgumentMustBePositive, "order");
            }

            if (a.Length != order*order)
            {
                throw new ArgumentException(Resources.ArgumentArraysSameLength, "a");
            }

            var info = SafeNativeMethods.d_cholesky_factor(order, a);

            if (info > 0)
            {
                throw new ArgumentException(Resources.ArgumentMatrixPositiveDefinite);
            }
        }

        /// <summary>
        /// Solves A*X=B for X using Cholesky factorization.
        /// </summary>
        /// <param name="a">The square, positive definite matrix A.</param>
        /// <param name="orderA">The number of rows and columns in A.</param>
        /// <param name="b">On entry the B matrix; on exit the X matrix.</param>
        /// <param name="columnsB">The number of columns in the B matrix.</param>
        /// <remarks>This is equivalent to the POTRF add POTRS LAPACK routines.
        /// </remarks>
        [SecuritySafeCritical]
        public override void CholeskySolve(double[] a, int orderA, double[] b, int columnsB)
        {
            if (a == null)
            {
                throw new ArgumentNullException("a");
            }

            if (b == null)
            {
                throw new ArgumentNullException("b");
            }

            if (b.Length != orderA*columnsB)
            {
                throw new ArgumentException(Resources.ArgumentArraysSameLength, "b");
            }

            if (ReferenceEquals(a, b))
            {
                throw new ArgumentException(Resources.ArgumentReferenceDifferent);
            }

            SafeNativeMethods.d_cholesky_solve(orderA, columnsB, a, b);
        }

        /// <summary>
        /// Solves A*X=B for X using a previously factored A matrix.
        /// </summary>
        /// <param name="a">The square, positive definite matrix A.</param>
        /// <param name="orderA">The number of rows and columns in A.</param>
        /// <param name="b">On entry the B matrix; on exit the X matrix.</param>
        /// <param name="columnsB">The number of columns in the B matrix.</param>
        /// <remarks>This is equivalent to the POTRS LAPACK routine.</remarks>
        [SecuritySafeCritical]
        public override void CholeskySolveFactored(double[] a, int orderA, double[] b, int columnsB)
        {
            if (a == null)
            {
                throw new ArgumentNullException("a");
            }

            if (b == null)
            {
                throw new ArgumentNullException("b");
            }

            if (b.Length != orderA*columnsB)
            {
                throw new ArgumentException(Resources.ArgumentArraysSameLength, "b");
            }

            if (ReferenceEquals(a, b))
            {
                throw new ArgumentException(Resources.ArgumentReferenceDifferent);
            }

            SafeNativeMethods.d_cholesky_solve_factored(orderA, columnsB, a, b);
        }

        /// <summary>
        /// Computes the QR factorization of A.
        /// </summary>
        /// <param name="r">On entry, it is the M by N A matrix to factor. On exit,
        /// it is overwritten with the R matrix of the QR factorization. </param>
        /// <param name="rowsR">The number of rows in the A matrix.</param>
        /// <param name="columnsR">The number of columns in the A matrix.</param>
        /// <param name="q">On exit, A M by M matrix that holds the Q matrix of the
        /// QR factorization.</param>
        /// <param name="tau">A min(m,n) vector. On exit, contains additional information
        /// to be used by the QR solve routine.</param>
        /// <remarks>This is similar to the GEQRF and ORGQR LAPACK routines.</remarks>
        [SecuritySafeCritical]
        public override void QRFactor(double[] r, int rowsR, int columnsR, double[] q, double[] tau)
        {
            if (r == null)
            {
                throw new ArgumentNullException("r");
            }

            if (q == null)
            {
                throw new ArgumentNullException("q");
            }

            if (r.Length != rowsR*columnsR)
            {
                throw new ArgumentException(string.Format(Resources.ArgumentArrayWrongLength, "rowsR * columnsR"), "r");
            }

            if (tau.Length < Math.Min(rowsR, columnsR))
            {
                throw new ArgumentException(string.Format(Resources.ArrayTooSmall, "min(m,n)"), "tau");
            }

            if (q.Length != rowsR*rowsR)
            {
                throw new ArgumentException(string.Format(Resources.ArgumentArrayWrongLength, "rowsR * rowsR"), "q");
            }

            var work = new double[columnsR*Control.BlockSize];
            SafeNativeMethods.d_qr_factor(rowsR, columnsR, r, tau, q, work, work.Length);
        }

        /// <summary>
        /// Computes the QR factorization of A.
        /// </summary>
        /// <param name="r">On entry, it is the M by N A matrix to factor. On exit,
        /// it is overwritten with the R matrix of the QR factorization. </param>
        /// <param name="rowsR">The number of rows in the A matrix.</param>
        /// <param name="columnsR">The number of columns in the A matrix.</param>
        /// <param name="q">On exit, A M by M matrix that holds the Q matrix of the 
        /// QR factorization.</param>
        /// <param name="tau">A min(m,n) vector. On exit, contains additional information
        /// to be used by the QR solve routine.</param>
        /// <param name="work">The work array. The array must have a length of at least N,
        /// but should be N*blocksize. The blocksize is machine dependent. On exit, work[0] contains the optimal
        /// work size value.</param>
        /// <remarks>This is similar to the GEQRF and ORGQR LAPACK routines.</remarks>
        [SecuritySafeCritical]
        public override void QRFactor(double[] r, int rowsR, int columnsR, double[] q, double[] tau, double[] work)
        {
            if (r == null)
            {
                throw new ArgumentNullException("r");
            }

            if (q == null)
            {
                throw new ArgumentNullException("q");
            }

            if (work == null)
            {
                throw new ArgumentNullException("work");
            }

            if (r.Length != rowsR*columnsR)
            {
                throw new ArgumentException(string.Format(Resources.ArgumentArrayWrongLength, "rowsR * columnsR"), "r");
            }

            if (tau.Length < Math.Min(rowsR, columnsR))
            {
                throw new ArgumentException(string.Format(Resources.ArrayTooSmall, "min(m,n)"), "tau");
            }

            if (q.Length != rowsR*rowsR)
            {
                throw new ArgumentException(string.Format(Resources.ArgumentArrayWrongLength, "rowsR * rowsR"), "q");
            }

            if (work.Length < columnsR*Control.BlockSize)
            {
                work[0] = columnsR*Control.BlockSize;
                throw new ArgumentException(Resources.WorkArrayTooSmall, "work");
            }

            SafeNativeMethods.d_qr_factor(rowsR, columnsR, r, tau, q, work, work.Length);
        }

        /// <summary>
        /// Computes the thin QR factorization of A where M &gt; N.
        /// </summary>
        /// <param name="q">On entry, it is the M by N A matrix to factor. On exit,
        /// it is overwritten with the Q matrix of the QR factorization.</param>
        /// <param name="rowsA">The number of rows in the A matrix.</param>
        /// <param name="columnsA">The number of columns in the A matrix.</param>
        /// <param name="r">On exit, A N by N matrix that holds the R matrix of the
        /// QR factorization.</param>
        /// <param name="tau">A min(m,n) vector. On exit, contains additional information
        /// to be used by the QR solve routine.</param>
        /// <remarks>This is similar to the GEQRF and ORGQR LAPACK routines.</remarks>
        [SecuritySafeCritical]
        public override void ThinQRFactor(double[] q, int rowsA, int columnsA, double[] r, double[] tau)
        {
            if (r == null)
            {
                throw new ArgumentNullException("r");
            }

            if (q == null)
            {
                throw new ArgumentNullException("q");
            }

            if (q.Length != rowsA*columnsA)
            {
                throw new ArgumentException(string.Format(Resources.ArgumentArrayWrongLength, "rowsR * columnsR"), "q");
            }

            if (tau.Length < Math.Min(rowsA, columnsA))
            {
                throw new ArgumentException(string.Format(Resources.ArrayTooSmall, "min(m,n)"), "tau");
            }

            if (r.Length != columnsA*columnsA)
            {
                throw new ArgumentException(string.Format(Resources.ArgumentArrayWrongLength, "columnsA * columnsA"), "r");
            }

            var work = new double[columnsA*Control.BlockSize];
            SafeNativeMethods.d_qr_thin_factor(rowsA, columnsA, q, tau, r, work, work.Length);
        }


        /// <summary>
        /// Computes the thin QR factorization of A where M &gt; N.
        /// </summary>
        /// <param name="q">On entry, it is the M by N A matrix to factor. On exit,
        /// it is overwritten with the Q matrix of the QR factorization.</param>
        /// <param name="rowsA">The number of rows in the A matrix.</param>
        /// <param name="columnsA">The number of columns in the A matrix.</param>
        /// <param name="r">On exit, A N by N matrix that holds the R matrix of the
        /// QR factorization.</param>
        /// <param name="tau">A min(m,n) vector. On exit, contains additional information
        /// to be used by the QR solve routine.</param>
        /// <param name="work">The work array. The array must have a length of at least N,
        /// but should be N*blocksize. The blocksize is machine dependent. On exit, work[0] contains the optimal
        /// work size value.</param>
        /// <remarks>This is similar to the GEQRF and ORGQR LAPACK routines.</remarks>
        [SecuritySafeCritical]
        public override void ThinQRFactor(double[] q, int rowsA, int columnsA, double[] r, double[] tau, double[] work)
        {
            if (r == null)
            {
                throw new ArgumentNullException("r");
            }

            if (q == null)
            {
                throw new ArgumentNullException("q");
            }

            if (work == null)
            {
                throw new ArgumentNullException("q");
            }

            if (q.Length != rowsA*columnsA)
            {
                throw new ArgumentException(string.Format(Resources.ArgumentArrayWrongLength, "rowsR * columnsR"), "q");
            }

            if (tau.Length < Math.Min(rowsA, columnsA))
            {
                throw new ArgumentException(string.Format(Resources.ArrayTooSmall, "min(m,n)"), "tau");
            }

            if (r.Length != columnsA*columnsA)
            {
                throw new ArgumentException(
                    string.Format(Resources.ArgumentArrayWrongLength, "columnsA * columnsA"), "r");
            }

            if (work.Length < columnsA*Control.BlockSize)
            {
                work[0] = columnsA*Control.BlockSize;
                throw new ArgumentException(Resources.WorkArrayTooSmall, "work");
            }

            SafeNativeMethods.d_qr_thin_factor(rowsA, columnsA, q, tau, r, work, work.Length);
        }

        /// <summary>
        /// Solves A*X=B for X using QR factorization of A.
        /// </summary>
        /// <param name="a">The A matrix.</param>
        /// <param name="rows">The number of rows in the A matrix.</param>
        /// <param name="columns">The number of columns in the A matrix.</param>
        /// <param name="b">The B matrix.</param>
        /// <param name="columnsB">The number of columns of B.</param>
        /// <param name="x">On exit, the solution matrix.</param>
        /// <param name="method">The type of QR factorization to perform. <seealso cref="QRMethod"/></param>
        /// <remarks>Rows must be greater or equal to columns.</remarks>
        [SecuritySafeCritical]
        public override void QRSolve(double[] a, int rows, int columns, double[] b, int columnsB, double[] x, QRMethod method = QRMethod.Full)
        {
            var work = new double[columns*Control.BlockSize];
            QRSolve(a, rows, columns, b, columnsB, x, work, method);
        }

        /// <summary>
        /// Solves A*X=B for X using QR factorization of A.
        /// </summary>
        /// <param name="a">The A matrix.</param>
        /// <param name="rows">The number of rows in the A matrix.</param>
        /// <param name="columns">The number of columns in the A matrix.</param>
        /// <param name="b">The B matrix.</param>
        /// <param name="columnsB">The number of columns of B.</param>
        /// <param name="x">On exit, the solution matrix.</param>
        /// <param name="work">The work array. The array must have a length of at least N,
        /// but should be N*blocksize. The blocksize is machine dependent. On exit, work[0] contains the optimal
        /// work size value.</param>
        /// <param name="method">The type of QR factorization to perform. <seealso cref="QRMethod"/></param>
        /// <remarks>Rows must be greater or equal to columns.</remarks>
        [SecuritySafeCritical]
        public override void QRSolve(double[] a, int rows, int columns, double[] b, int columnsB, double[] x, double[] work, QRMethod method = QRMethod.Full)
        {
            if (a == null)
            {
                throw new ArgumentNullException("a");
            }

            if (b == null)
            {
                throw new ArgumentNullException("b");
            }

            if (x == null)
            {
                throw new ArgumentNullException("x");
            }

            if (work == null)
            {
                throw new ArgumentNullException("work");
            }

            if (a.Length != rows*columns)
            {
                throw new ArgumentException(Resources.ArgumentArraysSameLength, "a");
            }

            if (b.Length != rows*columnsB)
            {
                throw new ArgumentException(Resources.ArgumentArraysSameLength, "b");
            }

            if (x.Length != columns*columnsB)
            {
                throw new ArgumentException(Resources.ArgumentArraysSameLength, "x");
            }

            if (rows < columns)
            {
                throw new ArgumentException(Resources.RowsLessThanColumns);
            }

            if (work.Length < 1)
            {
                work[0] = rows*Control.BlockSize;
                throw new ArgumentException(Resources.WorkArrayTooSmall, "work");
            }

            SafeNativeMethods.d_qr_solve(rows, columns, columnsB, a, b, x, work, work.Length);
        }

        /// <summary>
        /// Solves A*X=B for X using a previously QR factored matrix.
        /// </summary>
        /// <param name="q">The Q matrix obtained by calling <see cref="QRFactor(double[],int,int,double[],double[])"/>.</param>
        /// <param name="r">The R matrix obtained by calling <see cref="QRFactor(double[],int,int,double[],double[])"/>. </param>
        /// <param name="rowsR">The number of rows in the A matrix.</param>
        /// <param name="columnsR">The number of columns in the A matrix.</param>
        /// <param name="tau">Contains additional information on Q. Only used for the native solver
        /// and can be <c>null</c> for the managed provider.</param>
        /// <param name="b">The B matrix.</param>
        /// <param name="columnsB">The number of columns of B.</param>
        /// <param name="x">On exit, the solution matrix.</param>
        /// <param name="method">The type of QR factorization to perform. <seealso cref="QRMethod"/></param>
        /// <remarks>Rows must be greater or equal to columns.</remarks>
        [SecuritySafeCritical]
        public override void QRSolveFactored(double[] q, double[] r, int rowsR, int columnsR, double[] tau, double[] b, int columnsB, double[] x, QRMethod method = QRMethod.Full)
        {
            var work = new double[columnsR*Control.BlockSize];
            QRSolveFactored(q, r, rowsR, columnsR, tau, b, columnsB, x, work, method);
        }

        /// <summary>
        /// Solves A*X=B for X using a previously QR factored matrix.
        /// </summary>
        /// <param name="q">The Q matrix obtained by QR factor. This is only used for the managed provider and can be
        /// <c>null</c> for the native provider. The native provider uses the Q portion stored in the R matrix.</param>
        /// <param name="r">The R matrix obtained by calling <see cref="QRFactor(double[],int,int,double[],double[])"/>. </param>
        /// <param name="rowsA">The number of rows in the A matrix.</param>
        /// <param name="columnsA">The number of columns in the A matrix.</param>
        /// <param name="tau">Contains additional information on Q. Only used for the native solver
        /// and can be <c>null</c> for the managed provider.</param>
        /// <param name="b">On entry the B matrix; on exit the X matrix.</param>
        /// <param name="columnsB">The number of columns of B.</param>
        /// <param name="x">On exit, the solution matrix.</param>
        /// <param name="work">The work array - only used in the native provider. The array must have a length of at least N,
        /// but should be N*blocksize. The blocksize is machine dependent. On exit, work[0] contains the optimal
        /// work size value.</param>
        /// <param name="method">The type of QR factorization to perform. <seealso cref="QRMethod"/></param>
        /// <remarks>Rows must be greater or equal to columns.</remarks>
        [SecuritySafeCritical]
        public override void QRSolveFactored(double[] q, double[] r, int rowsA, int columnsA, double[] tau, double[] b, int columnsB, double[] x, double[] work, QRMethod method = QRMethod.Full)
        {
            if (r == null)
            {
                throw new ArgumentNullException("r");
            }

            if (q == null)
            {
                throw new ArgumentNullException("q");
            }

            if (b == null)
            {
                throw new ArgumentNullException("q");
            }

            if (x == null)
            {
                throw new ArgumentNullException("q");
            }

            if (work == null)
            {
                throw new ArgumentNullException("work");
            }

            int rowsQ, columnsQ, rowsR, columnsR;
            if (method == QRMethod.Full)
            {
                rowsQ = columnsQ = rowsR = rowsA;
                columnsR = columnsA;
            }
            else
            {
                rowsQ = rowsA;
                columnsQ = rowsR = columnsR = columnsA;
            }

            if (r.Length != rowsR*columnsR)
            {
                throw new ArgumentException(string.Format(Resources.ArgumentArrayWrongLength, rowsR*columnsR), "r");
            }

            if (q.Length != rowsQ*columnsQ)
            {
                throw new ArgumentException(string.Format(Resources.ArgumentArrayWrongLength, rowsQ*columnsQ), "q");
            }

            if (b.Length != rowsA*columnsB)
            {
                throw new ArgumentException(string.Format(Resources.ArgumentArrayWrongLength, rowsA*columnsB), "b");
            }

            if (x.Length != columnsA*columnsB)
            {
                throw new ArgumentException(string.Format(Resources.ArgumentArrayWrongLength, columnsA*columnsB), "x");
            }

            if (work.Length < 1)
            {
                work[0] = rowsA*Control.BlockSize;
                throw new ArgumentException(Resources.WorkArrayTooSmall, "work");
            }

            if (method == QRMethod.Full)
            {
                SafeNativeMethods.d_qr_solve_factored(rowsA, columnsA, columnsB, r, b, tau, x, work, work.Length);
            }
            else
            {
                // we don't have access to the raw Q matrix any more(it is stored in R in the full QR), need to think about this.
                // let just call the managed version in the meantime. The heavy lifting has already been done. -marcus
                base.QRSolveFactored(q, r, rowsA, columnsA, tau, b, columnsB, x, QRMethod.Thin);
            }
        }

        /// <summary>
        /// Computes the singular value decomposition of A.
        /// </summary>
        /// <param name="computeVectors">Compute the singular U and VT vectors or not.</param>
        /// <param name="a">On entry, the M by N matrix to decompose. On exit, A may be overwritten.</param>
        /// <param name="rowsA">The number of rows in the A matrix.</param>
        /// <param name="columnsA">The number of columns in the A matrix.</param>
        /// <param name="s">The singular values of A in ascending value.</param>
        /// <param name="u">If <paramref name="computeVectors"/> is <c>true</c>, on exit U contains the left
        /// singular vectors.</param>
        /// <param name="vt">If <paramref name="computeVectors"/> is <c>true</c>, on exit VT contains the transposed
        /// right singular vectors.</param>
        /// <remarks>This is equivalent to the GESVD LAPACK routine.</remarks>
        [SecuritySafeCritical]
        public override void SingularValueDecomposition(bool computeVectors, double[] a, int rowsA, int columnsA, double[] s, double[] u, double[] vt)
        {
            if (a == null)
            {
                throw new ArgumentNullException("a");
            }

            if (s == null)
            {
                throw new ArgumentNullException("s");
            }

            if (u == null)
            {
                throw new ArgumentNullException("u");
            }

            if (vt == null)
            {
                throw new ArgumentNullException("vt");
            }

            if (u.Length != rowsA*rowsA)
            {
                throw new ArgumentException(Resources.ArgumentArraysSameLength, "u");
            }

            if (vt.Length != columnsA*columnsA)
            {
                throw new ArgumentException(Resources.ArgumentArraysSameLength, "vt");
            }

            if (s.Length != Math.Min(rowsA, columnsA))
            {
                throw new ArgumentException(Resources.ArgumentArraysSameLength, "s");
            }

            var work = new double[Math.Max((3*Math.Min(rowsA, columnsA)) + Math.Max(rowsA, columnsA), 5*Math.Min(rowsA, columnsA))];
            SingularValueDecomposition(computeVectors, a, rowsA, columnsA, s, u, vt, work);
        }

        /// <summary>
        /// Solves A*X=B for X using the singular value decomposition of A.
        /// </summary>
        /// <param name="a">On entry, the M by N matrix to decompose.</param>
        /// <param name="rowsA">The number of rows in the A matrix.</param>
        /// <param name="columnsA">The number of columns in the A matrix.</param>
        /// <param name="b">The B matrix.</param>
        /// <param name="columnsB">The number of columns of B.</param>
        /// <param name="x">On exit, the solution matrix.</param>
        public override void SvdSolve(double[] a, int rowsA, int columnsA, double[] b, int columnsB, double[] x)
        {
            if (a == null)
            {
                throw new ArgumentNullException("a");
            }

            if (b == null)
            {
                throw new ArgumentNullException("b");
            }

            if (x == null)
            {
                throw new ArgumentNullException("x");
            }

            if (b.Length != rowsA*columnsB)
            {
                throw new ArgumentException(Resources.ArgumentArraysSameLength, "b");
            }

            if (x.Length != columnsA*columnsB)
            {
                throw new ArgumentException(Resources.ArgumentArraysSameLength, "b");
            }

            var work = new double[Math.Max((3*Math.Min(rowsA, columnsA)) + Math.Max(rowsA, columnsA), 5*Math.Min(rowsA, columnsA))];
            var s = new double[Math.Min(rowsA, columnsA)];
            var u = new double[rowsA*rowsA];
            var vt = new double[columnsA*columnsA];

            var clone = new double[a.Length];
            a.Copy(clone);
            SingularValueDecomposition(true, clone, rowsA, columnsA, s, u, vt, work);
            SvdSolveFactored(rowsA, columnsA, s, u, vt, b, columnsB, x);
        }

        /// <summary>
        /// Computes the singular value decomposition of A.
        /// </summary>
        /// <param name="computeVectors">Compute the singular U and VT vectors or not.</param>
        /// <param name="a">On entry, the M by N matrix to decompose. On exit, A may be overwritten.</param>
        /// <param name="rowsA">The number of rows in the A matrix.</param>
        /// <param name="columnsA">The number of columns in the A matrix.</param>
        /// <param name="s">The singular values of A in ascending value.</param>
        /// <param name="u">If <paramref name="computeVectors"/> is <c>true</c>, on exit U contains the left
        /// singular vectors.</param>
        /// <param name="vt">If <paramref name="computeVectors"/> is <c>true</c>, on exit VT contains the transposed
        /// right singular vectors.</param>
        /// <param name="work">The work array. For real matrices, the work array should be at least
        /// Max(3*Min(M, N) + Max(M, N), 5*Min(M,N)). For complex matrices, 2*Min(M, N) + Max(M, N).
        /// On exit, work[0] contains the optimal work size value.</param>
        /// <remarks>This is equivalent to the GESVD LAPACK routine.</remarks>
        [SecuritySafeCritical]
        public override void SingularValueDecomposition(bool computeVectors, double[] a, int rowsA, int columnsA, double[] s, double[] u, double[] vt, double[] work)
        {
            if (a == null)
            {
                throw new ArgumentNullException("a");
            }

            if (s == null)
            {
                throw new ArgumentNullException("s");
            }

            if (u == null)
            {
                throw new ArgumentNullException("u");
            }

            if (vt == null)
            {
                throw new ArgumentNullException("vt");
            }

            if (work == null)
            {
                throw new ArgumentNullException("work");
            }

            if (u.Length != rowsA*rowsA)
            {
                throw new ArgumentException(Resources.ArgumentArraysSameLength, "u");
            }

            if (vt.Length != columnsA*columnsA)
            {
                throw new ArgumentException(Resources.ArgumentArraysSameLength, "vt");
            }

            if (s.Length != Math.Min(rowsA, columnsA))
            {
                throw new ArgumentException(Resources.ArgumentArraysSameLength, "s");
            }

            if (work.Length == 0)
            {
                throw new ArgumentException(Resources.ArgumentSingleDimensionArray, "work");
            }

            if (work.Length < Math.Max((3*Math.Min(rowsA, columnsA)) + Math.Max(rowsA, columnsA), 5*Math.Min(rowsA, columnsA)))
            {
                work[0] = Math.Max((3*Math.Min(rowsA, columnsA)) + Math.Max(rowsA, columnsA), 5*Math.Min(rowsA, columnsA));
                throw new ArgumentException(Resources.WorkArrayTooSmall, "work");
            }

            SafeNativeMethods.d_svd_factor(computeVectors, rowsA, columnsA, a, s, u, vt, work, work.Length);
        }

        /// <summary>
        /// Does a point wise add of two arrays <c>z = x + y</c>. This can be used
        /// to add vectors or matrices.
        /// </summary>
        /// <param name="x">The array x.</param>
        /// <param name="y">The array y.</param>
        /// <param name="result">The result of the addition.</param>
        /// <remarks>There is no equivalent BLAS routine, but many libraries
        /// provide optimized (parallel and/or vectorized) versions of this
        /// routine.</remarks>
        public override void AddArrays(double[] x, double[] y, double[] result)
        {
            if (y == null)
            {
                throw new ArgumentNullException("y");
            }

            if (x == null)
            {
                throw new ArgumentNullException("x");
            }

            if (x.Length != y.Length)
            {
                throw new ArgumentException(Resources.ArgumentArraysSameLength);
            }

            if (x.Length != result.Length)
            {
                throw new ArgumentException(Resources.ArgumentArraysSameLength);
            }

            SafeNativeMethods.d_vector_add(x.Length, x, y, result);
        }

        /// <summary>
        /// Does a point wise subtraction of two arrays <c>z = x - y</c>. This can be used
        /// to subtract vectors or matrices.
        /// </summary>
        /// <param name="x">The array x.</param>
        /// <param name="y">The array y.</param>
        /// <param name="result">The result of the subtraction.</param>
        /// <remarks>There is no equivalent BLAS routine, but many libraries
        /// provide optimized (parallel and/or vectorized) versions of this
        /// routine.</remarks>
        public override void SubtractArrays(double[] x, double[] y, double[] result)
        {
            if (y == null)
            {
                throw new ArgumentNullException("y");
            }

            if (x == null)
            {
                throw new ArgumentNullException("x");
            }

            if (x.Length != y.Length)
            {
                throw new ArgumentException(Resources.ArgumentArraysSameLength);
            }

            if (x.Length != result.Length)
            {
                throw new ArgumentException(Resources.ArgumentArraysSameLength);
            }

            SafeNativeMethods.d_vector_subtract(x.Length, x, y, result);
        }

        /// <summary>
        /// Does a point wise multiplication of two arrays <c>z = x * y</c>. This can be used
        /// to multiple elements of vectors or matrices.
        /// </summary>
        /// <param name="x">The array x.</param>
        /// <param name="y">The array y.</param>
        /// <param name="result">The result of the point wise multiplication.</param>
        /// <remarks>There is no equivalent BLAS routine, but many libraries
        /// provide optimized (parallel and/or vectorized) versions of this
        /// routine.</remarks>
        public override void PointWiseMultiplyArrays(double[] x, double[] y, double[] result)
        {
            if (y == null)
            {
                throw new ArgumentNullException("y");
            }

            if (x == null)
            {
                throw new ArgumentNullException("x");
            }

            if (x.Length != y.Length)
            {
                throw new ArgumentException(Resources.ArgumentArraysSameLength);
            }

            if (x.Length != result.Length)
            {
                throw new ArgumentException(Resources.ArgumentArraysSameLength);
            }

            SafeNativeMethods.d_vector_multiply(x.Length, x, y, result);
        }

        /// <summary>
        /// Does a point wise division of two arrays <c>z = x / y</c>. This can be used
        /// to divide elements of vectors or matrices.
        /// </summary>
        /// <param name="x">The array x.</param>
        /// <param name="y">The array y.</param>
        /// <param name="result">The result of the point wise division.</param>
        /// <remarks>There is no equivalent BLAS routine, but many libraries
        /// provide optimized (parallel and/or vectorized) versions of this
        /// routine.</remarks>
        public override void PointWiseDivideArrays(double[] x, double[] y, double[] result)
        {
            if (y == null)
            {
                throw new ArgumentNullException("y");
            }

            if (x == null)
            {
                throw new ArgumentNullException("x");
            }

            if (x.Length != y.Length)
            {
                throw new ArgumentException(Resources.ArgumentArraysSameLength);
            }

            if (x.Length != result.Length)
            {
                throw new ArgumentException(Resources.ArgumentArraysSameLength);
            }

            SafeNativeMethods.d_vector_divide(x.Length, x, y, result);
        }

        /// <summary>
        /// Computes the eigenvalues and eigenvectors of a matrix.
        /// </summary>
        /// <param name="isSymmetric">Wether the matrix is symmetric or not.</param>
        /// <param name="order">The order of the matrix.</param>
        /// <param name="matrix">The matrix to decompose. The lenth of the array must be order * order.</param>
        /// <param name="matrixEv">On output, the matrix contains the eigen vectors. The lenth of the array must be order * order.</param>
        /// <param name="vectorEv">On output, the eigen values (λ) of matrix in ascending value. The length of the arry must <paramref name="order"/>.</param>
        /// <param name="matrixD">On output, the block diagonal eigenvalue matrix. The lenth of the array must be order * order.</param>
        public override void EigenDecomp(bool isSymmetric, int order, double[] matrix, double[] matrixEv, Complex[] vectorEv, double[] matrixD)
        {
            if (matrix == null)
            {
                throw new ArgumentNullException("matrix");
            }

            if (matrix.Length != order*order)
            {
                throw new ArgumentException(String.Format(Resources.ArgumentArrayWrongLength, order*order), "matrix");
            }

            if (matrixEv == null)
            {
                throw new ArgumentNullException("matrixEv");
            }

            if (matrixEv.Length != order*order)
            {
                throw new ArgumentException(String.Format(Resources.ArgumentArrayWrongLength, order*order), "matrixEv");
            }

            if (vectorEv == null)
            {
                throw new ArgumentNullException("vectorEv");
            }

            if (vectorEv.Length != order)
            {
                throw new ArgumentException(String.Format(Resources.ArgumentArrayWrongLength, order), "vectorEv");
            }

            if (matrixD == null)
            {
                throw new ArgumentNullException("matrixD");
            }

            if (matrixD.Length != order*order)
            {
                throw new ArgumentException(String.Format(Resources.ArgumentArrayWrongLength, order*order), "matrixD");
            }

            SafeNativeMethods.d_eigen(isSymmetric, order, matrix, matrixEv, vectorEv, matrixD);
        }
    }
}

#endif
