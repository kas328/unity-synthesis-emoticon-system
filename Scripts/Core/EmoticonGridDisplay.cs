using Mingle.Dev.JHC_TEST._02._Scripts._04._ChatManager._01._AgitChat;
using Mingle.Dev.JHC_TEST._02._Scripts._04._ChatManager._03._ChatInput;
using UnityEngine;
using UnityEngine.UI;

public class EmoticonGridDisplay : MonoBehaviour 
{
    #region Fields and Properties
    [SerializeField] public EmoticonContainer emoticonContainer;
    [SerializeField] public Image[] gridCells;
    [SerializeField] private Transform[] pageContents;
    private const int ItemsPerPage = 13;
    private const int GridSize = 9;
    #endregion

    #region Enums
    public enum SynthesisDirection
    {
        Top,
        Bottom,
        Left,
        Right
    }
    #endregion

    #region Unity Lifecycle Methods
    private void Awake()
    {
        UpdateAllEmoticons();
    }
    #endregion

    #region Emoticon Grid Management
    public void UpdateAllEmoticons()
    {
        if (emoticonContainer == null) return;

        int totalPages = emoticonContainer.pageLength;
    
        for (int page = 0; page < totalPages; page++)
        {
            int baseIndex = page * ItemsPerPage;
            int gridBaseIndex = page * GridSize;
        
            for (int i = 0; i < GridSize; i++)
            {
                int containerIndex = baseIndex + i;
                int displayIndex = gridBaseIndex + i;
            
                if (displayIndex >= gridCells.Length) continue;
                if (containerIndex >= emoticonContainer.emoticons.Length) continue;

                EmoticonData currentEmoticon = emoticonContainer.emoticons[containerIndex];
                Image cellImage = gridCells[displayIndex];
            
                if (cellImage == null || currentEmoticon == null) continue;
            
                cellImage.sprite = currentEmoticon.sprite;
                SetupClickHandler(cellImage.gameObject, containerIndex);
            }
        }
    }

    private void SetupClickHandler(GameObject cellObject, int emoticonIndex)
    {
        Button button = cellObject.GetComponent<Button>();

        if (button == null)
        {
            button = cellObject.AddComponent<Button>();
        }

        // * Inspector 에서 등록한 함수는 지워지지 않음. (기능 이모티콘)
        button.onClick.RemoveAllListeners();
    
        int indexInPage = emoticonIndex % ItemsPerPage;
        bool isSynthesisEmoticon = indexInPage >= GridSize;

        
        // * 승기님 이부분 합성 이모티콘에선 전혀 호출되고 있지 않은데 리팩터링 부탁드려요!
        button.onClick.AddListener(() => 
        {
            if (isSynthesisEmoticon)
            {
                PlayEmoticonEffect(emoticonIndex);
            }
            else
            {
                SendEmoticonChat(emoticonIndex);
                PlayerActionManager actionManager = GameManager.Instance.MyAvatar.GetComponent<PlayerActionManager>();
                actionManager.TriggerEffect(emoticonIndex);
            }
            CloseChatPanel();
        });
    }

    public void PlayEmoticonEffect(int emoticonIndex)
    {
        SendEmoticonChat(emoticonIndex);
        
        EmoticonData emoticonData = emoticonContainer.emoticons[emoticonIndex];
        if (emoticonData != null && emoticonData.prefab != null)
        {
            for (int i = 0; i < gridCells.Length; i++)
            {
                Image cell = gridCells[i];
                if (cell != null)
                {
                    Color color = cell.color;
                    color.a = (cell.sprite == emoticonData.sprite) ? 1f : 0.5f;
                    cell.color = color;
                }
            }

            PlayerActionManager actionManager = GameManager.Instance.MyAvatar.GetComponent<PlayerActionManager>();
            actionManager.TriggerEffect(emoticonIndex);
        }
    }
    #endregion

    #region Synthesis Methods
    public int GetSynthesisIndex(int page, SynthesisDirection direction)
    {
        int baseIndex = page * ItemsPerPage;
        return direction switch
        {
            SynthesisDirection.Top => baseIndex + 9,
            SynthesisDirection.Left => baseIndex + 10,
            SynthesisDirection.Right => baseIndex + 11,
            SynthesisDirection.Bottom => baseIndex + 12,
            _ => -1
        };
    }
    
    public int GetSynthesisTargetIndex(int page, SynthesisDirection direction)
    {
        int baseIndex = page * ItemsPerPage;
        return direction switch
        {
            SynthesisDirection.Top => baseIndex + 9,
            SynthesisDirection.Left => baseIndex + 10,
            SynthesisDirection.Right => baseIndex + 11,
            SynthesisDirection.Bottom => baseIndex + 12,
            _ => -1
        };
    }
    
    public EmoticonData GetSynthesisEmoticon(int index, Vector2 distance)
    {
        int currentPage = index / GridSize;
        if (currentPage >= emoticonContainer.pageLength) return null;

        SynthesisDirection direction;
        if (Mathf.Abs(distance.x) > Mathf.Abs(distance.y))
        {
            direction = distance.x > 0 ? SynthesisDirection.Right : SynthesisDirection.Left;
        }
        else
        {
            direction = distance.y > 0 ? SynthesisDirection.Top : SynthesisDirection.Bottom;
        }
    
        int synthesisIndex = GetSynthesisIndex(currentPage, direction);
        return synthesisIndex >= 0 && synthesisIndex < emoticonContainer.emoticons.Length ? emoticonContainer.emoticons[synthesisIndex] : null;
    }
    #endregion

    #region UI Management
    public void CloseChatPanel()
    {
        var chatInputUIController = FindAnyObjectByType<ChatInputUIController>();
        if (chatInputUIController) chatInputUIController.CloseByMode();
    }

    public void CloseChatPanel(bool isSynthesis)
    {
        foreach (var cell in gridCells)
        {
            if (!cell) continue;
            
            var color = cell.color;
            color.a = 1f;
            cell.color = color;
        }
        
        var chatInputUIController = FindAnyObjectByType<ChatInputUIController>();
        if (chatInputUIController) chatInputUIController.CloseByMode();
    }
    #endregion

    #region Emoticon Chat

    private void SendEmoticonChat(int index)
    {
        if (UniversalSceneManager.GetCurrentSceneName() != SceneName.RoomEditScene) return;

        var chatController = FindAnyObjectByType<ChatController>();
        chatController.SendEmojiMessage(index.ToString());

    }
    #endregion
}
