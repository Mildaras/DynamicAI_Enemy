using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays real-time weight evolution for Main enemy.
/// Toggle with Tab key.
/// </summary>
public class StatsOverlay : MonoBehaviour
{
    public static StatsOverlay Instance { get; private set; }
    
    [Header("UI References")]
    public GameObject overlayPanel;
    public RectTransform graphsContainer;
    public TMP_Text adaptationsText;
    public TMP_Text currentSessionText;
    public TMP_Text pageIndicatorText;
    
    [Header("Settings")]
    public KeyCode toggleKey = KeyCode.Tab;
    public int graphsPerPage = 6;
    
    [Header("Graph Colors")]
    public Color greyColor = new Color(0.5f, 0.5f, 0.5f, 1f); // No enemy
    public Color blueColor = new Color(0.3f, 0.7f, 1f, 1f);    // Enemy spawned
    public Color greenColor = new Color(0.2f, 1f, 0.2f, 1f);   // Weight increased
    public Color redColor = new Color(1f, 0.2f, 0.2f, 1f);     // Weight decreased
    public float lineThickness = 2f;
    public float dotSize = 6f;
    
    private bool _isVisible = false;
    private List<GameObject> _graphObjects = new List<GameObject>();
    private int _currentPage = 0;
    private int _totalPages = 1;
    
    // Weight history: each entry is a snapshot at a point in time
    private List<WeightSnapshot> _history = new List<WeightSnapshot>();
    
    private class WeightSnapshot
    {
        public Dictionary<string, float> weights = new Dictionary<string, float>();
    }
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    void Start()
    {
        if (overlayPanel != null)
            overlayPanel.SetActive(false);
            
        // Add initial grey snapshot (no enemy)
        AddGreySnapshot();
    }
    
    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleOverlay();
        }
        
        // Page navigation with arrow keys (only when overlay is visible)
        if (_isVisible)
        {
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                NextPage();
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                PreviousPage();
            }
        }
    }
    
    void ToggleOverlay()
    {
        _isVisible = !_isVisible;
        
        if (overlayPanel != null)
            overlayPanel.SetActive(_isVisible);
            
        if (_isVisible)
            RefreshGraphs();
    }
    
    /// <summary>
    /// Add grey snapshot with all weights at 0
    /// </summary>
    void AddGreySnapshot()
    {
        var snapshot = new WeightSnapshot();
        var weightNames = GetAllWeightNames();
        
        foreach (var name in weightNames)
            snapshot.weights[name] = 0f;
            
        _history.Add(snapshot);
    }
    
    /// <summary>
    /// Called when adaptation happens or when we want to capture current state
    /// </summary>
    public void CaptureCurrentWeights()
    {
        var mainEnemy = FindMainEnemy();
        if (mainEnemy == null)
        {
            Debug.LogWarning("StatsOverlay: No Main enemy found");
            return;
        }
        
        var snapshot = new WeightSnapshot();
        
        // Capture all weights from enemy
        snapshot.weights["FastSpell"] = mainEnemy.fastSpell;
        snapshot.weights["MediumSpell"] = mainEnemy.mediumSpell;
        snapshot.weights["SlowSpell"] = mainEnemy.slowSpell;
        snapshot.weights["Attack"] = mainEnemy.atck;
        snapshot.weights["CoverForHealth"] = mainEnemy.coverForHealth;
        snapshot.weights["CoverFromAttacks"] = mainEnemy.coverFromAttacks;
        snapshot.weights["WallRangedAttack"] = mainEnemy.wallRangedAttack;
        snapshot.weights["HealHigh"] = mainEnemy.healHigh;
        snapshot.weights["HealCover"] = mainEnemy.healCover;
        snapshot.weights["WallHeal"] = mainEnemy.wallHeal;
        snapshot.weights["SpawnExploder"] = mainEnemy.spawnExpl;
        snapshot.weights["SpawnBouncer"] = mainEnemy.spawnBoun;
        snapshot.weights["SpawnTank"] = mainEnemy.spawnTnk;
        
        _history.Add(snapshot);
        
        Debug.Log($"StatsOverlay: Captured snapshot #{_history.Count}");
        
        // Refresh if visible
        if (_isVisible && overlayPanel != null && overlayPanel.activeSelf)
            RefreshGraphs();
    }
    
    EnemyController FindMainEnemy()
    {
        var enemies = FindObjectsOfType<EnemyController>();
        foreach (var enemy in enemies)
        {
            if (enemy.role == EnemyRole.Main && enemy.gameObject.activeSelf)
                return enemy;
        }
        return null;
    }
    
    string[] GetAllWeightNames()
    {
        return new[]
        {
            "FastSpell", "MediumSpell", "SlowSpell", "Attack",
            "CoverForHealth", "CoverFromAttacks", "WallRangedAttack",
            "HealHigh", "HealCover", "WallHeal",
            "SpawnExploder", "SpawnBouncer", "SpawnTank"
        };
    }
    
    void RefreshGraphs()
    {
        // Update text displays
        UpdateTextDisplays();
        
        // Clear old graphs
        foreach (var obj in _graphObjects)
        {
            if (obj != null) Destroy(obj);
        }
        _graphObjects.Clear();
        
        if (_history.Count == 0) return;
        
        // Build timelines for each weight
        var timelines = BuildTimelines();
        
        // Get top 6 most changed weights
        var topWeights = GetTopChangedWeights(timelines, 6);
        
        // Draw graphs
        float graphHeight = 60f;
        float graphSpacing = 10f;
        float yPos = 0f;
        
        // Calculate pagination
        _totalPages = Mathf.CeilToInt((float)topWeights.Count / graphsPerPage);
        _currentPage = Mathf.Clamp(_currentPage, 0, Mathf.Max(0, _totalPages - 1));
        
        // Get weights for current page
        int startIndex = _currentPage * graphsPerPage;
        int endIndex = Mathf.Min(startIndex + graphsPerPage, topWeights.Count);
        var pageWeights = topWeights.GetRange(startIndex, endIndex - startIndex);
        
        Debug.Log($"Page {_currentPage + 1}/{_totalPages} - Showing {pageWeights.Count} graphs");
        
        foreach (var weightName in pageWeights)
        {
            if (!timelines.ContainsKey(weightName)) continue;
            
            var timeline = timelines[weightName];
            if (timeline.Count == 0) continue;
            
            Debug.Log($"Creating graph for {weightName} at yPos={yPos}");
            CreateGraph(weightName, timeline, yPos, graphHeight);
            yPos -= (graphHeight + graphSpacing);
        }
    }
    
    Dictionary<string, List<float>> BuildTimelines()
    {
        var timelines = new Dictionary<string, List<float>>();
        var weightNames = GetAllWeightNames();
        
        foreach (var name in weightNames)
            timelines[name] = new List<float>();
            
        foreach (var snapshot in _history)
        {
            foreach (var name in weightNames)
            {
                if (snapshot.weights.ContainsKey(name))
                    timelines[name].Add(snapshot.weights[name]);
            }
        }
        
        return timelines;
    }
    
    List<string> GetTopChangedWeights(Dictionary<string, List<float>> timelines, int count)
    {
        var changes = new Dictionary<string, float>();
        
        foreach (var kvp in timelines)
        {
            if (kvp.Value.Count < 2) continue;
            
            float first = kvp.Value.First();
            float last = kvp.Value.Last();
            float absoluteChange = Mathf.Abs(last - first);
            
            changes[kvp.Key] = absoluteChange;
        }
        
        // If no changes, return all weights by highest final value
        if (changes.Count == 0)
        {
            return timelines
                .OrderByDescending(kvp => kvp.Value.LastOrDefault())
                .Select(kvp => kvp.Key)
                .ToList();
        }
        
        // Return ALL weights sorted by change amount (not limited to count)
        return changes
            .OrderByDescending(kvp => kvp.Value)
            .Select(kvp => kvp.Key)
            .ToList();
    }
    
    void NextPage()
    {
        if (_currentPage < _totalPages - 1)
        {
            _currentPage++;
            RefreshGraphs();
        }
    }
    
    void PreviousPage()
    {
        if (_currentPage > 0)
        {
            _currentPage--;
            RefreshGraphs();
        }
    }
    
    void CreateGraph(string weightName, List<float> timeline, float yPos, float height)
    {
        // Container
        GameObject graphObj = new GameObject($"Graph_{weightName}");
        graphObj.transform.SetParent(graphsContainer, false);
        
        RectTransform containerRect = graphObj.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0, 1);
        containerRect.anchorMax = new Vector2(1, 1);
        containerRect.pivot = new Vector2(0, 1);
        containerRect.anchoredPosition = new Vector2(0, yPos);
        containerRect.sizeDelta = new Vector2(0, height);
        
        // Determine color
        Color lineColor = DetermineColor(timeline);
        
        // Create label
        CreateLabel(graphObj, weightName, timeline, lineColor);
        
        // Create graph area
        GameObject graphArea = new GameObject("GraphArea");
        graphArea.transform.SetParent(graphObj.transform, false);
        
        RectTransform graphRect = graphArea.AddComponent<RectTransform>();
        graphRect.anchorMin = new Vector2(0, 0);
        graphRect.anchorMax = new Vector2(1, 0);
        graphRect.pivot = new Vector2(0, 0);
        graphRect.anchoredPosition = new Vector2(10, 5);
        graphRect.sizeDelta = new Vector2(-20, height - 25);
        
        Canvas.ForceUpdateCanvases();
        
        float graphWidth = graphsContainer.rect.width - 40f;
        float graphHeight = height - 25f;
        
        if (graphWidth > 0 && graphHeight > 0)
        {
            DrawLine(graphArea, timeline, graphWidth, graphHeight, lineColor);
            DrawDots(graphArea, timeline, graphWidth, graphHeight, lineColor);
        }
        
        _graphObjects.Add(graphObj);
    }
    
    Color DetermineColor(List<float> timeline)
    {
        if (timeline.Count == 1)
        {
            // Only grey snapshot
            return timeline[0] == 0 ? greyColor : blueColor;
        }
        
        // Check if all zeroes (grey state)
        if (timeline.All(v => v == 0))
            return greyColor;
            
        // Check first non-zero value
        float firstNonZero = timeline.FirstOrDefault(v => v != 0);
        if (firstNonZero == 0)
            return greyColor;
            
        float lastValue = timeline.Last();
        
        // If only one non-zero snapshot, return blue
        int nonZeroCount = timeline.Count(v => v != 0);
        if (nonZeroCount <= 1)
            return blueColor;
            
        // Compare first non-zero to last
        float change = lastValue - firstNonZero;
        float threshold = Mathf.Abs(firstNonZero) * 0.01f; // 1% threshold
        
        if (change > threshold)
            return greenColor;
        else if (change < -threshold)
            return redColor;
        else
            return blueColor;
    }
    
    void CreateLabel(GameObject parent, string weightName, List<float> timeline, Color color)
    {
        GameObject labelObj = new GameObject("Label", typeof(TextMeshProUGUI));
        labelObj.transform.SetParent(parent.transform, false);
        
        TextMeshProUGUI label = labelObj.GetComponent<TextMeshProUGUI>();
        
        // Calculate current value and change from LAST adaptation (not from start)
        float lastValue = timeline.Last();
        float changeFromLastAdaptation = 0f;
        
        if (timeline.Count > 1)
        {
            float previousValue = timeline[timeline.Count - 2];
            changeFromLastAdaptation = lastValue - previousValue;
        }
        
        // Determine arrow and color for change
        string arrow;
        string changeColor;
        
        if (Mathf.Abs(changeFromLastAdaptation) < 0.01f)
        {
            arrow = "-";
            changeColor = ColorUtility.ToHtmlStringRGB(blueColor);
        }
        else if (changeFromLastAdaptation > 0)
        {
            arrow = "↑";
            changeColor = ColorUtility.ToHtmlStringRGB(greenColor);
        }
        else
        {
            arrow = "↓";
            changeColor = ColorUtility.ToHtmlStringRGB(redColor);
        }
        
        string changeAmount = $"{Mathf.Abs(changeFromLastAdaptation):F0}";
        
        // Format: "WeightName | CurrentValue | ColoredChange"
        label.text = $"<color=#FFFFFF>{weightName}</color> | <color=#FFFFFF>{lastValue:F0}</color> | <color=#{changeColor}>{arrow} {changeAmount}</color>";
        label.fontSize = 16; // Larger font for better readability
        label.color = Color.white;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.enableAutoSizing = false;
        label.enableWordWrapping = false;
        label.overflowMode = TextOverflowModes.Overflow;
        label.richText = true;
        label.fontSharedMaterial.SetFloat("_FaceDilate", 0); // Sharper text
        label.fontSharedMaterial.SetFloat("_OutlineWidth", 0); // No outline blur
        
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        // Stretch horizontally within parent, fixed height at top
        labelRect.anchorMin = new Vector2(0, 1);
        labelRect.anchorMax = new Vector2(1, 1);
        labelRect.pivot = new Vector2(0, 1);
        labelRect.anchoredPosition = new Vector2(10, -5);
        // Left/Right offsets from edges, then fixed height
        labelRect.offsetMin = new Vector2(10, labelRect.offsetMin.y); // Left offset
        labelRect.offsetMax = new Vector2(-10, 0); // Right offset (negative from right edge)
        labelRect.sizeDelta = new Vector2(0, 20); // Height only, width is stretched
        
        // Ensure no rotation
        labelRect.localRotation = Quaternion.identity;
        labelRect.localScale = Vector3.one;
    }
    
    void DrawLine(GameObject parent, List<float> timeline, float width, float height, Color color)
    {
        if (timeline.Count < 1) return;
        
        // Handle single point
        if (timeline.Count == 1)
        {
            float value = timeline[0];
            float maxValue = Mathf.Max(value, 1f) * 1.1f;
            float y = (value / maxValue) * height;
            CreateLineSegment(parent, new Vector2(0, y), new Vector2(width, y), color);
            return;
        }
        
        // Multiple points
        float max = timeline.Max();
        if (max < 0.01f) max = 1f;
        max *= 1.1f; // 10% headroom
        
        for (int i = 0; i < timeline.Count - 1; i++)
        {
            float x1 = (i / (float)(timeline.Count - 1)) * width;
            float y1 = (timeline[i] / max) * height;
            
            float x2 = ((i + 1) / (float)(timeline.Count - 1)) * width;
            float y2 = (timeline[i + 1] / max) * height;
            
            CreateLineSegment(parent, new Vector2(x1, y1), new Vector2(x2, y2), color);
        }
    }
    
    void DrawDots(GameObject parent, List<float> timeline, float width, float height, Color color)
    {
        if (timeline.Count < 1) return;
        
        float max = timeline.Max();
        if (max < 0.01f) max = 1f;
        max *= 1.1f;
        
        for (int i = 0; i < timeline.Count; i++)
        {
            float x = (i / (float)Mathf.Max(1, timeline.Count - 1)) * width;
            float y = (timeline[i] / max) * height;
            
            CreateDot(parent, new Vector2(x, y), color);
        }
    }
    
    void CreateLineSegment(GameObject parent, Vector2 start, Vector2 end, Color color)
    {
        GameObject line = new GameObject("Line", typeof(Image));
        line.transform.SetParent(parent.transform, false);
        
        Image img = line.GetComponent<Image>();
        img.color = color;
        
        Vector2 diff = end - start;
        float distance = diff.magnitude;
        float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
        
        RectTransform rect = line.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(0, 0);
        rect.pivot = new Vector2(0, 0.5f);
        rect.sizeDelta = new Vector2(distance, lineThickness);
        rect.anchoredPosition = start;
        rect.localEulerAngles = new Vector3(0, 0, angle);
        
        _graphObjects.Add(line);
    }
    
    void CreateDot(GameObject parent, Vector2 position, Color color)
    {
        GameObject dot = new GameObject("Dot", typeof(Image));
        dot.transform.SetParent(parent.transform, false);
        
        Image img = dot.GetComponent<Image>();
        img.color = color;
        
        RectTransform rect = dot.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(0, 0);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(dotSize, dotSize);
        rect.anchoredPosition = position;
        
        _graphObjects.Add(dot);
    }
    
    void UpdateTextDisplays()
    {
        // Adaptations count (history - 1 because first snapshot is grey baseline)
        int adaptationCount = Mathf.Max(0, _history.Count - 1);
        if (adaptationsText != null)
        {
            adaptationsText.text = $"Adaptations: {adaptationCount}";
        }
        
        // Page indicator
        if (pageIndicatorText != null)
        {
            pageIndicatorText.text = $"Page {_currentPage + 1}/{_totalPages} (←/→ to navigate)";
        }
        
        // Current session name
        if (currentSessionText != null)
        {
            string sessionName = "No Session";
            if (SessionManager.Instance != null)
            {
                sessionName = SessionManager.Instance.GetSessionId();
                if (string.IsNullOrEmpty(sessionName))
                    sessionName = "Active Session";
            }
            currentSessionText.text = $"Session: {sessionName}";
            currentSessionText.fontSize = 16;
        }
    }
    
    /// <summary>
    /// Reset for new session
    /// </summary>
    public void ResetSession()
    {
        _history.Clear();
        AddGreySnapshot();
        
        if (_isVisible)
            RefreshGraphs();
    }
}
