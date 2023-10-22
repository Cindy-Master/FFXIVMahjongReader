﻿using System;
using System.IO;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Component.GUI;
using MahjongReader.Windows;
using System.Collections.Generic;
using System.Linq;

namespace MahjongReader
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Sample Plugin";
        private const string CommandName = "/mahjong";


        [PluginService] public static IGameGui GameGui { get; private set; } = null!;
        [PluginService] public static IPluginLog PluginLog { get; private set; } = null!;
        [PluginService] internal static IAddonLifecycle AddonLifecycle { get; private set; } = null!;

        private DalamudPluginInterface PluginInterface { get; init; }
        private ICommandManager CommandManager { get; init; }
        public Configuration Configuration { get; init; }
        public WindowSystem WindowSystem = new("MahjongReader");

        private ConfigWindow ConfigWindow { get; init; }
        private MainWindow MainWindow { get; init; }

        private ImportantPointers ImportantPointers { get; init; }
        private NodeCrawlerUtils NodeCrawlerUtils { get; init; }

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] ICommandManager commandManager)
        {
            this.PluginInterface = pluginInterface;
            this.CommandManager = commandManager;

            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);

            // you might normally want to embed resources and load them from the manifest stream
            var imagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "goat.png");
            var goatImage = this.PluginInterface.UiBuilder.LoadImage(imagePath);

            ConfigWindow = new ConfigWindow(this);
            MainWindow = new MainWindow(this, goatImage);
            
            WindowSystem.AddWindow(ConfigWindow);
            WindowSystem.AddWindow(MainWindow);

            this.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "A useful message to display in /xlhelp"
            });

            this.PluginInterface.UiBuilder.Draw += DrawUI;
            this.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

            AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "Emj", OnAddonPostSetup);
            ImportantPointers = new ImportantPointers(PluginLog);
            NodeCrawlerUtils = new NodeCrawlerUtils(PluginLog);
        }

        public void Dispose()
        {
            AddonLifecycle.UnregisterListener(OnAddonPostSetup);
            AddonLifecycle.UnregisterListener(OnAddonPostRefresh);
            this.WindowSystem.RemoveAllWindows();
            
            ConfigWindow.Dispose();
            MainWindow.Dispose();
            
            this.CommandManager.RemoveHandler(CommandName);
        }

        private void OnAddonPostSetup(AddonEvent type, AddonArgs args) {
            AddonLifecycle.RegisterListener(AddonEvent.PostRefresh, "Emj", OnAddonPostRefresh);
        }

        private unsafe void OnAddonPostRefresh(AddonEvent type, AddonArgs args) {
            var addonPtr = args.Addon;
            if (addonPtr == IntPtr.Zero) {
                PluginLog.Info("Could not find Emj");
                return;
            }

            var addon = (AtkUnitBase*)addonPtr;
            var rootNode = addon->RootNode;
            var prevNode = rootNode->PrevSiblingNode;
        }

        private unsafe void OnCommand(string command, string args)
        {
            var addonPtr = GameGui.GetAddonByName("Emj", 1);

            if (addonPtr == IntPtr.Zero) {
                PluginLog.Info("Could not find Emj");
                return;
            }
            // TileTextureMap.PrintTest(PluginLog);
            PluginLog.Info("Found Emj");
            ImportantPointers.WipePointers();
            var addon = (AtkUnitBase*)addonPtr;
            var rootNode = addon->RootNode;
            NodeCrawlerUtils.TraverseAllAtkResNodes(rootNode, (intPtr) => ImportantPointers.MaybeTrackPointer(intPtr));
            var observedTileTextures = new List<TileTexture>();
            PluginLog.Info("player hand");
            ImportantPointers.PlayerHand.ForEach(ptr => {
                var castedPtr = (AtkResNode*)ptr;
                NodeCrawlerUtils.DelveNode(castedPtr, null);
                var tileTexture = NodeCrawlerUtils.GetTileTextureFromPlayerHandTile(ptr);
                if (tileTexture != null) {
                    PluginLog.Info(tileTexture.ToString());
                    observedTileTextures.Add(tileTexture);
                }
            });
        
            PluginLog.Info("player discards");
            ImportantPointers.PlayerDiscardPile.ForEach(ptr => {
                var castedPtr = (AtkResNode*)ptr;
                var tileTexture = NodeCrawlerUtils.GetTileTextureFromDiscardTile(ptr);
                if (tileTexture != null) {
                    PluginLog.Info(tileTexture.ToString());
                    if (!tileTexture.IsMelded) {
                        observedTileTextures.Add(tileTexture.TileTexture);
                    }
                }
            });

            PluginLog.Info("right discards");
            ImportantPointers.RightDiscardPile.ForEach(ptr => {
                var castedPtr = (AtkResNode*)ptr;
                var tileTexture = NodeCrawlerUtils.GetTileTextureFromDiscardTile(ptr);
                if (tileTexture != null) {
                    PluginLog.Info(tileTexture.ToString());
                    if (!tileTexture.IsMelded) {
                        observedTileTextures.Add(tileTexture.TileTexture);
                    }
                }
            });

            PluginLog.Info("far discards");
            ImportantPointers.FarDiscardPile.ForEach(ptr => {
                var castedPtr = (AtkResNode*)ptr;
                var tileTexture = NodeCrawlerUtils.GetTileTextureFromDiscardTile(ptr);
                if (tileTexture != null) {
                    PluginLog.Info(tileTexture.ToString());
                    if (!tileTexture.IsMelded) {
                        observedTileTextures.Add(tileTexture.TileTexture);
                    }
                }
            });

            PluginLog.Info("left discards");
            ImportantPointers.LeftDiscardPile.ForEach(ptr => {
                var castedPtr = (AtkResNode*)ptr;
                var tileTexture = NodeCrawlerUtils.GetTileTextureFromDiscardTile(ptr);
                if (tileTexture != null) {
                    PluginLog.Info(tileTexture.ToString());
                    if (!tileTexture.IsMelded) {
                        observedTileTextures.Add(tileTexture.TileTexture);
                    }
                }
            });

            PluginLog.Info("player melds");
            ImportantPointers.PlayerMeldGroups.ForEach(ptr => {
                var castedPtr = (AtkResNode*)ptr;
                var tileTextures = NodeCrawlerUtils.GetTileTexturesFromPlayerMeldGroup(ptr);
                tileTextures?.ForEach(texture => PluginLog.Info(texture.ToString()));
            });

            PluginLog.Info("right melds");
            ImportantPointers.RightMeldGroups.ForEach(ptr => {
                var castedPtr = (AtkResNode*)ptr;
                var tileTextures = NodeCrawlerUtils.GetTileTexturesFromMeldGroup(ptr);
                tileTextures?.ForEach(texture => PluginLog.Info(texture.ToString()));
            });

            PluginLog.Info("far melds");
            ImportantPointers.FarMeldGroups.ForEach(ptr => {
                var castedPtr = (AtkResNode*)ptr;
                var tileTextures = NodeCrawlerUtils.GetTileTexturesFromMeldGroup(ptr);
                tileTextures?.ForEach(texture => PluginLog.Info(texture.ToString()));
            });

            PluginLog.Info("left melds");
            ImportantPointers.LeftMeldGroups.ForEach(ptr => {
                var castedPtr = (AtkResNode*)ptr;
                var tileTextures = NodeCrawlerUtils.GetTileTexturesFromMeldGroup(ptr);
                tileTextures?.ForEach(texture => PluginLog.Info(texture.ToString()));
            });
            PluginLog.Info($"Observed count: {observedTileTextures.Count}");
        }

        private void DrawUI()
        {
            this.WindowSystem.Draw();
        }

        public void DrawConfigUI()
        {
            ConfigWindow.IsOpen = true;
        }
    }
}
