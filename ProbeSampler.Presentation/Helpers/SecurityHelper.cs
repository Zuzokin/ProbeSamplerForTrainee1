using System.Threading.Tasks;

namespace ProbeSampler.Presentation.Helpers
{
    public static class SecurityHelper
    {
        private static string GetDialogIdentifier(ViewModelBase vm)
        {
            string identifier = "RootDialog";

            foreach (var key in DialogHelper.ChildWindows.Keys)
            {
                if (DialogHelper.ChildWindows[key].Contains(vm))
                {
                    return key.Id;
                }
            }

            return identifier;
        }

        public static async Task<bool> SetAdminPassword(string dialogIdentifier = "RootDialog")
        {
            return await DialogHelper.ShowDialog<SetAdminPasswordViewModel>(dialogIdentifier);
        }

        public static async Task<bool> ChangeAdminPassword(string dialogIdentifier = "RootDialog")
        {
            return await DialogHelper.ShowDialog<ChangeAdminPasswordViewModel>(dialogIdentifier);
        }

        public static async Task<bool> RequestAdminPassword(string dialogIdentifier = "RootDialog")
        {
            return await DialogHelper.ShowDialog<AdminPasswordRequestViewModel>(dialogIdentifier);
        }

        public static async Task<bool> RequestAdminPassword(this ViewModelBase vm)
        {
            return await RequestAdminPassword(GetDialogIdentifier(vm));
        }

        public static async Task<bool> ChangeAdminPassword(this ViewModelBase vm)
        {
            return await ChangeAdminPassword(GetDialogIdentifier(vm));
        }

        public static async Task<bool> SetAdminPassword(this ViewModelBase vm)
        {
            return await SetAdminPassword(GetDialogIdentifier(vm));
        }
    }
}
