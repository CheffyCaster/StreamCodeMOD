using BepInEx;
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
        private Vector2 windowPos = new Vector2(28f, 28f);
        private Vector2 dragOffset = Vector2.zero;
        private float alpha = 1f;
        private float targetAlpha = 1f;
        private const float W = 360f;
        private const float H = 150f;
        private const int R = 22;
        public static string RoomCode = "";
        public static int PlayerCount = 0;
        public static int MaxPlayers = 0;
        private Texture2D tPanel = null;
        private GUIStyle sTitle = null;
        private GUIStyle sLabel = null;
        private GUIStyle sValue = null;
        private void Awake()
        {
            Instance = this;
            Logger = base.Logger;
            Logger.LogInfo($"[{PluginInfo.PLUGIN_NAME}] Awake");
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
        private void OnDestroy() => harmony.UnpatchSelf();
        private void Update()
        {
            targetAlpha = isVisible ? 1f : 0f;
            alpha = Mathf.Lerp(alpha, targetAlpha, Time.deltaTime * 10f);
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
            if (tPanel == null) BuildTexture();
            if (sTitle == null) BuildStyles();
            windowPos.x = Mathf.Clamp(windowPos.x, 0, Screen.width - W);
            windowPos.y = Mathf.Clamp(windowPos.y, 0, Screen.height - H);
            Event e = Event.current;
            Rect win = new Rect(windowPos.x, windowPos.y, W, H);
            switch (e.type)
            {
                case EventType.MouseDown when e.button == 0 && win.Contains(e.mousePosition):
                    isDragging = true; dragOffset = e.mousePosition - windowPos; e.Use(); break;
                case EventType.MouseDrag when isDragging:
                    windowPos = e.mousePosition - dragOffset; e.Use(); break;
                case EventType.MouseUp when isDragging:
                    isDragging = false; e.Use(); break;
            }
            float x = Mathf.Round(windowPos.x);
            float y = Mathf.Round(windowPos.y);
            GUI.color = new Color(0f, 0f, 0f, 0.45f * alpha);
            GUI.DrawTexture(new Rect(x + 3f, y + 5f, W, H), tPanel);
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.DrawTexture(new Rect(x, y, W, H), tPanel);
            GUI.Label(new Rect(x, y + 12f, W, 26f), "StreamerCodeMOD", sTitle);
            GUI.color = new Color(1f, 1f, 1f, 0.12f * alpha);
            GUI.DrawTexture(new Rect(x + 24f, y + 46f, W - 48f, 1f), tPanel);
            GUI.color = new Color(1f, 1f, 1f, alpha);
            float lx = x + 24f;
            float vx = x + 170f;
            float vw = W - 194f;
            GUI.Label(new Rect(lx, y + 56f, 136f, 30f), "ROOM CODE", sLabel);
            GUI.Label(new Rect(vx, y + 56f, vw, 30f),
                string.IsNullOrEmpty(RoomCode) ? "—" : RoomCode, sValue);
            GUI.Label(new Rect(lx, y + 96f, 136f, 30f), "PLAYERS", sLabel);
            string ps = MaxPlayers > 0 ? $"{PlayerCount} / {MaxPlayers}"
                      : PlayerCount > 0 ? $"{PlayerCount}" : "—";
            GUI.Label(new Rect(vx, y + 96f, vw, 30f), ps, sValue);
            GUI.color = Color.white;
        }
        private void BuildTexture()
        {
            int w = (int)W, h = (int)H;
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
        private static float Circ(float px, float py, float cx, float cy, float r)
        {
            float d = Mathf.Sqrt((px - cx) * (px - cx) + (py - cy) * (py - cy));
            return 1f - Mathf.Clamp01((d - r + 2f) / 2f);
        }
        private void BuildStyles()
        {
            sTitle = new GUIStyle
            {
                normal = { textColor = new Color(1f, 1f, 1f, 0.55f) },
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperCenter,
            };
            sLabel = new GUIStyle
            {
                normal = { textColor = new Color(1f, 1f, 1f, 0.35f) },
                fontSize = 13,
                alignment = TextAnchor.MiddleLeft,
            };
            sValue = new GUIStyle
            {
                normal = { textColor = new Color(1f, 1f, 1f, 1f) },
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight,
            };
        }
    }
}