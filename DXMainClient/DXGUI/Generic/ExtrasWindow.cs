using ClientCore;
using ClientGUI;
using DTAClient.Domain;
using ClientCore.Extensions;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.Tools;
using System;
using System.Diagnostics;

namespace DTAClient.DXGUI.Generic
{
    public class ExtrasWindow : INItializableWindow
    {
        private StatisticsWindow statisticsWindow;

        public ExtrasWindow(WindowManager windowManager, StatisticsWindow statisticsWindow) : base(windowManager)
        {
            this.statisticsWindow = statisticsWindow;
        }

        public override void Initialize()
        {
            Name = "ExtrasWindow";
            ClientRectangle = new Rectangle(0, 0, 284, 190);
            BackgroundTexture = AssetLoader.LoadTexture("extrasMenu.png");

            base.Initialize();

            XNAClientButton btnExStatistics;
            btnExStatistics = FindChild<XNAClientButton>(nameof(btnExStatistics), true);
            if (btnExStatistics != null)
                btnExStatistics.LeftClick += BtnExStatistics_LeftClick;

            XNAClientButton btnExMapEditor;
            btnExMapEditor = FindChild<XNAClientButton>(nameof(btnExMapEditor), true);
            if (btnExMapEditor != null)
                btnExMapEditor.LeftClick += BtnExMapEditor_LeftClick;

            XNAClientButton btnExCredits;
            btnExCredits = FindChild<XNAClientButton>(nameof(btnExCredits), true);
            if (btnExCredits != null)
                btnExCredits.LeftClick += BtnExCredits_LeftClick;

            XNAClientButton btnExCancel;
            btnExCancel = FindChild<XNAClientButton>(nameof(btnExCancel), true);
            if (btnExCancel != null)
                btnExCancel.LeftClick += BtnExCancel_LeftClick;

            CenterOnParent();
        }

        private void BtnExStatistics_LeftClick(object sender, EventArgs e)
        {
            Disable();
            statisticsWindow.Enable();
        }

        private void BtnExMapEditor_LeftClick(object sender, EventArgs e)
        {
            OSVersion osVersion = ClientConfiguration.Instance.GetOperatingSystemVersion();
            using var mapEditorProcess = new Process();

            if (osVersion != OSVersion.UNIX)
                mapEditorProcess.StartInfo.FileName = SafePath.CombineFilePath(ProgramConstants.GamePath, ClientConfiguration.Instance.MapEditorExePath);
            else
                mapEditorProcess.StartInfo.FileName = SafePath.CombineFilePath(ProgramConstants.GamePath, ClientConfiguration.Instance.UnixMapEditorExePath);

            mapEditorProcess.StartInfo.UseShellExecute = false;

            mapEditorProcess.Start();

            Disable();
        }

        private void BtnExCredits_LeftClick(object sender, EventArgs e)
        {
            ProcessLauncher.StartShellProcess(ClientConfiguration.Instance.CreditsURL);
        }

        private void BtnExCancel_LeftClick(object sender, EventArgs e)
        {
            Disable();
        }
    }
}
