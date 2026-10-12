Rainbow MA Gaming — Unity 2022.3.22f1 / VRChat PC

使い方
1. VRChat Avatars SDK、Modular Avatar 1.18.1以降（NDMFを含む）、lilToon 2.3.4をインストールします。
2. RainbowMA-Gaming-Speed-ColliderJump.unitypackageをインポートします。
3. Assets/RainbowMA/Prefabs/Rainbow MA.prefab をアバターの直下へドラッグします。
4. Play ModeまたはNDMFの手動ビルドで確認してからアップロードします。
   元のRenderer・マテリアル・FX・Expression Menuを直接編集する必要はありません。
   Prefabを外すと元の構成に戻ります。ひとつのアバターにつきひとつ置いてください。

追加されるメニュー
Rainbow MA > Render Queue: 元の値 / 2000 / 2450 / 3000 / 3100 / 4000 / 4500 / 4700 / 4800 / 4900 / 4999 / 5000
Rainbow MA > Gaming Rainbow Wire: 白い不透明な面・陰影・動き続ける虹色の発光ワイヤー線
Rainbow MA > Rainbow Lights: 虹色が巡る7個のPoint LightのON/OFF
QueueとWireは組み合わせて利用できます。Queueはアバター内の全Rendererの全マテリアルスロットに一括適用します。
Queueの数値自体はアニメーションできないため、事前生成したマテリアルへ切り替えています。
連続した任意の整数ではなく、上記のプリセットから選択します。
PrefabのQueue Presetsは1〜256個、0〜5000で編集できます。0は元のQueueです。
パラメータはRainbowMA/Queue(Int)、RainbowMA/Wire(Bool)、RainbowMA/Lights(Bool)で、回転はRainbowMA/Rotate(Float)で、RotationSpeed(Float)追加で同期容量は合計26 bitsです。ColliderJump(Bool)はLocal Onlyです。

発光とライティング
アバター全体の高さで色の帯を合わせ、上から下へ虹色が常にスクロールします。
メッシュは元のlilToonのテクスチャ・陰影に虹色を掛けて自己発光を加え、アウトラインにも動く虹色を付けます。本体より約1/3周、Wireは約2/3周、色の開始位置をずらします。
周囲の照明にはUnityのリアルタイムPoint Lightを使います。
Light Volumesをアバターへ追加する方式ではありません。lilToonのワールド照明受光処理は引き続き利用します。
Bloomによる光のにじみはワールド・カメラ側のBloom設定に依存します。
7色の光は重なり合うため、距離や形状によっては混色します。
リアルタイムLightを受けないワールドのシェーダーや、ライトを無効化する表示設定では周囲への照明は出ません。
Lightは影なし・範囲1.2m・強度0.4が既定です。Prefabで強度と範囲を変更できます。
Unity Light、カスタムシェーダー、Geometry Shaderを使うPC用です。Quest/Android用ではありません。

非破壊の仕組み
NDMFのResolvingフェーズでアバターのビルド用コピーからRendererを取得します。
マテリアルを複製し、MA Material Setter、MA Merge Animator、MA Parameters、MA Menu Installerを生成します。
編集専用のRainbowAvatarコンポーネントはビルドから除去します。実行時C#スクリプトは不要です。
非アクティブのオブジェクトも対象です。EditorOnly階層は対象外です。
同名兄弟などでRendererのアニメーションパスが重複する場合はエラーを出します。名前を分けてください。
新しく衣装を追加した場合も次回ビルドで自動的に検出します。
他のギミックが同じマテリアルスロットをアニメーションすると競合する場合があります。

マテリアル互換性
lilToonはプロパティを複製し、Opaque / Cutout / Transparentへ変換します。
他のシェーダーも置換しますが、主テクスチャと色を基本として変換するため、独自の描画機能は再現しません。
Fur、Gem、Refraction、特殊な多重パス・Stencil設定は元の見た目を完全には維持できません。
不透明な白いWire表示では元の透明度・テクスチャは使いません。
三角形単位のWireなので、元のメッシュの三角形分割が見えます。
Gaming設定: Rainbow Speed = 0.15（約6.67秒で1周）、Outline Phase Offset = 0.333333、Wire Phase Offset = 0.666667、Wire Emission = 1.5。PrefabのInspectorで変更できます。
本体・アウトライン・Wireはシェーダーの共通時間で動くため、QueueやWireの切り替えで色の時計がリセットされません。
Unity LightはFXのループアニメーションで色が巡ります。LightをONにするたびにそのループが先頭から始まります。

配布ファイル
RainbowMA-Gaming-Speed-ColliderJump.unitypackage: lilToonが既に入っているプロジェクト用。
RainbowMA-Gaming-Speed-ColliderJump-lilToon234.unitypackage: lilToonが入っていないプロジェクト用。
  既存のPackages/jp.lilxyzw.liltoonとAssets/lilToonを同時に入れないでください。
どちらもMAとVRChat SDKは別途必要です。
lilToon 2.3.4の公式配布ZIPも同梱の出力として用意しています。

検証
検証に用いたUnity・依存バージョンと結果はspeed-collider-verification.txtを参照してください。
テスト用アバターでNDMFビルド、MA置換、メニューとパラメータ、Queue/Wire全24組み合わせ、元データの保持を確認します。
VRChat実機での表示確認は含みません。

lilToon公式: https://github.com/lilxyzw/lilToon/releases/tag/2.3.4
Modular Avatar: https://modular-avatar.nadena.dev/



アウトラインの例外設定
アバター上のRainbow MA Prefabインスタンスを選択し、Rainbow AvatarコンポーネントのOutline Excluded Objectsへ、対象オブジェクトをHierarchyからドラッグします。
Outline Exclude Children: ON（既定）では子階層もまとめて除外。OFFでは指定オブジェクト自身のRendererだけを除外します。
例外はアウトラインだけに適用されます。本体の虹色発光、Queue切り替え、Gaming Rainbow Wireは引き続き適用されます。
非アクティブの子も対象です。空の項目やアバター外の参照は無視します。
同じマテリアルを共有する別のオブジェクトのアウトラインには影響しません。元マテリアルは変更しません。
Prefabのシェーダー参照が欠けた場合は、ビルド用コピーで同梱シェーダーを自動解決します。シェーダー自体が未導入・インポート失敗の場合は、Missing一覧付きエラーになります。

回転をON
Rainbow MA > 回転をON: アバターの見た目をルート原点のY軸で回転し続けます。既定は4秒で1周です。
PrefabのRotation Secondsで一周の秒数を変更できます。OFFで角度0へ戻ります。回転ON状態はアバター再読み込み時には保存されません。
回転はFXでシェーダーの角度を動かし、既存の表情・ポーズ・ブレンドシェイプ・骨アニメーションの結果へ加算します。通常表示とWire表示の両方が対象です。
ON中だけBaseの移動・ジャンプ・落下・着地モーションを静止側へ切り替えます。OFFで元のモーションへ戻ります。移動・ジャンプの操作は制限しません。
元のBase ControllerとBlendTreeは変更しません。ビルド用コピーだけを複製してゲートを追加します。Gesture/Actionのコントローラーはそのままです。
標準SDKのBase、および速度パラメータを使うBlendTreeとwalk/run/strafe/locomotion/jump/hop/fall/landを名前に含むステートを自動判定します。
独自のBaseで自動判定されない移動・ジャンプは、Additional Locomotion Statesへ該当ステート名を完全一致で追加してください。
実際のTransform・視点・PhysBoneや当たり判定は回転しません。回転は描画上の効果です。
RendererのTransform自体を別アニメーション・Constraintで動かす小物では、回転中心がずれる場合があります。通常のSkinnedMeshRendererの骨・ブレンドシェイプは維持します。
描画の消失を避けるため、ビルド用Rendererの描画範囲を広げ、SkinnedMeshRendererのUpdate When Offscreenを有効にします。
Unity 2022.3.22f1の別プロジェクトでMA/NDMFビルド、全4シェーダーのコンパイル、0/90/360度のGPU描画、既存データ保持を検証します。VRChat実機・このkiiアバターでの動作確認は別途必要です。

回転速度の可変設定
Rainbow MA > 回転速度: Radial Puppetで0〜100%を選択します。
既定の最大倍率4では、0%=停止、25%=1倍（4秒で1周）、50%=2倍、100%=4倍（1秒で1周）です。
速度変更で回転ループを再起動しないため、回転途中に速度を変えられます。速度設定は保存されます。
PrefabのRotation Max Speed Multiplierで最大倍率を変更できます。既存の保存値がある場合はスライダー位置を再調整してください。
回転をONのまま速度0にすると、その角度で静止します。回転をOFFにすると角度0へ戻り、移動・ジャンプのアニメーションも元に戻ります。

高いRender Queue
5000まで追加しました。高い値はRender Queue > 次へから選択できます。各ページは8項目以内です。
Unity 2022.3の有効範囲の上限は5000です。5000を超える値は設定しません。
Queue Presetsは1〜256個まで設定できます。元のマテリアルを複製して切り替えるため、プリセットを増やすほどビルド時のマテリアル数が増えます。
https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Material-renderQueue.html

コライダージャンプ
Rainbow MA > コライダージャンプ: 足元のBox ColliderをON/OFFします。回転と独立に切り替えられます。
参考: Kaiser『コライダージャンプ＆ダッシュ導入方法 2022/01/20版』 https://note.com/kaiservrchat/n/n506e7572c3ef
記事のジャンプ用設定を元に、アバター直下のTransform Position=(0,10,0)、Box Collider Center=(0,-10,0)、Size=(1,1,1)、Is Trigger=OFFで生成します。
ON状態を保存せず、読み込み時はOFFです。Local OnlyパラメータとIsLocal条件により、自分のローカル表示だけで有効になります。
PrefabのJump Collider Position/Center/Sizeでアバターの大きさに合わせて調整できます。
これは足元のColliderとの物理的な押し出しを利用する方式で、単なる見た目のジャンプではありません。記事は2022年時点の方式です。
UnityでColliderの生成、ON/OFF、ローカル限定条件を検証します。現在のVRChatクライアントと利用ワールドで実際に上昇することは未検証です。ワールドやクライアントの制限で動作しない場合があります。
