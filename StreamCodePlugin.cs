using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
namespace RoomInfoMod
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class RoomInfoPlugin : BaseUnityPlugin
    {
        public static RoomInfoPlugin Instance { get; private set; }
        internal static new ManualLogSource Logger;
        private readonly Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);
        private bool isVisible = true;
        private bool isDragging = false;
        private bool isResizing = false;
        private int resizeCorner = -1;
        private Vector2 windowPos = new Vector2(28f, 28f);
        private Vector2 dragOffset = Vector2.zero;
        private Vector2 resizeStart = Vector2.zero;
        private Vector2 resizePosStart = Vector2.zero;
        private float resizeWStart = 0f;
        private float resizeHStart = 0f;
        private float alpha = 1f;
        private float targetAlpha = 1f;
        private float panelW = 360f;
        private float panelH = 150f;
        private const float MIN_W = 200f;
        private const float MIN_H = 100f;
        private const float HANDLE = 18f;
        private const int R = 22;
        private ConfigEntry<float> cfgX;
        private ConfigEntry<float> cfgY;
        private ConfigEntry<float> cfgW;
        private ConfigEntry<float> cfgH;
        private float lastBuiltPanelH = -1f;
        private float saveTimer = 0f;
        private const float SAVE_DELAY = 1f;
        public static string RoomCode = "";
        public static int PlayerCount = 0;
        public static int MaxPlayers = 0;
        private Texture2D tPanel = null;
        private Texture2D tHandle = null;
        private GUIStyle sWatermark = null;
        private GUIStyle sTitle = null;
        private GUIStyle sLabel = null;
        private GUIStyle sValue = null;
        private void Awake()
        {
            Instance = this;
            Logger = base.Logger;
            Logger.LogInfo($"[{PluginInfo.PLUGIN_NAME}] Awake");
            cfgX = Config.Bind("Window", "PosX", 28f, "Window X position");
            cfgY = Config.Bind("Window", "PosY", 28f, "Window Y position");
            cfgW = Config.Bind("Window", "Width", 360f, "Window width");
            cfgH = Config.Bind("Window", "Height", 150f, "Window height");
            windowPos.x = cfgX.Value;
            windowPos.y = cfgY.Value;
            panelW = cfgW.Value;
            panelH = cfgH.Value;
            try
            {
                harmony.PatchAll(typeof(RoomInfoPlugin));
                Logger.LogInfo($"[{PluginInfo.PLUGIN_NAME}] Harmony OK");
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"[{PluginInfo.PLUGIN_NAME}] Harmony FAIL: {ex.Message}");
            }
        }
        private void OnDestroy()
        {
            SaveConfig();
            harmony.UnpatchSelf();
        }
        private void SaveConfig()
        {
            cfgX.Value = windowPos.x;
            cfgY.Value = windowPos.y;
            cfgW.Value = panelW;
            cfgH.Value = panelH;
            Config.Save();
        }
        private void Update()
        {
            targetAlpha = isVisible ? 1f : 0f;
            alpha = Mathf.Lerp(alpha, targetAlpha, Time.deltaTime * 10f);
            if (isDragging || isResizing)
                saveTimer = SAVE_DELAY;
            else if (saveTimer > 0f)
            {
                saveTimer -= Time.deltaTime;
                if (saveTimer <= 0f) SaveConfig();
            }
            SafePoll();
        }
        private static void SafePoll()
        {
            try
            {
                Room r = PhotonNetwork.CurrentRoom;
                if (r == null) return;
                RoomCode = r.Name ?? RoomCode;
                PlayerCount = PhotonNetwork.PlayerList?.Length ?? r.PlayerCount;
                MaxPlayers = r.MaxPlayers;
            }
            catch { }
        }
        [HarmonyPatch(typeof(MonoBehaviourPunCallbacks), nameof(MonoBehaviourPunCallbacks.OnJoinedRoom))]
        [HarmonyPostfix]
        static void P_Joined()
        {
            try
            {
                Room r = PhotonNetwork.CurrentRoom;
                if (r == null) return;
                RoomCode = r.Name ?? "";
                PlayerCount = PhotonNetwork.PlayerList?.Length ?? r.PlayerCount;
                MaxPlayers = r.MaxPlayers;
            }
            catch { }
        }
        [HarmonyPatch(typeof(MonoBehaviourPunCallbacks), nameof(MonoBehaviourPunCallbacks.OnPlayerEnteredRoom))]
        [HarmonyPostfix]
        static void P_Enter(Player newPlayer)
        {
            try { PlayerCount = PhotonNetwork.PlayerList?.Length ?? PlayerCount + 1; } catch { }
        }
        [HarmonyPatch(typeof(MonoBehaviourPunCallbacks), nameof(MonoBehaviourPunCallbacks.OnPlayerLeftRoom))]
        [HarmonyPostfix]
        static void P_Leave(Player otherPlayer)
        {
            try { PlayerCount = PhotonNetwork.PlayerList?.Length ?? Mathf.Max(0, PlayerCount - 1); } catch { }
        }
        [HarmonyPatch(typeof(MonoBehaviourPunCallbacks), nameof(MonoBehaviourPunCallbacks.OnLeftRoom))]
        [HarmonyPostfix]
        static void P_Left() { RoomCode = ""; PlayerCount = 0; MaxPlayers = 0; }
        private void OnGUI()
        {
            Event ev = Event.current;
            if (ev != null && ev.type == EventType.KeyDown && ev.keyCode == KeyCode.K)
            {
                isVisible = !isVisible;
                ev.Use();
            }
            if (alpha < 0.01f) return;
            if (tPanel == null || tPanel.width != (int)panelW || tPanel.height != (int)panelH)
                BuildTexture();
            if (tHandle == null) BuildHandle();
            if (sTitle == null || lastBuiltPanelH != panelH) BuildStyles();
            windowPos.x = Mathf.Clamp(windowPos.x, 0, Screen.width - panelW);
            windowPos.y = Mathf.Clamp(windowPos.y, 0, Screen.height - panelH);
            float x = Mathf.Round(windowPos.x);
            float y = Mathf.Round(windowPos.y);
            Rect cornerTL = new Rect(x, y, HANDLE, HANDLE);
            Rect cornerTR = new Rect(x + panelW - HANDLE, y, HANDLE, HANDLE);
            Rect cornerBL = new Rect(x, y + panelH - HANDLE, HANDLE, HANDLE);
            Rect cornerBR = new Rect(x + panelW - HANDLE, y + panelH - HANDLE, HANDLE, HANDLE);
            Event e = Event.current;
            bool hoverTL = cornerTL.Contains(e.mousePosition);
            bool hoverTR = cornerTR.Contains(e.mousePosition);
            bool hoverBL = cornerBL.Contains(e.mousePosition);
            bool hoverBR = cornerBR.Contains(e.mousePosition);
            bool hoverAny = hoverTL || hoverTR || hoverBL || hoverBR;
            switch (e.type)
            {
                case EventType.MouseDown when e.button == 0:
                    if (hoverTL) { isResizing = true; resizeCorner = 0; }
                    else if (hoverTR) { isResizing = true; resizeCorner = 1; }
                    else if (hoverBL) { isResizing = true; resizeCorner = 2; }
                    else if (hoverBR) { isResizing = true; resizeCorner = 3; }
                    else if (new Rect(x, y, panelW, panelH).Contains(e.mousePosition))
                    { isDragging = true; dragOffset = e.mousePosition - windowPos; }
                    if (isResizing)
                    {
                        resizeStart = e.mousePosition;
                        resizePosStart = windowPos;
                        resizeWStart = panelW;
                        resizeHStart = panelH;
                    }
                    e.Use();
                    break;
                case EventType.MouseDrag when isResizing:
                    float dx = e.mousePosition.x - resizeStart.x;
                    float dy = e.mousePosition.y - resizeStart.y;
                    switch (resizeCorner)
                    {
                        case 0:
                            panelW = Mathf.Max(MIN_W, resizeWStart - dx);
                            panelH = Mathf.Max(MIN_H, resizeHStart - dy);
                            windowPos.x = resizePosStart.x + (resizeWStart - panelW);
                            windowPos.y = resizePosStart.y + (resizeHStart - panelH);
                            break;
                        case 1:
                            panelW = Mathf.Max(MIN_W, resizeWStart + dx);
                            panelH = Mathf.Max(MIN_H, resizeHStart - dy);
                            windowPos.y = resizePosStart.y + (resizeHStart - panelH);
                            break;
                        case 2:
                            panelW = Mathf.Max(MIN_W, resizeWStart - dx);
                            panelH = Mathf.Max(MIN_H, resizeHStart + dy);
                            windowPos.x = resizePosStart.x + (resizeWStart - panelW);
                            break;
                        case 3:
                            panelW = Mathf.Max(MIN_W, resizeWStart + dx);
                            panelH = Mathf.Max(MIN_H, resizeHStart + dy);
                            break;
                    }
                    e.Use();
                    break;
                case EventType.MouseDrag when isDragging:
                    windowPos = e.mousePosition - dragOffset; e.Use(); break;
                case EventType.MouseUp:
                    isDragging = false; isResizing = false; resizeCorner = -1; e.Use(); break;
            }
            x = Mathf.Round(windowPos.x);
            y = Mathf.Round(windowPos.y);
            GUI.color = new Color(0f, 0f, 0f, 0.45f * alpha);
            GUI.DrawTexture(new Rect(x + 3f, y + 5f, panelW, panelH), tPanel);
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.DrawTexture(new Rect(x, y, panelW, panelH), tPanel);
            float titleH = panelH * 0.18f;
            float titleY = y + titleH * 0.4f;
            GUI.Label(new Rect(x, titleY, panelW, titleH * 1.2f), "StreamCodeMOD", sTitle);
            GUI.color = new Color(1f, 1f, 1f, 0.12f * alpha);
            GUI.DrawTexture(new Rect(x + panelW * 0.08f, y + panelH * 0.36f, panelW * 0.84f, 1f), tPanel);
            GUI.color = new Color(1f, 1f, 1f, alpha);
            float lx = x + panelW * 0.08f;
            float vx = x + panelW * 0.47f;
            float vw = panelW * 0.45f;
            float lw = panelW * 0.38f;
            float rh = panelH * 0.22f;
            float row1 = y + panelH * 0.42f;
            float row2 = y + panelH * 0.68f;
            GUI.Label(new Rect(lx, row1, lw, rh), "ROOM CODE", sLabel);
            GUI.Label(new Rect(vx, row1, vw, rh), string.IsNullOrEmpty(RoomCode) ? "—" : RoomCode, sValue);
            GUI.Label(new Rect(lx, row2, lw, rh), "PLAYERS", sLabel);
            string ps = MaxPlayers > 0 ? $"{PlayerCount} / {MaxPlayers}"
                      : PlayerCount > 0 ? $"{PlayerCount}" : "—";
            GUI.Label(new Rect(vx, row2, vw, rh), ps, sValue);
            if (sWatermark == null) BuildWatermarkStyle();
            float wmW = 260f;
            float wmH = 22f;
            GUI.color = new Color(1f, 1f, 1f, 0.30f);
            GUI.Label(new Rect(Screen.width - wmW - 6f, Screen.height - wmH - 6f, wmW, wmH), "StreamCodeMOD  Made by Cheffy", sWatermark);
            GUI.color = Color.white;
        }
        private void BuildTexture()
        {
            if (tPanel != null) Object.Destroy(tPanel);
            int w = (int)panelW, h = (int)panelH;
            tPanel = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tPanel.filterMode = FilterMode.Bilinear;
            tPanel.wrapMode = TextureWrapMode.Clamp;
            Color fill = new Color(0.10f, 0.09f, 0.09f, 0.91f);
            Color[] px = new Color[w * h];
            for (int row = 0; row < h; row++)
                for (int col = 0; col < w; col++)
                {
                    float a = CornerAlpha(col, row, w, h, R);
                    px[row * w + col] = new Color(fill.r, fill.g, fill.b, fill.a * a);
                }
            tPanel.SetPixels(px);
            tPanel.Apply(false);
        }
        private void BuildHandle()
        {
            int s = (int)HANDLE;
            tHandle = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tHandle.filterMode = FilterMode.Bilinear;
            Color[] px = new Color[s * s];
            float half = s * 0.5f;
            for (int i = 0; i < s * s; i++)
            {
                float fx = (i % s) - half + 0.5f;
                float fy = (i / s) - half + 0.5f;
                float d = Mathf.Sqrt(fx * fx + fy * fy);
                float a = 1f - Mathf.Clamp01((d - half + 2f) / 2f);
                px[i] = new Color(1f, 1f, 1f, a);
            }
            tHandle.SetPixels(px);
            tHandle.Apply(false);
        }
        private static float CornerAlpha(int col, int row, int w, int h, int r)
        {
            float sx = col;
            float sy = (h - 1) - row;
            float cx = Mathf.Clamp(sx, r, w - r);
            float cy = Mathf.Clamp(sy, r, h - r);
            float dx = sx - cx;
            float dy = sy - cy;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            return 1f - Mathf.Clamp01((dist - r + 2f) / 2f);
        }
        private void BuildStyles()
        {
            sTitle = new GUIStyle
            {
                normal = { textColor = new Color(1f, 1f, 1f, 0.55f) },
                fontSize = Mathf.RoundToInt(panelH * 0.09f),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperCenter,
            };
            sLabel = new GUIStyle
            {
                normal = { textColor = new Color(1f, 1f, 1f, 0.35f) },
                fontSize = Mathf.RoundToInt(panelH * 0.085f),
                alignment = TextAnchor.MiddleLeft,
            };
            sValue = new GUIStyle
            {
                normal = { textColor = new Color(1f, 1f, 1f, 1f) },
                fontSize = Mathf.RoundToInt(panelH * 0.115f),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight,
            };
            lastBuiltPanelH = panelH;
        }
        private void BuildWatermarkStyle()
        {
            sWatermark = new GUIStyle
            {
                normal = { textColor = new Color(1f, 1f, 1f, 1f) },
                fontSize = 14,
                fontStyle = FontStyle.Italic,
                alignment = TextAnchor.MiddleRight,
            };
        }
    }
}
