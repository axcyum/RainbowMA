using UnityEngine;
using VRC.SDKBase;

namespace RainbowMA
{
    [AddComponentMenu("Rainbow MA/Rainbow Avatar")]
    public sealed class RainbowAvatar : MonoBehaviour, IEditorOnly
    {
        [Min(0)] public float emission = 1.5f;
        [Tooltip("lilToon outline width in centimetres.")]
        [Min(0)] public float outlineWidth = 0.2f;
        [Min(0)] public float outlineEmission = 2f;
        [Header("アウトラインの例外")]
        [Tooltip("アウトラインを適用しないオブジェクト。アバター上のPrefabインスタンスに、Hierarchyからドラッグしてください。")]
        public GameObject[] outlineExcludedObjects = new GameObject[0];
        [Tooltip("ONなら指定オブジェクトの子階層も除外。OFFなら指定オブジェクト自身のRendererだけを除外。")]
        public bool outlineExcludeChildren = true;
        [Header("回転")]
        [Tooltip("見た目のY軸回転が一周する秒数。")]
        [Min(0.1f)] public float rotationSeconds = 4f;
        [Tooltip("回転速度メニューの最大倍率。0%で停止、100%でこの倍率。")]
        [Min(1f)] public float rotationMaxSpeedMultiplier = 4f;
        [Tooltip("自動判定に加えて回転ON中に停止するBaseレイヤーのステート名。独自の移動・ジャンプ用。")]
        public string[] additionalLocomotionStates = new string[0];
        [Header("コライダージャンプ")]
        [Tooltip("記事の方式: Transform Y=10、Collider Center Y=-10。アバターの大きさに合わせて調整。")]
        public Vector3 jumpColliderPosition = new Vector3(0,10,0);
        public Vector3 jumpColliderCenter = new Vector3(0,-10,0);
        public Vector3 jumpColliderSize = Vector3.one;
        [Header("ゲーミング設定")]
        [Tooltip("Rainbow cycles per second.")]
        [Min(0.01f)] public float rainbowSpeed = 0.15f;
        [Range(0,1)] public float outlinePhaseOffset = 0.333333f;
        [Range(0,1)] public float wirePhaseOffset = 0.666667f;
        [Min(0)] public float wireEmission = 1.5f;
        [Min(0)] public float lightIntensity = 0.4f;
        [Min(0.01f)] public float lightRange = 1.2f;
        public bool lightsOnByDefault = true;
        [Tooltip("0 = preserve original queue; other values are actual queue presets.")]
        public int[] queuePresets = { 0, 2000, 2450, 3000, 3100, 4000, 4500, 4700, 4800, 4900, 4999, 5000 };
        public Shader opaqueShader;
        public Shader cutoutShader;
        public Shader transparentShader;
        public Shader wireShader;
    }
}
