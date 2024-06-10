using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ProbeSampler.WPF.Infrastructure
{
    public partial class CancelableProgressButton : UserControl
    {
        private object? originalContent;

        public ICommand Command
        {
            get { return (ICommand)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register("Command", typeof(ICommand), typeof(CancelableProgressButton), new PropertyMetadata(null, OnCommandChanged));

        private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as CancelableProgressButton;
            if (control != null)
            {
                control.btn.Command = (ICommand)e.NewValue;
            }
        }

        public object ButtonContent
        {
            get { return GetValue(ButtonContentProperty); }
            set { SetValue(ButtonContentProperty, value); }
        }

        public static readonly DependencyProperty ButtonContentProperty =
            DependencyProperty.Register("ButtonContent", typeof(object), typeof(CancelableProgressButton), new PropertyMetadata(null, OnButtonContentChanged));

        private static void OnButtonContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as CancelableProgressButton;
            if (control != null)
            {
                control.btn.Content = e.NewValue;
            }
        }

        private void btnCancel_MouseEnter(object sender, MouseEventArgs e)
        {
            originalContent = btn.Content;
            btn.Content = "Отмена";
        }

        private void btnCancel_MouseLeave(object sender, MouseEventArgs e)
        {
            btn.Content = originalContent;
        }

        public CancelableProgressButton()
        {
            InitializeComponent();
        }
    }
}
