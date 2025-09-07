# 相册整理助手（发布版）

一个免安装的小工具，帮助你将相册按“横/竖（长宽比）→ 颜色 → 亮度”排序；支持就地重命名或复制到输出目录，可“仅预览”不改名。

## 目录结构

```
发布/
├─ 相册整理助手.exe          # 双击运行
└─ scripts/                 # 程序调用的脚本
   ├─ normalize-folders.ps1
   ├─ reorder-photos.ps1
   └─ reorder-photos-color-first.ps1
```

## 系统要求

- Windows 10 / Windows 11（通常自带 .NET Framework 4.8/4.8.1）
- 无需安装 .NET 运行时，直接双击 exe 即可

## 快速上手

1. 打开“相册整理助手.exe”
2. 在页面上方“大虚线方框”：
   - 点击：打开资源管理器
   - 拖拽：把包含照片的文件夹拖拽到方框
3. 下方“目标文件夹路径”可手动粘贴/编辑路径
4. 选择“排序策略”和“输出模式”
5. 点“开始运行”，右侧“日志”会显示进度与结果

## 概念说明

- 横/竖：按照片长宽比分组（横向/纵向）
- 颜色：在同一组内，按主色调排序
- 亮度：再按明暗程度排序
- 仅预览：不会改名/复制，只输出预期结果到日志
- 输出模式：
  - 就地重命名：直接在原目录改名
  - 复制到输出文件夹：保留原图，将排序后的结果复制到指定目录

## 常见问题（FAQ）

- 双击没反应？
  - 若文件来自网络，请右键 exe → 属性 → （如有）勾选“解除阻止”
  - 请确保“scripts”文件夹与 exe 位于同一目录
  - 也可在 PowerShell 执行：`& "路径\相册整理助手.exe"` 查看是否被安全策略拦截

- 日志区太小/窗口不完整？
  - 程序启动默认最大化；如有显示问题，可调整窗口大小后再次运行

## 更新方式

- 直接用新的 exe 覆盖本目录同名文件，scripts 目录保持不变

---

## 发布到 GitHub 的建议

> 建议把“源码”和“成品”分开：源码放仓库，成品放 Release 附件（zip）。

### 仓库建议包含

- `PhotoSorter.Wpf/`（源码工程：XAML、.cs、.csproj）
- `README.md`（项目简介、构建与使用说明）
- `LICENSE`（开源许可证，常见：MIT）
- `.gitignore`（使用 VisualStudio/.NET 模板，忽略 bin/obj）
- `docs/`（可选：截图、动图演示）

示例 .gitignore（节选）：

```
bin/
obj/
*.user
*.suo
*.userosscache
*.sln.docstates
``` 

### 发布 Release（推荐做法）

1. 在本地构建“发布版”：得到“发布/”目录
2. 将“发布/”打包为 zip（保留 scripts/ 子目录）
3. 在 GitHub 仓库 → Releases → New Release：
   - Tag：v1.0.0（按语义化版本）
   - Release notes：更新说明
   - Attach binaries：上传打包好的 zip

### 可选项

- 代码签名（证书）：减少 Windows SmartScreen 警告
- CI/CD：使用 GitHub Actions（Windows runner）自动构建并上传 Release
- 问题模板/讨论区：提升反馈效率

如需，我可以：

- 生成项目根目录的 `README.md`、`LICENSE` 与 `.gitignore`
- 制作 Release 用的 zip 包，或配置 GitHub Actions 自动出包

