using System.Collections.Generic;
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

        const int JournalLines = 4;

        readonly List<string> journal = new List<string>();
        EventDefinition[] library;
        RunEngine engine;
        int resultShownFrame;
        string endingResultText;   // 選到結局選項時，該選項的結果描述（結算畫面的死因用）
        Phase phase;
        float eventTimer;
        bool fastForward;
        CharacterAppearance characterAppearance;

        public string PhaseName => phase.ToString();
        public RunEngine Engine => engine;

        // 流程訊號：給音訊這類被動 consumer 訂閱。訂閱者只做表現，不要反過來改遊戲狀態。
        public event System.Action RunStarted;
        /// <summary>事件面板打開；參數為 true 代表三個選項全鎖定（無路可走）。</summary>
        public event System.Action<bool> EventOpened;
        public event System.Action<ChoiceResult> OptionChosen;
        public event System.Action ResultClosed;
        public event System.Action<RunEndReason> RunEnded;

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
            endingResultText = null;
            journal.Clear();

            eventPanel.Hide();
            resultPanel.Hide();
            gameOver.Hide();
            hud.ResetState();
            hud.SetJournal(journal);
            ClearSelection();
            characterAppearance = CharacterAppearance.Default;
            if (stage != null) stage.SetCharacterAppearance(characterAppearance);
            RunStarted?.Invoke();
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
            // 結果畫面：點滑鼠（或觸控）任一處也能繼續。跳過選選項那一下所在的幀，免得同一下點擊直接跳過結果。
            var pointer = Pointer.current;
            if (phase == Phase.Result && Time.frameCount > resultShownFrame &&
                pointer != null && pointer.press.wasPressedThisFrame)
            {
                Continue();
                return;
            }

            var keyboard = Keyboard.current;
            fastForward = keyboard != null && keyboard.tabKey.isPressed;
            if (keyboard == null) return;

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
            EventOpened?.Invoke(phase == Phase.DeadEnd);
        }

        void ShowEventPanel()
        {
            var e = engine.CurrentEvent;
            var checks = engine.CurrentChecks;
            eventPanel.Show(e, e.image, checks, Choose, ConfirmDeadEnd);
            ClearSelection();
        }

        public void Choose(int index)
        {
            if (phase != Phase.Choosing) return;
            var checks = engine.CurrentChecks;
            if (index < 0 || index >= checks.Length || !checks[index].available) return;

            var before = engine.Stats;
            var result = engine.Choose(index);
            characterAppearance = RunRules.ResolveCharacterAppearance(characterAppearance, before, engine.Stats);
            if (stage != null) stage.SetCharacterAppearance(characterAppearance);
            eventPanel.Hide();
            AddJournal(result.option.resultText);
            if (result.endingTitleId != 0) endingResultText = result.option.resultText;

            phase = Phase.Result;
            resultShownFrame = Time.frameCount;
            resultPanel.Show(result.option.resultText, GameText.ChangeSummary(result.applied));
            ClearSelection();
            OptionChosen?.Invoke(result);
        }

        public void Continue()
        {
            if (phase != Phase.Result) return;
            resultPanel.Hide();
            ResultClosed?.Invoke();
            if (engine.IsEnded) ShowGameOver();
            else phase = Phase.Running;
            ClearSelection();
        }

        public void ConfirmDeadEnd()
        {
            if (phase != Phase.DeadEnd) return;
            eventPanel.Hide();
            ShowGameOver();
            ClearSelection();
        }

        void ShowGameOver()
        {
            phase = Phase.Ended;
            var titleTable = balance.titleTable;
            var title = engine.EndingTitleId != 0
                ? RunRules.FindTitleById(titleTable.specialEndingTitles, engine.EndingTitleId)
                : RunRules.ResolveTitle(titleTable.titles, engine.Stats);
            gameOver.Show(title != null ? title.titleName : GameText.EndTitle(engine.EndReason),
                title != null ? title.description : string.Empty,
                GameText.DeathCause(engine.EndReason, endingResultText), StartRun);
            RunEnded?.Invoke(engine.EndReason);
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
        }

        static void ClearSelection()
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }

        public void DebugOpenEventNow()
        {
            if (phase != Phase.Running) return;
            eventTimer = 0f;
            OpenEvent();
        }
    }
}
