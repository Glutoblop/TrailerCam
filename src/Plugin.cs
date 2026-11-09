using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace TrailerCam
{
    [BepInPlugin(Plugin.Guid, "Trailer Cam", Plugin.Version)]
    [BepInProcess("valheim.exe")]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Guid = "jordan.valheim.trailercam";
        public const string Version = "0.1.2";

        private static Plugin _activeFlight;
        private ConfigEntry<KeyboardShortcut> _toggleKey;
        private ConfigEntry<float> _startSpeed;
        private ConfigEntry<float> _lookSmoothing;
        private ConfigEntry<float> _movementSmoothing;
        private ConfigEntry<bool> _hideHud;
        private Harmony _harmony;

        private Player _flightPlayer;
        private GameCamera _flightCamera;
        private Hud _flightHud;
        private bool _previousHudHidden;
        private float _previousSmoothness;
        private float _previousSpeed;

        private void Awake()
        {
            _toggleKey = Config.Bind("Camera", "ToggleKey", new KeyboardShortcut(KeyCode.F8),
                "Detach the camera or return to your character. Hold any Ctrl, Alt, or Shift modifiers before pressing the main key.");
            _startSpeed = Config.Bind("Camera", "StartSpeed", 8f,
                new ConfigDescription("Starting speed in metres per second. Scroll to change it in flight.",
                    new AcceptableValueRange<float>(1f, 1000f)));
            // Keep the existing config key and use its value for movement when upgrading.
            _lookSmoothing = Config.Bind("Camera", "Smoothness", 0.25f,
                new ConfigDescription("Smooth camera turns. Zero makes turning immediate.",
                    new AcceptableValueRange<float>(0f, 1f)));
            _movementSmoothing = Config.Bind("Camera", "MovementSmoothing", _lookSmoothing.Value,
                new ConfigDescription("Smooth acceleration and stopping. Zero makes movement immediate.",
                    new AcceptableValueRange<float>(0f, 1f)));
            _hideHud = Config.Bind("Camera", "HideHud", true, "Hide the HUD during flight and restore it on exit.");

            _harmony = new Harmony(Guid);
            _harmony.PatchAll(typeof(Plugin).Assembly);
        }

        internal static bool IsFilming(GameCamera camera) => ReferenceEquals(_activeFlight?._flightCamera, camera);

        internal static float GetMovementSmoothing(GameCamera camera) =>
            IsFilming(camera) ? _activeFlight._movementSmoothing.Value : camera.GetFreeFlySmoothness();

        private void Update()
        {
            Player player = Player.m_localPlayer;
            GameCamera camera = GameCamera.instance;
            bool toggle = TogglePressed();
            if (_flightCamera is not null)
            {
                if (!camera || camera != _flightCamera || !player || player != _flightPlayer
                    || !GameCamera.InFreeFly() || player.IsDead() || player.IsTeleporting() || player.InCutscene()
                    || Menu.IsActive() || Console.IsVisible() || InventoryGui.IsVisible() || Minimap.IsOpen()
                    || TextInput.IsVisible() || (Chat.instance && Chat.instance.HasFocus())
                    || ZInput.GetKeyDown(KeyCode.Escape) || toggle)
                {
                    StopFlight();
                }
                return;
            }

            if (toggle && camera && player && Hud.instance && !GameCamera.InFreeFly() && !Menu.IsActive() && player.TakeInput())
                StartFlight(player, camera);
        }

        private bool TogglePressed()
        {
            KeyboardShortcut shortcut = _toggleKey.Value;
            if (shortcut.MainKey == KeyCode.None || !ZInput.GetKeyDown(shortcut.MainKey, logWarning: false)) return false;

            // Check the required modifiers without blocking the shortcut when movement keys are held.
            foreach (KeyCode modifier in shortcut.Modifiers)
                if (!ZInput.GetKey(modifier, logWarning: false)) return false;

            return true;
        }

        private void StartFlight(Player player, GameCamera camera)
        {
            _flightPlayer = player;
            _flightCamera = camera;
            _previousSmoothness = camera.GetFreeFlySmoothness();
            _previousSpeed = camera.m_freeFlySpeed;

            // The native toggle keeps the last flight's heading, targets and momentum.
            Vector3 angles = camera.transform.eulerAngles;
            camera.m_freeFlyYaw = angles.y;
            camera.m_freeFlyPitch = Mathf.DeltaAngle(0f, angles.x);
            camera.m_freeFlyTarget = null;
            camera.m_freeFlyLockon = null;
            camera.m_freeFlySavedVel = Vector3.zero;
            camera.m_freeFlyVel = Vector3.zero;
            camera.m_freeFlyAcc = Vector3.zero;
            camera.m_freeFlyRef = Quaternion.identity;
            camera.m_freeFlySpeed = _startSpeed.Value;
            camera.SetFreeFlySmoothness(_lookSmoothing.Value);

            if (_hideHud.Value)
            {
                _flightHud = Hud.instance;
                _previousHudHidden = _flightHud.m_userHidden;
                _flightHud.m_userHidden = true;
            }

            // The game's free-fly input lock leaves an existing autorun active.
            player.m_autoRun = false;
            _activeFlight = this;
            camera.ToggleFreeFly();
        }

        private void StopFlight()
        {
            if (_flightCamera)
            {
                if (_flightCamera.m_freeFly) _flightCamera.ToggleFreeFly();
                _flightCamera.SetFreeFlySmoothness(_previousSmoothness);
                _flightCamera.m_freeFlySpeed = _previousSpeed;
            }
            if (_flightHud) _flightHud.m_userHidden = _previousHudHidden;

            if (_activeFlight == this) _activeFlight = null;
            _flightCamera = null;
            _flightPlayer = null;
            _flightHud = null;
        }

        private void OnDisable() => StopFlight();

        private void OnDestroy() => _harmony?.UnpatchSelf();
    }
}
