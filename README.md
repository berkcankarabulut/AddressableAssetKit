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
