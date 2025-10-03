# 📦 Addressable Asset Kit

A comprehensive Unity package for managing Addressable asset downloads and lifecycle with reactive programming support. Perfect for mobile games, live service applications, and any project that needs dynamic content delivery.

[![Unity Version](https://img.shields.io/badge/Unity-2021.3%2B-blue.svg)](https://unity3d.com/get-unity/download)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

## ✨ Features

### 🚀 Download Manager
- **Concurrent Downloads** - Download multiple assets simultaneously with configurable limits
- **Progress Tracking** - Real-time progress updates with byte-level accuracy
- **Queue Management** - Automatic queue processing with priority support
- **Auto Retry** - Configurable retry logic for failed downloads
- **Catalog Updates** - Check and update content catalogs automatically
- **Cache Management** - Monitor and clear cache storage

### 🎨 Asset Manager
- **Smart Loading** - Load assets with automatic reference counting
- **Multiple Load Types** - Support for single assets, multiple assets, and labels
- **GameObject Instantiation** - Direct instantiation support with parent transform
- **Memory Management** - Automatic handle tracking and cleanup
- **Reference Counting** - Prevents premature asset unloading

### 🔄 Reactive Programming
- Built on **UniRx** for powerful event handling
- Observable events for all download states
- Reactive properties for state management
- Easy integration with UI and game logic

## 📋 Requirements

### Dependencies
- **Unity 2021.3+**
- **com.unity.addressables** (1.21.0+)
- **com.cysharp.unitask** (2.3.3+)
- **com.neuecc.unirx** (7.1.0+)

### Optional
- **Zenject/Extenject** - For dependency injection support

## 📥 Installation

### Via Unity Package Manager (Git URL)

1. Open **Window > Package Manager**
2. Click **[+]** button → **Add package from git URL**
3. Enter: `https://github.com/berkcankarabulut/AddressableAssetKit.git`

## 🎯 Quick Start

### Without Dependency Injection

```csharp
using AddressableAssetKit.Runtime;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private AddressableDownloadManager _downloadManager;
    private AddressableAssetManager _assetManager;

    void Start()
    {
        // Initialize managers
        _downloadManager = new AddressableDownloadManager();
        _downloadManager.Initialize();
        
        _assetManager = new AddressableAssetManager();
        
        // Start downloading
        DownloadAndLoadContent().Forget();
    }

    async UniTaskVoid DownloadAndLoadContent()
    {
        // Download content
        await _downloadManager.DownloadAsync("Level1");
        
        // Load asset
        var prefab = await _assetManager.LoadAssetAsync<GameObject>("Level1Prefab");
        Instantiate(prefab);
    }

    void OnDestroy()
    {
        _downloadManager?.Dispose();
        _assetManager?.Dispose();
    }
}
```

### With Zenject

```csharp
using AddressableAssetKit.Runtime;
using Zenject;

public class GameInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        // Bind settings (optional)
        Container.Bind<DownloadSettings>()
            .FromInstance(new DownloadSettings
            {
                MaxConcurrentDownloads = 3,
                AutoRetryOnFail = true,
                MaxRetryCount = 3
            })
            .AsSingle();

        // Bind managers
        Container.BindInterfacesAndSelfTo<AddressableDownloadManager>()
            .AsSingle();
            
        Container.Bind<AddressableAssetManager>()
            .AsSingle();
    }
}
```

```csharp
public class LevelLoader : MonoBehaviour
{
    [Inject] private AddressableDownloadManager _downloadManager;
    [Inject] private AddressableAssetManager _assetManager;

    async UniTask LoadLevel(string levelLabel)
    {
        await _downloadManager.DownloadAsync(levelLabel);
        var level = await _assetManager.LoadAssetAsync<GameObject>($"{levelLabel}Scene");
        Instantiate(level);
    }
}
```

## 📚 Usage Examples

### 1. Download with Progress Tracking

```csharp
using UniRx;

void SetupDownloadUI()
{
    // Subscribe to progress updates
    _downloadManager.OnDownloadProgress
        .Subscribe(data => {
            progressBar.value = data.Progress;
            
            float downloadedMB = data.DownloadedBytes / 1024f / 1024f;
            float totalMB = data.TotalBytes / 1024f / 1024f;
            
            statusText.text = $"Downloading {data.Label}: {downloadedMB:F2} MB / {totalMB:F2} MB";
        })
        .AddTo(this);

    // Track completion
    _downloadManager.OnDownloadCompleted
        .Subscribe(label => {
            Debug.Log($"✓ {label} downloaded!");
        })
        .AddTo(this);

    // Handle errors
    _downloadManager.OnDownloadFailed
        .Subscribe(error => {
            Debug.LogError($"Failed to download {error.Label}: {error.Error}");
            ShowErrorPopup(error.Error);
        })
        .AddTo(this);
}
```

### 2. Check Download Size Before Starting

```csharp
async UniTask DownloadWithConfirmation(string label)
{
    // Check size first
    long bytes = await _downloadManager.CheckDownloadSizeAsync(label);
    
    if (bytes == 0)
    {
        Debug.Log("Content already downloaded!");
        return;
    }
    
    float sizeMB = bytes / 1024f / 1024f;
    
    // Show confirmation dialog
    bool confirmed = await ShowConfirmDialog(
        $"Download {sizeMB:F2} MB of content?"
    );
    
    if (confirmed)
    {
        await _downloadManager.DownloadAsync(label);
    }
}
```

### 3. Download Multiple Assets

```csharp
async UniTask DownloadGameContent()
{
    var labels = new List<string> 
    { 
        "Characters", 
        "Levels", 
        "UI", 
        "Audio" 
    };
    
    // Download all sequentially
    await _downloadManager.DownloadMultipleAsync(labels);
    
    // Or download with individual control
    foreach (var label in labels)
    {
        _downloadManager.DownloadContent(label); // Non-blocking
    }
    
    // Wait for all to complete
    await _downloadManager.OnAllDownloadsCompleted.First();
}
```

### 4. Load Assets by Label

```csharp
async UniTask LoadAllCharacters()
{
    // Load all assets with "Characters" label
    var characters = await _assetManager.LoadAssetsByLabelAsync<GameObject>("Characters");
    
    foreach (var character in characters)
    {
        Debug.Log($"Loaded character: {character.name}");
    }
}
```

### 5. Instantiate with Proper Cleanup

```csharp
async UniTask SpawnEnemy(string enemyKey)
{
    // Instantiate directly
    var enemy = await _assetManager.InstantiateAsync(enemyKey, transform);
    
    // Use it...
    enemy.GetComponent<Enemy>().Initialize();
    
    // Clean up when done
    await UniTask.Delay(5000);
    _assetManager.ReleaseInstance(enemy);
}
```

### 6. Reference Counting

```csharp
async UniTask SharedAssetExample()
{
    // First load - refCount = 1
    var texture = await _assetManager.LoadAssetAsync<Texture2D>("SharedTexture");
    
    // Second load - refCount = 2 (returns cached)
    var sameTexture = await _assetManager.LoadAssetAsync<Texture2D>("SharedTexture");
    
    // Release once - refCount = 1 (still in memory)
    _assetManager.ReleaseAsset("SharedTexture");
    
    // Release again - refCount = 0 (unloaded)
    _assetManager.ReleaseAsset("SharedTexture");
}
```

### 7. Update Catalogs and Download New Content

```csharp
async UniTask CheckForUpdates()
{
    // Check if there are catalog updates
    bool hasUpdates = await _downloadManager.UpdateCatalogsAsync();
    
    if (hasUpdates)
    {
        Debug.Log("New content available!");
        
        // Download new season content
        await _downloadManager.DownloadAsync("Season2Content");
    }
}
```

### 8. Cache Management

```csharp
void DisplayCacheInfo()
{
    long cacheSize = _downloadManager.GetCacheSize();
    float cacheMB = cacheSize / 1024f / 1024f;
    
    cacheSizeText.text = $"Cache: {cacheMB:F2} MB";
}

void ClearAllCache()
{
    _downloadManager.ClearCache();
    Debug.Log("Cache cleared!");
}
```

### 9. Cancellation Support

```csharp
private CancellationTokenSource _cts;

async UniTask DownloadWithCancel()
{
    _cts = new CancellationTokenSource();
    
    try
    {
        await _downloadManager.DownloadAsync("LargeContent", ct: _cts.Token);
    }
    catch (OperationCanceledException)
    {
        Debug.Log("Download cancelled by user");
    }
}

void OnCancelButtonClicked()
{
    _cts?.Cancel();
    // Or cancel all downloads
    _downloadManager.CancelAll();
}
```

## 🎮 Real-World Example: Level Loader

```csharp
using AddressableAssetKit.Runtime;
using AddressableAssetKit.Runtime.Data;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class LevelManager : MonoBehaviour
{
    [Inject] private AddressableDownloadManager _downloadManager;
    [Inject] private AddressableAssetManager _assetManager;
    
    [SerializeField] private Slider progressBar;
    [SerializeField] private Text statusText;
    [SerializeField] private GameObject loadingPanel;
    
    private GameObject _currentLevel;

    void Start()
    {
        SetupUI();
    }

    void SetupUI()
    {
        _downloadManager.OnDownloadProgress
            .Subscribe(UpdateProgress)
            .AddTo(this);
            
        _downloadManager.OnDownloadCompleted
            .Subscribe(_ => statusText.text = "Download Complete!")
            .AddTo(this);
    }

    public async UniTask LoadLevel(int levelNumber)
    {
        string levelLabel = $"Level{levelNumber}";
        string sceneName = $"Level{levelNumber}Scene";
        
        loadingPanel.SetActive(true);
        
        try
        {
            // Check download size
            long size = await _downloadManager.CheckDownloadSizeAsync(levelLabel);
            
            if (size > 0)
            {
                float sizeMB = size / 1024f / 1024f;
                statusText.text = $"Downloading {sizeMB:F2} MB...";
                
                // Download level content
                await _downloadManager.DownloadAsync(levelLabel);
            }
            
            // Unload previous level
            if (_currentLevel != null)
            {
                _assetManager.ReleaseInstance(_currentLevel);
            }
            
            // Load new level
            statusText.text = "Loading level...";
            _currentLevel = await _assetManager.InstantiateAsync(sceneName);
            
            statusText.text = "Ready!";
        }
        catch (Exception ex)
        {
            statusText.text = $"Error: {ex.Message}";
            Debug.LogError(ex);
        }
        finally
        {
            loadingPanel.SetActive(false);
        }
    }

    void UpdateProgress(ProgressData data)
    {
        progressBar.value = data.Progress;
        
        float downloadedMB = data.DownloadedBytes / 1024f / 1024f;
        float totalMB = data.TotalBytes / 1024f / 1024f;
        
        statusText.text = $"{downloadedMB:F2} / {totalMB:F2} MB ({data.Progress * 100:F0}%)";
    }

    void OnDestroy()
    {
        if (_currentLevel != null)
        {
            _assetManager.ReleaseInstance(_currentLevel);
        }
    }
}
```

## ⚙️ Configuration

### Download Settings

```csharp
public class DownloadSettings
{
    public int MaxConcurrentDownloads = 3;  // How many downloads can run at once
    public bool AutoRetryOnFail = true;     // Automatically retry failed downloads
    public int MaxRetryCount = 3;           // Maximum retry attempts
}
```

### Custom Configuration

```csharp
var settings = new DownloadSettings
{
    MaxConcurrentDownloads = 5,  // More aggressive downloading
    AutoRetryOnFail = true,
    MaxRetryCount = 5           // More retry attempts
};

var downloadManager = new AddressableDownloadManager(settings);
```

## 📊 API Reference

### AddressableDownloadManager

#### Properties
- `IsDownloading` - Observable property indicating if downloads are active
- `ActiveDownloadCount` - Number of currently downloading assets
- `QueuedDownloadCount` - Number of assets waiting in queue
- `TotalProgressPercentage` - Overall progress of all downloads (0-1)

#### Events (IObservable)
- `OnDownloadStarted` - Fires when a download begins
- `OnDownloadProgress` - Progress updates during download
- `OnDownloadCompleted` - Fires when a download succeeds
- `OnDownloadFailed` - Fires when a download fails
- `OnAllDownloadsCompleted` - Fires when all queued downloads finish

#### Methods
- `DownloadContent(string label, int priority = 0)` - Start download (non-blocking)
- `DownloadAsync(string label, int priority, CancellationToken)` - Await download completion
- `DownloadMultipleAsync(List<string> labels, ...)` - Download multiple assets
- `CheckDownloadSizeAsync(string label, ...)` - Get download size in bytes
- `CancelAll()` - Cancel all active and queued downloads
- `UpdateCatalogsAsync(CancellationToken)` - Check and update content catalogs
- `ClearCache()` - Clear all cached content
- `GetCacheSize()` - Get total cache size in bytes

### AddressableAssetManager

#### Methods
- `LoadAssetAsync<T>(string key, ...)` - Load single asset
- `LoadAssetsAsync<T>(List<string> keys, ...)` - Load multiple assets
- `LoadAssetsByLabelAsync<T>(string label, ...)` - Load all assets by label
- `InstantiateAsync(string key, Transform parent, ...)` - Instantiate GameObject
- `GetLoadedAsset<T>(string key)` - Get already loaded asset
- `IsAssetLoaded(string key)` - Check if asset is loaded
- `GetRefCount(string key)` - Get reference count for asset
- `ReleaseAsset(string key)` - Decrease ref count and possibly unload
- `ReleaseInstance(GameObject instance)` - Release instantiated GameObject
- `ReleaseAllAssets()` - Release all loaded assets

## 🎯 Best Practices

### 1. Always Release Assets
```csharp
// ❌ Bad - Memory leak
var asset = await _assetManager.LoadAssetAsync<Texture2D>("Icon");

// ✅ Good - Proper cleanup
var asset = await _assetManager.LoadAssetAsync<Texture2D>("Icon");
// ... use asset ...
_assetManager.ReleaseAsset("Icon");
```

### 2. Use Reference Counting Wisely
```csharp
// Multiple systems can safely share assets
class UIManager {
    async UniTask ShowIcon() {
        _icon = await _assetManager.LoadAssetAsync<Texture2D>("Icon");
        // refCount = 1
    }
    void OnDisable() => _assetManager.ReleaseAsset("Icon");
}

class CharacterPanel {
    async UniTask ShowIcon() {
        _icon = await _assetManager.LoadAssetAsync<Texture2D>("Icon");
        // refCount = 2 (same asset)
    }
    void OnDisable() => _assetManager.ReleaseAsset("Icon");
    // Asset only unloaded when both release
}
```

### 3. Check Download Size for Mobile
```csharp
async UniTask SmartDownload(string label)
{
    long bytes = await _downloadManager.CheckDownloadSizeAsync(label);
    
    // Warn on cellular if > 50MB
    if (Application.internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork
        && bytes > 50 * 1024 * 1024)
    {
        bool proceed = await ShowCellularWarning(bytes);
        if (!proceed) return;
    }
    
    await _downloadManager.DownloadAsync(label);
}
```

### 4. Update Catalogs on App Start
```csharp
async UniTask Initialize()
{
    // Always check for new content on startup
    bool hasUpdates = await _downloadManager.UpdateCatalogsAsync();
    
    if (hasUpdates)
    {
        // Show "New Content Available" notification
        ShowUpdateNotification();
    }
}
```

### 5. Handle Errors Gracefully
```csharp
_downloadManager.OnDownloadFailed
    .Subscribe(error => {
        // Log to analytics
        Analytics.LogError("DownloadFailed", error.Label, error.Error);
        
        // Show user-friendly message
        if (error.Error.Contains("Network"))
            ShowError("Network error. Please check your connection.");
        else if (error.Error.Contains("Space"))
            ShowError("Not enough storage space.");
        else
            ShowError("Download failed. Please try again.");
    })
    .AddTo(this);
```

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 👤 Author

**Berkcan Karabulut**
- GitHub: [@berkcankarabulut](https://github.com/berkcankarabulut)

## 🙏 Acknowledgments

- Built with [UniTask](https://github.com/Cysharp/UniTask) by Cysharp
- Powered by [UniRx](https://github.com/neuecc/UniRx) by neuecc
- Uses Unity's [Addressables](https://docs.unity3d.com/Packages/com.unity.addressables@latest) system

---

⭐ If you find this package useful, please consider giving it a star on GitHub!-
