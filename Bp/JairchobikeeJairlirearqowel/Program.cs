// See https://aka.ms/new-console-template for more information

using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

const double x0 = 0.35;
const double x1 = 0.9;

const double y_out = 0.5;

var a = x0;
var b = x1;

var w11 = 0.1;
var w12 = 0.8;

var w21 = 0.4;
var w22 = 0.6;

var weightCount = 2;
var nodeCount = 2;

var input = Matrix.Build.SparseOfRowArrays
 (
[
    [x0],
    [x1],
]);


var layer1Weight = Matrix.Build.SparseOfRows(nodeCount, weightCount,
[
    [w11, w12],
    [w21, w22],
]);

var w31 = 0.3;
var w32 = 0.9;

var layer2NodeCount = 1;
var layer2Weight = Matrix.Build.SparseOfRows(layer2NodeCount, weightCount,
[
    [w31, w32],
]);

var count = 0;

while (true)
{
    //var z0 = w11 * a + w12 * b;
    //var z1 = w21 * a + w22 * b;

    // layer1 2x2
    // input 2x1
    // z1Matrix 2x1
    // ����� z1Matrix ��ʾ z0 �� z1 ��ɵľ���
    Matrix<double> z1Matrix = layer1Weight.Multiply(input);

    //var y0 = F(z0);
    //var y1 = F(z1);

    // y1Matrix 2x1
    // ����� y1Matrix ��ʾ y0 �� y1 ��ɵľ��󣬼� z0 �� z1 ��ɵ� z1Matrix ���󾭹��˼���� F ֮��Ľ��
    Matrix<double> y1Matrix = FMatrix(z1Matrix);

    //var z2 = w31 * y0 + w32 * y1;
    // layer2 1x2
    // z2Matrix 1x1
    // ����� z2Matrix ���� z2 ��ֵ����Ϊ��һ�� 1x1 �ľ���
    var z2Matrix = layer2Weight.Multiply(y1Matrix);
    //if (z2Matrix.RowCount == 1 && z2Matrix.ColumnCount == 1)
    //{
    //    z2 = z2Matrix[0, 0];
    //}

    // y2 ��������������
    var y2 = F(z2Matrix[0, 0]);

    var c = C(y2);

    if (c < 0.0000001)
    {
        break;
    }

    double dc_dz2 = (y2 - y_out) * (y2 * (1 - y2)); // dc/dy2 * dy2/dz2

    //var dc_dw31 = dc_dz2 * y0;
    //var dc_dw32 = dc_dz2 * y1;

    // Ϊ���ܹ��� dc_dw3132Matrix ���ӵ� layer2 1x2 �����ϣ���Ҫ�Ƚ� y1Matrix ת��Ϊ 1x2 �������� dc_dz2 ���
    var dc_dw31w32Matrix = dc_dz2 * y1Matrix.Transpose(); // dc/dy2 * dy2/dz2 * dy2/|dw31,32| = dc/|dw31,dw32|
    Matrix<double> layer2Delta = dc_dw31w32Matrix; // 1x2 ����

    // 2x1
    var y1MatrixDerivative = y1Matrix.Map(x => x * (1 - x)); // y1 �ĵ�������

    //var dc_dw11 = (y2 - y_out) * (y2 * (1 - y2)) * w31 * (y0 * (1 - y0)) * a;
    //var dc_dw12 = (y2 - y_out) * (y2 * (1 - y2)) * w31 * (y0 * (1 - y0)) * b;

    //var dc_dw21 = (y2 - y_out) * (y2 * (1 - y2)) * w32 * (y1 * (1 - y1)) * a;
    //var dc_dw22 = (y2 - y_out) * (y2 * (1 - y2)) * w32 * (y1 * (1 - y1)) * b;

    // ���� layer2 �� 1x2 �ľ������� layer1Error Ҳ�� 1x2 �ľ���
    // dc/dy2 * dy2/dz2 * dz2/|dy0,dy1| = dc/|dy0,dy1|
    Matrix<double> dc_dy0y1Matrix = dc_dz2 * layer2Weight; // ���򴫲�
    // dc/dy2 * dy2/dz2 * dz2/|dy0,dy1|* |dy0,dy1|/|dz0,dz1| = dc/|dz0,dz1|
    var dc_dz0z1Matrix = dc_dy0y1Matrix.Transpose().PointwiseMultiply(y1MatrixDerivative); // ���
    // layer1Delta ����
    // | dc_dz2 * layer2[0, 0] * y1MatrixD[0, 0] |
    // | dc_dz2 * layer2[0, 1] * y1MatrixD[1, 0] |
    // =
    // | (y2 - y_out) * (y2 * (1 - y2)) * w31 * (y0 * (1 - y0)) |
    // | (y2 - y_out) * (y2 * (1 - y2)) * w32 * (y1 * (1 - y1)) |

    var dc_dw11 = dc_dz2 * layer2Weight[0, 0] * y1MatrixDerivative[0, 0] * input[0, 0];
    var dc_dw12 = dc_dz2 * layer2Weight[0, 0] * y1MatrixDerivative[0, 0] * input[1, 0];

    var dc_dw21 = dc_dz2 * layer2Weight[0, 1] * y1MatrixDerivative[1, 0] * input[0, 0];
    var dc_dw22 = dc_dz2 * layer2Weight[0, 1] * y1MatrixDerivative[1, 0] * input[1, 0];

    var dLayer1Matrix = Matrix.Build.SparseOfRows(nodeCount, weightCount,
    [
        [dc_dw11, dc_dw12],
        [dc_dw21, dc_dw22],
    ]);

    // dc/|dz0,dz1| * |dz0,dz1|/|dw11,w12| = dc/|dw11,w12|
    // input �� 2x1 �ģ�������Ҫ���� 1x2 �ģ����ܺ� dc_dz0z1Matrix ��� 2x1 �ľ�����ˣ��õ� 2x2 �ľ���
    var dc_dw11d12Matrix = dc_dz0z1Matrix * input.Transpose();
    if (dc_dw11d12Matrix.Equals(dLayer1Matrix))
    {
        // ֤���������ֶ�����Ľ����һ����
    }
    var layer1Delta = dc_dw11d12Matrix; // 2x2 ����

    //w31 = w31 - dc_dw31;
    //w32 = w32 - dc_dw32;

    layer2Weight = layer2Weight - layer2Delta;

    //w11 = w11 - dc_dw11;
    //w12 = w12 - dc_dw12;

    //w21 = w21 - dc_dw21;
    //w22 = w22 - dc_dw22;

    layer1Weight = layer1Weight - layer1Delta;

    count++;
}

Console.WriteLine("Hello, World!");

double F(double x)
{
    return 1.0 / (1 + Math.Pow(Math.E, -x));
}

Matrix<double> FMatrix(Matrix<double> x)
{
    return x.Map(F);
}

static double C(double y2)
{
    return 1.0 / 2 * Math.Pow((y2 - y_out), 2);
}

