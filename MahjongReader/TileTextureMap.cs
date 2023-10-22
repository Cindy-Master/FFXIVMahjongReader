using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Dalamud.Logging;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace MahjongReader
{
    public class DiscardTile {
        public TileTexture TileTexture { get; }
        public bool IsMelded { get; }
        public bool IsImmediatelyDiscarded { get; }

        public DiscardTile(TileTexture tileTexture, bool isMelded, bool isImmediatelyDiscarded) {
            TileTexture = tileTexture;
            IsMelded = isMelded;
            IsImmediatelyDiscarded = isImmediatelyDiscarded;
        }

        public override string ToString() {
            return $"{TileTexture} IsMelded: {IsMelded} IsImmediatelyDiscarded: {IsImmediatelyDiscarded}";
        }
    }

    public class TileTexture {
        private string textureId;
        private string mjaiNotation;

        public TileTexture(string iTextureId, string iMjaiNotation) {
            textureId = iTextureId;
            mjaiNotation = iMjaiNotation;
        }

        public string TextureId
        {
            get { return textureId; }
        }

        public string MjaiNotation
        {
            get { return mjaiNotation; }
        }

        public override string ToString() {
            return $"textureId: {textureId} mjaiNotation: {mjaiNotation}";
        }
    }

    public class TileCountTracker {
        private Dictionary<string, int> notationToRemainingCount;

        public TileCountTracker(Dictionary<string, int> aNotationToRemainingCount) {
            notationToRemainingCount = aNotationToRemainingCount;
        }

        public Dictionary<string, int> RemainingFromObserved(List<TileTexture> observedTiles) {
            Dictionary<string, int> copiedRemaining = notationToRemainingCount.ToDictionary(entry => entry.Key, entry => entry.Value);

            foreach (TileTexture tileTexture in observedTiles) {
                copiedRemaining[tileTexture.MjaiNotation]--;
            }

            return copiedRemaining;
        }
    }

    public class TileTextureUtilities {
        public static Dictionary<string, TileTexture> TextureIdToNotation;
        public static TileCountTracker TileCountTracker;

        static TileTextureUtilities() {
            Dictionary<string, TileTexture> map = new Dictionary<string, TileTexture>();
            Dictionary<string, int> countMap = new Dictionary<string, int>();

            Action<int, string> updateCountMap = (number, notation) => {
                var suit = notation[1];
                var tileCount = 0;

                // honor tiles (z) don't have aka dora and we know that there are four instances of each
                // for the characters, dots, bamboo 5 there is the aka dora usually referring to as 0
                if (suit == 'z') {
                    tileCount = 4;
                } else {
                    if (number == 5) {
                        tileCount = 3;
                    } else {
                        tileCount = 4;
                    }
                }

                countMap.Add(notation, tileCount);
            };

            // characters/man
            for (int i = 1; i < 10; i++) {
                int nextTextureId = i + 76040; //41 is the actual character/man start
                string fullTextureId = "0" + nextTextureId.ToString();
                string notation = i.ToString() + "m";
                map.Add(fullTextureId, new TileTexture(fullTextureId, notation));
                updateCountMap(i, notation);
            }

            // dots/pin
            for (int i = 1; i < 10; i++) {
                int nextTextureId = i + 076049; //50 is the actual dots/pin start
                string fullTextureId = "0" + nextTextureId.ToString();
                string notation = i.ToString() + "p";
                map.Add(fullTextureId, new TileTexture(fullTextureId, notation));
                updateCountMap(i, notation);
            }

            // bamboo/sou
            for (int i = 1; i < 10; i++) {
                int nextTextureId = i + 076058; //59 is the actual bamboo/sou start
                string fullTextureId = "0" + nextTextureId.ToString();
                string notation = i.ToString() + "s";
                map.Add(fullTextureId, new TileTexture(fullTextureId, notation));
                updateCountMap(i, notation);
            }

            // honor tiles / wind first / dragons
            for (int i = 1; i < 8; i++) {
                int nextTextureId = i + 076067; //68 is the actual honor tile start
                string fullTextureId = "0" + nextTextureId.ToString();
                string notation = i.ToString() + "z";
                map.Add(fullTextureId, new TileTexture(fullTextureId, notation));
                updateCountMap(i, notation);
            }

            map.Add("076075", new TileTexture("076075", "0m"));
            countMap.Add("0m", 1);
            map.Add("076076", new TileTexture("076076", "0p"));
            countMap.Add("0p", 1);
            map.Add("076077", new TileTexture("076077", "0s"));
            countMap.Add("0s", 1);

            TextureIdToNotation = map;
            TileCountTracker = new TileCountTracker(countMap);
        }

        // TODO: support traditonal (IRL Mahjong tile face) textures? Same pattern, different texture id offset
        private const string TEXTURE_PATH_TILE_ICON_PREFIX = "ui/icon/076000/";

        public static bool IsValidTileTexturePath(string? texturePath) {
            return texturePath?.StartsWith(TEXTURE_PATH_TILE_ICON_PREFIX) ?? false;
        }

        public static TileTexture GetTileTextureFromTexturePath(string texturePath) {
            var textureTex = texturePath.Substring(15, 6);
            if (!TextureIdToNotation.ContainsKey(textureTex)) {
                throw new ApplicationException("Unknown key: " + texturePath);
            }

            return TextureIdToNotation[textureTex];
        }

        public static void PrintTest(IPluginLog pluginLog) {
            foreach (var kvp in TextureIdToNotation) {
                TileTexture value = kvp.Value;
                pluginLog.Info(value.ToString());
            }
        }
    }
}