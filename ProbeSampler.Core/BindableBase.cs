using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ProbeSampler.Core
{
    /// <summary>
    /// Реализация <see cref="INotifyPropertyChanged"/>.
    /// </summary>
    public class BindableBase : INotifyPropertyChanged
    {
        /// <summary>
        /// Событие, указывающие на изменение свойства
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Установка нового значения свойства, при условии, что оно не равно текущему,
        /// а также вызов события <see cref="PropertyChanged"/>.
        /// </summary>
        /// <typeparam name="T">Тип свойства.</typeparam>
        /// <param name="member">Внутреннее приватное свойство.</param>
        /// <param name="value">Новое значение.</param>
        /// <param name="propertyName">Наименование свойста(опционально).</param>
        protected virtual bool SetProperty<T>(ref T member, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(member, value))
            {
                return false;
            }

            member = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// Вызов события <see cref="PropertyChanged"/>.
        /// </summary>
        /// <param name="propertyName">Наименование свойтсва.</param>
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
