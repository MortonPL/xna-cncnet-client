using System;
using System.Xml.Linq;

using ClientCore;
using ClientCore.CnCNet5;
using ClientCore.Enums;
using ClientCore.Extensions;

using ClientGUI;

using ClientUpdater;

using DTAConfig.OptionPanels;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAConfig
{
    public class OptionsWindow : INItializableWindow
    {
        public OptionsWindow(WindowManager windowManager, GameCollection gameCollection) : base(windowManager)
        {
            this.gameCollection = gameCollection;
        }

        public event EventHandler OnForceUpdate;

        private XNAOptionsPanel[] optionsPanels;
        private XNAClientButton[] optionsButtons;
        private ComponentsPanel componentsPanel;

        private DisplayOptionsPanel displayOptionsPanel;
        private XNAControl topBar;

        private GameCollection gameCollection;

        private int currentTab = 0;
        private Texture2D savedButtonTexture;

        public override void Initialize()
        {
            Name = "OptionsWindow";
            ClientRectangle = new Rectangle(0, 0, 576, 475);
            BackgroundTexture = AssetLoader.LoadTextureUncached("optionsbg.png");

            base.Initialize();

            XNAClientButton btnCancel;
            btnCancel = FindChild<XNAClientButton>(nameof(btnCancel));
            btnCancel.LeftClick += BtnBack_LeftClick;

            XNAClientButton btnSave;
            btnSave = FindChild<XNAClientButton>(nameof(btnSave));
            btnSave.LeftClick += BtnSave_LeftClick;

            XNAClientButton btnTab1;
            btnTab1 = FindChild<XNAClientButton>(nameof(btnTab1));
            btnTab1.LeftClick += BtnTab1_LeftClick;

            XNAClientButton btnTab2;
            btnTab2 = FindChild<XNAClientButton>(nameof(btnTab2));
            btnTab2.LeftClick += BtnTab2_LeftClick;

            XNAClientButton btnTab3;
            btnTab3 = FindChild<XNAClientButton>(nameof(btnTab3));
            btnTab3.LeftClick += BtnTab3_LeftClick;

            XNAClientButton btnTab4;
            btnTab4 = FindChild<XNAClientButton>(nameof(btnTab4));
            btnTab4.LeftClick += BtnTab4_LeftClick;

            XNAClientButton btnTab5;
            btnTab5 = FindChild<XNAClientButton>(nameof(btnTab5));
            btnTab5.LeftClick += BtnTab5_LeftClick;

            XNAClientButton btnTab6;
            btnTab6 = FindChild<XNAClientButton>(nameof(btnTab6));
            btnTab6.LeftClick += BtnTab6_LeftClick;

            optionsButtons =
            [
                btnTab1,
                btnTab2,
                btnTab3,
                btnTab4,
                btnTab5,
                btnTab6,
            ];
            savedButtonTexture = btnTab1.IdleTexture;

            displayOptionsPanel = new DisplayOptionsPanel(WindowManager, UserINISettings.Instance);
            componentsPanel = new ComponentsPanel(WindowManager, UserINISettings.Instance);
            var updaterOptionsPanel = new UpdaterOptionsPanel(WindowManager, UserINISettings.Instance);
            updaterOptionsPanel.OnForceUpdate += (s, e) => { Disable(); OnForceUpdate?.Invoke(this, EventArgs.Empty); };

            optionsPanels = new XNAOptionsPanel[]
            {
                displayOptionsPanel,
                new AudioOptionsPanel(WindowManager, UserINISettings.Instance),
                new GameOptionsPanel(WindowManager, UserINISettings.Instance, topBar),
                new CnCNetOptionsPanel(WindowManager, UserINISettings.Instance, gameCollection),
                updaterOptionsPanel,
                componentsPanel
            };

            if (ClientConfiguration.Instance.ModMode || Updater.UpdateMirrors == null || Updater.UpdateMirrors.Count < 1)
            {
                btnTab5.Enabled = false;
                btnTab6.Enabled = false;
            }
            else if (Updater.CustomComponents == null || Updater.CustomComponents.Count < 1)
                btnTab6.Enabled = false;

            foreach (var panel in optionsPanels)
            {
                AddChild(panel);
                panel.Load();
                panel.Disable();
            }

            TabControl_SelectedIndexChanged(0);

            var iniFile = new CCIniFile(SafePath.CombineFilePath(ProgramConstants.GetResourcePath(), FormattableString.Invariant($"{Name}.ini")));
            foreach (var panel in optionsPanels)
                panel.ParseUserOptions(iniFile);

            CenterOnParent();
        }

        public void SetTopBar(XNAControl topBar) => this.topBar = topBar;

        private void BtnTab1_LeftClick(object sender, EventArgs e) => TabControl_SelectedIndexChanged(0);
        private void BtnTab2_LeftClick(object sender, EventArgs e) => TabControl_SelectedIndexChanged(1);
        private void BtnTab3_LeftClick(object sender, EventArgs e) => TabControl_SelectedIndexChanged(2);
        private void BtnTab4_LeftClick(object sender, EventArgs e) => TabControl_SelectedIndexChanged(3);
        private void BtnTab5_LeftClick(object sender, EventArgs e) => TabControl_SelectedIndexChanged(4);
        private void BtnTab6_LeftClick(object sender, EventArgs e) => TabControl_SelectedIndexChanged(5);

        private void TabControl_SelectedIndexChanged(int i)
        {
            optionsPanels[currentTab].Disable();
            optionsPanels[i].Enable();
            optionsPanels[i].RefreshPanel();

            optionsButtons[currentTab].IdleTexture = savedButtonTexture;
            savedButtonTexture = optionsButtons[i].IdleTexture;
            optionsButtons[i].IdleTexture = optionsButtons[i].HoverTexture;

            currentTab = i;
        }

        private void BtnBack_LeftClick(object sender, EventArgs e)
        {
            if (Updater.IsComponentDownloadInProgress())
            {
                var msgBox = new XNAMessageBox(WindowManager, "Downloads in progress".L10N("Client:DTAConfig:DownloadingTitle"),
                    ("Optional component downloads are in progress. The downloads will be cancelled if you exit the Options menu.\n\n" +
                    "Are you sure you want to continue?").L10N("Client:DTAConfig:DownloadingText"), XNAMessageBoxButtons.YesNo);
                msgBox.Show();
                msgBox.YesClickedAction = ExitDownloadCancelConfirmation_YesClicked;

                return;
            }

            WindowManager.SoundPlayer.SetVolume(Convert.ToSingle(UserINISettings.Instance.ClientVolume));
            Disable();
        }

        private void ExitDownloadCancelConfirmation_YesClicked(XNAMessageBox messageBox)
        {
            componentsPanel.CancelAllDownloads();
            WindowManager.SoundPlayer.SetVolume(Convert.ToSingle(UserINISettings.Instance.ClientVolume));
            Disable();
        }

        private void BtnSave_LeftClick(object sender, EventArgs e)
        {
            if (Updater.IsComponentDownloadInProgress())
            {
                XNAMessageBox msgBox = new XNAMessageBox(WindowManager, "Downloads in progress".L10N("Client:DTAConfig:DownloadingTitle"),
                      ("Optional component downloads are in progress. The downloads will be cancelled if you exit the Options menu.\n\n" +
                      "Are you sure you want to continue?").L10N("Client:DTAConfig:DownloadingText"), XNAMessageBoxButtons.YesNo);
                msgBox.Show();
                msgBox.YesClickedAction = SaveDownloadCancelConfirmation_YesClicked;

                return;
            }

            SaveSettings();
        }

        private void SaveDownloadCancelConfirmation_YesClicked(XNAMessageBox messageBox)
        {
            componentsPanel.CancelAllDownloads();

            SaveSettings();
        }

        private void SaveSettings()
        {
            if (RefreshOptionPanels())
                return;

            bool restartRequired = false;

            try
            {
                foreach (var panel in optionsPanels)
                    restartRequired = panel.Save() || restartRequired;

                UserINISettings.Instance.SaveSettings();
            }
            catch (Exception ex)
            {
                Logger.Log("Saving settings failed! Error message: " + ex.ToString());
                XNAMessageBox.Show(WindowManager, "Saving Settings Failed".L10N("Client:DTAConfig:SaveSettingFailTitle"),
                    "Saving settings failed! Error message:".L10N("Client:DTAConfig:SaveSettingFailText") + " " + ex.Message);
            }

            Disable();

            if (restartRequired)
            {
                var msgBox = new XNAMessageBox(WindowManager, "Restart Required".L10N("Client:DTAConfig:RestartClientTitle"),
                    ("The client needs to be restarted for some of the changes to take effect.\n\n" +
                    "Do you want to restart now?").L10N("Client:DTAConfig:RestartClientText"), XNAMessageBoxButtons.YesNo);
                msgBox.Show();
                msgBox.YesClickedAction = RestartMsgBox_YesClicked;
            }
        }

        private void RestartMsgBox_YesClicked(XNAMessageBox messageBox) => WindowManager.RestartGame();

        /// <summary>
        /// Refreshes the option panels to account for possible
        /// changes that could affect theirs functionality.
        /// Shows the popup to inform the user if needed.
        /// </summary>
        /// <returns>A bool that determines whether the 
        /// settings values were changed.</returns>
        private bool RefreshOptionPanels()
        {
            bool optionValuesChanged = false;

            foreach (var panel in optionsPanels)
                optionValuesChanged = panel.RefreshPanel() || optionValuesChanged;

            if (optionValuesChanged)
            {
                XNAMessageBox.Show(WindowManager, "Setting Value(s) Changed".L10N("Client:DTAConfig:SettingChangedTitle"),
                    ("One or more setting values are\n" +
                    "no longer available and were changed.\n\n" +
                    "You may want to verify the new setting\n" +
                    "values in client's options window.").L10N("Client:DTAConfig:SettingChangedText"));

                return true;
            }

            return false;
        }

        public void RefreshSettings()
        {
            foreach (var panel in optionsPanels)
                panel.Load();

            RefreshOptionPanels();

            foreach (var panel in optionsPanels)
                panel.Save();

            UserINISettings.Instance.SaveSettings();
        }

        public void Open()
        {
            foreach (var panel in optionsPanels)
                panel.Load();

            RefreshOptionPanels();

            componentsPanel.Open();

            Enable();
        }

        public void ToggleMainMenuOnlyOptions(bool enable)
        {
            foreach (var panel in optionsPanels)
            {
                panel.ToggleMainMenuOnlyOptions(enable);
            }
        }

        public void SwitchToCustomComponentsPanel() => TabControl_SelectedIndexChanged(6);

        public void InstallCustomComponent(int id) => componentsPanel.InstallComponent(id);

        public void PostInit()
        {
            if (ClientConfiguration.Instance.ClientGameType == ClientType.TS)
                displayOptionsPanel.PostInit();
        }
    }
}
