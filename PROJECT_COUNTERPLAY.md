# Project Counterplay

## Tondemo Shogi Redesign and Implementation Plan

## 1. Purpose

`tondemoShogi` を、現在の遊べるプロトタイプから「ルールを安全に拡張でき、敵の意図が感じられる5x6将棋ゲーム」へ育てる。

この計画では全面的な作り直しを避け、既存のシーン、Prefab、画像、効果音を活かしながら、次の順序で改善する。

1. 現在の進行不能や不整合を修正する。
2. 盤面ルールをUnityの表示処理から分離する。
3. CPUをランダム移動から評価ベースの思考へ置き換える。
4. AssassinとBombを、対処法のある特殊駒として調整する。
5. テストとデバッグ表示を整え、継続的に調整できる状態にする。

## Implementation Status

### Stable Turn - implemented on 2026-09-21

- `TurnPhase` を追加し、CPU思考中と手の解決中の人間入力を停止した。
- Bomb捕獲を「捕獲駒を爆心地へ移動してから3x3範囲を破壊する」順序へ修正した。
- Bombの捕獲駒も爆心地で消滅し、両王が同時に爆発した場合は引き分けとした。
- 王捕獲後の成り、ターン変更、CPU予約を停止した。
- CPUに合法な盤上移動がない場合は、進行停止ではなく引き分けで終了するようにした。
- 爆発SEのnull確認対象を `explosionSE` へ修正した。
- 混在していたC#の文字コードをUTF-8へ統一し、`.editorconfig` で固定した。
- `Assembly-CSharp.csproj` のビルドは警告0・エラー0で成功した。
- Unity Play ModeでのBomb、王捕獲、CPU中入力の手動確認は未実施。

### Known limitation after Stable Turn

CPUの「合法手なし」は現在、盤上移動だけを数えている。持ち駒を打てる場合も引き分けになり得るため、CPUの打つ手を実装する際に判定を更新する。相手の駒台表示も未修正。

## Delivery Roadmap and Estimates

2026-10-29時点の見積もり。**1日 = 集中して5〜6時間**を想定し、調査・実装・簡単なUnity動作確認を含む。Unityのシーン表示や特殊駒の仕様調整によって増減するため、確約の納期ではない。すでに実装したStable Turnの作業は含めない。

| Order | Deliverable | Focused effort | Completion check |
| --- | --- | ---: | --- |
| A | 相手の持ち駒取得・駒台表示を修正 | 1〜2時間 | CPUが歩などを取るとWhite駒台の種類・枚数が増える |
| B | CPUが持ち駒を打つ。合法手0件の判定も修正 | 3〜6時間 | 打てるときは候補に含まれ、打った後に枚数が減る。打てる手がある限り引き分けない |
| C | Unity表示なしで複製・仮実行できる盤面モデル、合法手、1手の解決 | 12〜24時間 | EditModeテストで移動・捕獲・成り・打ち・爆発・勝敗を再現できる |
| D | 2〜3手先を読む探索AIと盤面評価 | 8〜16時間 | 即勝利を逃さず、明白な王の損失と不利なBomb捕獲を避ける |
| E | Unity接続、速度測定、難易度・バランス調整 | 6〜12時間 | 実際の対局で待ち時間と手の質を確認し、進行停止がない |

**A+Bのみ:** 約4〜8時間。相手の持ち駒を見せて使わせる最短コース。探索AIへの作り替え前に、今のゲームだけで完結できる。

**A〜E:** 動く探索AIまで約30〜60時間、集中作業で6〜12日程度。週末に5〜8時間ずつ進めるなら、おおむね1〜3か月を見込む。

**計画書のPhase 0〜5をすべて実施:** A〜Eに加え、事前の仕様記録とテスト基盤、特殊駒の再調整、配布向けの仕上げを含めて約47〜94時間。集中作業で10〜19日程度。仕様変更やUnity上の表示調整が増えればさらに延びる。

### AI approach

既存の学習済み将棋モデルは、5x6盤とAssassin/Bombのルールをそのまま扱えない。初回は**学習不要の探索AI**を採用する。合法手を作り、仮の手を適用し、相手の返しを読んで盤面を評価する。`minimax` と `alpha-beta pruning` を基本とし、駒の価値、王の安全、成り、Bombの巻き込みを評価に使う。これは「次の手を固定ルールで決める」方式とは異なり、実際の局面を先読みする方式。ただし評価の重みは手作業で調整する。

持ち駒の打ちとAssassinの広い移動範囲により、5x6でも候補手が多くなる可能性がある。2手読みから始め、Unity上で思考時間を測ってから深さを増やす。自己対戦による機械学習は別プロジェクトとして扱い、この見積もりには含めない。

## 2. Current Game Snapshot

- Unity: `6000.0.45f1`
- Board: 5 columns x 6 rows
- Player: Black
- CPU: White
- Win condition: checkmateではなく王の捕獲
- Player special piece: Gin / Assassin / Bombから1体を選択
- CPU special piece: なし。通常のGinを使用
- Promotion: 敵陣への進入または敵陣からの移動時に自動成り
- Captured pieces: プレイヤーは通常駒を再配置可能。CPUの捕獲加算・再配置は未実装
- Existing tests: なし

### Current responsibility map

| Area | Current owner | Main concern |
| --- | --- | --- |
| Board generation and board data | `CellGenerator` | 盤面モデルとGameObject生成が一体化している |
| Piece rules and Prefab lookup | `PieceData`, `Direction` | ルールデータと表示アセット参照が混在している |
| Input, turns, rules, capture, effects, UI | `GameManager` | 責務が集中し、処理順序の不具合が起きやすい |
| CPU decision making | `CPUPlayer` | 捕獲優先、それ以外はランダム |
| Captured-piece UI | `SlotController`, `SlotImage` | UI状態がゲーム状態そのものとして使われている |
| Scene navigation and loadout | `TitleUI`, `MenuUI`, `ResultUI` | 文字列の`PlayerPrefs`に依存している |

## 3. Findings and Repair Priorities

### P0: Correctness and soft-lock risks

#### 3.1 Human input is not locked during the CPU turn

`GameManager.OnCellClicked` は手番中のチームだけを確認するため、CPU待機中の0.5秒間にWhiteの駒を人間が選択して動かせる。持ち駒も同様にWhite側を操作できる。

**Repair:** `TurnPhase` を導入し、`HumanInput`, `CpuThinking`, `ResolvingMove`, `GameOver` を明示する。盤面入力は `HumanInput` のときだけ受け付ける。

#### 3.2 Bomb capture is not resolved atomically

Bombを取る前に爆発処理が周囲のGameObjectを削除するため、隣接した攻撃駒が移動元で破壊された後、そのTransformへアクセスする可能性がある。モデルと表示の更新順も分散している。

**Repair:** 1手を `MoveAction` として解決し、盤面モデルを先に確定してから表示差分を反映する。Bombの仕様は次で固定する。

- Bomb自身は盤面から消える。
- 捕獲した駒も爆心地で消滅する。
- 爆心地を含む3x3範囲の駒はすべて消滅する。
- 消滅した駒は持ち駒にならない。
- 王が爆発範囲に入った場合は、その王の敗北とする。
- 両王が同時に爆発範囲へ入るケースは引き分けとする。

#### 3.3 CPU has no action when board moves are unavailable

Stable Turnで盤上の候補が0件なら引き分けで終了するよう修正済み。ただし `CPUPlayer.MakeMove` は持ち駒を候補に入れず、打てる手があっても誤って引き分けにする。

**Repair:** 盤上移動と持ち駒配置を同じ合法手一覧へ含める。双方が0件の場合だけ、現在の王捕獲ルールでは `NoLegalMove` の引き分けとして終了する。

#### 3.3a CPU captures do not reach the White piece stand

`GameManager.MovePieceByCPU` は `ClearSelection()` の後に `CapturePiece` を呼ぶ。一方、`CapturePiece` は `selectedPieceData` から捕獲したチームを取得する。CPUの捕獲時にはそれが `null` なので、Whiteのスロット枚数が増えず、相手の持ち駒も表示されない。`Game.unity` にはWhite用の駒台とスロットが配置されている。Unity上で表示位置も確認する。

**Repair:** 捕獲処理へ移動した駒の `Team` を明示的に渡し、Black/White共通の持ち駒加算を行う。Whiteスロットの有効化と枚数表示をPlay Modeで確認する。

#### 3.4 Game-over processing continues into EndTurn

王を取った後も移動、成り、ターン変更、CPU予約処理まで進む。結果表示後の状態が不安定になる。

**Repair:** `MoveResult` に勝敗を含め、解決完了後に一度だけ結果を確定する。`GameOver` へ遷移したら入力、ターン変更、CPU予約を停止する。

### P1: Rule and data integrity

#### 3.5 Legal move generation mutates selection state

CPU用の `GetMovableCells` が、一時的に `selectedPieceData` を差し替えて合法手を計算している。AI計算と画面上の選択状態が密結合になっている。

**Repair:** 合法手生成を `MoveGenerator.Generate(BoardState, Team)` の純粋な処理にする。UIの選択状態はルール計算へ渡さない。

#### 3.6 Board dimensions are configurable only on the surface

`CellGenerator.width` と `height` はInspectorで変更できるが、配列は `[5, 6]`、配置座標と初期配置も固定されている。

**Repair:** 当面は5x6をゲーム仕様として定数化する。将来可変盤面が必要になった時点で `BoardDefinition` を追加する。

#### 3.7 Drop rules are incomplete

二歩だけを確認しており、歩を最終段へ置くと以後動けない。打ち歩詰めは現在の王捕獲ルールと相性が悪く、導入コストも高い。

**Repair:** 最初の段階では二歩と行き所のない歩だけを禁止する。打ち歩詰めは対象外と明記する。

#### 3.8 Prefab loading depends on fragile names

`Resources.Load` は enum名からパスを生成している。`black_Assassin.prefab` の大文字表記は、Windowsでは動いても大文字小文字を区別する環境で失敗する可能性がある。

**Repair:** Prefab参照は `PieceViewCatalog` のInspector参照へ移す。短期対応ではファイル名を小文字へ統一し、ロード失敗時に駒種を含む明確なエラーを出す。

#### 3.9 Serialized enum order is fragile

`Type` はシーン内の持ち駒スロットに整数として保存されている。途中へ値を追加したり並べ替えたりすると、既存シーンの駒種が変わる。

**Repair:** `PieceType` へ改名する際に各値へ明示的な番号を与え、既存の番号を維持する。以後は末尾追加を原則とする。

### P2: Maintainability and presentation

#### 3.10 GameManager owns too many systems

入力、選択、合法手、捕獲、成り、持ち駒、ターン、CPU起動、音、エフェクト、結果UIが1クラスに集中している。

**Repair:** ドメイン、進行制御、表示の3層へ段階的に分離する。最初から大規模DI構成にはせず、Unityシーンから扱いやすい小さなクラス群にする。

#### 3.11 The board model stores scene-facing mutable objects

`PieceData` が移動方向、成り状態、Prefab取得を持ち、`CellGenerator` が盤面データとGameObjectを同時に更新している。AIが手を試すための盤面コピーを作りにくい。

**Repair:** 盤面上の駒は値データにし、PrefabやSpriteは表示カタログへ分離する。

#### 3.12 Input and highlights are scene-object driven

`OnMouseDown`、都度Instantiateするハイライト、シーン検索による持ち駒登録に依存している。

**Repair:** 初期段階では現行入力を維持しつつ、入力可否を一元化する。安定後にInput Systemとハイライトプールを検討する。

#### 3.13 Small cleanup items

- `SlotController.count` と `slotContainer` は未使用。
- `PieceData.canMoveMultiple` は実際の判定に使われていない。
- 爆発SE再生時に `explosionSE` ではなく `pieceSE` のnullを確認している。
- `GameManager.Instance` に重複検知と破棄時の解除がない。
- `FindObjectsByType` の結果で同じキーが重複しても検知されない。
- 選択、CPU、クリックのデバッグログが常時出力される。

## 4. Target Architecture

小規模ゲームとして扱いやすさを保つため、以下の4領域に限定する。

```text
Unity Input / UI
       |
       v
MatchController -----> CpuAgent
       |                  |
       v                  v
MoveGenerator <---- BoardState ----> MoveResolver
       |
       v
BoardView / Effects / Audio / HUD
```

### Domain: Unity sceneから独立したルール

`Assets/Scripts/Game/Domain/`

- `BoardPosition`: x, yと範囲判定
- `PieceState`: `PieceType`, `Team`, promotion state
- `BoardState`: 盤上駒、両陣営の持ち駒、手番
- `MoveAction`: 盤上移動または持ち駒配置
- `MoveGenerator`: 合法手の列挙
- `MoveResolver`: 捕獲、成り、爆発、勝敗を1手として解決
- `MoveResult`: 移動、捕獲、爆発、昇格、勝敗の差分
- `RuleSet`: 5x6、敵陣、二歩、特殊駒ルール

ここでは `GameObject`, `Transform`, `MonoBehaviour`, `Resources.Load` を使用しない。

### Application: 対局進行

`Assets/Scripts/Game/Application/`

- `MatchController`: 手番とフェーズの管理
- `TurnPhase`: `HumanInput`, `CpuThinking`, `ResolvingMove`, `GameOver`
- `GameSession`: 初期配置と選択した特殊駒から盤面を作る

### AI: 敵の意思決定

`Assets/Scripts/Game/AI/`

- `CpuAgent`: 合法手を受け取り1手を返す
- `MoveEvaluator`: 盤面と手を点数化する
- `PieceValues`: 駒価値と位置ボーナス
- `CpuProfile`: 難易度、探索深度、ランダム幅

### Presentation: Unity上の表示

`Assets/Scripts/Game/Presentation/`

- `BoardView`: `BoardState` と `MoveResult` をGameObjectへ反映
- `BoardInput`: クリックを盤面座標へ変換
- `SelectionPresenter`: 選択と合法手ハイライト
- `CapturedPiecePanel`: 持ち駒数を状態から表示
- `GameHud`: 手番と結果表示
- `EffectsPlayer`: 移動音、爆発音、パーティクル
- `PieceViewCatalog`: 駒種とPrefabの対応

## 5. Enemy Movement Redesign

### 5.1 Desired behavior

敵は最強である必要はない。次の性質を優先する。

- 王を取れる手を必ず選ぶ。
- 自分の王をすぐ取られる場所へ不用意に動かさない。
- 高価値の駒を取るが、取り返される損失も考える。
- 成り、中央支配、前進、持ち駒配置を使う。
- Bombの爆発範囲とAssassinの攻撃範囲を認識する。
- 同点の手に少量の揺らぎを入れ、毎回完全に同じ対局にしない。

### 5.2 First evaluator

最初は1手読みで十分な改善を得る。各候補を適用した盤面へ次を加点・減点する。

| Evaluation | Suggested score |
| --- | ---: |
| Capture enemy king | +100000 |
| Lose own king on next move | -100000 |
| Capture piece | captured piece value x 10 |
| Lose moved piece on next move | moving piece value x -6 |
| Promotion | +25 |
| Increase legal moves | +1 per move |
| Move toward enemy side | +2 per rank |
| Protect own king neighborhood | +4 per protected square |
| Enter Bomb blast risk | expected material loss as penalty |
| Create immediate threat on enemy king | +40 |

Initial piece values:

| Piece | Value |
| --- | ---: |
| Fu | 1 |
| Gin | 4 |
| Kin | 5 |
| Kaku | 7 |
| Hisha | 8 |
| Tokin / Narigin | 5 |
| Uma | 9 |
| Ryu | 10 |
| Assassin | 8, then tune from play data |
| Bomb | board-dependent, base 5 |
| Ou | 10000 |

### 5.3 Difficulty profiles

- **Casual:** 上位5手から重み付きランダム。明確な即勝利だけは逃さない。
- **Standard:** 1手先の反撃まで評価する2-ply minimax。
- **Tactical:** 3-ply、alpha-beta pruning、候補順序付けを使用する。

5x6盤なので、まず全合法手を正しく扱うことを優先し、探索最適化は計測後に行う。

### 5.4 Enemy readability

CPUの強さだけでなく、プレイヤーが行動を理解できる表示を追加する。

- CPUが動かした移動元と移動先を次の手まで表示する。
- 捕獲、成り、爆発に短い間を設ける。
- 危険なBombの周囲を選択時に表示する。
- Assassin選択時は「移動可能」と「捕獲可能」を色分けする。

## 6. Special Piece Redesign Decisions

### Assassin

現在の「味方と両王を除く任意マスへ移動」は強烈で、相手の大駒をほぼ確実に取れる。まずは現仕様を正しく再現してテストし、その後に以下を推奨案として比較する。

**Recommended: Shadow Step**

- 任意の空きマスへ移動できる。
- 捕獲は周囲8マスの敵駒だけ可能。
- 王は捕獲できない。
- 捕獲されても持ち駒にならない。

盤面移動の驚きは残しつつ、無条件の遠距離捕獲をなくし、相手が距離を取る対策を持てる。

### Bomb

現在の説明どおり、Blackから見た前方3マスへ1マス移動する。爆発仕様は3.2で定義した内容へ統一する。

将来White側でも使用可能にする場合、前方方向は `Team` に応じて反転させる。現在の `Direction.ForType` はBombだけTeamを見ていないため、そのまま流用しない。

## 7. Implementation Phases

### Phase 0: Baseline and safety net

**Goal:** 現行仕様を壊さず変更できる足場を作る。

**Estimate:** 約3〜6時間。A+Bの後、盤面モデルの分離に着手する前に実施する。

- EditModeテスト用asmdefとテストフォルダを追加する。
- 既存の移動方向、成り、二歩、Assassin、Bombをcharacterization testへ記録する。
- 既存の5x6初期配置をテストデータ化する。
- CPU乱数へseedを渡せるようにする。
- P0の4項目を再現するテストを先に追加する。

**Exit criteria:** バグを再現する失敗テストがあり、通常駒の移動テストが通る。

### Phase 1: Stabilize the existing game

**Goal:** 現構成のまま致命的な不具合を止める。

**Remaining estimate:** A+Bを含む約5〜10時間。前半のStable Turnは実装済みだが、Play Mode検証が残る。

- `TurnPhase` でCPU中の入力をロックする。
- Bomb解決順序と爆発SEのnull確認を修正する。
- 王捕獲後の追加処理を停止する。
- CPUの合法手0件をゲーム結果へ変換する。
- CPU捕獲時にもWhiteの持ち駒数を加算し、駒台表示を復旧する。
- CPUの候補に持ち駒配置を加え、盤上移動と打ちの両方がない場合だけ引き分けにする。
- 歩の最終段配置を禁止する。
- 盤面サイズを5x6定数へ統一する。
- Prefabロード失敗とslot重複を検知する。

**Exit criteria:** Whiteが取った駒が駒台に表示され、CPUが合法な打ちを行える。20局連続で進行不能、二重操作、MissingReferenceExceptionが発生しない。

### Phase 2: Extract the domain core

**Goal:** GameObjectなしで1局を進行・複製できるようにする。

**Estimate:** 約12〜24時間。Phase 1で直した持ち駒取得と打ちの挙動を、盤面モデルへ移して維持する。

- `BoardState`, `PieceState`, `MoveAction`, `MoveResult` を追加する。
- `Direction` を副作用のない移動パターン定義へ置き換える。
- `MoveGenerator` と `MoveResolver` を追加する。
- `CellGenerator` の配列を `BoardState` の表示先へ変更する。
- `GameManager` を薄い `MatchController` へ段階的に移行する。
- 持ち駒数をSlot UIではなく `BoardState` に保存する。

**Exit criteria:** EditModeテストだけで初期配置から複数手、捕獲、持ち駒配置、成り、爆発、勝敗まで実行できる。

### Phase 3: Build Counterplay AI

**Goal:** 敵が目的のある手を選ぶ。

**Estimate:** 探索AI本体に約8〜16時間。Unity接続・調整は別途Phase 4〜5で見込む。

- 盤上移動と持ち駒配置を含む全合法手を列挙する。
- 即時勝利、王の安全、駒得、成り、機動力を評価する。
- CasualとStandardを実装する。
- 同じseedで同じ手を選ぶテストを追加する。
- BombとAssassinへの危険評価を追加する。
- CPU思考をCoroutineまたはasync待機へ移し、キャンセル可能にする。

**Exit criteria:** CPUが即時勝利を100%選び、明らかに王を失う手を合法手がある限り避け、持ち駒を使用する。

### Phase 4: Rebalance special pieces

**Goal:** 特殊駒に驚きと対策の両方を持たせる。

**Estimate:** 約6〜12時間。ルールを変更する場合は対局テストの回数に応じて増える。

- Assassinの現仕様とShadow Step案をプレイ比較する。
- Bombの爆発結果を事前表示する。
- Piece valueと位置評価を対局ログから調整する。
- 必要ならCPU側にも特殊駒loadoutを追加する。
- メニュー説明文を実際のルールから生成または一元管理する。

**Exit criteria:** 説明文、合法手表示、実際の解決結果が一致し、各特殊駒に明確な長所と対処法がある。

### Phase 5: Presentation and release polish

**Goal:** 対局の読みやすさと配布時の品質を上げる。

**Estimate:** 約8〜16時間。A〜Eの基本的なUnity接続・AI調整とは別の仕上げ作業。

- CPU直前手、選択駒、合法手、爆発範囲の表示を整理する。
- ハイライトをプールし、シーン直下の一時Object増加を防ぐ。
- 音量設定、リスタート、難易度選択を追加する。
- `Resources.Load` を `PieceViewCatalog` へ置き換える。
- READMEへUnityバージョン、Git LFS、起動方法、ルールを記載する。
- 日本語フォントアセットを必要文字だけで再生成し、LFS依存を減らせるか検証する。

**Exit criteria:** 新規clone環境でエラーなく起動でき、READMEだけで対局開始まで進める。

## 8. Test Matrix

### Move generation

- 全通常駒のBlack/White方向
- Hisha/Kaku/Ryu/Umaの経路遮断
- 味方駒のあるマスを除外
- 盤外移動を除外
- AssassinとBombの専用移動

### Capture and special effects

- 通常捕獲が自軍持ち駒になる
- 成り駒捕獲が元の駒種へ戻る
- Assassin/Bombが持ち駒にならない
- Bomb中心、周囲、盤端での爆発
- Bomb周辺の王、両王、捕獲駒の結果

### Drops and promotion

- 二歩禁止
- 歩の最終段配置禁止
- occupied cellへの配置禁止
- 持ち駒数の増減
- CPUが捕獲した駒がWhiteの持ち駒に入り、駒台に表示される
- CPUが打てる位置と打った後の枚数
- Black/White双方の成りゾーン
- 成れない駒が変化しない

### Turn flow

- Human -> CPU -> Humanの遷移
- CPU中の人間入力拒否
- GameOver後の入力拒否
- CPU合法手0件の終了処理
- 盤上移動0件でも持ち駒を打てる場合は対局が続く
- シーン再読込時に保留中CPU処理が残らない

### AI behavior

- 即時王捕獲を選ぶ
- 価値の高い安全な捕獲を優先する
- 自王が即時捕獲される手を避ける
- 持ち駒配置を候補に含める
- 同一seedで再現可能

## 9. Recommended Next Milestone

次の実装単位は **White Hand Recovery** とする。目安はA+Bの4〜8時間。Stable TurnのPlay Mode確認も含めると5〜10時間。

1. `CapturePiece` に捕獲者の `Team` を渡し、選択状態への依存を外す。
2. CPUの捕獲でWhiteの駒台スロットと枚数が正しく表示されることを確認する。
3. CPUが持ち駒を打てるマスを列挙し、盤上移動と同じ候補群に加える。
4. CPUの打ちを実行して枚数を減らす。二歩と行き所のない歩を双方に適用する。
5. 盤上移動も打ちもない場合だけ引き分けとする。
6. CPU捕獲、White表示、CPUの打ち、手なし判定をPlay Modeで確認する。

この段階は現行の構造内で完結する。後で盤面モデルを分離する際は、持ち駒の枚数を `BoardState` に移し、UIスロットは表示専用にする。

## 10. Scope Boundaries

初回の再設計には含めない。

- 9x9の本将棋対応
- 完全な王手、詰み、打ち歩詰め判定
- オンライン対戦
- セーブデータや対局棋譜のクラウド同期
- 大規模な依存性注入フレームワーク
- 最強AIを目的とした大規模探索

Project Counterplayの成功条件は、機能数ではなく「ルールが一貫している」「敵の手に理由を感じる」「次の特殊駒を安全に追加できる」の3点とする。
