# unity-synthesis-emoticon-system

Unity drag-and-drop emoticon synthesis system with real-time visual feedback and custom editor tools

<img width="362" alt="image" src="https://github.com/user-attachments/assets/b7ee1377-5c84-493c-8e18-7dfc991e4b93" />


## 🛠 Tech Stack

- Unity 2021.3+
- C#
- Unity Editor Extensions
- DOTween
- Unity Event System
- ScriptableObject
- Coroutines

## ⭐ Key Features

- 드래그 앤 드롭 이모티콘 합성
- 실시간 방향 감지 시스템
- 파티클 이펙트 연동
- 커스텀 에디터 툴
- ScriptableObject 데이터 관리
- 시각적 피드백 시스템
- 그리드 기반 UI 관리

## 🎮 How It Works

1. 이모티콘을 상하좌우로 드래그
2. 드래그 방향에 따라 합성 대상 자동 감지
3. 실시간 파티클 이펙트 및 시각적 피드백
4. 합성 완료 시 새로운 이모티콘으로 변환

## 🎯 System Flow

1. **드래그 감지**: Unity Event System으로 드래그 시작/진행/종료 처리
2. **방향 계산**: 드래그 벡터 분석으로 합성 방향 결정
3. **시각적 피드백**: 하이라이트, 파티클 이펙트, 스프라이트 변환
4. **합성 처리**: ScriptableObject 데이터 기반 합성 결과 적용
5. **에디터 툴**: 커스텀 인스펙터로 이모티콘 데이터 관리

## 🔧 Editor Tools

- **Custom Inspector**: 그리드 기반 시각적 이모티콘 편집
- **Editor Window**: 전용 이모티콘 에디터 창
- **페이지 관리**: 다중 페이지 이모티콘 시스템
- **실시간 프리뷰**: 변경사항 즉시 확인
