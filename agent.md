这是一个Rimworld游戏Mod工程。

# 实现目标

实现一个简单的常识库生成供RimTalk Memory Mod (https://steamcommunity.com/sharedfiles/filedetails/?id=3608181242)使用。
你可能需要反编译游戏或者mod源代码仓库来查看

# 代码风格

因为是mod，尽量避免patch，并且出于安全性考量尽量在程序边界做完整的null check，mod内部不必防御性编程。
尽量避免任何Update每帧调用的代码。
