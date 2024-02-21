using ClientCore;
using ClientCore.Statistics;
using ClientGUI;
using DTAClient.Domain;
using DTAClient.Domain.Multiplayer;
using ClientCore.Extensions;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.IO;

namespace DTAClient.DXGUI.Multiplayer
{
    /// <summary>
    /// An abstract base class for a multiplayer game loading lobby.
    /// </summary>
    public abstract class GameLoadingLobbyBase : INItializableWindow, ISwitchable
    {
        public GameLoadingLobbyBase(WindowManager windowManager, DiscordHandler discordHandler) : base(windowManager)
        {
            this.discordHandler = discordHandler;
        }

        public event EventHandler GameLeft;

        /// <summary>
        /// The list of players in the current saved game.
        /// </summary>
        protected List<SavedGamePlayer> SGPlayers = new List<SavedGamePlayer>();

        /// <summary>
        /// The list of players in the game lobby.
        /// </summary>
        protected List<PlayerInfo> Players = new List<PlayerInfo>();

        protected bool IsHost = false;

        protected DiscordHandler discordHandler;

        protected XNAClientDropDown ddSavedGame;

        protected ChatListBox lbChatMessages;
        protected XNATextBox tbChatInput;

        protected EnhancedSoundEffect sndGetReadySound;
        protected EnhancedSoundEffect sndJoinSound;
        protected EnhancedSoundEffect sndLeaveSound;
        protected EnhancedSoundEffect sndMessageSound;

        protected XNAPanel panelPlayers;
        protected XNALabel[] lblPlayerNames;

        protected XNALabel lblMapNameValue;
        protected XNALabel lblGameModeValue;

        protected XNAClientButton btnLoadGame;
        protected XNAClientButton btnLeaveGame;

        private List<MultiplayerColor> MPColors = new List<MultiplayerColor>();

        private string loadedGameID;

        private bool isSettingUp = false;
        private FileSystemWatcher fsw;

        private int uniqueGameId = 0;
        private DateTime gameLoadTime;

        public override void Initialize()
        {
            Name = nameof(GameLoadingLobbyBase);
            ClientRectangle = new Rectangle(0, 0, 590, 510);
            BackgroundTexture = AssetLoader.LoadTexture("loadmpsavebg.png");

            base.Initialize();

            panelPlayers = FindChild<XNAPanel>(nameof(panelPlayers));

            lblPlayerNames = new XNALabel[8];
            for (int i = 0; i < 8; i++)
            {
                XNALabel lblPlayerName = new XNALabel(WindowManager);
                lblPlayerName.Name = nameof(lblPlayerName) + i;

                if (i < 4)
                    lblPlayerName.ClientRectangle = new Rectangle(9, 9 + 30 * i, 0, 0);
                else
                    lblPlayerName.ClientRectangle = new Rectangle(190, 9 + 30 * (i - 4), 0, 0);

                lblPlayerName.Text = string.Format("Player {0}".L10N("Client:Main:PlayerX"), i) + " ";
                panelPlayers.AddChild(lblPlayerName);
                lblPlayerNames[i] = lblPlayerName;
            }

            lblMapNameValue = FindChild<XNALabel>(nameof(lblMapNameValue));
            lblGameModeValue = FindChild<XNALabel>(nameof(lblGameModeValue));
            ddSavedGame = FindChild<XNAClientDropDown>(nameof(ddSavedGame));
            ddSavedGame.SelectedIndexChanged += DdSavedGame_SelectedIndexChanged;
            lbChatMessages = FindChild<ChatListBox>(nameof(lbChatMessages));
            tbChatInput = FindChild<XNATextBox>(nameof(tbChatInput));
            tbChatInput.MaximumTextLength = 200;
            tbChatInput.EnterPressed += TbChatInput_EnterPressed;
            btnLoadGame = FindChild<XNAClientButton>(nameof(btnLoadGame));
            btnLoadGame.LeftClick += BtnLoadGame_LeftClick;
            btnLeaveGame = FindChild<XNAClientButton>(nameof(btnLeaveGame));
            btnLeaveGame.LeftClick += BtnLeaveGame_LeftClick;

            sndJoinSound = new EnhancedSoundEffect("joingame.wav", 0.0, 0.0, ClientConfiguration.Instance.SoundGameLobbyJoinCooldown);
            sndLeaveSound = new EnhancedSoundEffect("leavegame.wav", 0.0, 0.0, ClientConfiguration.Instance.SoundGameLobbyLeaveCooldown);
            sndMessageSound = new EnhancedSoundEffect("message.wav", 0.0, 0.0, ClientConfiguration.Instance.SoundMessageCooldown);
            sndGetReadySound = new EnhancedSoundEffect("getready.wav", 0.0, 0.0, ClientConfiguration.Instance.SoundGameLobbyGetReadyCooldown);

            MPColors = MultiplayerColor.LoadColors();

            WindowManager.CenterControlOnScreen(this);

            if (SavedGameManager.AreSavedGamesAvailable())
            {
                fsw = new FileSystemWatcher(SafePath.CombineDirectoryPath(ProgramConstants.GamePath, "Saved Games"), "*.NET");
                fsw.EnableRaisingEvents = false;
                fsw.Created += fsw_Created;
                fsw.Changed += fsw_Created;
            }
        }

        /// <summary>
        /// Updates Discord Rich Presence with actual information.
        /// </summary>
        /// <param name="resetTimer">Whether to restart the "Elapsed" timer or not</param>
        protected abstract void UpdateDiscordPresence(bool resetTimer = false);

        /// <summary>
        /// Resets Discord Rich Presence to default state.
        /// </summary>
        protected void ResetDiscordPresence() => discordHandler.UpdatePresence();

        private void BtnLeaveGame_LeftClick(object sender, EventArgs e) => LeaveGame();

        protected virtual void LeaveGame()
        {
            GameLeft?.Invoke(this, EventArgs.Empty);
            ResetDiscordPresence();
        }

        private void fsw_Created(object sender, FileSystemEventArgs e) =>
            AddCallback(new Action<FileSystemEventArgs>(HandleFSWEvent), e);

        private void HandleFSWEvent(FileSystemEventArgs e)
        {
            Logger.Log("FSW Event: " + e.FullPath);

            if (Path.GetFileName(e.FullPath) == "SAVEGAME.NET")
            {
                SavedGameManager.RenameSavedGame();
            }
        }

        private void BtnLoadGame_LeftClick(object sender, EventArgs e)
        {
            if (!IsHost)
            {
                RequestReadyStatus();
                return;
            }

            if (Players.Find(p => !p.Ready) != null)
            {
                GetReadyNotification();
                return;
            }

            if (Players.Count != SGPlayers.Count)
            {
                NotAllPresentNotification();
                return;
            }

            HostStartGame();
        }

        protected abstract void RequestReadyStatus();

        protected virtual void GetReadyNotification()
        {
            AddNotice("The game host wants to load the game but cannot because not all players are ready!".L10N("Client:Main:GetReadyPlease"));

            if (!IsHost && !Players.Find(p => p.Name == ProgramConstants.PLAYERNAME).Ready)
                sndGetReadySound.Play();
#if WINFORMS

            WindowManager.FlashWindow();
#endif
        }

        protected virtual void NotAllPresentNotification() =>
            AddNotice("You cannot load the game before all players are present.".L10N("Client:Main:NotAllPresent"));

        protected abstract void HostStartGame();

        protected void LoadGame()
        {
            FileInfo spawnFileInfo = SafePath.GetFile(ProgramConstants.GamePath, "spawn.ini");

            spawnFileInfo.Delete();

            File.Copy(SafePath.CombineFilePath(ProgramConstants.GamePath, "Saved Games", "spawnSG.ini"), spawnFileInfo.FullName);

            IniFile spawnIni = new IniFile(spawnFileInfo.FullName);

            int sgIndex = (ddSavedGame.Items.Count - 1) - ddSavedGame.SelectedIndex;

            spawnIni.SetStringValue("Settings", "SaveGameName",
                string.Format("SVGM_{0}.NET", sgIndex.ToString("D3")));
            spawnIni.SetBooleanValue("Settings", "LoadSaveGame", true);

            PlayerInfo localPlayer = Players.Find(p => p.Name == ProgramConstants.PLAYERNAME);

            if (localPlayer == null)
                return;

            spawnIni.SetIntValue("Settings", "Port", localPlayer.Port);

            for (int i = 1; i < Players.Count; i++)
            {
                string otherName = spawnIni.GetStringValue("Other" + i, "Name", string.Empty);

                if (string.IsNullOrEmpty(otherName))
                    continue;

                PlayerInfo otherPlayer = Players.Find(p => p.Name == otherName);

                if (otherPlayer == null)
                    continue;

                spawnIni.SetStringValue("Other" + i, "Ip", otherPlayer.IPAddress);
                spawnIni.SetIntValue("Other" + i, "Port", otherPlayer.Port);
            }

            WriteSpawnIniAdditions(spawnIni);
            spawnIni.WriteIniFile();

            FileInfo spawnMapFileInfo = SafePath.GetFile(ProgramConstants.GamePath, "spawnmap.ini");
            spawnMapFileInfo.Delete();
            using (var spawnMapStreamWriter = new StreamWriter(spawnMapFileInfo.FullName))
            {
                spawnMapStreamWriter.WriteLine("[Map]");
                spawnMapStreamWriter.WriteLine("Size=0,0,50,50");
                spawnMapStreamWriter.WriteLine("LocalSize=0,0,50,50");
                spawnMapStreamWriter.WriteLine();
            }

            gameLoadTime = DateTime.Now;

            GameProcessLogic.GameProcessExited += SharedUILogic_GameProcessExited;
            GameProcessLogic.StartGameProcess(WindowManager);

            fsw.EnableRaisingEvents = true;
            UpdateDiscordPresence(true);
        }

        private void SharedUILogic_GameProcessExited() =>
            AddCallback(new Action(HandleGameProcessExited), null);

        protected virtual void HandleGameProcessExited()
        {
            fsw.EnableRaisingEvents = false;

            GameProcessLogic.GameProcessExited -= SharedUILogic_GameProcessExited;

            var matchStatistics = StatisticsManager.Instance.GetMatchWithGameID(uniqueGameId);

            if (matchStatistics != null)
            {
                int oldLength = matchStatistics.LengthInSeconds;
                int newLength = matchStatistics.LengthInSeconds +
                    (int)(DateTime.Now - gameLoadTime).TotalSeconds;

                matchStatistics.ParseStatistics(ProgramConstants.GamePath,
                    ClientConfiguration.Instance.LocalGame, true);

                matchStatistics.LengthInSeconds = newLength;

                StatisticsManager.Instance.SaveDatabase();
            }
            UpdateDiscordPresence(true);
        }

        protected virtual void WriteSpawnIniAdditions(IniFile spawnIni)
        {
            // Do nothing by default
        }

        protected void AddNotice(string notice) => AddNotice(notice, Color.White);

        protected abstract void AddNotice(string message, Color color);

        /// <summary>
        /// Refreshes the UI  based on the latest saved game
        /// and information in the saved spawn.ini file, as well
        /// as information on whether the local player is the host of the game.
        /// </summary>
        public void Refresh(bool isHost)
        {
            isSettingUp = true;
            IsHost = isHost;

            SGPlayers.Clear();
            Players.Clear();
            ddSavedGame.Items.Clear();
            lbChatMessages.Clear();
            lbChatMessages.TopIndex = 0;

            ddSavedGame.AllowDropDown = isHost;
            btnLoadGame.Text = isHost ? "Load Game".L10N("Client:Main:ButtonLoadGame") : "I'm Ready".L10N("Client:Main:ButtonGetReady");

            IniFile spawnSGIni = new IniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, "Saved Games", "spawnSG.ini"));

            loadedGameID = spawnSGIni.GetStringValue("Settings", "GameID", "0");
            lblMapNameValue.Tag = spawnSGIni.GetStringValue("Settings", "UIMapName", string.Empty);
            lblMapNameValue.Text = ((string)lblGameModeValue.Tag).L10N($"INI:Maps:{spawnSGIni.GetStringValue("Settings", "MapID", string.Empty)}:Description");
            lblGameModeValue.Tag = spawnSGIni.GetStringValue("Settings", "UIGameMode", string.Empty);
            lblGameModeValue.Text = ((string)lblGameModeValue.Tag).L10N($"INI:GameModes:{(string)lblGameModeValue.Tag}:UIName");

            uniqueGameId = spawnSGIni.GetIntValue("Settings", "GameID", -1);

            int playerCount = spawnSGIni.GetIntValue("Settings", "PlayerCount", 0);

            SavedGamePlayer localPlayer = new SavedGamePlayer();
            localPlayer.Name = ProgramConstants.PLAYERNAME;
            localPlayer.ColorIndex = MPColors.FindIndex(
                c => c.GameColorIndex == spawnSGIni.GetIntValue("Settings", "Color", 0));

            SGPlayers.Add(localPlayer);

            for (int i = 1; i < playerCount; i++)
            {
                string sectionName = "Other" + i;

                SavedGamePlayer sgPlayer = new SavedGamePlayer();
                sgPlayer.Name = spawnSGIni.GetStringValue(sectionName, "Name", "Unknown player".L10N("Client:Main:UnknownPlayer"));
                sgPlayer.ColorIndex = MPColors.FindIndex(
                    c => c.GameColorIndex == spawnSGIni.GetIntValue(sectionName, "Color", 0));

                SGPlayers.Add(sgPlayer);
            }

            for (int i = 0; i < SGPlayers.Count; i++)
            {
                lblPlayerNames[i].Enabled = true;
                lblPlayerNames[i].Visible = true;
            }

            for (int i = SGPlayers.Count; i < 8; i++)
            {
                lblPlayerNames[i].Enabled = false;
                lblPlayerNames[i].Visible = false;
            }

            List<string> timestamps = SavedGameManager.GetSaveGameTimestamps();
            timestamps.Reverse(); // Most recent saved game first

            timestamps.ForEach(ts => ddSavedGame.AddItem(ts));

            if (ddSavedGame.Items.Count > 0)
                ddSavedGame.SelectedIndex = 0;

            CopyPlayerDataToUI();
            isSettingUp = false;
        }

        protected void CopyPlayerDataToUI()
        {
            for (int i = 0; i < SGPlayers.Count; i++)
            {
                SavedGamePlayer sgPlayer = SGPlayers[i];

                PlayerInfo pInfo = Players.Find(p => p.Name == SGPlayers[i].Name);

                XNALabel playerLabel = lblPlayerNames[i];

                if (pInfo == null)
                {
                    playerLabel.RemapColor = Color.Gray;
                    playerLabel.Text = sgPlayer.Name + " " + "(Not present)".L10N("Client:Main:NotPresentSuffix");
                    continue;
                }

                playerLabel.RemapColor = sgPlayer.ColorIndex > -1 ? MPColors[sgPlayer.ColorIndex].XnaColor
                    : Color.White;
                playerLabel.Text = pInfo.Ready ? sgPlayer.Name : sgPlayer.Name + " " + "(Not Ready)".L10N("Client:Main:NotReadySuffix");
            }
        }

        protected virtual string GetIPAddressForPlayer(PlayerInfo pInfo) => "0.0.0.0";

        private void DdSavedGame_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!IsHost)
                return;

            for (int i = 1; i < Players.Count; i++)
                Players[i].Ready = false;

            CopyPlayerDataToUI();

            if (!isSettingUp)
                BroadcastOptions();
            UpdateDiscordPresence();
        }

        private void TbChatInput_EnterPressed(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(tbChatInput.Text))
                return;

            SendChatMessage(tbChatInput.Text);
            tbChatInput.Text = string.Empty;
        }

        /// <summary>
        /// Override in a derived class to broadcast player ready statuses and the selected
        /// saved game to players.
        /// </summary>
        protected abstract void BroadcastOptions();

        protected abstract void SendChatMessage(string message);

        public override void Draw(GameTime gameTime)
        {
            Renderer.FillRectangle(new Rectangle(0, 0, WindowManager.RenderResolutionX, WindowManager.RenderResolutionY),
                new Color(0, 0, 0, 255));

            base.Draw(gameTime);
        }

        public void SwitchOn() => Enable();

        public void SwitchOff() => Disable();

        public abstract string GetSwitchName();
    }
}
