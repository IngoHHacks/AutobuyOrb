using BepInEx.Configuration;
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
    public const string PluginVer = "1.1.3";

    internal static ManualLogSource Log;
    internal static readonly Harmony Harmony = new(PluginGuid);

    internal static string PluginPath;

    private static bool renderNumAffordable;

    private int _listening;

    private readonly KeyCode[] modifierKeys =
    [
        KeyCode.LeftControl, KeyCode.RightControl, KeyCode.LeftShift, KeyCode.RightShift, KeyCode.LeftAlt,
        KeyCode.RightAlt
    ];

    public static Plugin Instance { get; private set; }

    public ConfigEntry<int> MaxBulkBuy { get; private set; }
    public ConfigEntry<string> ToggleRenderAffordableKeybind { get; private set; }
    public ConfigEntry<string> CycleAutoBuyModeKeybind { get; private set; }
    public ConfigEntry<string> CycleAutoBuyModeReverseKeybind { get; private set; }
    public ConfigEntry<string> BuyMaxKeybind { get; private set; }
    public ConfigEntry<int> TweenLimitOverride { get; private set; }
    public ConfigEntry<bool> RespectActionMultiplier { get; private set; }


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
            renderNumAffordable = !renderNumAffordable;
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

    private void OnGUI()
    {
        if (SceneManager.GetActiveScene().name == "Start")
        {
            var (w, h) = (Screen.width, Screen.height);
            var ratio = w / 2560f;
            GUI.skin.label.fontSize = (int)(16 * ratio);
            GUI.skin.button.fontSize = (int)(12 * ratio);
            GUI.skin.textField.fontSize = (int)(12 * ratio);
            GUI.skin.toggle.fontSize = (int)(12 * ratio);
            GUI.skin.box.fontSize = (int)(16 * ratio);
            GUI.Box(new Rect(0.75f * w, 0.05f * h, 0.2f * w, 0.4f * h), "AutobuyOrb Settings");
            if (_listening == 0)
            {
                GUI.Label(new Rect(0.775f * w, 0.11f * h, 0.15f * w, 24 * ratio), "Max Bulk Buy:");
                var contentStr = GUI.TextField(new Rect(0.775f * w, 0.13f * h, 0.15f * w, 20 * ratio),
                    MaxBulkBuy.Value.ToString());
                if (int.TryParse(contentStr, out var newVal))
                {
                    MaxBulkBuy.Value = newVal;
                }

                if (MaxBulkBuy.Value < -1000000)
                {
                    MaxBulkBuy.Value = -1000000;
                }

                if (MaxBulkBuy.Value > 1000000)
                {
                    MaxBulkBuy.Value = 1000000;
                }

                GUI.Label(new Rect(0.775f * w, 0.15f * h, 0.15f * w, 24 * ratio),
                    "Tween Limit Override [Requires Restart]:");
                contentStr = GUI.TextField(new Rect(0.775f * w, 0.17f * h, 0.15f * w, 20 * ratio),
                    TweenLimitOverride.Value.ToString());
                if (int.TryParse(contentStr, out newVal))
                {
                    TweenLimitOverride.Value = newVal;
                }

                if (TweenLimitOverride.Value < 0)
                {
                    TweenLimitOverride.Value = 0;
                }

                if (TweenLimitOverride.Value > 1000000)
                {
                    TweenLimitOverride.Value = 1000000;
                }

                GUI.Label(new Rect(0.775f * w, 0.19f * h, 0.15f * w, 24 * ratio), "Respect Action Multiplier:");
                RespectActionMultiplier.Value = GUI.Toggle(new Rect(0.775f * w, 0.21f * h, 0.15f * w, 20 * ratio),
                    RespectActionMultiplier.Value, RespectActionMultiplier.Value ? "Enabled" : "Disabled");

                GUI.Label(new Rect(0.775f * w, 0.23f * h, 0.15f * w, 24 * ratio),
                    $"Toggle Render Affordable Keybind: {ToggleRenderAffordableKeybind.Value}");
                if (GUI.Button(new Rect(0.775f * w, 0.25f * h, 0.15f * w, 20 * ratio), "Set Keybind"))
                {
                    _listening = 1;
                }

                GUI.Label(new Rect(0.775f * w, 0.27f * h, 0.15f * w, 24 * ratio),
                    $"Cycle AutoBuy Mode Keybind: {CycleAutoBuyModeKeybind.Value}");
                if (GUI.Button(new Rect(0.775f * w, 0.29f * h, 0.15f * w, 20 * ratio), "Set Keybind"))
                {
                    _listening = 2;
                }

                GUI.Label(new Rect(0.775f * w, 0.31f * h, 0.15f * w, 24 * ratio),
                    $"Cycle AutoBuy Mode Reverse Keybind: {CycleAutoBuyModeReverseKeybind.Value}");
                if (GUI.Button(new Rect(0.775f * w, 0.33f * h, 0.15f * w, 20 * ratio), "Set Keybind"))
                {
                    _listening = 3;
                }

                GUI.Label(new Rect(0.775f * w, 0.35f * h, 0.15f * w, 24 * ratio),
                    $"Buy Max Keybind: {BuyMaxKeybind.Value}");
                if (GUI.Button(new Rect(0.775f * w, 0.37f * h, 0.15f * w, 20 * ratio), "Set Keybind"))
                {
                    _listening = 4;
                }
            }
            else
            {
                GUI.Label(new Rect(0.775f * w, 0.1f * h, 0.15f * w, 24 * ratio), "Press a key to set the keybind...");
                var key = Event.current.keyCode;
                if (key != KeyCode.None && !modifierKeys.Contains(key))
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
                    switch (_listening)
                    {
                        case 1:
                            ToggleRenderAffordableKeybind.Value = newKeybind;
                            break;
                        case 2:
                            CycleAutoBuyModeKeybind.Value = newKeybind;
                            break;
                        case 3:
                            CycleAutoBuyModeReverseKeybind.Value = newKeybind;
                            break;
                        case 4:
                            BuyMaxKeybind.Value = newKeybind;
                            break;
                    }

                    _listening = 0;
                }
            }
        }
    }

    private void DefineConfig()
    {
        MaxBulkBuy = Config.Bind("General", "MaxBulkBuy", 1,
            "The maximum number of structures the auto-buyer will buy in one update. Set to 0 for no limit. Set to a negative number to limit purchases so that the given number of queue spots are left free. Does not apply to buy max. High values may result in performance issues.");
        ToggleRenderAffordableKeybind = Config.Bind("Keybinds", "ToggleRenderAffordable",
            new Keybind(KeyCode.F1).ToString(), "Keybind to toggle rendering the number of affordable structures.");
        CycleAutoBuyModeKeybind = Config.Bind("Keybinds", "CycleAutoBuyMode", new Keybind(KeyCode.Period,
            [KeyModifier.LAlt]).ToString(), "Keybind to cycle through auto-buy modes.");
        CycleAutoBuyModeReverseKeybind = Config.Bind("Keybinds", "CycleAutoBuyModeReverse", new Keybind(KeyCode.Comma,
            [KeyModifier.LAlt]).ToString(), "Keybind to cycle through auto-buy modes in reverse.");
        BuyMaxKeybind = Config.Bind("Keybinds", "BuyMax", new Keybind(KeyCode.M, [KeyModifier.LAlt]).ToString(),
            "Keybind to buy as many structures as possible in one update, ignoring any auto-buy and max bulk buy limits. This is a one-off action, and will not change the auto-buy mode.");
        TweenLimitOverride = Config.Bind("General", "TweenLimitOverride", 4000,
            "Overrides the default tween limit for the game. When auto-buying lots of structures at once, the popups may cause the tween limit to be exceeded and result in the UI breaking. This setting allows you to increase the limit to avoid that. Set to 0 to use the default limit (400). Requires a restart of the game to take effect.");
        RespectActionMultiplier = Config.Bind("General", "RespectActionMultiplier", false,
            "Whether to respect the action multiplier when auto-buying structures, e.g. max actions. This may result in more resources being spent than intended. If disabled, only one structure will be purchased at a time regardless of the action multiplier. Be careful with this enabled, as holding Shift or Ctrl changes the action multiplier.");
    }

    [HarmonyPatch(typeof(UIStructureItem), nameof(UIStructureItem.RenderContent))]
    [HarmonyPostfix]
    public static void RenderAmountAffordable(UIStructureItem __instance)
    {
        if (!renderNumAffordable) return;
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

    private float delay;
    private int mode;
    private float previewTime = 10;

    public void Update()
    {
        var oneOff = false;
        if (SceneManager.GetActiveScene().name != "Main") return;
        if (Plugin.Keybind.Of(Plugin.Instance.CycleAutoBuyModeKeybind.Value).IsPressed())
        {
            mode = (mode + 1) % 5;
            delay = 5;
        }

        if (Plugin.Keybind.Of(Plugin.Instance.CycleAutoBuyModeReverseKeybind.Value).IsPressed())
        {
            mode = (mode + 4) % 5;
            delay = 5;
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
            previewTime -= Time.deltaTime;
        }

        if (mode == 0 && !oneOff) return;
        if (delay > 0 && !oneOff)
        {
            delay -= Time.deltaTime;
            return;
        }

        var maxBulkBuy = Plugin.Instance.MaxBulkBuy.Value;
        if (maxBulkBuy < 0)
        {
            maxBulkBuy = Math.Max(0, ActionManager.GetRemainingRoom() + maxBulkBuy);
            if (maxBulkBuy == 0)
            {
                return;
            }
        }

        var numPurchased = 0;
        var temp = GlobalVariables.GetMultiBuy().AsInt();
        if (!Plugin.Instance.RespectActionMultiplier.Value)
        {
            GlobalVariables.GetMultiBuy().SetValue(1);
        }

        while (ActionManager.GetRemainingRoom() > 0)
        {
            StructureSO mins = null;
            BigDouble minamt = oneOff ? 1 : 1 * Math.Pow(10, 1 - mode);
            foreach (var s in StructureSO.All)
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

                    if (max < minamt)
                    {
                        minamt = max;
                        mins = s;
                    }
                }

            if (mins != null)
            {
                mins.Purchase();
            }
            else
            {
                break;
            }

            numPurchased++;
            if (!oneOff && maxBulkBuy > 0 && numPurchased >= maxBulkBuy)
            {
                break;
            }
        }

        if (!Plugin.Instance.RespectActionMultiplier.Value)
        {
            GlobalVariables.GetMultiBuy().SetValue(temp);
        }
    }

    public void OnGUI()
    {
        if (SceneManager.GetActiveScene().name != "Main" || (mode == 0 && previewTime <= 0)) return;
        var (w, h) = (Screen.width, Screen.height);
        var style = new GUIStyle(GUI.skin.label);
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = 18;
        style.fontStyle = FontStyle.Bold;
        var opacity = 1f;
        if (mode == 0 && previewTime < 5)
        {
            opacity = previewTime / 5;
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
            text = $"AutoBuyOrb Mode: {MODE_NAMES[mode]}{(delay > 0 ? $" [{delay:F1}]" : "")}";
        }

        foreach (var offset in offsets) GUI.Label(new Rect(offset.x, h - 28 + offset.y, w, 28), text, outlineStyle);
        GUI.Label(new Rect(0, h - 28, w, 28), text, style);
    }
}