using System.Reactive.Linq;
using System.Windows;
using System.Windows.Forms;
using DynamicData;
using Emgu.CV;
using MaterialDesignThemes.Wpf;
using ProbeSampler.WPF.Infrastructure;

namespace ProbeSampler.WPF
{
    public partial class CalibrationView
    {
        private ReadOnlyObservableCollection<LoadedImageData>? loadedItems;
        readonly OpenFileDialog openFileDialog = new();

        public CalibrationView()
        {
            InitializeComponent();

            openFileDialog.Filter = "Image files (*.jpg;*.bmp;*.png)|*.jpg;*.bmp;*.png";
            openFileDialog.Multiselect = true;

            SetupBindings();
        }

        private void SetupBindings()
        {
            this.WhenActivated(d =>
            {
                this.OneWayBind(ViewModel, vm => vm.ViewMode, v => v.Transitioner.SelectedIndex, (CalibrationViewMode val) => { return (int)val; });
                this.OneWayBind(ViewModel, vm => vm.CameraVM, v => v.CameraView.ViewModel).DisposeWith(d);
                this.OneWayBind(ViewModel, vm => vm.CancelConnectCommand, v => v.btnCancel.Command).DisposeWith(d);
                this.OneWayBind(ViewModel, vm => vm.CameraVM.CameraState, v => v.btnCancel.ButtonContent, LocalizationHelper.ToDisplayText).DisposeWith(d);
                this.BindCommand(ViewModel, vm => vm.LoadImageFilesCommand, v => v.btnAddImages).DisposeWith(d);
                this.BindCommand(ViewModel, vm => vm.GoBackCommand, v => v.btnGoBack).DisposeWith(d);
                this.BindCommand(ViewModel, vm => vm.ProceedToCalibrationCommand, v => v.btnProceed).DisposeWith(d);
                this.BindCommand(ViewModel, vm => vm.ClearLoadedImagesCommand, v => v.btnClearLoadedImages).DisposeWith(d);
                this.BindInteraction(ViewModel, vm => vm.LoadFileConfirm,
                    context => Observable.Return(openFileDialog.ShowDialog())
                    .Do(result => context.SetOutput(result.Equals(DialogResult.OK) ? openFileDialog.FileNames : new List<string>())

                )).DisposeWith(d);

                this.Bind(ViewModel, vm => vm.PatternHeight, v => v.intPatternHeight.Text).DisposeWith(d);
                this.Bind(ViewModel, vm => vm.PatternWidth, v => v.intPatternWidth.Text).DisposeWith(d);
                this.Bind(ViewModel, vm => vm.SquareSize, v => v.floatSquareSize.Text).DisposeWith(d);
                this.Bind(ViewModel, vm => vm.Alpha, v => v.alphaSlider.Value).DisposeWith(d);
                this.Bind(ViewModel, vm => vm.RotationValue, v => v.rotationSlider.Value).DisposeWith(d);
                this.OneWayBind(ViewModel, vm => vm.CameraVM.CameraState, v => v.btnCancel.Visibility, (state) =>
                {
                    return state == CameraState.Connecting ? Visibility.Visible : Visibility.Collapsed;
                });


                this.ViewModel?.SourceCacheImageData
                    .Connect()
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Transform(it => new LoadedImageData { Id = it.Id, Name = it.Name, ImageMat = it.ImageMat, IsPatternFounded = it.IsPatternFounded })
                    .Bind(out loadedItems)
                    .Subscribe()
                    .DisposeWith(d);
                this.WhenAnyValue(x => x.loadedItems)
                    .BindTo(this, x => x.MatList.ItemsSource)
                    .DisposeWith(d);
                this.ViewModel.WhenAnyValue(x => x.IsCurentlyLoading)
                    .Subscribe(isLoading =>
                    {
                        if (isLoading)
                        {
                            var view = new CancelableProgressButton
                            {
                                Command = DialogHost.CloseDialogCommand,
                                ButtonContent = "Загрузка"
                            };
                            DialogHost.Show(view, "RootDialog", DialogClosingEventHandler);
                        }
                        else
                        {
                            DialogHost.CloseDialogCommand.Execute(null, this);
                        }
                    })
                    .DisposeWith(d);
            });
        }

        private void DialogClosingEventHandler(object sender, DialogClosingEventArgs eventArgs)
        {
            if (this.ViewModel != null && this.ViewModel.IsCurentlyLoading)
            {
                this.ViewModel.CancelImageLoadingCommand?.Execute().Subscribe();
            }
        }

        class LoadedImageData
        {
            public Guid Id { get; set; }
            public UMat? ImageMat { get; set; }
            public string? Name { get; set; }
            public bool IsPatternFounded { get; set; }
        }
    }
}
