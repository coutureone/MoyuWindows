# 🚀 快速开始指南

## 📋 概述

这个指南将帮助你在 5 分钟内将 MoyuWindows 项目发布到 GitHub 并启用自动构建和发布。

## ✅ 前置检查

确保你已经：
- [ ] 完成了所有代码开发
- [ ] 本地测试通过
- [ ] 拥有 GitHub 账号
- [ ] 安装了 Git

## 🎯 三步发布流程

### 第 1 步：初始化 Git 仓库（2 分钟）

```bash
# 进入项目目录
cd /Users/couture/Dev/MoyuWindows

# 初始化 Git
git init

# 添加所有文件
git add .

# 创建初始提交
git commit -m "Initial commit: MoyuWindows v1.0.0"
```

### 第 2 步：创建 GitHub 仓库（1 分钟）

**选项 A: 使用 GitHub 网站**
1. 访问 https://github.com/new
2. 仓库名: `MoyuWindows`
3. 描述: `🐟 摸鱼背单词 - Windows 原生版本`
4. 选择 Public
5. **不要**勾选任何初始化选项
6. 点击 "Create repository"

**选项 B: 使用 GitHub CLI（如果已安装）**
```bash
gh repo create MoyuWindows --public --source=. --remote=origin
```

### 第 3 步：推送并发布（2 分钟）

```bash
# 连接远程仓库（替换 YOUR_USERNAME）
git remote add origin https://github.com/YOUR_USERNAME/MoyuWindows.git

# 推送代码
git branch -M main
git push -u origin main

# 创建并推送第一个 Release
git tag -a v1.0.0 -m "Release v1.0.0"
git push origin v1.0.0
```

## 🎉 完成！

现在访问你的 GitHub 仓库：
1. 点击 "Actions" 标签 - 查看构建进度
2. 等待 5-10 分钟构建完成
3. 点击 "Releases" 标签 - 下载发布的文件

## 📦 构建产物

构建完成后，你会得到：
- `Moyu-win-x64.zip` - Windows x64 版本
- `Moyu-win-x64.zip.sha256` - 校验和
- `Moyu-win-arm64.zip` - Windows ARM64 版本
- `Moyu-win-arm64.zip.sha256` - 校验和

## 🧪 本地测试（可选）

在推送到 GitHub 之前，可以先本地测试构建：

**Windows 用户:**
```powershell
.\test-build.ps1
```

**macOS/Linux 用户:**
```bash
./test-build.sh
```

## 🔄 后续更新

每次发布新版本：

```bash
# 1. 修改代码并提交
git add .
git commit -m "修复某个问题"

# 2. 更新版本号（编辑 MoyuWindows.csproj）
# <Version>1.0.1</Version>

# 3. 创建新 tag
git tag -a v1.0.1 -m "Release v1.0.1"

# 4. 推送
git push origin main
git push origin v1.0.1
```

## 📚 详细文档

- **完整设置指南**: `.agent/workflows/github-setup.md`
- **配置总结**: `GITHUB_ACTIONS_SUMMARY.md`
- **项目 README**: `README.md`

## ❓ 常见问题

### Q: 如何只构建不发布？
A: 只推送代码，不创建 tag：
```bash
git push origin main
```

### Q: 如何删除错误的 Release？
A: 在 GitHub 网站上进入 Releases 页面，点击对应 Release 的 "Delete" 按钮。

### Q: 构建失败怎么办？
A: 
1. 访问 Actions 页面查看错误日志
2. 检查 `moyu.db` 和 `Resources/icon.ico` 是否存在
3. 确保项目可以在本地成功构建

### Q: 如何更新 README 中的用户名？
A: 替换所有 `YOUR_USERNAME` 为你的 GitHub 用户名：
```bash
# macOS/Linux
sed -i '' 's/YOUR_USERNAME/你的用户名/g' README.md

# Windows (PowerShell)
(Get-Content README.md) -replace 'YOUR_USERNAME', '你的用户名' | Set-Content README.md
```

## 🎯 检查清单

发布前确认：
- [ ] 代码已完成并测试
- [ ] 版本号已更新
- [ ] Git 仓库已初始化
- [ ] GitHub 仓库已创建
- [ ] 代码已推送
- [ ] Tag 已创建并推送
- [ ] GitHub Actions 构建成功
- [ ] Release 已自动创建
- [ ] 下载并测试发布文件

## 💡 提示

- 第一次构建可能需要 10-15 分钟
- 后续构建有缓存，只需 5-8 分钟
- 可以在 Actions 页面实时查看构建日志
- Release 说明会自动生成，包含详细的功能列表

---

**需要帮助？** 查看 `.agent/workflows/github-setup.md` 获取更详细的说明。
