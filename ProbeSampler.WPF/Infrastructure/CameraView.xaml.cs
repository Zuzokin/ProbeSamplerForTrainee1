using Emgu.CV;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ProbeSampler.WPF
{
    public partial class CameraView : IEnableLogger
    {
        private WriteableBitmap? writeableBitmapCache;

        public CameraView()
        {
            InitializeComponent();
            SetupBindings();
        }

        private void SetupBindings()
        {
            this.WhenActivated(d =>
            {
                this.WhenAnyValue(x => x.ViewModel)
                    .Where(x => x != null)
                    .Subscribe(x =>
                    {
                        try
                        {
                            x.WhenAnyValue(x => x.CapturedFrame)
                                .Where(x => x != null && !x.IsEmpty)
                                .ObserveOn(RxApp.MainThreadScheduler)
                                .Subscribe(mat =>
                                {
                                    var writeableBitmap = ConvertMatToWriteableBitmap(mat);
                                    CameraImage.Source = writeableBitmap;
                                })
                                .DisposeWith(d);
                        }
                        catch (AccessViolationException)
                        {
                            this.Log().Debug("Got AccessViolationException");
                        }
                    })
                    .DisposeWith(d);

                /*this.OneWayBind(ViewModel, vm => vm.CameraState, v => v.btnCancel.Content,
                    LocalizationHelper.ToDisplayText).DisposeWith(d);

                this.BindCommand(ViewModel, vm => vm.CancelConnectCommand, v => v.btnCancel).DisposeWith(d);*/
            });
        }

        private WriteableBitmap ConvertMatToWriteableBitmap(Mat mat)
        {
            if (writeableBitmapCache == null ||
                writeableBitmapCache.PixelWidth != mat.Width ||
                writeableBitmapCache.PixelHeight != mat.Height)
            {
                writeableBitmapCache = new WriteableBitmap(
                    mat.Width, mat.Height, 96, 96, PixelFormats.Bgr24, null);
            }

            writeableBitmapCache.Lock();

            byte[] tempData = new byte[mat.Width * mat.Height * mat.ElementSize];
            Marshal.Copy(mat.DataPointer, tempData, 0, tempData.Length);

            writeableBitmapCache.WritePixels(
                new Int32Rect(0, 0, mat.Width, mat.Height),
                tempData,
                mat.Width * mat.ElementSize,
                0);

            writeableBitmapCache.Unlock();

            return writeableBitmapCache;
        }
    }
}
