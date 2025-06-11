using System;
using System.Collections;
using Coffee.UIExtensions;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DraggableIcon : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    #region Private Fields
    private Vector2 _startAnchoredPos;
    private RectTransform _rectTransform;
    private Image _targetImage;
    private Button _button;
    private EmoticonGridDisplay _gridDisplay;
    private Image[] _gridCells;
    private const float MinDragDistance = 50f;
    private bool _isDragging;
    private Vector2 _dragStartPosition;
    private Sprite _originalSprite;
    private Image _lastHighlightedImage;
    private Image _originalPositionImage;
    private Sprite _synthesisSprite;
    private bool _isEffectActive;
    private UIParticle _effectParticle;
    private int _lastHighlightedTargetIndex = -1;
    #endregion

    #region SerializeField
    [SerializeField] private GameObject crossPanel;
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        _button = GetComponent<Button>();
        _targetImage = GetComponent<Image>();
        _rectTransform = _targetImage.rectTransform;
        _startAnchoredPos = _rectTransform.anchoredPosition;
        _originalSprite = _targetImage.sprite;

        _gridDisplay = GameObject.Find("EmoticonEditor").GetComponent<EmoticonGridDisplay>();
        if (_gridDisplay != null)
        {
            _gridCells = _gridDisplay.gridCells;
        }

        GameObject emoticonEffect = GameObject.Find("GameManager/GameManagerUIGroup/UICanvas/EmoticonEffect");
        if (emoticonEffect != null)
        {
            _effectParticle = emoticonEffect.GetComponent<UIParticle>();
        }

        // 원본 위치 이미지 생성
        GameObject originalPosObj = new GameObject("OriginalPosition");
        originalPosObj.transform.SetParent(transform.parent);
        _originalPositionImage = originalPosObj.AddComponent<Image>();
        _originalPositionImage.raycastTarget = false;
        _originalPositionImage.gameObject.SetActive(false);

        if (crossPanel != null)
        {
            crossPanel.SetActive(false);
        }
    }

    private void OnDisable()
    {
        ResetHighlight();
        _lastHighlightedTargetIndex = -1;
        RestoreAllGridCells();

        if (crossPanel != null)
        {
            crossPanel.SetActive(false);
            ResetCornerPanelsAlpha();
        }
    }

    private void OnDestroy()
    {
        if (_originalPositionImage != null)
        {
            Destroy(_originalPositionImage.gameObject);
        }
    }
    #endregion

    #region Drag Handling
    public void OnBeginDrag(PointerEventData eventData)
    {
        _isDragging = true;
        if (_button != null)
        {
            _button.enabled = false;
        }

        _targetImage.sprite = _originalSprite;
        _synthesisSprite = null;
        _isEffectActive = false;
        _lastHighlightedTargetIndex = -1;

        // 원본 위치에 이미지 표시
        if (_originalPositionImage != null)
        {
            _originalPositionImage.sprite = _originalSprite;

            // RectTransform 설정 복사
            RectTransform originalRT = _originalPositionImage.rectTransform;
            originalRT.anchorMin = _rectTransform.anchorMin;
            originalRT.anchorMax = _rectTransform.anchorMax;
            originalRT.pivot = _rectTransform.pivot;
            originalRT.anchoredPosition = _rectTransform.anchoredPosition;
            originalRT.sizeDelta = _rectTransform.sizeDelta;
            originalRT.localScale = _rectTransform.localScale;

            _originalPositionImage.color = new Color(1, 1, 1, 0.5f);
            _originalPositionImage.gameObject.SetActive(true);
        }

        if (crossPanel != null)
        {
            crossPanel.SetActive(true);
        }

        SetCornerPanelsAlpha(0.1f);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform.parent as RectTransform,
            eventData.position, eventData.pressEventCamera, out _dragStartPosition);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform.parent as RectTransform,
                eventData.position, eventData.pressEventCamera, out var localPoint)) return;

        // 드래그 위치 계산
        Vector2 offset = localPoint - _dragStartPosition;
        Vector2 newPosition = _startAnchoredPos + offset;
        _rectTransform.anchoredPosition = ValidPosition(newPosition);

        Vector2 distance = _rectTransform.anchoredPosition - _startAnchoredPos;

        // 최소 드래그 거리보다 작을 경우 (원래 위치 근처로 돌아온 경우)
        if (Mathf.Abs(distance.x) <= MinDragDistance && Mathf.Abs(distance.y) <= MinDragDistance)
        {
            // 이펙트가 실행 중이면 종료
            if (_isEffectActive && _effectParticle != null)
            {
                StopAllCoroutines();
                _effectParticle.gameObject.SetActive(false);
                _isEffectActive = false;
                _targetImage.enabled = true;
            }

            ResetHighlight();
            _synthesisSprite = null;
            _targetImage.sprite = _originalSprite;
            _lastHighlightedTargetIndex = -1;
            RestoreAllGridCells();
            return;
        }

        // 최소 드래그 거리보다 클 경우 계속 진행
        if (!(Mathf.Abs(distance.x) > MinDragDistance || Mathf.Abs(distance.y) > MinDragDistance)) return;

        // 이전 하이라이트된 셀 복원
        if (_lastHighlightedTargetIndex >= 0 && _lastHighlightedTargetIndex < _gridCells.Length)
        {
            Image prevCell = _gridCells[_lastHighlightedTargetIndex];
            if (prevCell != null)
            {
                prevCell.enabled = true;
                prevCell.color = new Color(prevCell.color.r, prevCell.color.g, prevCell.color.b, 1f);
            }
        }

        // 페이지 및 인덱스 정보 계산
        Transform pageParent = transform.parent;
        while (pageParent != null && !pageParent.name.StartsWith("Page"))
        {
            pageParent = pageParent.parent;
        }

        int pageNumber = pageParent != null ? int.Parse(pageParent.name.Replace("Page", "")) - 1 : 0;
        int localIndex = transform.GetSiblingIndex();
        int globalIndex = (pageNumber * 9) + localIndex;
        int baseIndex = pageNumber * 9;

        // 드래그 방향 결정
        EmoticonGridDisplay.SynthesisDirection direction;
        if (Mathf.Abs(distance.x) > Mathf.Abs(distance.y))
        {
            direction = distance.x > 0
                ? EmoticonGridDisplay.SynthesisDirection.Right
                : EmoticonGridDisplay.SynthesisDirection.Left;
        }
        else
        {
            direction = distance.y > 0
                ? EmoticonGridDisplay.SynthesisDirection.Top
                : EmoticonGridDisplay.SynthesisDirection.Bottom;
        }

        // 이전 하이라이트 제거
        if (_lastHighlightedImage != null)
        {
            _lastHighlightedImage.color = new Color(_lastHighlightedImage.color.r, _lastHighlightedImage.color.g,
                _lastHighlightedImage.color.b, 1f);
            _lastHighlightedImage = null;
        }

        // 합성 대상 인덱스 계산
        int targetIndex = direction switch
        {
            EmoticonGridDisplay.SynthesisDirection.Top => baseIndex + 1,
            EmoticonGridDisplay.SynthesisDirection.Left => baseIndex + 3,
            EmoticonGridDisplay.SynthesisDirection.Right => baseIndex + 5,
            EmoticonGridDisplay.SynthesisDirection.Bottom => baseIndex + 7,
            _ => -1
        };

        if (targetIndex < 0 || targetIndex >= _gridCells.Length)
        {
            _lastHighlightedTargetIndex = -1;
            RestoreAllGridCells();
            return;
        }

        Image targetCell = _gridCells[targetIndex];
        if (targetCell == null) return;

        // 셀을 하이라이트
        targetCell.enabled = true;
        targetCell.color = new Color(targetCell.color.r, targetCell.color.g, targetCell.color.b, 0.5f);
        _lastHighlightedImage = targetCell;

        // 새로운 합성 이모티콘 대상 체크
        bool isNewTarget = targetIndex != _lastHighlightedTargetIndex;
        EmoticonData synthesisResult = _gridDisplay.GetSynthesisEmoticon(globalIndex, distance);

        if (synthesisResult == null)
        {
            _synthesisSprite = null;
            _targetImage.sprite = _originalSprite;
            _lastHighlightedTargetIndex = -1;
            targetCell.enabled = true;
            return;
        }

        _synthesisSprite = synthesisResult.sprite;

        // 이펙트 실행 - 새로운 합성 대상일 경우에만
        if (_effectParticle != null && isNewTarget)
        {
            if (_isEffectActive)
            {
                StopAllCoroutines();
                _effectParticle.gameObject.SetActive(false);
                _targetImage.enabled = true;
            }

            Vector3 targetWorldPosition = targetCell.transform.position;
            _effectParticle.transform.position = targetWorldPosition;

            _effectParticle.gameObject.SetActive(true);
            _isEffectActive = true;
            _targetImage.enabled = false;

            // 애니메이션 지속 시간 후 합성 이모티콘 표시
            StartCoroutine(DeactivateEffectAndShowSynthesisSprite(_effectParticle.gameObject, 0.75f, targetCell));
            _lastHighlightedTargetIndex = targetIndex;
        }

        // 이펙트가 활성화되어 있지 않을 때만 스프라이트 변경
        if (!_isEffectActive)
        {
            _targetImage.sprite = _synthesisSprite;
            targetCell.enabled = false;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // 이펙트 처리
        if (_isEffectActive && _effectParticle != null && _effectParticle.gameObject.activeSelf)
        {
            StopAllCoroutines();
            _effectParticle.gameObject.SetActive(false);
            _isEffectActive = false;
            _targetImage.enabled = true;

            // 합성 스프라이트가 있으면 적용
            if (_synthesisSprite != null)
            {
                _targetImage.sprite = _synthesisSprite;

                // 합성 타겟 셀 숨김
                if (_lastHighlightedTargetIndex >= 0 && _lastHighlightedTargetIndex < _gridCells.Length)
                {
                    Image targetCell = _gridCells[_lastHighlightedTargetIndex];
                    if (targetCell != null)
                    {
                        targetCell.enabled = false;
                    }
                }
            }
        }

        // 검은 십자 패널 비활성화
        if (crossPanel != null)
        {
            crossPanel.SetActive(false);
        }

        if (!_isDragging || _gridDisplay == null)
        {
            ResetState();
            return;
        }

        Vector2 distance = _rectTransform.anchoredPosition - _startAnchoredPos;

        // 최소 드래그 거리 미달시 원위치로 복귀 및 종료
        if (!(Mathf.Abs(distance.x) > MinDragDistance) && !(Mathf.Abs(distance.y) > MinDragDistance))
        {
            ResetState();
            return;
        }

        // 페이지 및 인덱스 정보 계산
        Transform pageParent = transform.parent;
        while (pageParent != null && !pageParent.name.StartsWith("Page"))
        {
            pageParent = pageParent.parent;
        }

        int pageNumber = pageParent != null ? int.Parse(pageParent.name.Replace("Page", "")) - 1 : 0;
        int localIndex = transform.GetSiblingIndex();
        int globalIndex = (pageNumber * 9) + localIndex;

        EmoticonData synthesisResult = _gridDisplay.GetSynthesisEmoticon(globalIndex, distance);
        if (synthesisResult == null)
        {
            ResetState();
            return;
        }

        // 합성 결과가 있는 경우 그리드 셀 복원
        RestoreAllGridCells();

        int synthesisIndex = Array.IndexOf(_gridDisplay.emoticonContainer.emoticons, synthesisResult);
        if (synthesisIndex != -1)
        {
            // 합성 스프라이트 적용
            _targetImage.enabled = true;
            if (_targetImage.sprite != synthesisResult.sprite)
            {
                _targetImage.sprite = synthesisResult.sprite;
            }

            _gridDisplay.PlayEmoticonEffect(synthesisIndex);

            eventData.Use();

            // 애니메이션과 함께 패널 닫기
            _rectTransform.DOAnchorPos(_startAnchoredPos, 0.1f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    _gridDisplay.CloseChatPanel(true);
                    _button.enabled = true;
                    _targetImage.sprite = _originalSprite;
                    RestoreAllGridCells();
                });
        }

        if (_originalPositionImage != null)
        {
            _originalPositionImage.gameObject.SetActive(false);
        }
    }
    #endregion

    #region Helper Methods
    private void ResetState()
    {
        _targetImage.enabled = true;
        _targetImage.sprite = _originalSprite;
        _synthesisSprite = null;
        _isEffectActive = false;
        _rectTransform.anchoredPosition = _startAnchoredPos;
        ResetHighlight();
        _isDragging = false;
        _lastHighlightedTargetIndex = -1;

        RestoreAllGridCells();

        if (crossPanel != null)
        {
            crossPanel.SetActive(false);
            ResetCornerPanelsAlpha();
        }

        if (_originalPositionImage != null)
        {
            _originalPositionImage.gameObject.SetActive(false);
        }

        if (_button != null)
        {
            _button.enabled = true;
        }
    }

    private void ResetHighlight()
    {
        if (_lastHighlightedImage != null)
        {
            _lastHighlightedImage.color = new Color(_lastHighlightedImage.color.r, _lastHighlightedImage.color.g,
                _lastHighlightedImage.color.b, 1f);
            _lastHighlightedImage = null;
        }
    }

    private Vector2 ValidPosition(Vector2 pos)
    {
        const float padding = 15;
        const float maxDragDistance = 220;

        Vector2 distance = pos - _startAnchoredPos;
        Vector2 result = pos;

        // 수평/수직 제한
        bool isHorizontalDrag = Mathf.Abs(distance.x) > Mathf.Abs(distance.y);
        if (isHorizontalDrag)
        {
            result.y = Mathf.Clamp(pos.y, _startAnchoredPos.y - padding, _startAnchoredPos.y + padding);
        }
        else
        {
            result.x = Mathf.Clamp(pos.x, _startAnchoredPos.x - padding, _startAnchoredPos.x + padding);
        }

        // 최대 드래그 거리 제한
        return new Vector2(
            Mathf.Clamp(result.x, _startAnchoredPos.x - maxDragDistance, _startAnchoredPos.x + maxDragDistance),
            Mathf.Clamp(result.y, _startAnchoredPos.y - maxDragDistance, _startAnchoredPos.y + maxDragDistance)
        );
    }

    private IEnumerator DeactivateEffectAndShowSynthesisSprite(GameObject effect, float delay, Image targetCell = null)
    {
        yield return new WaitForSeconds(delay);

        if (effect)
        {
            effect.SetActive(false);
        }

        _isEffectActive = false;
        _targetImage.enabled = true;

        if (_synthesisSprite && _isDragging)
        {
            _targetImage.sprite = _synthesisSprite;

            // 합성 타겟 셀 숨기기
            if (targetCell)
            {
                targetCell.enabled = false;
            }
            else if (_lastHighlightedTargetIndex >= 0 && _lastHighlightedTargetIndex < _gridCells.Length)
            {
                Image cell = _gridCells[_lastHighlightedTargetIndex];
                if (cell)
                {
                    cell.enabled = false;
                }
            }
        }
    }

    private void RestoreAllGridCells()
    {
        if (_gridCells == null) return;

        foreach (Image cell in _gridCells)
        {
            if (cell != null)
            {
                cell.enabled = true;
                cell.color = new Color(cell.color.r, cell.color.g, cell.color.b, 1f);
            }
        }
    }

    private void SetCornerPanelsAlpha(float alpha)
    {
        if (_gridDisplay == null || _gridCells == null) return;

        // 페이지 찾기
        Transform pageParent = transform.parent;
        while (pageParent != null && !pageParent.name.StartsWith("Page"))
        {
            pageParent = pageParent.parent;
        }

        int pageNumber = pageParent != null ? int.Parse(pageParent.name.Replace("Page", "")) - 1 : 0;
        int baseIndex = pageNumber * 9;

        // 모서리 인덱스 (좌상, 우상, 좌하, 우하)
        int[] cornerIndices = { baseIndex, baseIndex + 2, baseIndex + 6, baseIndex + 8 };

        foreach (int index in cornerIndices)
        {
            if (index >= 0 && index < _gridCells.Length && _gridCells[index] != null)
            {
                Color color = _gridCells[index].color;
                color.a = alpha;
                _gridCells[index].color = color;
            }
        }
    }

    private void ResetCornerPanelsAlpha()
    {
        if (_gridCells == null) return;

        foreach (Image cell in _gridCells)
        {
            if (cell != null)
            {
                Color color = cell.color;
                color.a = 1.0f;
                cell.color = color;
            }
        }
    }
    #endregion
}
