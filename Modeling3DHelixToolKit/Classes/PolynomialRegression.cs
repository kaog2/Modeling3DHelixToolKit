using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modeling3DHelixToolKit.Classes
{
    class PolynomialRegression
    {

        public PolynomialRegression() { }

        public double[] GetPolynomialRegresion(List<double> xPoint, List<double> yPoint, int degree)
        {
            int i, j, k, n, N;
            //List<double> xcoordinates = new List<double>();
            //List<double> ycoordinates = new List<double>();
            N = xPoint.Count();
            //xcoordinates = xPoint.ToList();
            //ycoordinates = yPoint.ToList();

            var x = xPoint.ToArray();
            var y = yPoint.ToArray();

            // n is the degree of Polynomial
            n = degree;

            //Array that will store the values of sigma(xi),sigma(xi^2),sigma(xi^3)....sigma(xi^2n)

            double[] X = new double[2 * n + 1];

            for (i = 0; i < 2 * n + 1; i++)
            {
                X[i] = 0;
                for (j = 0; j < N; j++)
				{
                    //consecutive positions of the array will store N,sigma(xi),sigma(xi^2),sigma(xi^3)....sigma(xi^2n)
                    X[i] = X[i] + Math.Pow(x[j], i);
                }
            }

            double[] a = new double[n + 1];
            //B is the Normal matrix(augmented) that will store the equations, 'a' is for value of the final coefficients
            double[][] B = new double[n + 1][];// n + 1 size rows

            for (int p = 0; p < n + 1; ++p)
			{
                //profil.XPoint.Count() columns per row
                B[p] = new double[n + 2];
            }
                

            for (i = 0; i <= n; i++)
			{
                for (j = 0; j <= n; j++)
				{
                    //Build the Normal matrix by storing the corresponding coefficients at the right positions except the last column of the matrix
                    B[i][j] = X[i + j];
                }
            }

            //Array to store the values of sigma(yi),sigma(xi*yi),sigma(xi^2*yi)...sigma(xi^n*yi)

            double[] Y = new double[n + 1];
            for (i = 0; i < n + 1; i++)
            {
                Y[i] = 0;
                for (j = 0; j < N; j++)
				{
                    //consecutive positions will store sigma(yi),sigma(xi*yi),sigma(xi^2*yi)...sigma(xi^n*yi)
                    Y[i] = Y[i] + Math.Pow(x[j], i) * y[j];
                }
            }

            //load the values of Y as the last column of B(Normal Matrix but augmented)
            for (i = 0; i <= n; i++)
			{
                B[i][n + 1] = Y[i];
            }

            //n is made n+1 because the Gaussian Elimination part below was for n equations, but here n is the degree of polynomial and for n degree we get n+1 equations
            n = n + 1;

            //From now Gaussian Elimination starts(can be ignored) to solve the set of linear equations (Pivotisation)
            for (i = 0; i < n; i++) 
            {
                for (k = i + 1; k < n; k++)
				{
                    if (B[i][i] < B[k][i])
					{
                        for (j = 0; j <= n; j++)
                        {
                            double temp = B[i][j];
                            B[i][j] = B[k][j];
                            B[k][j] = temp;
                        }
                    }   
                }   
            }

            //loop to perform the gauss elimination
            for (i = 0; i < n - 1; i++)
			{
                //make the elements below the pivot elements equal to zero or elimnate the variables
                for (k = i + 1; k < n; k++)
                {
                    double t = B[k][i] / B[i][i];
                    for (j = 0; j <= n; j++)
                        B[k][j] = B[k][j] - t * B[i][j];
                }
            }

            //back-substitution
            for (i = n - 1; i >= 0; i--)
            {
                //x is an array whose values correspond to the values of x,y,z..
                //make the variable to be calculated equal to the rhs of the last equation
                a[i] = B[i][n];
                for (j = 0; j < n; j++)
				{
                    //then subtract all the lhs values except the coefficient of the variable whose value
                    ////is being calculated
                    if (j != i) {
                        a[i] = a[i] - B[i][j] * a[j];
                    }            
                        
                }

                //now finally divide the rhs by the coefficient of the variable to be calculated
                a[i] = a[i] / B[i][i];
            }

            return a;
        }

    }
}
