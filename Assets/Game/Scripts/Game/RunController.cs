using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace GrassRun
{
    /// <summary>奔跑計時、事件選擇與結果畫面的協調層。</summary>
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
        [SerializeField] string eventsResourcePath = "Events";

        [Header("畫面")]
        [SerializeField] HudView hud;
        [SerializeField] EventPanelView eventPanel;
        [SerializeField] ResultPanelView resultPanel;
        [SerializeField] GameOverView gameOver;
        [SerializeField] StageView stage;

        [Header("除錯")]
        [SerializeField] int seed;
        [SerializeField] float fastForwardScale = 8f;
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
            if (library.Length == 0) Debug.LogError($"Resources/{eventsResourcePath} 底下沒有任何事件。");
            StartRun();
        }

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
                float dt = Time.deltaTime * (fastForward ? fastForwardScale : 1f);
                eventTimer += dt;
                if (eventTimer >= balance.eventInterval)
                {
                    eventTimer = 0f;
                    OpenEvent();
                }
            }

            RefreshHud();
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
                Debug.LogWarning("目前事件批次沒有可抽的事件。");
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
                lockTexts[i] = checks[i].available ? string.Empty : GameText.LockReason(checks[i]);
                designerTexts[i] = DesignerText(e.options[i]);
            }

            eventPanel.Show(e, checks, lockTexts, designerTexts, designerMode, Choose, ConfirmDeadEnd);
            ClearSelection();
        }

        static string DesignerText(EventOption option)
        {
            var sb = new StringBuilder();
            sb.Append(string.IsNullOrEmpty(option.requirement) ? "無需求" : option.requirement);
            sb.Append('\n').Append(string.IsNullOrEmpty(option.offset) ? "數值不變" : option.offset);
            if (option.endingTitleId != 0) sb.Append("　結局 ").Append(option.endingTitleId);
            return sb.ToString();
        }

        public void Choose(int index)
        {
            if (phase != Phase.Choosing) return;
            var checks = engine.CurrentChecks;
            if (index < 0 || index >= checks.Length || !checks[index].available) return;

            var result = engine.Choose(index);
            eventPanel.Hide();
            AddJournal(result.option.resultText);

            phase = Phase.Result;
            resultPanel.Show(result.option.description, result.option.resultText, string.Empty,
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

            gameOver.Show(GameText.EndTitle(engine.EndReason), StartRun);
            ClearSelection();
        }

        void AddJournal(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return;
            journal.Add(line);
            if (journal.Count > JournalLines) journal.RemoveAt(0);
            hud.SetJournal(journal);
        }

        void RefreshHud()
        {
            hud.Render(engine.Stats);
            hud.SetRunningVisible(phase == Phase.Running);
            hud.SetDesigner(designerMode, designerMode ? DesignerOverlay() : null);
        }

        string DesignerOverlay()
        {
            var stats = engine.Stats;
            var sb = new StringBuilder();
            sb.Append("設計師模式（F1）\n");
            sb.Append($"已經歷 {engine.EventsResolved} 個事件　狀態 {phase}\n");
            sb.Append($"mor {stats.morality}　hmd {stats.moisture}　spd {stats.speed}　tgh {stats.toughness}");
            if (engine.CurrentEvent != null) sb.Append($"\n目前事件：{engine.CurrentEvent.eventId}");
            return sb.ToString();
        }

        static void ClearSelection()
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }

        public void SetDesignerMode(bool on)
        {
            designerMode = on;
            eventPanel.SetDesigner(on);
        }

        public void DebugOpenEventNow()
        {
            if (phase != Phase.Running) return;
            eventTimer = 0f;
            OpenEvent();
        }
    }
}
