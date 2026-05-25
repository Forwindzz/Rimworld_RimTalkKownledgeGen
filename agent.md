这是一个Rimworld游戏Mod工程。

# 实现目标

实现一个简单的常识库生成供RimTalk Memory Mod (https://steamcommunity.com/sharedfiles/filedetails/?id=3608181242)使用。
你可能需要反编译游戏或者mod源代码仓库来查看

# 实现步骤

任何讨论请创建一个文件记录在Doc/Discuss下，然后与我讨论，完成后将其归档到Doc/Spec/下，编写代码时参照Doc/Spec/下的文档进行编写。


# 代码风格

因为是mod，尽量避免patch，并且出于安全性考量尽量在程序边界做完整的null check，mod内部不必防御性编程。
尽量避免任何Update每帧调用的代码。
所有文件都是utf-8编码。

# 构建与部署

- 统一使用仓库根目录的 `build_deploy.bat` 做一键构建和部署。
- 构建产物统一输出到 `./Build` 目录（包含 `Build/1.6/Assemblies/GenKnowledge.dll` 和中间文件 `Build/obj`）。
- 部署目标 RimWorld 路径固定为：
  - `E:\Program Files\Steam\steamapps\common\RimWorld`
- 部署目标 Mod 目录固定为：
  - `E:\Program Files\Steam\steamapps\common\RimWorld\Mods\RimTalk_GenKnowledge`
- 该脚本仅用于“当前仓库代码 -> 本地 RimWorld Mods 目录”的快速调试流程。
