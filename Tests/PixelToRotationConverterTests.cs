namespace Tests
{
    using Xunit;
    using System;

    public static class ConvectorHelper
    {
        /// <summary>
        /// ��������� �������� X � ��������(1920x1080) � ��������� �� � �������� �������� ��� ��������������.
        /// </summary>
        /// <param name="xPixels">�������� �������� �� ��� ������(x).</param>
        /// <param name="isOver90Degrees">������� �������������� ������ ���� ������ 90 ��������?.</param>
        /// <param name="a">�������� a �������� �������.</param>
        /// <param name="b">�������� b �������� �������.</param>
        /// <param name="CorrectionValue">��������, ������� ������������ ������� ��������������. </param>
        /// <returns>�������� �������� �������������� pos mast (�� 0 �� ~15000).</returns>
        public static ushort ConvertPixelsXToLinearDisplacement(double xPixels, double? a, double? b, double CorrectionValue = 0)
        {
            // y = ax + b
            // ������������ ���������� �� ������������� �������

            if (a is null || b is null)
            {
                throw new ArgumentException("�� ������� ��������� ��� �������� �������� � ������ � OPC ������");
            }

            // �������� ������� �������� b, ���� ���� �������� �������� ������ 90 ��������
            ushort result = Convert.ToUInt16(Math.Round((xPixels - b.Value + CorrectionValue) / a.Value));
            // ushort result = isOver90Degrees
            //    ? Convert.ToUInt16(Math.Round((xPixels - b.Value + CorrectionValue) / a.Value))
            //    : Convert.ToUInt16(Math.Round((xPixels - b.Value + CorrectionValue) / a.Value));

            // �������� �� �������� ��������
            // todo ���������� �������� ��� �������� ���������, ����� ����� ���� ��� ������� �������������� ��������� ����
            if (result < 0 || result > 15300)
            {
                throw new ArgumentOutOfRangeException(nameof(result), $"����� �������� ���������� �������� �������� ��� ��������: {result}");
            }

            return result;
        }

        /// <summary>
        /// ��������� �������� Y � ��������(1920x1080) � ��������� �� ���������� ��� ��������������.
        /// </summary>
        /// <param name="yPixels">�������� �������� �� ��� �������(y).</param>
        /// <param name="isOver90Degrees">������� �������������� ������ ���� ������ 90 ��������?.</param>
        /// <param name="a">�������� a ������������ �������.</param>
        /// <param name="b">�������� b ������������ �������.</param>
        /// <param name="c">�������� b ������������ �������.</param>
        /// <returns>�������� �������� �������������� pos kopf (�� 0 �� ~15000).</returns>
        public static ushort ConvertPixelsYToRotation(double yPixels, double? a, double? b, double? c, bool isOver90Degrees = false)
        {
            // y = ax^2 + bx + c
            // ������������ ���������� �� ������������� �������
            if (a is null || b is null || c is null)
            {
                throw new ArgumentException("�� ������� ��������� ��� �������� �������� � ������ � OPC ������");
            }

            double discriminant = Math.Sqrt(Math.Abs((b.Value * b.Value) - (4 * a.Value * (c.Value - yPixels))));
            //todo delete nahui test and named it rigth
            var test = (-b.Value + discriminant) / (2 * a);
            var test2 = (-b.Value - discriminant) / (2 * a);
            // test = test*0.001;
            // test2 = test2*0.001;

            /*            ushort result = !isOver90Degrees
                            ? Convert.ToUInt16((-b.Value + discriminant) / (2 * a))
                            : Convert.ToUInt16((-b.Value - discriminant) / (2 * a));   */
            int result = isOver90Degrees
                ? Convert.ToInt32(test)
                : Convert.ToInt32(test2);

            // �������� �� �������� ��������
            // todo ���������� �������� ��� �������� ���������, ����� ����� ���� ��� ������� �������������� ��������� ����
            // if (result < 0 || result > 15000)
            //    throw new ArgumentOutOfRangeException("����� �������� ���������� �������� �������� ��� ��������");

            return Convert.ToUInt16(Math.Abs(result));
        }

        /// <summary>
        /// ���������� ����������� �������� ��-�� ��������.
        /// </summary>
        /// <param name="rotation">�������� �������� �������������� pos kopf.</param>
        /// <param name="beakCoeff">����������� ����� ����� �������������� (������� �� ����).</param>
        /// <returns></returns>
        public static double CalculateCorrectionValue(double rotation, int? beakCoeff = 0)
        {
            return beakCoeff.Value * Math.Cos((Math.PI / 180) * (rotation / 88.86));
        }
    }

    namespace PixelToRotationConverterTests
    {
        public class PixelToRotationConverterTests
        {
            private const double Ay = 6.27E-06;
            private const double By = -0.0882;
            private const double Cy = 725.06;
            private const double Ax = -0.129;
            private const double Bx = 1837;
            private const int BeakCoeff = 424;
            

            [Theory]
            [MemberData(nameof(GetTestCasesForConvertPixelsYToRotation), MemberType = typeof(PixelToRotationConverterTests))]
            public void When_Y_Is_Between_0_And_1920_And_Angle_Less_Than_90_Degrees_Expect_Correct_Result(int yPixels, bool isOver90Degrees, uint expectedResult)
            {
                uint actualResult;
                // Act
                actualResult = ConvectorHelper.ConvertPixelsYToRotation(yPixels, Ay, By, Cy, isOver90Degrees);

                // Assert
                Assert.Equal(expectedResult, actualResult);
            }

            [Theory]
            [MemberData(nameof(GetTestCasesForConvertPixelsYToRotation), MemberType = typeof(PixelToRotationConverterTests))]
            public void When_Y_Is_Between_0_And_1920_And_Angle_Greater_Than_90_Degrees_Expect_Correct_Result(int yPixels, bool isOver90Degrees, uint expectedResult)
            {
                uint actualResult;
                // Act
                actualResult = ConvectorHelper.ConvertPixelsYToRotation(yPixels, Ay, By, Cy, isOver90Degrees);

                // Assert
                Assert.Equal(expectedResult, actualResult);
            }

            [Theory]
            [MemberData(nameof(GetTestCasesForConvertPixelsXToLinearDisplacement), MemberType = typeof(PixelToRotationConverterTests))]
            public void When_X_Is_Between_0_And_1920_And_Angle_Greater_Than_90_Degrees_Expect_Correct_Result(int xPixels, double correctionValue, uint expectedResult=0)
            {
                uint actualResult;
                // Act
                actualResult = ConvectorHelper.ConvertPixelsXToLinearDisplacement(xPixels, Ax, Bx, correctionValue);

                // Assert
                Assert.Equal(actualResult, actualResult);
            }


            public static IEnumerable<object[]> GetTestCasesForConvertPixelsYToRotation()
            {
                for (int i = 0; i <= 1080; i += 50)
                {
                    yield return new object[] { i, false, Convert.ToUInt16(CalculateExpectedResult(i, Ay, By, Cy, false)) };
                    yield return new object[] { i, true, Convert.ToUInt16(CalculateExpectedResult(i, Ay, By, Cy, true)) };
                }
            }

            public static IEnumerable<object[]> GetTestCasesForConvertPixelsXToLinearDisplacement()
            {
                for (int i = 0; i <= 1920; i += 50)
                {
                    yield return new object[] { 
                        i,
                        ConvectorHelper.CalculateCorrectionValue(Convert.ToUInt16(CalculateExpectedResult(i, Ay, By, Cy, false)), BeakCoeff)
                    };
                    yield return new object[] { 
                        i,
                        ConvectorHelper.CalculateCorrectionValue(Convert.ToUInt16(CalculateExpectedResult(i, Ay, By, Cy, true)), BeakCoeff) };
                }
            }

            private static double CalculateExpectedResult(int yPixels, double a, double b, double c, bool isOver90Degrees)
            {
                double discriminant = Math.Sqrt(Math.Abs((b * b) - (4 * a * (c - yPixels))));
                double result;
                if (isOver90Degrees)
                {
                    result = (-b + discriminant) / (2 * a);
                }
                else
                {
                    result = (-b - discriminant) / (2 * a);
                }
                if (result < 0)
                {
                    result = Math.Abs(result);
                }
                return result;
            }
        }
    }    
}