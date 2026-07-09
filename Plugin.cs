using System.Globalization;
using BepInEx.Configuration;
using TMPro;
using UnityEngine.SceneManagement;

namespace AutobuyOrb;

[BepInPlugin(PluginGuid, PluginName, PluginVer)]
[HarmonyPatch]
public class Plugin : BaseUnityPlugin
{
    public enum KeyModifier
    {
        Ctrl,
        Shift,
        Alt,
        LCtrl,
        RCtrl,
        LShift,
        RShift,
        LAlt,
        RAlt
    }

    public const string PluginGuid = "IngoH.OrbOfCreation.AutoBuyOrb";
    public const string PluginName = "AutobuyOrb";
    public const string PluginVer = "1.1.4";

    internal static ManualLogSource Log;
    internal static readonly Harmony Harmony = new(PluginGuid);

    internal static string PluginPath;

    private static bool _renderNumAffordable;

    private int _listening;
    internal int ShiftAmount;
    
    private readonly KeyCode[] _modifierKeys =
    [
        KeyCode.LeftControl, KeyCode.RightControl, KeyCode.LeftShift, KeyCode.RightShift, KeyCode.LeftAlt,
        KeyCode.RightAlt
    ];

    public static Plugin Instance { get; private set; }

    public ConfigEntry<int> MaxBulkBuy { get; private set; }
    public ConfigEntry<int> MinBuy { get; private set; }
    public ConfigEntry<bool> BuyMoreThanMin { get; private set; }
    public ConfigEntry<float> BuyInterval { get; private set; }
    public ConfigEntry<bool> CheapestFirst { get; private set; }
    public ConfigEntry<float> MaxTimeFramePct { get; private set; }
    public ConfigEntry<bool> RespectActionMultiplier { get; private set; }
    public ConfigEntry<int> TweenLimitOverride { get; private set; }
    public ConfigEntry<string> CycleAutoBuyModeKeybind { get; private set; }
    public ConfigEntry<string> CycleAutoBuyModeReverseKeybind { get; private set; }
    public ConfigEntry<string> CycleMinBuyKeybind { get; private set; }
    public ConfigEntry<string> CycleMinBuyReverseKeybind { get; private set; }
    public ConfigEntry<string> BuyMaxKeybind { get; private set; }
    public ConfigEntry<string> ToggleRenderAffordableKeybind { get; private set; }
    public ConfigEntry<bool> CheatBypassQueueLimit { get; private set; }


    private void Awake()
    {
        Log = Logger;
        Instance = this;
        PluginPath = Path.GetDirectoryName(Info.Location);
        gameObject.AddComponent<AutoBuyer>();
        DefineConfig();
        if (TweenLimitOverride.Value > 0 && TweenLimitOverride.Value != LeanTween.maxSimulataneousTweens)
        {
            if (LeanTween.tweens != null)
            {
                Logger.LogError(
                    "Failed to override tween limit: LeanTween has already been initialized. Please restart the game to apply the new tween limit.");
                return;
            }

            LeanTween.init(TweenLimitOverride.Value, TweenLimitOverride.Value);
        }
    }

    private void Update()
    {
        if (Keybind.Of(ToggleRenderAffordableKeybind.Value).IsPressed())
        {
            _renderNumAffordable = !_renderNumAffordable;
        }
    }


    private void OnEnable()
    {
        Harmony.PatchAll();
        Logger.LogInfo($"Loaded {PluginName}!");
    }

    private void OnDisable()
    {
        Harmony.UnpatchSelf();
        Logger.LogInfo($"Unloaded {PluginName}!");
    }

    private Vector2 scrollPos;

    private void OnGUI()
    {
        if (SceneManager.GetActiveScene().name == "Start")
        {
            var (w, h) = (Screen.width, Screen.height);
            var ratio = w / 2560f;
            GUI.skin.label.fontSize = (int)(18 * ratio);
            GUI.skin.button.fontSize = (int)(16 * ratio);
            GUI.skin.textField.fontSize = (int)(16 * ratio);
            GUI.skin.toggle.fontSize = (int)(16 * ratio);
            GUI.skin.box.fontSize = (int)(24 * ratio);
            GUI.Box(new Rect(0.725f * w, 0.075f * h, 0.25f * w, 0.35f * h), "AutobuyOrb Settings");
            if (_listening == 0)
            {
                var totalHeight = 20 + Config.Keys.Count(k => k.Section != "Cheats") * 0.05f * h;
                scrollPos = GUI.BeginScrollView(new Rect(0.75f * w, 0.125f * h, 0.2f * w, 0.275f * h),
                    scrollPos, new Rect(0, 0, 0.15f * w - 20, totalHeight));
                
                foreach(var entry in Config.Keys)
                {
                    var configEntry = Config[entry.Section, entry.Key];
                    var entryName = entry.Key;
                    if (entryName == "TweenLimitOverride")
                    {
                        entryName += " (requires restart)";
                    }
                    switch (entry.Section)
                    {
                        case "Keybinds":
                            GUI.Label(new Rect(0, 0.05f * h * Array.IndexOf(Config.Keys.ToArray(), entry), w, 24 * ratio + 4),
                                $"{entryName.CamelToSpaces()} Keybind: {configEntry.BoxedValue}");
                            if (GUI.Button(
                                    new Rect(0, 0.05f * h * Array.IndexOf(Config.Keys.ToArray(), entry) + 24 * ratio + 4,
                                        0.2f * w - 20, 24 * ratio + 4), "Set Keybind"))
                            {
                                _listening = Array.IndexOf(Config.Keys.ToArray(), entry) + 1;
                            }

                            break;
                        case "Cheats":
                            break; // Cheats are not editable in the GUI, as they are not intended to be used by most users. They are only here for testing and debugging purposes.
                        default:
                            if (configEntry is ConfigEntry<int> intEntry)
                            {
                                GUI.Label(new Rect(0, 0.05f * h * Array.IndexOf(Config.Keys.ToArray(), entry), w, 24 * ratio + 4),
                                    $"{entryName.CamelToSpaces()}: {SpecialFormatting(entryName, intEntry.Value)}");
                                var contentStr = GUI.TextField(
                                    new Rect(0, 0.05f * h * Array.IndexOf(Config.Keys.ToArray(), entry) + 24 * ratio + 4,
                                        0.2f * w - 20, 24 * ratio + 4),
                                    intEntry.Value.ToString());
                                if (int.TryParse(contentStr, out var newIntVal))
                                {
                                    intEntry.Value = newIntVal;
                                    if (intEntry.Description.AcceptableValues is AcceptableValueRange<int> range)
                                    {
                                        if (intEntry.Value < range.MinValue)
                                        {
                                            intEntry.Value = range.MinValue;
                                        }
                                        else if (intEntry.Value > range.MaxValue)
                                        {
                                            intEntry.Value = range.MaxValue;
                                        }
                                    }
                                }
                            }
                            else if (configEntry is ConfigEntry<float> floatEntry)
                            {
                                GUI.Label(new Rect(0, 0.05f * h * Array.IndexOf(Config.Keys.ToArray(), entry), w, 24 * ratio + 4),
                                    $"{entryName.CamelToSpaces()}: {SpecialFormatting(entryName,floatEntry.Value)}");
                                var contentStr = GUI.TextField(
                                    new Rect(0, 0.05f * h * Array.IndexOf(Config.Keys.ToArray(), entry) + 24 * ratio + 4,
                                        0.2f * w - 20, 24 * ratio + 4),
                                    floatEntry.Value.ToString(CultureInfo.InvariantCulture));
                                if (float.TryParse(contentStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var newFloatVal))
                                {
                                    floatEntry.Value = newFloatVal;
                                    if (floatEntry.Description.AcceptableValues is AcceptableValueRange<float> range)
                                    {
                                        if (floatEntry.Value < range.MinValue)
                                        {
                                            floatEntry.Value = range.MinValue;
                                        }
                                        else if (floatEntry.Value > range.MaxValue)
                                        {
                                            floatEntry.Value = range.MaxValue;
                                        }
                                    }
                                }
                            }
                            else if (configEntry is ConfigEntry<bool> boolEntry)
                            {
                                GUI.Label(new Rect(0, 0.05f * h * Array.IndexOf(Config.Keys.ToArray(), entry), w, 24 * ratio + 4),
                                    $"{entryName.CamelToSpaces()}: {(boolEntry.Value ? "On" : "Off")}");
                                boolEntry.Value = GUI.Toggle(
                                    new Rect(0, 0.05f * h * Array.IndexOf(Config.Keys.ToArray(), entry) + 24 * ratio + 4,
                                        0.2f * w - 20, 24 * ratio + 4), boolEntry.Value, boolEntry.Value ? "On" : "Off");
                            }
                            else
                            {
                                throw new ArgumentOutOfRangeException($"Unsupported config entry type: {configEntry.GetType()}");
                            }

                            break;
                    }
                }
                GUI.EndScrollView();
            }
            else
            {
                GUI.Label(new Rect(0.775f * w, 0.15f * h, 0.2f * w, 24 * ratio), "Press a key to set the keybind...");
                var key = Event.current.keyCode;
                if (key != KeyCode.None && !_modifierKeys.Contains(key))
                {
                    var modifiers = new List<KeyModifier>();
                    if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                    {
                        modifiers.Add(KeyModifier.Ctrl);
                    }

                    if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                    {
                        modifiers.Add(KeyModifier.Shift);
                    }

                    if (Input.GetKey(KeyCode.LeftAlt))
                    {
                        modifiers.Add(KeyModifier.LAlt);
                    }

                    if (Input.GetKey(KeyCode.RightAlt))
                    {
                        modifiers.Add(KeyModifier.RAlt);
                    }

                    var newKeybind = new Keybind(key, modifiers);
                    ConfigEntry<string> configEntry = null;
                    if (_listening > 0 && _listening <= Config.Keys.Count)
                    {
                        var entry = Config.Keys.ToArray()[_listening - 1];
                        configEntry = Config[entry.Section, entry.Key] as ConfigEntry<string>;
                    }
                    if (configEntry != null)
                    {
                        configEntry.Value = newKeybind.ToString();
                    }
                    _listening = 0;
                }
            }
        }
    }
            
    private string SpecialFormatting(string entryName, int value)
    {
        switch (entryName)
        {
            case "MaxBulkBuy":
                if (value < 0)
                {
                    return $"Leave {-value} queue spots free";
                }
                if (value == 9999)
                {
                    return Utils.BeautifyInt(9999);
                }

                break;
            case "MinBuy":
                if (value == 0)
                {
                    return "Bulk development amount";
                }
                break;
            case "TweenLimitOverride (requires restart)":
                if (value == 0)
                {
                    return "Default (400)";
                }
                break;
        }
        return Utils.BeautifyInt(value);
    }
            
            
    private string SpecialFormatting(string entryName, float value)
    {
        switch (entryName)
        {
            case "BuyInterval":
                return Utils.BeautifyNumber(value) + "s";
            case "MaxTimeFramePct":
                if (value == 0)
                {
                    return "Unlimited";
                }
                return Utils.BeautifyNumber(value) + "%";
        }
        return Utils.BeautifyNumber(value);
    }

    private void DefineConfig()
    {
        MaxBulkBuy = Config.Bind("General", "MaxBulkBuy", 1, new ConfigDescription(description:
            "The maximum number of attributes the autobuyer will buy in one update. Set to a negative number to limit purchases so that the given number of queue spots are left free. Does not apply to buy max. High values may result in performance issues. The autobuyer has a hard limit of 9999. Setting this to 0 is equivalent to setting it to 9999.",
            acceptableValues: new AcceptableValueRange<int>(-9999, 9999)));
        MinBuy = Config.Bind("General", "MinBuy", 0, new ConfigDescription(description:
            "The minimum number of attributes of one type that must be affordable before the autobuyer will buy any of that type. Set to 0 to bulk development amount. This setting can also be changed using the CycleMinBuy keybind, which will modify this config value.",
            acceptableValues: new AcceptableValueRange<int>(0, 1000000)));
        BuyMoreThanMin = Config.Bind("General", "BuyMoreThanMin", false,
            "Whether to buy more than the minimum number of attributes when the autobuyer is set to buy at least a certain number. If enabled, the autobuyer will buy as many attributes of the same type as possible, up to the max bulk buy limit. If disabled, the autobuyer will only buy the minimum number of attributes of each type at once.");
        BuyInterval = Config.Bind("General", "BuyInterval", 0f, new ConfigDescription(description:
            "The interval between autobuy actions, in seconds. Set to 0 to run the autobuyer every frame.",
            acceptableValues: new AcceptableValueRange<float>(0f, 3600f)));
        CheapestFirst = Config.Bind("General", "CheapestFirst", false,
            "Whether to buy the attributes in order from cheapest to most expensive. If disabled, the autobuyer will buy attributes that meet the cost and minimum buy requirements in arbitrary order in a cycle (each tick, the next attribute will be considered first). Buying cheapest first is significantly slower, but may be more efficient.");
        MaxTimeFramePct = Config.Bind("General", "MaxTimeFramePct", 50f, new ConfigDescription(description:
            "The maximum percentage of a frame that the autobuyer is allowed to run for. This is to prevent the autobuyer from causing the game to lag. Set to 0 to disable this limit. This is a percentage, not a float, so a value of 50 means 50% of a frame. If the frame limit is set to unlimited, the maximum frame time will be considered as if the frame limit was 60 FPS.",
            acceptableValues: new AcceptableValueRange<float>(0f, 1000f)));
        RespectActionMultiplier = Config.Bind("General", "RespectActionMultiplier", false,
            "Whether to respect the action multiplier when autobuying attributes, e.g. max actions. This may result in more resources being spent than intended. If disabled, only one attribute will be purchased at a time regardless of the action multiplier. Be careful with this enabled, as holding Shift or Ctrl changes the action multiplier.");
        TweenLimitOverride = Config.Bind("General", "TweenLimitOverride", 4000, new ConfigDescription(description:
            "Overrides the default tween limit for the game. When autobuying lots of attributes at once, the popups may cause the tween limit to be exceeded and result in the UI breaking. This setting allows you to increase the limit to avoid that. Set to 0 to use the default limit (400). Requires a restart of the game to take effect.",
            acceptableValues: new AcceptableValueRange<int>(0, 1000000)));
        CycleAutoBuyModeKeybind = Config.Bind("Keybinds", "CycleAutoBuyMode", new Keybind(KeyCode.Period,
            [KeyModifier.LAlt]).ToString(), "Keybind to cycle through autobuy modes.");
        CycleAutoBuyModeReverseKeybind = Config.Bind("Keybinds", "CycleAutoBuyModeReverse", new Keybind(KeyCode.Comma,
            [KeyModifier.LAlt]).ToString(), "Keybind to cycle through autobuy modes in reverse.");
        CycleMinBuyKeybind = Config.Bind("Keybinds", "CycleMinBuy", new Keybind(KeyCode.Equals,
            [KeyModifier.LAlt]).ToString(), "Keybind to cycle through minimum buy modes.");
        CycleMinBuyReverseKeybind = Config.Bind("Keybinds", "CycleMinBuyReverse", new Keybind(KeyCode.Minus,
            [KeyModifier.LAlt]).ToString(), "Keybind to cycle through minimum buy modes in reverse.");
        BuyMaxKeybind = Config.Bind("Keybinds", "BuyMax", new Keybind(KeyCode.M, [KeyModifier.LAlt]).ToString(),
            "Keybind to buy as many attributes as possible in one update, ignoring any autobuy and max bulk buy limits. This is a one-off action, and will not change the autobuy mode.");
        ToggleRenderAffordableKeybind = Config.Bind("Keybinds", "ToggleRenderAffordable",
            new Keybind(KeyCode.F1).ToString(), "Keybind to toggle rendering the number of affordable attributes.");
        CheatBypassQueueLimit = Config.Bind("Cheats", "CheatBypassQueueLimit", false,
            "Whether to bypass the queue limit when autobuying attributes. This is a cheat option! Use at your own risk. This will absolutely cause significant performance issues if many attributes are affordable and the autobuyer is set to buy them all.");
    }

    [HarmonyPatch(typeof(UIStructureItem), nameof(UIStructureItem.RenderContent))]
    [HarmonyPostfix]
    public static void RenderAmountAffordable(UIStructureItem __instance)
    {
        if (!_renderNumAffordable) return;
        var s = __instance.item;
        var numAffordable = 0;
        var noRoom = false;
        var resTracker = new Dictionary<ResourceSO, BigDouble>();
        foreach (var tuple in s.baseCost.costs) resTracker[tuple.resource] = tuple.resource.quantity;
        for (var i = 0; i < ActionManager.GetRemainingRoom() + 1; i++)
        {
            var cost = s.baseCost.AdjustAsAttribute()
                .AdjustWith(s.costPerQuantity.GetModifier().MultiplyScalar(s.costScalingMod.AsPercent())
                    .MultiplyScalar(s.quantity + s.queuedQuantity + i)).Multiply(s.GetNextCostMod().AsPercent())
                .RoundToTwoSigsEarly().AdjustByQualityCost();
            foreach (var tuple in cost.costs)
            {
                if (!resTracker.ContainsKey(tuple.resource) || resTracker[tuple.resource] < tuple.GetValue())
                {
                    goto exit;
                }

                resTracker[tuple.resource] -= tuple.GetValue();
            }

            numAffordable++;
        }

        noRoom = true;
        numAffordable -=
            1; // The loop goes one extra time to check if there's room for one more, so we need to subtract 1 from the count.
        exit:
        if (numAffordable > 0)
        {
            __instance.quantityElement.text =
                $"{(s.IsQueued() ? "+" + Utils.BeautifyInt(s.GetQueuedQuantity()) : Utils.BeautifyInt(s.GetBaseLevel()))} [{Utils.BeautifyInt(numAffordable)}{(noRoom ? "*" : "")}]";
        }
    }
    
    [HarmonyPatch(typeof(ActionManager), nameof(ActionManager.GetRemainingRoom))]
    [HarmonyPostfix]
    public static void BypassQueueLimit(ref int __result)
    {
        if (Instance.CheatBypassQueueLimit.Value)
        {
            __result = 9999;
        }
    }

    public class Keybind
    {
        private readonly Tuple<List<KeyModifier>, KeyCode> _keybind;

        public Keybind(KeyCode key, List<KeyModifier> modifiers = null)
        {
            if (modifiers == null)
            {
                modifiers = new List<KeyModifier>();
            }

            _keybind = new Tuple<List<KeyModifier>, KeyCode>(modifiers, key);
        }

        public bool IsPressed()
        {
            foreach (var mod in _keybind.Item1)
                switch (mod)
                {
                    case KeyModifier.Ctrl:
                        if (!Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl)) return false;
                        break;
                    case KeyModifier.Shift:
                        if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift)) return false;
                        break;
                    case KeyModifier.Alt:
                        if (!Input.GetKey(KeyCode.LeftAlt) && !Input.GetKey(KeyCode.RightAlt)) return false;
                        break;
                    case KeyModifier.LCtrl:
                        if (!Input.GetKey(KeyCode.LeftControl)) return false;
                        break;
                    case KeyModifier.RCtrl:
                        if (!Input.GetKey(KeyCode.RightControl)) return false;
                        break;
                    case KeyModifier.LShift:
                        if (!Input.GetKey(KeyCode.LeftShift)) return false;
                        break;
                    case KeyModifier.RShift:
                        if (!Input.GetKey(KeyCode.RightShift)) return false;
                        break;
                    case KeyModifier.LAlt:
                        if (!Input.GetKey(KeyCode.LeftAlt)) return false;
                        break;
                    case KeyModifier.RAlt:
                        if (!Input.GetKey(KeyCode.RightAlt)) return false;
                        break;
                }

            return Input.GetKeyDown(_keybind.Item2);
        }

        public override string ToString()
        {
            if (_keybind.Item1.Count == 0)
            {
                return _keybind.Item2.ToString();
            }

            return string.Join("+", _keybind.Item1) + "+" + _keybind.Item2;
        }

        public static Keybind Of(string str)
        {
            return str;
        }

        public static implicit operator Keybind(KeyCode key)
        {
            return new Keybind(key);
        }

        public static implicit operator Keybind(Tuple<List<KeyModifier>, KeyCode> tuple)
        {
            return new Keybind(tuple.Item2, tuple.Item1);
        }

        public static implicit operator Keybind(string str)
        {
            var parts = str.Split('+');
            var modifiers = new List<KeyModifier>();
            var key = KeyCode.None;
            foreach (var part in parts)
                if (Enum.TryParse(part, out KeyModifier mod))
                {
                    modifiers.Add(mod);
                }
                else if (Enum.TryParse(part, out KeyCode k))
                {
                    if (key != KeyCode.None)
                    {
                        throw new ArgumentException($"Invalid keybind string: {str}. Multiple keys specified.");
                    }

                    key = k;
                }
                else
                {
                    throw new ArgumentException($"Invalid keybind string: {str}. Unknown part: {part}");
                }

            if (key == KeyCode.None)
            {
                throw new ArgumentException($"Invalid keybind string: {str}");
            }

            return new Keybind(key, modifiers);
        }

        public static implicit operator string(Keybind keybind)
        {
            return keybind.ToString();
        }
    }
}

public class AutoBuyer : MonoBehaviour
{
    private static readonly Dictionary<int, string> MODE_NAMES = new()
    {
        { 1, "Buy all" },
        { 2, "Buy at 10 times excess" },
        { 3, "Buy at 100 times excess" },
        { 4, "Buy at 1000 times excess" }
    };

    public static readonly int[] MIN_BUY_VALUES = [0, 1, 2, 3, 5, 10, 100];

    private float startDelay;
    private float delay;
    private int mode;
    private float previewTime = 10;
    private float showMinBuy;

    public void Update()
    {
        var oneOff = false;
        if (SceneManager.GetActiveScene().name != "Main") return;
        if (Plugin.Keybind.Of(Plugin.Instance.CycleAutoBuyModeKeybind.Value).IsPressed())
        {
            mode = (mode + 1) % 5;
            startDelay = 5;
        }

        if (Plugin.Keybind.Of(Plugin.Instance.CycleAutoBuyModeReverseKeybind.Value).IsPressed())
        {
            mode = (mode + 4) % 5;
            startDelay = 5;
        }
        
        if (Plugin.Keybind.Of(Plugin.Instance.CycleMinBuyKeybind.Value).IsPressed())
        {
            Plugin.Instance.MinBuy.Value = MIN_BUY_VALUES[(Array.IndexOf(MIN_BUY_VALUES, Plugin.Instance.MinBuy.Value) + 1) % MIN_BUY_VALUES.Length];
            showMinBuy = 5;
        }

        if (Plugin.Keybind.Of(Plugin.Instance.CycleMinBuyReverseKeybind.Value).IsPressed())
        {
            Plugin.Instance.MinBuy.Value = MIN_BUY_VALUES[(Array.IndexOf(MIN_BUY_VALUES, Plugin.Instance.MinBuy.Value) + 6) % MIN_BUY_VALUES.Length];
            showMinBuy = 5;
        }

        if (Plugin.Keybind.Of(Plugin.Instance.BuyMaxKeybind.Value).IsPressed())
        {
            oneOff = true;
        }
#if !BEPINEX
            // Support for manually destroying the GameObject in environments where the component may be created multiple times (e.g. UnityExplorer's C# console). This is not needed in BepInEx since the component is guaranteed to only exist once, and we don't want users to accidentally destroy it.
            if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.Backspace))
            {
                Destroy(this.gameObject);
            }
#endif
        if (previewTime > 0)
        {
            previewTime -= Time.unscaledDeltaTime;
        }
        
        if (startDelay > 0)
        {
            startDelay -= Time.unscaledDeltaTime;
        }
        
        if (showMinBuy > 0)
        {
            showMinBuy -= Time.unscaledDeltaTime;
        }

        if (mode == 0 && !oneOff) return;
        if (startDelay > 0 && !oneOff)
        {
            return;
        }
        if (delay > 0 && !oneOff)
        {
            delay -= Time.unscaledDeltaTime;
            return;
        }

        var maxBulkBuy = Plugin.Instance.MaxBulkBuy.Value;
        if (maxBulkBuy < 0)
        {
            maxBulkBuy = Math.Max(0, ActionManager.GetRemainingRoom() + maxBulkBuy);
            if (maxBulkBuy <= 0 && ActionManager.instance.actionableItems.maxQueuedItems.AsInt() ==
                ActionManager.GetRemainingRoom()) // The autobuyer can buy one attribute if the queue is completely empty if the minimum queue space left is set to be greater than or equal to the maximum queue size. This is to prevent the autobuyer from being completely disabled in this case.
            {
                maxBulkBuy = 1;
            }

            if (maxBulkBuy == 0)
            {
                return;
            }
        }
        if (maxBulkBuy > ActionManager.GetRemainingRoom())
        {
            maxBulkBuy = ActionManager.GetRemainingRoom();
        }
        if (maxBulkBuy > 9999)
        {
            maxBulkBuy = 9999;
        }

        var numPurchased = 0;
        var temp = GlobalVariables.GetMultiBuy().AsInt();
        if (!Plugin.Instance.RespectActionMultiplier.Value)
        {
            GlobalVariables.GetMultiBuy().SetValue(1);
        }
        
        var targetFrameTime = Application.targetFrameRate > 0 ? 1f / Application.targetFrameRate : 1f / 60f;
        var maxTime = targetFrameTime * (Plugin.Instance.MaxTimeFramePct.Value / 100f);
        if (Plugin.Instance.MaxTimeFramePct.Value <= 0)
        {
            maxTime = float.MaxValue;
        }
        var startTime = DateTime.UtcNow;
        
        var minBuy = Plugin.Instance.MinBuy.Value;
        if (minBuy == 0)
        {
            minBuy = Player.GetBulkDevelopment();
        }
        
        if (minBuy > ActionManager.instance.actionableItems.maxQueuedItems.AsInt() && !Plugin.Instance.CheatBypassQueueLimit.Value)
        {
            minBuy = ActionManager.instance.actionableItems.maxQueuedItems.AsInt();
        }

        if (oneOff)
        {
            minBuy = 1;
        }
        
        if (minBuy > ActionManager.GetRemainingRoom())
        {
            return;
        }

        var its = 0;
        while (ActionManager.GetRemainingRoom() > 0 && its < 9999)
        {
            its++;
            StructureSO mins = null;
            BigDouble baseMinAmt = oneOff ? 1 : 1 * Math.Pow(10, 1 - mode);
            BigDouble minAmt = baseMinAmt;
            int minAffordable = 0;
            foreach (var s in StructureSO.All.Shift(Plugin.Instance.ShiftAmount))
            {
                if (s.IsAvailable())
                {
                    BigDouble max = 0;
                    foreach (var c in s.GetNextCost().costs)
                    {
                        // Dividing by 0 normally returns infinity, which would be fine. However, for small numbers, it actually returns Infinity to a negative exponent, which is essentially 1/Infinity and thus would always be considered the smallest ratio. To avoid this, we check for 0 and set max to infinity in that case.
                        if (c.resource.quantity <= 0)
                        {
                            max = double.MaxValue;
                            break;
                        }

                        var r = c.resource.GetTrueSpend(c.GetValue()) / c.resource.quantity;
                        if (r > max)
                        {
                            max = r;
                        }
                    }

                    var eligible = false;
                    if ((minBuy > 1 || Plugin.Instance.BuyMoreThanMin.Value) && max < baseMinAmt)
                    {
                        int numAffordable = 0;
                        var resTracker = new Dictionary<ResourceSO, BigDouble>();
                        foreach (var tuple in s.baseCost.costs) resTracker[tuple.resource] = tuple.resource.quantity * baseMinAmt;
                        for (var i = 0; i < ActionManager.GetRemainingRoom() + 1; i++)
                        {
                            var cost = s.baseCost.AdjustAsAttribute()
                                .AdjustWith(s.costPerQuantity.GetModifier().MultiplyScalar(s.costScalingMod.AsPercent())
                                    .MultiplyScalar(s.quantity + s.queuedQuantity + i))
                                .Multiply(s.GetNextCostMod().AsPercent())
                                .RoundToTwoSigsEarly().AdjustByQualityCost();
                            foreach (var tuple in cost.costs)
                            {
                                if (!resTracker.ContainsKey(tuple.resource) ||
                                    resTracker[tuple.resource] < tuple.GetValue())
                                {
                                    goto exit;
                                }

                                resTracker[tuple.resource] -= tuple.GetValue();
                            }
                            numAffordable++;
                        }
                        numAffordable -= 1;
                        exit:
                        if (numAffordable < minBuy)
                        {
                            continue;
                        }
                        if (numAffordable > minAffordable)
                        {
                            minAffordable = numAffordable;
                            minAmt = max;
                            mins = s;
                            eligible = true;
                        }
                        else // if equal
                        {
                            if (max < minAmt)
                            {
                                minAmt = max;
                                mins = s;
                                eligible = true;
                            }
                        }
                    }
                    else
                    {
                        if (max < minAmt)
                        {
                            eligible = true;
                            minAffordable = 1;
                            minAmt = max;
                            mins = s;
                        }
                    }

                    // If we are not buying the cheapest first, we can break early if we found an eligible structure and buy it.
                    if (eligible && !Plugin.Instance.CheapestFirst.Value)
                    {
                        break;
                    }
                }
            }
            Plugin.Instance.ShiftAmount++;
            if (Plugin.Instance.ShiftAmount >= StructureSO.All.Count)
            {
                Plugin.Instance.ShiftAmount = 0;
            }
            if (mins != null)
            {
                var numToBuy = Math.Min(Plugin.Instance.BuyMoreThanMin.Value ? minAffordable : minBuy, maxBulkBuy - numPurchased);
                for (var i = 0; i < numToBuy; i++)
                {
                    mins.Purchase();
                }
                numPurchased += numToBuy;
            }
            else
            {
                break;
            }

            if ((!oneOff && maxBulkBuy > 0 && numPurchased >= maxBulkBuy) || (DateTime.UtcNow - startTime).TotalSeconds > maxTime)
            {
                break;
            }
        }

        if (!Plugin.Instance.RespectActionMultiplier.Value)
        {
            GlobalVariables.GetMultiBuy().SetValue(temp);
        }

        delay = Plugin.Instance.BuyInterval.Value;
    }

    public void OnGUI()
    {
        var showTime = Math.Max(previewTime, showMinBuy);
        if (SceneManager.GetActiveScene().name != "Main" || (mode == 0 && showTime <= 0)) return;
        var (w, h) = (Screen.width, Screen.height);
        var style = new GUIStyle(GUI.skin.label);
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = 18;
        style.fontStyle = FontStyle.Bold;
        var opacity = 1f;
        if (mode == 0 && showTime < 5)
        {
            opacity = showTime / 5;
        }

        style.normal.textColor = new Color(1, 1, 1, opacity);
        var outlineStyle = new GUIStyle(style);
        outlineStyle.normal.textColor = new Color(0, 0, 0, opacity);
        Vector2[] offsets =
        [
            new(-1, -1),
            new(-1, 0),
            new(-1, 1),
            new(0, -1),
            new(0, 1),
            new(1, -1),
            new(1, 0),
            new(1, 1)
        ];
        var text =
            $"AutoBuyOrb loaded! Use {Plugin.Instance.CycleAutoBuyModeKeybind.Value}/{Plugin.Instance.CycleAutoBuyModeReverseKeybind.Value} to cycle modes";
        if (mode > 0)
        {
            text = $"AutoBuyOrb Mode: {MODE_NAMES[mode]}{(startDelay > 0 ? $" [{startDelay:F1}]" : "")}";
        }
        
        if (showMinBuy > 0)
        {
            var minBuyName = Plugin.Instance.MinBuy.Value == 0 ? "Bulk Development Amount" : Plugin.Instance.MinBuy.Value.ToString();
            if (previewTime > 0)
            {
                text += $" | Min Buy: {minBuyName}";
            }
            else
            {
                text = $"AutoBuyOrb Min Buy: {minBuyName}";
            }
        }

        foreach (var offset in offsets) GUI.Label(new Rect(offset.x, h - 28 + offset.y, w, 28), text, outlineStyle);
        GUI.Label(new Rect(0, h - 28, w, 28), text, style);
    }
}

public static class ListExtensions
{
    public static List<T> Shift<T>(this List<T> list, int amount)
    {
        if (list.Count == 0) return list;
        amount %= list.Count;
        if (amount < 0) amount += list.Count;
        return list.Skip(amount).Concat(list.Take(amount)).ToList();
    }
}

public static class StringExtensions
{
    public static string CamelToSpaces(this string str)
    {
        if (string.IsNullOrEmpty(str)) return str;
        var newStr = new StringBuilder();
        newStr.Append(str[0]);
        for (var i = 1; i < str.Length; i++)
        {
            if (char.IsUpper(str[i]) && !char.IsUpper(str[i - 1]) && !char.IsWhiteSpace(str[i - 1]))
            {
                newStr.Append(' ');
            }

            newStr.Append(str[i]);
        }

        return newStr.ToString();
    }
}