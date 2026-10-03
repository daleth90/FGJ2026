using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using Object = UnityEngine.Object;

namespace GrassRun
{
    /// <summary>
    /// 把專案內嵌的中文字型掛到 TextMeshPro 的全域 fallback。
    /// 字型資產（圖集）只存在記憶體裡，不會寫進專案；進 build 的只有字型檔本身。
    /// WebGL 沒有系統字型，所以一定要靠內嵌字型；系統字型只在內嵌字型載不到時當備援。
    /// </summary>
    public static class CjkFontFallback
    {
        /// <summary>內嵌中文字型在 Resources 底下的路徑（不含副檔名）。</summary>
        public const string EmbeddedFontPath = "Fonts/NotoSansTC-Regular";

        const string AssetName = "CJK OS Fallback (runtime)";
        const int SamplingPointSize = 48;
        const int AtlasPadding = 9;
        const int AtlasSize = 1024;

        // 內嵌字型載不到時依序嘗試。本機的 Noto Sans TC 是可變字型，系統只回報 Thin 一種字重，所以排在最後。
        static readonly string[][] Candidates =
        {
            new[] { "Microsoft JhengHei", "Regular" },
            new[] { "PingFang TC", "Regular" },
            new[] { "Noto Sans CJK TC", "Regular" },
            new[] { "Noto Sans TC", "Regular" },
            new[] { "Heiti TC", "Medium" },
            new[] { "Microsoft YaHei", "Regular" },
            new[] { "Noto Sans TC", "Thin" },
        };

        static TMP_FontAsset current;
        static int protectedCount;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void InstallAtRuntime() => Install();

        /// <summary>確保 fallback 已掛上而且完好。重複呼叫是安全的。</summary>
        public static bool Install()
        {
            if (TMP_Settings.instance == null) return false;

            var fallbacks = TMP_Settings.fallbackFontAssets;
            if (fallbacks == null)
            {
                fallbacks = new List<TMP_FontAsset>();
                TMP_Settings.fallbackFontAssets = fallbacks;
            }
            fallbacks.RemoveAll(font => font == null);

            var embedded = Resources.Load<Font>(EmbeddedFontPath);

            // 編輯器裡這個字型會跨 Play 留著。留下來的如果已經壞了（圖集被回收），
            // 或來源不是現在該用的字型（例如先前用系統字型建的），整個丟掉重建。
            TMP_FontAsset healthy = null;
            bool discarded = false;
            foreach (var font in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
            {
                if (font.name != AssetName) continue;
                if (healthy == null && font.sourceFontFile == embedded && IsHealthy(font))
                {
                    healthy = font;
                    continue;
                }
                fallbacks.Remove(font);
                Discard(font);
                discarded = true;
            }

            bool created = false;
            if (healthy == null)
            {
                healthy = Create(embedded);
                if (healthy == null)
                {
                    Debug.LogWarning($"載不到內嵌中文字型 Resources/{EmbeddedFontPath}，也沒有可用的系統字型，中文會顯示成方塊。");
                    return false;
                }
                created = true;
            }

            if (!fallbacks.Contains(healthy)) fallbacks.Add(healthy);
            current = healthy;
            protectedCount = 0;
            Protect();

            Canvas.willRenderCanvases -= Protect;
            Canvas.willRenderCanvases += Protect;

            if (discarded) ForgetCachedCharacters();
            if (discarded || created) RedrawTexts();
            return true;
        }

        /// <summary>
        /// TextMeshPro 圖集滿了會自己再開一張貼圖，但新貼圖沒有保護旗標，
        /// 離開 Play 模式時會被 Unity 當成「Play 期間產生的物件」回收，留下一個指向空貼圖的字型。
        /// 所以每次畫 UI 之前檢查一下，把新開的貼圖也標成不回收。
        /// </summary>
        static void Protect()
        {
            if (current == null) return;

            int count = current.atlasTextureCount;
            if (count == protectedCount) return;

            var textures = current.atlasTextures;
            for (int i = 0; i < count && i < textures.Length; i++)
                if (textures[i] != null) textures[i].hideFlags = HideFlags.HideAndDontSave;
            protectedCount = count;
        }

        static bool IsHealthy(TMP_FontAsset font)
        {
            if (font.material == null) return false;

            var textures = font.atlasTextures;
            if (textures == null) return false;
            for (int i = 0; i < font.atlasTextureCount; i++)
                if (i >= textures.Length || textures[i] == null) return false;
            return true;
        }

        static void Discard(TMP_FontAsset font)
        {
            if (font.atlasTextures != null)
                foreach (var texture in font.atlasTextures) Kill(texture);
            Kill(font.material);
            Kill(font);
        }

        static void Kill(Object target)
        {
            if (target == null) return;
            if (Application.isPlaying) Object.Destroy(target);
            else Object.DestroyImmediate(target);
        }

        /// <summary>其他字型會快取「這個字去 fallback 找」的結果，fallback 重建後要讓它們重查。</summary>
        static void ForgetCachedCharacters()
        {
            foreach (var font in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
                if (font != null && font.name != AssetName) font.ReadFontAssetDefinition();
        }

        static void RedrawTexts()
        {
            foreach (var text in Resources.FindObjectsOfTypeAll<TMP_Text>())
                if (text != null && text.gameObject.scene.IsValid()) text.ForceMeshUpdate(true, true);
        }

        static TMP_FontAsset Create(Font embedded)
        {
            TMP_FontAsset font = null;
            if (embedded != null)
                font = TMP_FontAsset.CreateFontAsset(embedded, SamplingPointSize, AtlasPadding,
                    GlyphRenderMode.SDFAA, AtlasSize, AtlasSize);

            if (font == null)
            {
                font = CreateFromSystemFont();
                if (font == null) return null;
                Debug.LogWarning($"載不到內嵌中文字型 Resources/{EmbeddedFontPath}，暫時改用系統字型。" +
                                 "WebGL 沒有系統字型，這樣 build 出來中文會是方塊。");
            }

            font.name = AssetName;
            font.hideFlags = HideFlags.HideAndDontSave;
            if (font.material != null) font.material.hideFlags = HideFlags.HideAndDontSave;
            return font;
        }

        static TMP_FontAsset CreateFromSystemFont()
        {
            string[] installed = FontEngine.GetSystemFontNames();
            if (installed == null) return null;

            foreach (var candidate in Candidates)
            {
                if (Array.IndexOf(installed, candidate[0] + " - " + candidate[1]) < 0) continue;

                var font = TMP_FontAsset.CreateFontAsset(candidate[0], candidate[1], SamplingPointSize);
                if (font != null) return font;
            }
            return null;
        }
    }
}
