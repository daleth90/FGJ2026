using TMPro;
using UnityEngine;

namespace GrassRun
{
    /// <summary>
    /// 選完之後的回饋：結果敘事與數值變化。按確認鍵繼續（由 RunController 處理）。
    /// </summary>
    public class ResultPanelView : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] TMP_Text resultLabel;
        [SerializeField] TMP_Text changeLabel;

        public void Show(string result, string changes)
        {
            root.SetActive(true);

            resultLabel.text = result;
            changeLabel.text = changes;
        }

        public void Hide() => root.SetActive(false);
    }
}
