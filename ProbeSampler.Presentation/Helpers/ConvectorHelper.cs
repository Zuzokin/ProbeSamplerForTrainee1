using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProbeSampler.Presentation.Helpers
{
    //TODO Добавить проверки на интервал пикселей (0,0) - (1920-1080) А надо ли?
    //TODO Можно добавить, чтобы проверялись только значения, которые находятся внутри сетки
    //TODO дописать комментарии к коду
    public static class ConvectorHelper
    {
        /// <summary>
        /// Принимает значение X в пикселях(1920x1080) и переводит их в линейные значения для пробоотборника
        /// </summary>
        /// <param name="xPixels"></param>
        /// <param name="isOver90Degrees"></param>
        /// <returns></returns>
        public static ushort ConvertPixelsXToLinearDisplacement(double xPixels, double? a, double? b, double CorrectionValue = 0, bool isOver90Degrees = false)
        {
            // y = ax + b
            //коэффициенты полученные из аппроксимации графика 

            if (a is null || b is null)
                throw new ArgumentException("Параметры a и b обязательны, но не были переданы в функцию");
            

            // Значение которое изменяет b, если угол поворота пробника больше 90 градусов
            ushort result = isOver90Degrees
                ? Convert.ToUInt16(Math.Round((xPixels - b.Value + CorrectionValue) / a.Value))
                : Convert.ToUInt16(Math.Round((xPixels - b.Value + CorrectionValue) / a.Value));

            //Проверка на диапазон значений
            //todo Передавать значения для верхнего диапазона, чтобы можно было для каждого пробоотборника указывать свой
            if (result < 0 || result > 15000)
                throw new ArgumentOutOfRangeException("После перевода получились неверные значения для пробника");

            return result;
        }
        /// <summary>
        /// Принимает значение Y в пикселях(1920x1080) и переводит их поворотные для пробоотборника
        /// </summary>
        /// <param name="yPixels"></param>
        /// <param name="isOver90Degrees"></param>
        /// <returns></returns>
        public static ushort ConvertPixelsYToRotation(double yPixels, double? a, double? b, double? c, bool isOver90Degrees = false)
        {
            // y = ax^2 + bx + c
            // коэффициенты полученные из аппроксимации графика
            if (a is null || b is null || c is null)
            {
                throw new ArgumentException("Параметры a, b, c обязательны, но не были переданы в функцию");
            }

            double discriminant = Math.Sqrt(Math.Abs(b.Value * b.Value - 4 * a.Value * (c.Value - yPixels)));

            var test = (-b.Value + discriminant) / (2 * a);
            var test2 = (-b.Value - discriminant) / (2 * a);
            //test = test*0.001;
            //test2 = test2*0.001;

            /*            ushort result = !isOver90Degrees
                            ? Convert.ToUInt16((-b.Value + discriminant) / (2 * a))
                            : Convert.ToUInt16((-b.Value - discriminant) / (2 * a));   */
            ushort result = isOver90Degrees
                ? Convert.ToUInt16(test)
                : Convert.ToUInt16(test2);

            //Проверка на диапазон значений
            //todo Передавать значения для верхнего диапазона, чтобы можно было для каждого пробоотборника указывать свой
            if (result < 0 || result > 15000)
                throw new ArgumentOutOfRangeException("После перевода получились неверные значения для пробника");

            return result;
        }
        public static double CalculateCorrectionValue(double rotation, int? beakCoeff = 0)
        {
            return beakCoeff.Value * Math.Cos((Math.PI / 180) * (rotation / 88.86));
        }
    }
}
