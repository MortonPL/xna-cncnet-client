using ClientGUI;
using ClientCore.Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.IO;
using System.Reflection;
using ClientCore.CnCNet5;
using SixLabors.ImageSharp;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace DTAClient.DXGUI.Multiplayer.CnCNet
{
    /// <summary>
    /// A box that allows users to make a choice,
    /// top-left of the game window.
    /// </summary>
    public class ChoiceNotificationBox : INItializableWindow
    {
        private const double DOWN_TIME_WAIT_SECONDS = 4.0;
        private const double DOWN_MOVEMENT_RATE = 2.0;
        private const double UP_MOVEMENT_RATE = 2.0;

        public ChoiceNotificationBox(WindowManager windowManager) : base(windowManager)
        {
            downTimeWaitTime = TimeSpan.FromSeconds(DOWN_TIME_WAIT_SECONDS);
        }

        public Action<ChoiceNotificationBox> AffirmativeClickedAction { get; set; }
        public Action<ChoiceNotificationBox> NegativeClickedAction { get; set; }

        private XNALabel lblHeader;
        private XNAPanel gameIconPanel;
        private XNALabel lblSender;
        private XNALabel lblChoiceText;
        private XNAClientButton btnYes;
        private XNAClientButton btnNo;

        private TimeSpan downTime = TimeSpan.Zero;

        private TimeSpan downTimeWaitTime;

        private bool isDown = false;

        private const int boxHeight = 101;

        private double locationY = -boxHeight;

        public override void Initialize()
        {
            Name = nameof(ChoiceNotificationBox);
            BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 196), 1, 1);

            base.Initialize();

            lblHeader = FindChild<XNALabel>(nameof(lblHeader));
            gameIconPanel = FindChild<XNAPanel>(nameof(gameIconPanel));
            lblSender = FindChild<XNALabel>(nameof(lblSender));
            lblChoiceText = FindChild<XNALabel>(nameof(lblChoiceText));
            btnYes = FindChild<XNAClientButton>(nameof(btnYes));
            btnYes.LeftClick += AffirmativeButton_LeftClick;
            btnNo = FindChild<XNAClientButton>(nameof(btnNo));
            btnNo.LeftClick += NegativeButton_LeftClick;
        }

        // a timeout of zero means the notification will never be automatically dismissed
        public void Show(
            string headerText,
            Texture2D gameIcon,
            string sender,
            string choiceText,
            string affirmativeText,
            string negativeText,
            int timeout = 0)
        {
            Enable();

            lblHeader.Text = headerText;
            gameIconPanel.BackgroundTexture = gameIcon;
            lblSender.Text = sender;
            lblChoiceText.Text = choiceText;
            btnYes.Text = affirmativeText;
            btnNo.Text = negativeText;

            // use the same clipping logic as the PM notification
            if (lblChoiceText.Width > Width)
            {
                while (lblChoiceText.Width > Width)
                {
                    lblChoiceText.Text = lblChoiceText.Text.Remove(lblChoiceText.Text.Length - 1);
                }
            }

            downTime = TimeSpan.Zero;
            isDown = true;

            downTimeWaitTime = TimeSpan.FromSeconds(timeout);
        }

        public void Hide()
        {
            isDown = false;
            locationY = -Height;
            ClientRectangle = new Rectangle(X, (int)locationY,
                Width, Height);
            Disable();
        }

        public override void Update(GameTime gameTime)
        {
            if (isDown)
            {
                if (locationY < 0)
                {
                    locationY += DOWN_MOVEMENT_RATE;
                    ClientRectangle = new Rectangle(X, (int)locationY,
                        Width, Height);
                }

                if (WindowManager.HasFocus)
                {
                    downTime += gameTime.ElapsedGameTime;

                    // only change our "down" state if we have a valid timeout
                    if (downTimeWaitTime != TimeSpan.Zero)
                    {
                        isDown = downTime < downTimeWaitTime;
                    }
                }
            }
            else
            {
                if (locationY > -Height)
                {
                    locationY -= UP_MOVEMENT_RATE;
                    ClientRectangle = new Rectangle(X, (int)locationY, Width, Height);
                }
                else
                {
                    // effectively delete ourselves when we've timed out
                    WindowManager.RemoveControl(this);
                }
            }

            base.Update(gameTime);
        }

        private void AffirmativeButton_LeftClick(object sender, EventArgs e)
        {
            AffirmativeClickedAction?.Invoke(this);
            WindowManager.RemoveControl(this);
        }

        private void NegativeButton_LeftClick(object sender, EventArgs e)
        {
            NegativeClickedAction?.Invoke(this);
            WindowManager.RemoveControl(this);
        }
    }
}
