namespace ProbeSampler.Presentation.Helpers
{
    // TODO Добавить проверки на интервал пикселей (0,0) - (1920-1080) А надо ли?
    // TODO Можно добавить, чтобы проверялись только значения, которые находятся внутри сетки
    // TODO дописать комментарии к коду
    /// <summary>
    /// Класс, для конвертации пикселей экрана (1920-1080) в значения для пробоотборника.
    /// </summary>
    public static class ConvectorHelper
    {
        /// <summary>
        /// Принимает значение X в пикселях(1920x1080) и переводит их в линейные значения для пробоотборника.
        /// </summary>
        /// <param name="xPixels">значение пикселей по оси абцисс(x).</param>
        /// <param name="isOver90Degrees">Поворот пробоотборника должен быть больше 90 градусов?.</param>
        /// <param name="a">параметр a линейной функции.</param>
        /// <param name="b">параметр b линейной функции.</param>
        /// <param name="CorrectionValue">Значение, которое компенсирует поворот пробоотборника. </param>
        /// <returns>значение смещения пробоотборника pos mast (от 0 до ~15000).</returns>
        public static ushort ConvertPixelsXToLinearDisplacement(double xPixels, double? a, double? b, double CorrectionValue = 0, bool isOver90Degrees = false)
        {
            // y = ax + b
            // коэффициенты полученные из аппроксимации графика

            if (a is null || b is null)
            {
                throw new ArgumentException("Не указаны параметры для перевода значений с экрана в OPC сервер");
            }

            // Значение которое изменяет b, если угол поворота пробника больше 90 градусов
            ushort result = Convert.ToUInt16(Math.Round((xPixels - b.Value + CorrectionValue) / a.Value));
            // ushort result = isOver90Degrees
            //    ? Convert.ToUInt16(Math.Round((xPixels - b.Value + CorrectionValue) / a.Value))
            //    : Convert.ToUInt16(Math.Round((xPixels - b.Value + CorrectionValue) / a.Value));

            // Проверка на диапазон значений
            // todo Передавать значения для верхнего диапазона, чтобы можно было для каждого пробоотборника указывать свой
            if (result < 0 || result > 15000)
            {
                throw new ArgumentOutOfRangeException(nameof(result), "После перевода получились неверные значения для пробника");
            }

            return result;
        }

        /// <summary>
        /// Принимает значение Y в пикселях(1920x1080) и переводит их поворотные для пробоотборника.
        /// </summary>
        /// <param name="yPixels">значение пикселей по оси ординат(y).</param>
        /// <param name="isOver90Degrees">Поворот пробоотборника должен быть больше 90 градусов?.</param>
        /// <param name="a">параметр a квадратичной функции.</param>
        /// <param name="b">параметр b квадратичной функции.</param>
        /// <param name="c">параметр b квадратичной функции.</param>
        /// <returns>значение поворота пробоотборника pos kopf (от 0 до ~15000).</returns>
        public static ushort ConvertPixelsYToRotation(double yPixels, double? a, double? b, double? c, bool isOver90Degrees = false)
        {
            // y = ax^2 + bx + c
            // коэффициенты полученные из аппроксимации графика
            if (a is null || b is null || c is null)
            {
                throw new ArgumentException("Не указаны параметры для перевода значений с экрана в OPC сервер");
            }

            double discriminant = Math.Sqrt(Math.Abs((b.Value * b.Value) - (4 * a.Value * (c.Value - yPixels))));

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

            // Проверка на диапазон значений
            // todo Передавать значения для верхнего диапазона, чтобы можно было для каждого пробоотборника указывать свой
            // if (result < 0 || result > 15000)
            //    throw new ArgumentOutOfRangeException("После перевода получились неверные значения для пробника");

            return Convert.ToUInt16(Math.Abs(result));
        }

        /// <summary>
        /// Вычисление компенсации смещения из-за поворота.
        /// </summary>
        /// <param name="rotation">Значение поворота пробоотборника pos kopf.</param>
        /// <param name="beakCoeff">Коэффициент длины клюва пробоотборника (получен на глаз).</param>
        /// <returns></returns>
        public static double CalculateCorrectionValue(double rotation, int? beakCoeff = 0)
        {
            return beakCoeff.Value * Math.Cos((Math.PI / 180) * (rotation / 88.86));
        }
    }
}
