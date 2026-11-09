using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// 指定した範囲にパーティクル(塵など)を漂わせます。範囲は区画に分けて、区画ごとに Particle System を置きます。
/// 動かすのはカメラの周り(画面 + 余白)の区画だけで、離れた区画は GameObject ごと無効にします。
/// 区画は最初に必要になったときに作り、有効にするときは寿命の分だけ早送りしてから再生するので、画面に入るころには満たされています。
/// 範囲の端はフェザーの幅だけ段階的に薄くします。範囲を重ねて置けば、重なった場所には両方の粒が出ます。
/// 区画の Box・Rate・Max・Order in Layer はこのコンポーネントが上書きし、粒の見た目はプレハブのままです。
/// 前提: 範囲はゲーム中に動かさない。ParticleZone の Transform は Scale 1・回転なし。カメラは Orthographic。
/// </summary>
[DisallowMultipleComponent]
public class ParticleZone : MonoBehaviour
{
    // 有効な範囲から外れた区画を無効にするまでのゆとり。境目で有効・無効が何度も入れ替わらないようにする
    private const float DeactivateHysteresis = 1f;
    // Max は平均の粒数にこの倍率を掛けて余裕を持たせる
    private const float MaxParticlesHeadroom = 1.5f;
    private const float MinCellLength = 0.01f;

    [SerializeField, Tooltip("範囲用のオブジェクト。位置と Scale(X, Y)が範囲になる(回転は無視)。ParticleZone の子に置く")]
    private Transform area;

    [SerializeField, Tooltip("区画に使う Particle System のプレハブ。粒の見た目(大きさ・色・Noise・Trails・Use Unscaled Time など)はプレハブで決める")]
    private ParticleSystem particlePrefab;

    [SerializeField, Tooltip("区画 1 つの幅・高さ(ユニット)。画面くらいが目安。範囲は等分するので、実際の区画はこれ以下になる")]
    private Vector2 cellSize = new(18f, 10f);

    [SerializeField, Min(0f), Tooltip("1 平方ユニットあたりの粒の数。区画の Rate と Max はここから計算する")]
    private float density = 0.2f;

    [SerializeField, Min(0f), Tooltip("範囲の端から粒を薄くする幅(ユニット)。0 で薄くしない")]
    private float featherWidth = 2f;

    [SerializeField, Min(1), Tooltip("フェザーの幅を何段階で薄くするか")]
    private int featherSteps = 3;

    [SerializeField, Min(0f), Tooltip("画面の外側の、この幅までの区画を有効にする(ユニット)。粒が区画からはみ出す距離より大きくする。Noise を強くして画面の端で粒が急に出るなら広げる")]
    private float activeMargin = 2f;

    [SerializeField, Tooltip("区画の Renderer の Order in Layer をこの値で上書きする")]
    private int orderInLayer;

    // 区画の分かれ目(横・縦)と、各列・各行の密度の倍率。区画 (i, j) の倍率は横の倍率 × 縦の倍率
    private readonly List<float> _edgesX = new();
    private readonly List<float> _edgesY = new();
    private readonly List<float> _weightsX = new();
    private readonly List<float> _weightsY = new();

    // 区画は j * 列数 + i で並べる。まだ作っていない区画は null
    private ParticleSystem[] _cells;
    private bool[] _isActive;
    private readonly List<int> _activeCells = new();
    private bool _built;

    private Vector2 _lastCenter;
    private Vector2 _lastHalfSize;
    private bool _hasLast;

    private int ColumnCount => _weightsX.Count;
    private int RowCount => _weightsY.Count;

    private void Start()
    {
        if (area == null)
        {
            Debug.LogWarning($"{name}: 範囲(area)が設定されていません。", this);
            return;
        }
        if (particlePrefab == null)
        {
            Debug.LogWarning($"{name}: 塵のプレハブ(particlePrefab)が設定されていません。", this);
            return;
        }

        GetAreaBounds(out Vector2 min, out Vector2 max);
        BuildAxis(min.x, max.x, cellSize.x, featherWidth, featherSteps, _edgesX, _weightsX);
        BuildAxis(min.y, max.y, cellSize.y, featherWidth, featherSteps, _edgesY, _weightsY);

        int count = ColumnCount * RowCount;
        _cells = new ParticleSystem[count];
        _isActive = new bool[count];
        _built = count > 0;
    }

    private void OnEnable()
    {
        _hasLast = false;
        CinemachineCore.CameraUpdatedEvent.AddListener(OnCameraUpdated);
    }

    private void OnDisable()
    {
        CinemachineCore.CameraUpdatedEvent.RemoveListener(OnCameraUpdated);
        for (int k = _activeCells.Count - 1; k >= 0; k--) Deactivate(_activeCells[k]);
        _activeCells.Clear();
    }

    /// <summary>Cinemachine がカメラを動かした直後に呼ばれます。</summary>
    private void OnCameraUpdated(CinemachineBrain brain)
    {
        var camera = brain != null ? brain.OutputCamera : null;
        if (camera != null) Apply(camera);
    }

    private void LateUpdate()
    {
        // Brain が無いシーンでの保険。Brain があれば CameraUpdatedEvent 側で更新する
        if (CinemachineBrain.ActiveBrainCount > 0) return;
        var camera = Camera.main;
        if (camera != null) Apply(camera);
    }

    /// <summary>カメラの周りの区画を有効にし、離れた区画を無効にします。</summary>
    private void Apply(Camera camera)
    {
        if (!_built) return;

        Vector2 center = camera.transform.position;
        float halfHeight = camera.orthographicSize;
        Vector2 halfSize = new(halfHeight * camera.aspect + activeMargin, halfHeight + activeMargin);
        if (_hasLast && center == _lastCenter && halfSize == _lastHalfSize) return;
        _lastCenter = center;
        _lastHalfSize = halfSize;
        _hasLast = true;

        // 離れた区画を無効にする(ゆとりを持たせた範囲の外に出たものだけ)
        Vector2 keepHalfSize = halfSize + Vector2.one * DeactivateHysteresis;
        bool hasKeepX = FindRange(_edgesX, center.x - keepHalfSize.x, center.x + keepHalfSize.x, out int keepX0, out int keepX1);
        bool hasKeepY = FindRange(_edgesY, center.y - keepHalfSize.y, center.y + keepHalfSize.y, out int keepY0, out int keepY1);
        for (int k = _activeCells.Count - 1; k >= 0; k--)
        {
            int index = _activeCells[k];
            int i = index % ColumnCount;
            int j = index / ColumnCount;
            if (hasKeepX && hasKeepY && i >= keepX0 && i <= keepX1 && j >= keepY0 && j <= keepY1) continue;

            Deactivate(index);
            _activeCells[k] = _activeCells[_activeCells.Count - 1];
            _activeCells.RemoveAt(_activeCells.Count - 1);
        }

        // カメラの周りの区画を有効にする
        if (!FindRange(_edgesX, center.x - halfSize.x, center.x + halfSize.x, out int x0, out int x1)) return;
        if (!FindRange(_edgesY, center.y - halfSize.y, center.y + halfSize.y, out int y0, out int y1)) return;
        for (int j = y0; j <= y1; j++)
        {
            for (int i = x0; i <= x1; i++)
            {
                int index = j * ColumnCount + i;
                if (_isActive[index]) continue;
                Activate(index);
                _activeCells.Add(index);
            }
        }
    }

    /// <summary>区画を有効にし、寿命の分だけ早送りしてから再生します。初めてなら作ります。</summary>
    private void Activate(int index)
    {
        var cell = _cells[index];
        if (cell == null)
        {
            cell = CreateCell(index % ColumnCount, index / ColumnCount);
            _cells[index] = cell;
        }

        _isActive[index] = true;
        cell.gameObject.SetActive(true);
        cell.Simulate(GetMaxLifetime(cell), true, true, true);
        cell.Play(true);
    }

    private void Deactivate(int index)
    {
        _isActive[index] = false;
        var cell = _cells[index];
        if (cell != null) cell.gameObject.SetActive(false);
    }

    /// <summary>区画 (i, j) の Particle System を作り、Box・Rate・Max・Order in Layer を区画に合わせて設定します。</summary>
    private ParticleSystem CreateCell(int i, int j)
    {
        float xMin = _edgesX[i];
        float xMax = _edgesX[i + 1];
        float yMin = _edgesY[j];
        float yMax = _edgesY[j + 1];
        Vector2 size = new(xMax - xMin, yMax - yMin);
        Vector3 position = new((xMin + xMax) * 0.5f, (yMin + yMax) * 0.5f, transform.position.z);

        var cell = Instantiate(particlePrefab, position, Quaternion.identity, transform);
        cell.name = $"{particlePrefab.name}_{i}_{j}";
        // プレハブの Play On Awake で再生が始まっていても、設定を変える前に止めて空にする
        cell.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        float count = density * _weightsX[i] * _weightsY[j] * size.x * size.y;

        var main = cell.main;
        main.playOnAwake = false;
        main.prewarm = false;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
        main.maxParticles = Mathf.CeilToInt(count * MaxParticlesHeadroom) + 1;

        var shape = cell.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.position = Vector3.zero;
        shape.rotation = Vector3.zero;
        shape.scale = new Vector3(size.x, size.y, shape.scale.z);

        // 平均の寿命の間に count 個出せば、箱の中の粒はおよそ count 個に保たれる
        var emission = cell.emission;
        emission.rateOverTime = count / Mathf.Max(GetMeanLifetime(cell), 0.01f);

        if (cell.TryGetComponent<ParticleSystemRenderer>(out var renderer))
        {
            renderer.sortingOrder = orderInLayer;
        }

        return cell;
    }

    /// <summary>範囲用のオブジェクトの位置と Scale から、範囲の左下と右上を求めます。</summary>
    private void GetAreaBounds(out Vector2 min, out Vector2 max)
    {
        Vector2 center = area.position;
        Vector3 scale = area.lossyScale;
        Vector2 half = new(Mathf.Abs(scale.x) * 0.5f, Mathf.Abs(scale.y) * 0.5f);
        min = center - half;
        max = center + half;
    }

    /// <summary>
    /// 1 つの軸について、区画の分かれ目と各区画の密度の倍率を求めます。
    /// 両端はフェザーの幅を段数で分けた細い帯にし、倍率を外側から 1/(2n), 3/(2n), … と上げます。
    /// 中央は区画の大きさ以下になるように等分し、倍率は 1 です。
    /// </summary>
    private static void BuildAxis(float min, float max, float cell, float feather, int steps, List<float> edges, List<float> weights)
    {
        edges.Clear();
        weights.Clear();
        float length = max - min;
        if (length <= MinCellLength) return;

        feather = Mathf.Min(feather, length * 0.5f);
        if (feather <= MinCellLength) steps = 0;

        edges.Add(min);

        // 始めの端(外側から内側へ)
        for (int s = 0; s < steps; s++)
        {
            edges.Add(min + feather * (s + 1) / steps);
            weights.Add((s + 0.5f) / steps);
        }

        // 中央
        float coreMin = min + (steps > 0 ? feather : 0f);
        float coreMax = max - (steps > 0 ? feather : 0f);
        float coreLength = coreMax - coreMin;
        if (coreLength > MinCellLength)
        {
            int count = Mathf.Max(1, Mathf.CeilToInt(coreLength / Mathf.Max(cell, MinCellLength)));
            for (int k = 0; k < count; k++)
            {
                edges.Add(coreMin + coreLength * (k + 1) / count);
                weights.Add(1f);
            }
        }

        // 終わりの端(内側から外側へ)
        for (int s = steps - 1; s >= 0; s--)
        {
            edges.Add(max - feather * s / steps);
            weights.Add((s + 0.5f) / steps);
        }
    }

    /// <summary>min〜max と重なる区画の番号の範囲(from〜to)を求めます。重なる区画が無ければ false。</summary>
    private static bool FindRange(List<float> edges, float min, float max, out int from, out int to)
    {
        from = -1;
        to = -1;
        for (int i = 0; i < edges.Count - 1; i++)
        {
            if (edges[i + 1] <= min) continue;
            if (edges[i] >= max) break;
            if (from < 0) from = i;
            to = i;
        }
        return from >= 0;
    }

    private static float GetMaxLifetime(ParticleSystem particleSystem)
    {
        var lifetime = particleSystem.main.startLifetime;
        return lifetime.mode switch
        {
            ParticleSystemCurveMode.Constant => lifetime.constant,
            ParticleSystemCurveMode.TwoConstants => lifetime.constantMax,
            _ => lifetime.curveMultiplier,
        };
    }

    private static float GetMeanLifetime(ParticleSystem particleSystem)
    {
        var lifetime = particleSystem.main.startLifetime;
        return lifetime.mode switch
        {
            ParticleSystemCurveMode.Constant => lifetime.constant,
            ParticleSystemCurveMode.TwoConstants => (lifetime.constantMin + lifetime.constantMax) * 0.5f,
            _ => lifetime.curveMultiplier,
        };
    }

#if UNITY_EDITOR
    /// <summary>選んでいるときに、範囲の枠と区画の分かれ目を描きます。</summary>
    private void OnDrawGizmosSelected()
    {
        if (area == null) return;

        GetAreaBounds(out Vector2 min, out Vector2 max);
        var edgesX = new List<float>();
        var edgesY = new List<float>();
        var weights = new List<float>();
        BuildAxis(min.x, max.x, cellSize.x, featherWidth, featherSteps, edgesX, weights);
        BuildAxis(min.y, max.y, cellSize.y, featherWidth, featherSteps, edgesY, weights);
        float z = transform.position.z;

        // 区画の分かれ目
        Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.35f);
        foreach (float x in edgesX) Gizmos.DrawLine(new Vector3(x, min.y, z), new Vector3(x, max.y, z));
        foreach (float y in edgesY) Gizmos.DrawLine(new Vector3(min.x, y, z), new Vector3(max.x, y, z));

        // 範囲の枠
        Gizmos.color = new Color(0.4f, 0.8f, 1f, 1f);
        Gizmos.DrawWireCube(new Vector3((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f, z), new Vector3(max.x - min.x, max.y - min.y, 0f));
    }
#endif
}
