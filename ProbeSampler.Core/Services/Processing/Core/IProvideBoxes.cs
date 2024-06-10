namespace ProbeSampler.Core.Services.Processing.Core
{
    public interface IProvideBoxes
    {
        /// <summary>
        /// Получить текущие результаты.
        /// </summary>
        /// <returns></returns>
        IEnumerable<BoundingBox> GetDetections();
    }
}
