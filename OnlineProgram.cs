using System;
using System.Windows.Forms;

namespace ClashOfDrayven
{
    internal static class OnlineProgram
    {
        [STAThread]
        private static void Main()
        {
            ApplicationConfiguration.Initialize();
            AssetPackRuntime.Prepare();
            DryTheme.Initialize();
            SoundBank.Initialize();

            using (var splash = new DrySplashForm())
                splash.ShowDialog();

            var api = new DrayvenApiClient(AppConfig.ServerBaseUrl);
            var token = SessionStore.LoadToken();
            AuthResult auth = null;

            if (!string.IsNullOrWhiteSpace(token))
            {
                api.Token = token;
                try
                {
                    var profile = api.GetProfile();
                    if (profile != null && profile.Ok)
                        auth = new AuthResult { Ok = true, Token = token, User = profile.User };
                }
                catch { SessionStore.Clear(); api.Token = null; }
            }

            if (auth == null)
            {
                using (var form = new AuthForm(api))
                {
                    if (form.ShowDialog() != DialogResult.OK || form.Result == null)
                        return;
                    auth = form.Result;
                }
            }

            api.Token = auth.Token;
            SessionStore.SaveToken(auth.Token);

            var state = GameState.Load();
            try { OnlineStateBridge.DownloadInto(api, state.SaveData); }
            catch (Exception ex)
            {
                MessageBox.Show("Server state could not be loaded.\n\n" + ex.Message,
                    "Clash Of Drayven", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (var game = new EnhancedGameForm(state, api, auth.User))
                Application.Run(game);
        }
    }
}
