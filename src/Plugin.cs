using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace TrailerCam
{
    [BepInPlugin("jordan.valheim.trailercam", "Trailer Cam", Plugin.Version)]
    [BepInProcess("valheim.exe")]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Version = "0.1.0";

        private ConfigEntry<KeyCode> _toggleKey;
        private ConfigEntry<float> _startSpeed;
        private ConfigEntry<float> _smoothness;
        private ConfigEntry<bool> _hideHud;

        private Player _flightPlayer;
        private GameCamera _flightCamera;
        private Hud _flightHud;
        private bool _previousHudHidden;
        private float _previousSmoothness;
        private float _previousSpeed;

        private void Awake()
        {
            _toggleKey = Config.Bind("Camera", "ToggleKey", KeyCode.F8, "Detach the camera or return to your character.");
            _startSpeed = Config.Bind("Camera", "StartSpeed", 8f,
                new ConfigDescription("Starting speed in metres per second. Scroll to change it in flight.",
                    new AcceptableValueRange<float>(1f, 1000f)));
            _smoothness = Config.Bind("Camera", "Smoothness", 0.25f,
                new ConfigDescription("Smooth the camera's movement and turns. Zero gives an immediate response.",
                    new AcceptableValueRange<float>(0f, 1f)));
            _hideHud = Config.Bind("Camera", "HideHud", true, "Hide the HUD during flight and restore it on exit.");
        }

        private void Update()
        {
            Player player = Player.m_localPlayer;
            GameCamera camera = GameCamera.instance;
            bool toggle = ZInput.GetKeyDown(_toggleKey.Value, logWarning: false);
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

            if (toggle && camera && player && Hud.instance && !GameCamera.InFreeFly() && player.TakeInput())
                StartFlight(player, camera);
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
            camera.SetFreeFlySmoothness(_smoothness.Value);

            if (_hideHud.Value)
            {
                _flightHud = Hud.instance;
                _previousHudHidden = _flightHud.m_userHidden;
                _flightHud.m_userHidden = true;
            }

            // The game's free-fly input lock leaves an existing autorun active.
            player.m_autoRun = false;
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

            _flightCamera = null;
            _flightPlayer = null;
            _flightHud = null;
        }

        private void OnDisable() => StopFlight();
    }
}
