using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace GrassRun
{
    /// <summary>
    /// 一局的主流程：奔跑 → 事件 → 結果敘事 → 繼續，直到沒有選項可選。
    /// 規則本身都在 <see cref="RunEngine"/>，這裡只負責時間、輸入和畫面。
    /// </summary>
    public class RunController : MonoBehaviour
    {
        enum Phase
        {
            Running,
            Choosing,
            DeadEnd,
            Result,
            Ended,
        }

        [SerializeField] GameBalance balance;
        [Tooltip("Resources 底下的事件資料夾，裡面所有 EventDefinition 都會進事件池。")]
        [SerializeField] string eventsResourcePath = "Events";

        [Header("畫面")]
        [SerializeField] HudView hud;
        [SerializeField] EventPanelView eventPanel;
        [SerializeField] ResultPanelView resultPanel;
        [SerializeField] GameOverView gameOver;
        [SerializeField] StageView stage;

        [Header("除錯")]
        [Tooltip("亂數種子。0 代表每次都不同。")]
        [SerializeField] int seed;
        [Tooltip("按住 Tab 時，奔跑加速的倍率。")]
        [SerializeField] float fastForwardScale = 8f;
        [Tooltip("設計師模式：顯示實際門檻與數值變化。執行時按 F1 切換。")]
        [SerializeField] bool designerMode;

        const int JournalLines = 4;

        readonly List<string> journal = new List<string>();
        EventDefinition[] library;
        RunEngine engine;
        Phase phase;
        float eventTimer;
        bool fastForward;

        public string PhaseName => phase.ToString();
        public RunEngine Engine => engine;

        void Start()
        {
            if (balance == null)
            {
                Debug.LogError("RunController 沒有指定 Balance 資產。", this);
                enabled = false;
                return;
            }

            library = Resources.LoadAll<EventDefinition>(eventsResourcePath);
            if (library.Length == 0)
                Debug.LogError($"Resources/{eventsResourcePath} 底下沒有任何事件。");

            StartRun();
        }

        /// <summary>開始新的一局。結算畫面的重新開始按鈕也是呼叫這裡。</summary>
        public void StartRun()
        {
            var rng = seed != 0 ? new System.Random(seed) : new System.Random();
            engine = new RunEngine(balance, library, rng);

            phase = Phase.Running;
            eventTimer = 0f;
            journal.Clear();

            eventPanel.Hide();
            resultPanel.Hide();
            gameOver.Hide();
            hud.ResetState();
            hud.SetJournal(journal);
            ClearSelection();
        }

        void Update()
        {
            HandleKeys();

            if (phase == Phase.Running)
            {
                eventTimer += Time.deltaTime * (fastForward ? fastForwardScale : 1f);
                if (eventTimer >= balance.eventInterval)
                {
                    eventTimer = 0f;
                    OpenEvent();
                }
            }

            hud.Render(engine.Stats);
            hud.SetRunningVisible(phase == Phase.Running);
            hud.SetDesigner(designerMode, designerMode ? DesignerOverlay() : null);
            if (stage != null)
                stage.SetState(engine.Stats.speed, phase == Phase.Running, fastForward ? fastForwardScale : 1f);
        }

        void HandleKeys()
        {
            var keyboard = Keyboard.current;
            fastForward = keyboard != null && keyboard.tabKey.isPressed;
            if (keyboard == null) return;

            if (keyboard.f1Key.wasPressedThisFrame) SetDesignerMode(!designerMode);

            bool confirm = keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame ||
                           keyboard.numpadEnterKey.wasPressedThisFrame;

            switch (phase)
            {
                case Phase.Choosing:
                    if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame) Choose(0);
                    else if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame) Choose(1);
                    else if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame) Choose(2);
                    break;
                case Phase.DeadEnd:
                    if (confirm) ConfirmDeadEnd();
                    break;
                case Phase.Result:
                    if (confirm) Continue();
                    break;
                case Phase.Ended:
                    if (confirm || keyboard.rKey.wasPressedThisFrame) StartRun();
                    break;
            }
        }

        void OpenEvent()
        {
            if (!engine.BeginNextEvent())
            {
                Debug.LogWarning("事件池裡沒有可抽的事件，這個時段直接跳過。");
                return;
            }

            phase = engine.IsEnded ? Phase.DeadEnd : Phase.Choosing;
            ShowEventPanel();
        }

        void ShowEventPanel()
        {
            var e = engine.CurrentEvent;
            var checks = engine.CurrentChecks;
            var lockTexts = new string[checks.Length];
            var designerTexts = new string[checks.Length];

            for (int i = 0; i < checks.Length; i++)
            {
                var option = e.options[i];
                lockTexts[i] = checks[i].available ? string.Empty : LockText(option, checks[i]);
                designerTexts[i] = DesignerText(option, engine.CurrentLevel);
            }

            eventPanel.Show(e, checks, lockTexts, designerTexts, designerMode, Choose, ConfirmDeadEnd);
            ClearSelection();
        }

        string LockText(EventOption option, OptionCheck check)
        {
            if (!string.IsNullOrWhiteSpace(option.lockedText) && check.lockKind == LockKind.Requirement)
                return option.lockedText;

            bool far = check.lockKind == LockKind.Requirement && balance.IsFarGap(check.gap, check.threshold);
            return GameText.LockReason(check, far);
        }

        string DesignerText(EventOption option, int level)
        {
            var sb = new StringBuilder();
            if (option.requirements == null || option.requirements.Length == 0)
            {
                sb.Append("無門檻");
            }
            else
            {
                for (int i = 0; i < option.requirements.Length; i++)
                {
                    if (i > 0) sb.Append("，");
                    var requirement = option.requirements[i];
                    sb.Append(GameText.RequirementDebug(requirement, balance.Threshold(requirement.tier, level)));
                }
            }

            sb.Append('\n');
            var net = RunRules.NetChange(option);
            bool any = false;
            for (int i = 0; i < StatBlock.StatCount; i++)
            {
                var stat = (StatType)i;
                if (net[stat] == 0) continue;
                if (any) sb.Append("　");
                sb.Append(GameText.StatName(stat)).Append(net[stat] > 0 ? " +" : " ").Append(net[stat]);
                any = true;
            }
            if (!any) sb.Append("數值不變");
            return sb.ToString();
        }

        public void Choose(int index)
        {
            if (phase != Phase.Choosing) return;
            var checks = engine.CurrentChecks;
            if (index < 0 || index >= checks.Length || !checks[index].available) return;

            var result = engine.Choose(index);
            eventPanel.Hide();

            string hint = result.option.hintText;
            AddJournal(string.IsNullOrWhiteSpace(hint) ? result.option.resultText : hint);

            phase = Phase.Result;
            resultPanel.Show(result.option.title, result.option.resultText, hint,
                GameText.ChangeSummary(result.applied), Continue);
            ClearSelection();
        }

        public void Continue()
        {
            if (phase != Phase.Result) return;

            resultPanel.Hide();
            phase = Phase.Running;
            ClearSelection();
        }

        public void ConfirmDeadEnd()
        {
            if (phase != Phase.DeadEnd) return;

            eventPanel.Hide();
            phase = Phase.Ended;
            gameOver.Show(GameText.PlaceholderTitle, StartRun);
            ClearSelection();
        }

        void AddJournal(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return;
            journal.Add(line);
            if (journal.Count > JournalLines) journal.RemoveAt(0);
            hud.SetJournal(journal);
        }

        string DesignerOverlay()
        {
            int cycle = engine.Cycle;
            var stats = engine.Stats;
            var sb = new StringBuilder();
            sb.Append("設計師模式（F1）\n");
            sb.Append($"第 {cycle + 1} 週期　事件 {engine.EventIndexInCycle + 1}/{balance.eventsPerCycle}　狀態 {phase}\n");
            sb.Append($"隨機事件門檻 L{cycle}：低 {balance.Threshold(Tier.Low, cycle)}　中 {balance.Threshold(Tier.Mid, cycle)}　高 {balance.Threshold(Tier.High, cycle)}\n");
            sb.Append($"檢驗點門檻 L{cycle + 1}：低 {balance.Threshold(Tier.Low, cycle + 1)}　中 {balance.Threshold(Tier.Mid, cycle + 1)}　高 {balance.Threshold(Tier.High, cycle + 1)}\n");
            sb.Append($"數值 {stats.speed}/{stats.moisture}/{stats.toughness}");
            if (engine.CurrentEvent != null) sb.Append($"\n目前事件：{engine.CurrentEvent.name}");
            return sb.ToString();
        }

        /// <summary>
        /// 清掉 UI 的選取狀態，避免滑鼠點過的按鈕之後被空白鍵或 Enter 再觸發一次。
        /// </summary>
        static void ClearSelection()
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }

        public void SetDesignerMode(bool on)
        {
            designerMode = on;
            eventPanel.SetDesigner(on);
        }

        /// <summary>除錯用：不等計時，立刻觸發下一個事件。</summary>
        public void DebugOpenEventNow()
        {
            if (phase != Phase.Running) return;
            eventTimer = 0f;
            OpenEvent();
        }
    }
}
