using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Menus
{
    class TitleUI : Menu
    {
        public SceneTitle Scene;
        TitleMenu TitleMenu;
        Menu SubMenu;

        public TitleUI(SceneTitle scene)
        {
            Scene = scene;

            TitleMenu = new TitleMenu(this);
        }

        public override bool IsMouseOver(int x, int y)
        {
            if (SubMenu != null)
                return SubMenu.IsMouseOver(x, y);
            return base.IsMouseOver(x, y);
        }

        public override void Update(Scene scene)
        {
            if (SubMenu != null)
            {
                SubMenu.Update(scene);
            }

            TitleMenu.Update(scene);

            base.Update(scene);
        }

        public override void HandleInput(Scene scene)
        {
            base.HandleInput(scene);

            InputTwinState state = Scene.InputState;

            if (SubMenu != null)
            {
                SubMenu.HandleInput(scene);
                if (SubMenu.ShouldClose)
                    SubMenu = null;
                return;
            }

            TitleMenu.HandleInput(scene);
        }

        public override void PreDraw(Scene scene)
        {
            base.PreDraw(scene);

            if (SubMenu != null)
                SubMenu.PreDraw(scene);
            if (TitleMenu != null)
                TitleMenu.PreDraw(scene);
        }

        public override void Draw(Scene scene)
        {
            if (TitleMenu != null)
            {
                TitleMenu.Draw(scene);
            }

            if (SubMenu != null)
            {
                SubMenu.Draw(scene);
            }
        }

        public void Open(Menu menu)
        {
            SubMenu = menu;
        }
    }

    class TitleMenu : MenuTextSelection
    {
        public TitleMenu(TitleUI ui) : base(String.Empty, new Vector2(ui.Scene.Viewport.Width / 2, ui.Scene.Viewport.Height / 2), 256, 8)
        {
            Add(new ActAction("New Game", "", () =>
            {
                ui.Scene.NewGame();
            }));
            Add(new ActAction("Load Game", "", () =>
            {
                ui.Open(new SaveGameSelection(ui.Scene, SaveFile.SaveDirectory));
            }));
        }
    }

    class SaveGameSelection : MenuAct
    {
        class SaveFileAction : ActAction
        {
            SaveGameSelection Menu;
            public SaveFile SaveFile;

            public override string Name
            {
                get
                {
                    return SaveFile.Cached ? $"{SaveFile.Name}{Game.FORMAT_BOLD} - {Path.DirectorySeparatorChar}{SaveFile.FileName}" : SaveFile.FileName;
                }
            }

            public override string Description
            {
                get
                {
                    return SaveFile.Cached ? $"Last played {Game.FORMAT_BOLD}{SaveFile.LastPlayedTime.ToString()}" : string.Empty;
                }
            }

            public SaveFileAction(SaveGameSelection menu, SaveFile saveFile) : base(null, null, null, null)
            {
                Menu = menu;
                SaveFile = saveFile;

                Action = Select;
                Enabled = IsEnabled;
            }

            private void Select()
            {
                Menu.SelectSaveGame(SaveFile);
            }

            private bool IsEnabled()
            {
                return SaveFile.Cached;
            }
        }

        SceneTitle Scene;
        DirectoryInfo Directory;
        List<ActAction> Actions = new List<ActAction>();

        InfoBox InfoWindow;
        MenuTextSelection SaveGameActions;

        SaveFile SelectedSaveGame;

        public override int SelectionCount => Actions.Count;
        public override int LineHeight => 32;
        IEnumerable<SaveFile> SaveFiles => Actions.OfType<SaveFileAction>().Select(x => x.SaveFile);

        public SaveGameSelection(SceneTitle scene, DirectoryInfo directory) : base("Select Savegame", new Vector2(scene.Viewport.Width * 1 / 4, scene.Viewport.Height / 2), 320, 8)
        {
            Scene = scene;
            Directory = directory;
            Reset();

            InfoWindow = new InfoBox(() => "Preview", () => GetDescription(), new Vector2(scene.Viewport.Width * 3 / 4, scene.Viewport.Height / 2), 320, 20 * 16);
        }

        public string GetDescription()
        {
            var currentAction = Actions[Selection];

            if(currentAction is SaveFileAction saveFileAction)
            {
                SaveFile saveFile = saveFileAction.SaveFile;
                if(saveFile.Cached)
                {
                    string description = string.Empty;
                    saveFile.AddDescription(ref description);
                    return description;
                }
            }

            return string.Empty;
        }

        public void SelectSaveGame(SaveFile saveFile)
        {
            SelectedSaveGame = saveFile;
            SaveGameActions = new MenuTextSelection(null, new Vector2(Scene.Viewport.Width / 2, Scene.Viewport.Height / 2), 256, 4);
            SaveGameActions.Add(new ActAction("Load", "", () => { Scene.LoadGame(SelectedSaveGame); }));
            SaveGameActions.AddDefault(new ActAction("Cancel", "", () => { SaveGameActions.Close(); }));
        }

        public void Reset()
        {
            Actions.Clear();
            if (!Directory.Exists)
                Directory.Create();
            foreach (var directory in Directory.GetDirectories())
            {
                FileInfo metaFile = new FileInfo(Path.Combine(directory.FullName, "meta.json"));
                if(metaFile.Exists)
                {
                    SaveFile saveFile = new SaveFile(directory);
                    Actions.Add(new SaveFileAction(this, saveFile));
                }
            }
            Actions.Add(new ActAction("Cancel", "Return to previous menu", () => {
                Close();
            }));
            DefaultSelection = SelectionCount - 1;
        }

        public override void Update(Scene scene)
        {
            base.Update(scene);
            InfoWindow.Update(scene);
            if (SaveGameActions != null)
                SaveGameActions.Update(scene);

            foreach (var savefile in SaveFiles)
            {
                if(!savefile.Cached)
                {
                    savefile.Preload();
                    break;
                }
            }
        }

        public override void HandleInput(Scene scene)
        {
            if (SaveGameActions != null)
            {
                SaveGameActions.HandleInput(scene);
                if (SaveGameActions.ShouldClose)
                    SaveGameActions = null;
                return;
            }
            InfoWindow.HandleInput(scene);
            base.HandleInput(scene);
        }

        public override void Select(int selection)
        {
            if (Actions[selection].Enabled())
                Actions[selection].Action();
        }

        public override void DrawLine(Scene scene, Vector2 linePos, int e)
        {
            ActAction action = Actions[e];
            SpriteReference cursor = SpriteLoader.Instance.AddSprite("content/cursor");
            if (Selection == e)
                scene.SpriteBatch.Draw(cursor.Texture, linePos + new Vector2(0, LineHeight / 2 - cursor.Width / 2), cursor.GetFrameRect(0), Color.White);
            scene.DrawText(action.Name, linePos + new Vector2(16, 0), Alignment.Left, new TextParameters().SetConstraints(Width - 32, 16).SetBold(true).SetColor(Color.White, Color.Black));
            scene.DrawText(action.Description, linePos + new Vector2(16, 16), Alignment.Left, new TextParameters().SetConstraints(Width - 32, 16).SetBold(false).SetColor(Color.Gray, Color.Black));
        }

        public override void PreDraw(Scene scene)
        {
            base.PreDraw(scene);

            if (InfoWindow != null)
                InfoWindow.PreDraw(scene);

            if (SaveGameActions != null)
                SaveGameActions.PreDraw(scene);
        }

        public override void Draw(Scene scene)
        {
            base.Draw(scene);

            InfoWindow.Draw(scene);

            if (SaveGameActions != null)
                SaveGameActions.Draw(scene);
        }
    }
}
