namespace FrontRooms.Audio
{
    /// <summary>
    /// FMOD event paths and parameter names. Must match SPEC in
    /// Tools/audio/fmod_frontrooms.py, which builds FMOD/FrontRooms.
    /// </summary>
    public static class SoundIds
    {
        // Ambience
        public const string HumBed = "event:/Ambience/HumBed";
        public const string AirBed = "event:/Ambience/AirBed";
        public const string Fixture = "event:/Ambience/Fixture";
        public const string FixtureEvent = "event:/Ambience/FixtureEvent";

        // Door modules
        public const string DoorHandle = "event:/Mechanism/Door/Handle";
        public const string DoorUnlatch = "event:/Mechanism/Door/Unlatch";
        public const string DoorSwing = "event:/Mechanism/Door/Swing";
        public const string DoorStopLimit = "event:/Mechanism/Door/StopLimit";
        public const string DoorLatchStrike = "event:/Mechanism/Door/LatchStrike";
        public const string DoorLocked = "event:/Mechanism/Door/Locked";
        public const string DoorBlow = "event:/Mechanism/Door/Blow";
        public const string DoorBreak = "event:/Mechanism/Door/Break";
        public const string DoorStreamOpen = "event:/Mechanism/Door/StreamOpen";     // per leaf, Leaf
        public const string DoorStreamClose = "event:/Mechanism/Door/StreamClose";   // per leaf, Leaf
        public const string DoorStreamLock = "event:/Mechanism/Door/StreamLock";     // once per pair, terminal door only
        public const string DoorExitBar = "event:/Mechanism/Door/ExitBar";           // the stairwell EXIT door's crash bar
        public const string DoorExitClose = "event:/Mechanism/Door/ExitClose";       // the steel EXIT door latching, Impact

        // The unlock shot's lock (FrontRoomsDoorShot.Beat, ShotKind.Unlock): one recorded key in its moving parts, at the keyhole
        public const string LockKeyInsert = "event:/Mechanism/Lock/KeyInsert";
        public const string LockKeyTurn = "event:/Mechanism/Lock/KeyTurn";
        public const string LockBoltRetract = "event:/Mechanism/Lock/BoltRetract";
        public const string LockKeyWithdraw = "event:/Mechanism/Lock/KeyWithdraw";
        public const string LockKnobTurn = "event:/Mechanism/Lock/KnobTurn";
        public const string LockKnobReturn = "event:/Mechanism/Lock/KnobReturn";

        // Window
        public const string WindowStress = "event:/Mechanism/Window/Stress";
        public const string WindowCrack = "event:/Mechanism/Window/Crack";
        public const string WindowShatter = "event:/Mechanism/Window/Shatter";

        // Player
        public const string Footstep = "event:/Foley/Player/Footstep";
        public const string Cloth = "event:/Foley/Player/Cloth";
        public const string KeyPickup = "event:/Foley/Player/KeyPickup";

        // The Relay
        public const string RelayFootstep = "event:/Relay/Footstep";
        public const string RelayPresence = "event:/Relay/Presence";
        public const string RelayClicks = "event:/Relay/Clicks";
        public const string RelayStinger = "event:/Relay/Stinger";
        public const string RelayHum = "event:/Relay/Hum";               // C30: the stage-gated low hum (W2), when HumByStage is on

        // HUD B (FrontRoomsKeyHud.SoundCue): 2D cues on the UI bus, on the HUD's own clock
        public const string HudKeyFlight = "event:/UI/HudKeyFlight";
        public const string HudKeyLand = "event:/UI/HudKeyLand";
        public const string HudLockTug = "event:/UI/HudLockTug";
        public const string HudUnlockDrop = "event:/UI/HudUnlockDrop";
        public const string HudBrokenBuild = "event:/UI/HudBrokenBuild";
        public const string HudBrokenKnock = "event:/UI/HudBrokenKnock";

        // Sounding screens (tutorial T6 / S1-S5, FrontRoomsScreenSound)
        public const string ScreenTitleTV = "event:/Screen/TitleTV";
        public const string ScreenWhine = "event:/Screen/Whine";
        public const string ScreenPowerOn = "event:/Screen/PowerOn";
        public const string ScreenPowerOff = "event:/Screen/PowerOff";

        // Subjective
        public const string Breath = "event:/Subjective/Breath";
        public const string Heartbeat = "event:/Subjective/Heartbeat";
        public const string Tinnitus = "event:/Subjective/Tinnitus";

        // Music
        public const string MusicTitle = "event:/Music/Title";

        // Buses
        public const string BusMaster = "bus:/";
        public const string BusAmbience = "bus:/AMB";
        public const string BusSfx = "bus:/SFX";
        public const string BusMusic = "bus:/Music";
        public const string BusSubjective = "bus:/Subjective";
        // The SFX group's children (Tools/audio/fmod_frontrooms.py buses), for the EXAMINE close-up (S1).
        public const string BusFoley = "bus:/SFX/Foley";
        public const string BusMechanism = "bus:/SFX/Mechanism";
        public const string BusRelay = "bus:/SFX/Relay";
        public const string BusScreen = "bus:/SFX/Screen";

        public static class Param
        {
            // global
            public const string Tension = "Tension";
            public const string Zone = "Zone";             // labels: Low, Standard, Tall, Office, Stair (append-only)
            public const string Tier = "Tier";
            // local
            public const string Openness = "Openness";
            public const string AngularVelocity = "AngularVelocity";
            public const string Impact = "Impact";
            public const string Damage = "Damage";
            public const string Progress = "Progress";
            public const string Level = "Level";
            public const string Stamina = "Stamina";
            public const string Proximity = "Proximity";
            public const string Occlusion = "Occlusion";   // Relay: the muffle (low-pass only since W2)
            public const string PathGain = "PathGain";     // Relay steps / hum: level from the path distance F (W2), 0..1
            public const string BeaconGain = "BeaconGain";     // Screen/TitleTV: the station TV's level from the path (S2), dB
            public const string BeaconCutoff = "BeaconCutoff"; // Screen/TitleTV: its low-pass from the path and shut doors (S2), Hz
            public const string Groan = "Groan";           // HumBed: ballast brown-out under a warning burst (W1), 0..1
            public const string Arousal = "Arousal";       // Heartbeat: exertion (slow beat) .. chase (fast), never distance
            public const string Dampness = "Dampness";     // 0 dry .. 0.4 Level 0 carpet .. >0.75 waterlogged
            public const string Surface = "Surface";       // Carpet, CarpetTile, Metal
            public const string Gait = "Gait";             // Walk, Run, Stop
            public const string RelayGait = "RelayGait";   // Walk, Run, Drag
            public const string RelayState = "RelayState"; // Hunt, Search, Chase, Lost
            public const string FixtureEvent = "FixtureEvent"; // Strike, Tick, Pop
            public const string Leaf = "Leaf";             // Lead, Follow: the two leaves of a stream double door
        }

        // Labeled parameter values are sent as their index.
        public enum Surface { Carpet = 0, CarpetTile = 1, Metal = 2 }
        public enum Gait { Walk = 0, Run = 1, Stop = 2 }
        public enum RelayGait { Walk = 0, Run = 1, Drag = 2 }
        public enum RelayState { Hunt = 0, Search = 1, Chase = 2, Lost = 3 }
        public enum FixtureKind { Strike = 0, Tick = 1, Pop = 2 }
        public enum Zone { Low = 0, Standard = 1, Tall = 2, Office = 3, Stair = 4 }
        public enum Leaf { Lead = 0, Follow = 1 }
    }
}
