using Rampastring.XNAUI.XNAControls;
using Rampastring.XNAUI;
using Rampastring.Tools;
using System;
using ClientCore;
using ClientCore.Extensions;

namespace ClientGUI
{
    public class XNAClientButton : XNAButton, IToolTipContainer
    {
        public ToolTip ToolTip { get; private set; }

        private string _initialToolTipText;
        public string ToolTipText
        {
            get => Initialized ? ToolTip?.Text : _initialToolTipText;
            set
            {
                if (Initialized)
                    ToolTip.Text = value;
                else
                    _initialToolTipText = value;
            }
        }


        /// <summary>
        /// Is the mouce cursor hovering over this button?
        /// </summary>
        public bool Hovering { get; private set; } = false;

        public XNAClientButton(WindowManager windowManager) : base(windowManager)
        {
            FontIndex = 1;
            Height = UIDesignConstants.BUTTON_HEIGHT;
        }

        public override void Initialize()
        {
            int width = Width;

            if (IdleTexture == null)
                IdleTexture = AssetLoader.LoadTexture(width + "pxbtn.png");

            if (HoverTexture == null)
                HoverTexture = AssetLoader.LoadTexture(width + "pxbtn_c.png");

            if (HoverSoundEffect == null)
                HoverSoundEffect = new EnhancedSoundEffect("button.wav");

            base.Initialize();

            if (Width == 0)
                Width = IdleTexture.Width;

            ToolTip = new ToolTip(WindowManager, this) { Text = _initialToolTipText };
        }

        protected override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
        {
            if (key == "MatchTextureSize" && Conversions.BooleanFromString(value, false))
            {
                Width = IdleTexture.Width;
                Height = IdleTexture.Height;
                return;
            }
            else if (key == "ToolTip")
            {
                ToolTipText = value.FromIniString();
                return;
            }

            base.ParseControlINIAttribute(iniFile, key, value);
        }

        public override void OnMouseEnter()
        {
            base.OnMouseEnter();
            Hovering = true;
        }

        public override void OnMouseLeave()
        {
            base.OnMouseLeave();
            Hovering = false;
        }
    }
}
