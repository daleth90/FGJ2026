using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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
        [Tooltip("HUD 的圖鑑按鈕會打開這個圖鑑。")]
        [SerializeField] CodexController codex;

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
            hud.GuideRequested += OpenCodex;
            StartRun();
        }

        void OnDestroy()
        {
            if (hud != null) hud.GuideRequested -= OpenCodex;
        }

        void OpenCodex()
        {
            if (codex != null) codex.Open();
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
            characterAppearance = RunRules.ResolveCharacterAppearance(engine.Stats);
            var decoration = RunRules.ResolveMoralityDecoration(engine.Stats);
            if (stage != null)
            {
                stage.SetCharacterAppearance(characterAppearance);
                stage.SetMoralityDecoration(decoration);
            }
            CodexStorage.UnlockForm(CodexRules.FormEntryId(characterAppearance, decoration));
            RunStarted?.Invoke();
        }

        void Update()
        {
            // 圖鑑開著時整局暫停：不吃輸入、不計時（Time.timeScale 也是 0）。
            if (CodexController.IsOpen) return;
#if UNITY_EDITOR
            // 作弊面板開著時也暫停，只更新 HUD 讓調整後的數值馬上顯示。
            if (StatsCheat.IsOpen)
            {
                RefreshHud();
                return;
            }
#endif

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
            // 點在 UI 按鈕上（例如 HUD 的圖鑑按鈕）不算繼續，交給按鈕自己處理。
            var pointer = Pointer.current;
            if (phase == Phase.Result && Time.frameCount > resultShownFrame &&
                pointer != null && pointer.press.wasPressedThisFrame &&
                !IsOverButton(pointer.position.ReadValue()))
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

            var result = engine.Choose(index);
            characterAppearance = RunRules.ResolveCharacterAppearance(engine.Stats);
            var decoration = RunRules.ResolveMoralityDecoration(engine.Stats);
            if (stage != null)
            {
                stage.SetCharacterAppearance(characterAppearance);
                stage.SetMoralityDecoration(decoration);
            }
            CodexStorage.UnlockForm(CodexRules.FormEntryId(characterAppearance, decoration));
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
            if (title != null) CodexStorage.UnlockTitle(title.titleId);
            gameOver.Show(title != null ? title.titleName : GameText.EndTitle(engine.EndReason),
                title != null ? title.image : null,
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

        static readonly List<RaycastResult> uiHits = new List<RaycastResult>();

        /// <summary>這個畫面位置最上層的 UI 是不是可點的按鈕（或在按鈕底下）。</summary>
        static bool IsOverButton(Vector2 screenPosition)
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null) return false;

            uiHits.Clear();
            eventSystem.RaycastAll(new PointerEventData(eventSystem) { position = screenPosition }, uiHits);
            if (uiHits.Count == 0) return false;

            var selectable = uiHits[0].gameObject.GetComponentInParent<Selectable>();
            return selectable != null && selectable.IsInteractable();
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
