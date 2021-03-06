﻿using System.Collections.Generic;
using System.IO;
using System.Runtime.Remoting.Messaging;
using System.Text;
using ParallelTasks;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using VRage;
using VRage.FileSystem;
using VRage.Game;
using VRage.Game.ObjectBuilders.Campaign;
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.Screens
{
    public class MyGuiScreenNewGame : MyGuiScreenBase
    {
        #region Campaign List

        private MyGuiControlList                m_campaignList;
        private MyGuiControlRadioButtonGroup    m_campaignTypesGroup;
        private MyObjectBuilder_Campaign        m_selectedCampaign;

        #endregion


        #region Right side of the screen

        private MyLayoutTable               m_tableLayout;

        private MyGuiControlLabel           m_nameLabel;
        private MyGuiControlTextbox         m_nameTextbox;

        private MyGuiControlLabel           m_difficultyLabel;
        private MyGuiControlCombobox        m_difficultyCombo;

        private MyGuiControlLabel           m_onlineModeLabel;
        private MyGuiControlCheckbox        m_onlineMode;

        private MyGuiControlMultilineText   m_descriptionMultilineText;

        private MyGuiControlButton          m_publishButton;

        #endregion

        float MARGIN_TOP = 0.12f;
        float MARGIN_BOTTOM = 0.12f;
        float MARGIN_LEFT = 0.3f;
        float MARGIN_RIGHT =0.075f;

        public MyGuiScreenNewGame()
            : base(new Vector2(0.5f, 0.5f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, new Vector2(0.9f, 0.97f))
        {
            EnabledBackgroundFade = true;

            RecreateControls(true);
        }

        public override string GetFriendlyName()
        {
            return "New Game";
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);
            // Mini loading screen
            MyGuiSandbox.AddScreen(
                new MyGuiScreenProgressAsync(
                    MyCommonTexts.LoadingPleaseWait,
                    null,
                    RunRefreshAsync,
                    (result, async) => { RefreshCampaignList(); MyScreenManager.CloseScreen(typeof(MyGuiScreenProgressAsync)); }
                )
            );

            AddCaption(MyCommonTexts.ScreenMenuButtonCampaign);

            InitCampaignList();
            InitRightSide();

            m_campaignTypesGroup.SelectByKey(0);
        }

        public override bool Update(bool hasFocus)
        {
            m_publishButton.Enabled = m_selectedCampaign != null && m_selectedCampaign.IsLocalMod;

            return base.Update(hasFocus);
        }

        private void InitCampaignList()
        {
            Vector2 originL = -m_size.Value / 2 + new Vector2(0.07f, MARGIN_TOP);

            m_campaignTypesGroup = new MyGuiControlRadioButtonGroup();
            m_campaignTypesGroup.SelectedChanged += CampaignSelectionChanged;

            m_campaignList = new MyGuiControlList
            {
              OriginAlign  = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
              Position = originL,
              Size = new Vector2(0.19f, m_size.Value.Y - 0.03f - MARGIN_TOP),
            };

            var separator = new MyGuiControlSeparatorList()
            {
                Size = new Vector2(0.03f, m_campaignList.Size.Y),
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                Position = m_campaignList.Position + new Vector2(m_campaignList.Size.X + 0.015f, 0f)
            };
            separator.AddVertical(new Vector2(0f, -m_campaignList.Size.Y/2), separator.Size.Y);

            Controls.Add(separator);
            Controls.Add(m_campaignList);
        }

        private void CampaignSelectionChanged(MyGuiControlRadioButtonGroup args)
        {
            var button = args.SelectedButton as MyGuiControlCampaignButton;
            if (button != null)
            {
                var campaign = button.UserData as MyObjectBuilder_Campaign;
                if(campaign == null) return;

                m_nameTextbox.Text = campaign.Name;
                m_onlineMode.IsChecked = campaign.IsMultiplayer;
                m_descriptionMultilineText.Text = new StringBuilder(campaign.Description);

                if(!string.IsNullOrEmpty(campaign.Difficulty))
                {
                    if(campaign.Difficulty.StartsWith("Easy"))
                        m_difficultyCombo.SelectItemByIndex(0);
                    else if(campaign.Difficulty.StartsWith("Normal"))
                        m_difficultyCombo.SelectItemByIndex(1);
                    else 
                        m_difficultyCombo.SelectItemByIndex(2);
                }

                m_selectedCampaign = campaign;
            }
        }

        private void InitRightSide()
        {
            var originL = -m_size.Value / 2 + new Vector2(MARGIN_LEFT, MARGIN_TOP);

            var screenSize = m_size.Value;
            var layoutSize = new Vector2(screenSize.X / 2 - originL.X, screenSize.Y - MARGIN_TOP - MARGIN_BOTTOM) - new Vector2(0.05f, 0.05f);
            var columnWidthLabel = layoutSize.X * 0.4f;
            var columnWidthControl = layoutSize.X - columnWidthLabel;
            var rowHeight = 0.052f;
            var leftHeight = layoutSize.Y - 4 * rowHeight;
            var descriptionMargin = 0.005f;

            m_tableLayout = new MyLayoutTable(this, originL, layoutSize);
            m_tableLayout.SetColumnWidthsNormalized(columnWidthLabel, columnWidthControl);
            m_tableLayout.SetRowHeightsNormalized(rowHeight, rowHeight, rowHeight, rowHeight, leftHeight);

            // Name
            m_nameLabel = new MyGuiControlLabel
            {
                Text = MyTexts.GetString(MyCommonTexts.Name),
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER,
            };

            m_nameTextbox = new MyGuiControlTextbox
            {
                MaxLength = MySession.MAX_NAME_LENGTH,
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER,
                Enabled = false
            };

            // Difficulty
            m_difficultyLabel = new MyGuiControlLabel
            {
                Text = MyTexts.GetString(MySpaceTexts.Difficulty),
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP
            };

            m_difficultyCombo = new MyGuiControlCombobox
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER,
                Enabled = false
            };
            m_difficultyCombo.AddItem((int)0, MySpaceTexts.DifficultyEasy);
            m_difficultyCombo.AddItem((int)1, MySpaceTexts.DifficultyNormal);
            m_difficultyCombo.AddItem((int)2, MySpaceTexts.DifficultyHard);

            // Online
            m_onlineModeLabel = new MyGuiControlLabel
            {
                Text = MyTexts.GetString(MyCommonTexts.WorldSettings_OnlineMode),
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP
            };
            m_onlineMode = new MyGuiControlCheckbox
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER,
                Enabled = false
            };

            // Description
            m_descriptionMultilineText = new MyGuiControlMultilineText(
                    selectable: false)
            {
                Name = "BriefingMultilineText",
                Position = new Vector2(-0.009f, -0.115f),
                Size = new Vector2(0.419f, 0.412f),
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                TextAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                TextBoxAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP
            };

            var panel = new MyGuiControlCompositePanel
            {
                BackgroundTexture = MyGuiConstants.TEXTURE_RECTANGLE_DARK,
            };


            m_tableLayout.Add(m_nameLabel, MyAlignH.Left, MyAlignV.Center, 0, 0);
            m_tableLayout.Add(m_difficultyLabel, MyAlignH.Left, MyAlignV.Center, 2, 0);
            m_tableLayout.Add(m_onlineModeLabel, MyAlignH.Left, MyAlignV.Center, 3, 0);

            m_tableLayout.AddWithSize(m_nameTextbox, MyAlignH.Left, MyAlignV.Center, 0, 1);
            m_tableLayout.AddWithSize(m_difficultyCombo, MyAlignH.Left, MyAlignV.Center, 2, 1);
            m_tableLayout.AddWithSize(m_onlineMode, MyAlignH.Left, MyAlignV.Center, 3, 1);

            m_tableLayout.AddWithSize(panel, MyAlignH.Left, MyAlignV.Top, 4, 0, 1, 2);
            m_tableLayout.AddWithSize(m_descriptionMultilineText, MyAlignH.Left, MyAlignV.Top, 4, 0, 1, 2);

            // Panel offset from text
            var bothSidesOffset = 0.003f;
            panel.Position = new Vector2(panel.PositionX - bothSidesOffset, panel.PositionY - bothSidesOffset);
            panel.Size = new Vector2(panel.Size.X + bothSidesOffset, panel.Size.Y + bothSidesOffset * 2);

            // The bulgarian constant is offset from side to match the size of the layout
            var buttonsOrigin = panel.Position + new Vector2(panel.Size.X, panel.Size.Y + 0.02f);
            var buttonSize = MyGuiConstants.BACK_BUTTON_SIZE;
            var buttonHorizontalSpacing = 0.02f;
            // Ok/Cancel/Publish
            m_publishButton = new MyGuiControlButton(position: buttonsOrigin, size: buttonSize, text: MyTexts.Get(MyCommonTexts.LoadScreenButtonPublish), onButtonClick: OnPublishButtonOnClick, originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP);
            var cancelButton = new MyGuiControlButton(position: buttonsOrigin + new Vector2(0, buttonSize.Y), size: buttonSize, text: MyTexts.Get(MyCommonTexts.Cancel), onButtonClick: OnCancelButtonClick, originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP);
            var okButton = new MyGuiControlButton(position: buttonsOrigin + new Vector2(-buttonSize.X - buttonHorizontalSpacing, buttonSize.Y), size: buttonSize, text: MyTexts.Get(MyCommonTexts.Ok), onButtonClick: OnOkButtonClicked, originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP);

            Controls.Add(m_publishButton);
            Controls.Add(okButton);
            Controls.Add(cancelButton);

            CloseButtonEnabled = true;
        }

        private void OnPublishButtonOnClick(MyGuiControlButton myGuiControlButton)
        {
            if(m_selectedCampaign == null) return;
            
            MyCampaignManager.Static.SwitchCampaign(m_selectedCampaign.Name, m_selectedCampaign.IsVanilla, m_selectedCampaign.IsLocalMod);
            MyScreenManager.AddScreen(
                    MyGuiSandbox.CreateMessageBox(
                        styleEnum: MyMessageBoxStyleEnum.Info,
                        buttonType: MyMessageBoxButtonsType.YES_NO,
                        messageText: MyTexts.Get(MyCommonTexts.MessageBoxTextDoYouWishToPublishCampaign),
                        messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionDoYouWishToPublishCampaign),
                        callback: (e) => MyCampaignManager.Static.PublishActive()
                    )
                );
        }

        private void OnOkButtonClicked(MyGuiControlButton myGuiControlButton)
        {
            if(m_selectedCampaign == null) return;

            MyCampaignManager.Static.SwitchCampaign(m_selectedCampaign.Name, m_selectedCampaign.IsVanilla, m_selectedCampaign.IsLocalMod);
            MyCampaignManager.Static.RunNewCampaign();
        }

        private void OnCancelButtonClick(MyGuiControlButton myGuiControlButton)
        {
            CloseScreen();
        }

        private void RefreshCampaignList()
        {
            var campaigns = MyCampaignManager.Static.Campaigns;
            List<MyObjectBuilder_Campaign> vanilla = new List<MyObjectBuilder_Campaign>();
            List<MyObjectBuilder_Campaign> localMods = new List<MyObjectBuilder_Campaign>();
            List<MyObjectBuilder_Campaign> mods = new List<MyObjectBuilder_Campaign>();

            foreach (var campaign in campaigns)
            {
                if(campaign.IsVanilla)
                    vanilla.Add(campaign);
                else if(campaign.IsLocalMod)
                    localMods.Add(campaign);
                else
                    mods.Add(campaign);
            }

            m_campaignList.Controls.Clear();
            m_campaignTypesGroup.Clear();

            AddSeparator("Official");

            foreach (var campaign in vanilla)
            {
                var button = new MyGuiControlCampaignButton(campaign.Name, GetImagePath(campaign));
                button.UserData = campaign;
                m_campaignTypesGroup.Add(button);
                m_campaignList.Controls.Add(button);
            }

            if(mods.Count > 0)
                AddSeparator("Workshop");

            foreach (var campaign in mods)
            {
                var button = new MyGuiControlCampaignButton(campaign.Name, GetImagePath(campaign));
                button.UserData = campaign;
                button.IsWorkshopMod = true;
                m_campaignTypesGroup.Add(button);
                m_campaignList.Controls.Add(button);
            }

            if(localMods.Count > 0)
                AddSeparator("Local");

            foreach (var campaign in localMods)
            {
                var button = new MyGuiControlCampaignButton(campaign.Name, GetImagePath(campaign));
                button.UserData = campaign;
                button.IsLocalMod = true;
                m_campaignTypesGroup.Add(button);
                m_campaignList.Controls.Add(button);
            }
        }

        private string GetImagePath(MyObjectBuilder_Campaign campaign)
        {
            string imagePath = campaign.ImagePath;

            if(string.IsNullOrEmpty(campaign.ImagePath))
                return string.Empty;

            if (!campaign.IsVanilla)
            {
                imagePath = Path.Combine(campaign.ModFolderPath, campaign.ImagePath);
                if (!MyFileSystem.FileExists(imagePath))
                {
                    imagePath = Path.Combine(MyFileSystem.ContentPath, campaign.ImagePath);
                }
            }

            return imagePath;
        }

        private void AddSeparator(string sectionName)
        {
            var panel = new MyGuiControlCompositePanel()
            {
                BackgroundTexture = MyGuiConstants.TEXTURE_RECTANGLE_DARK,
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                Position = Vector2.Zero
            };

            var label = new MyGuiControlLabel()
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                Text = sectionName,
                Font = MyFontEnum.Blue,
                PositionX = 0.005f
            };

            var imageOffset = 0.003f;
            var colorMask = MyGuiConstants.THEMED_GUI_LINE_COLOR;
            var img = new MyGuiControlImage(textures: new[] { @"Textures\GUI\FogSmall3.dds" })
            {
                Size = new Vector2(label.Size.X + imageOffset * 10, 0.007f),
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                //ColorMask = new Vector4(0,0,0,1),
                ColorMask = colorMask.ToVector4(),
                Position = new Vector2(-imageOffset, label.Size.Y)
            };

            var parent = new MyGuiControlParent
            {
                Size = new Vector2(m_campaignList.Size.X, label.Size.Y),
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                Position = Vector2.Zero
            };

            panel.Size = parent.Size + new Vector2(-0.035f, 0.01f);

            panel.Position -= parent.Size / 2 - new Vector2(-0.01f, 0f);
            label.Position -= parent.Size / 2;
            img.Position -= parent.Size / 2;

            parent.Controls.Add(panel);
            parent.Controls.Add(img);
            parent.Controls.Add(label);

            m_campaignList.Controls.Add(parent);
        }

        private AsyncCampaingLoader RunRefreshAsync()
        {
            return new AsyncCampaingLoader();
        }

        class AsyncCampaingLoader : IMyAsyncResult
        {
            public bool IsCompleted { get { return this.Task.IsComplete; } }
            public Task Task { get; private set; }

            public AsyncCampaingLoader()
            {
                Task = Parallel.Start(MyCampaignManager.Static.RefreshModData);
            }
        }
    }
}
