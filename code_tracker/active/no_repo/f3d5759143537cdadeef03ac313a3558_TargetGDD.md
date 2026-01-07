�(# 【预研】优化-中⻥后的迅速感知 

#### 版本 内容 编辑⼈ 时间 

V1.0 创建⽂档 斧头 20251210 
v1.1 与战⽃组讨论后更新 斧头 20251210 

# 1.背景 

 中⻥波动性掩盖问题 
其中收尾阶段：

# 2.优化思路 

#### 从以下⻆度来思考，对玩家最重要的不是fish on（咬实），⽽是第⼀次尝饵的节点： 

- 线下钓⻥，感知到中⻥、提升情绪的，是⼿上或漂相传来“咬⼝”感觉；

- 从玩家时间 不需要应对⻥⾏为 | 需要应对⻥⾏为 的分界上，是第⼀次尝饵时开始，玩家需要 注

 意、观察、⾏动；

- 以 第⼀次尝饵 作重点，才能感受到“接⼝”； 

可以从视频中看到，从中⻥⽣成 → 界⾯fish on强提⽰，⼤约2秒；若⻥的尝饵周期更⻓，会“包进
去”更多的时间。
优化⽅式：

- 削弱原 FISH ON时的表现强度； 

- 轻尝的第⼀瞬间，作明显反馈/提⽰； 

- 极速的⾸次轻尝；

  - 注意：在惊⻥补偿阶段之后发⽣；（惊⻥补偿时间）

  - 尝-等-尝，⽽不是 等-尝-等； 

# 3.参考逻辑 

## 3.1削弱Fish On表现强度 

不⽤居中⽂字，⽽是⼒度条来表现；
⼈物可以发出声⾳，但是不从界⾯上做太明显的展⽰。

- （声⾳后续放在战⽃预研中做，不放在第⼀优先级）

## 3.2加强⾸次尝饵时的表现 

这个时候是玩家最关注的时候，考虑以下⽅法：

- （漂相是浮钓的乐趣重点；但将有单独的设计，不在本⽂档之中）

- 屏幕边缘变⾊（+镜头抖动？）。通过预研来检验效果是否恰当； 

  - 传达的感受：

    - 唤起、兴奋的感受

    - “Yeah！⻥来了！” 

    - “注意！”“让我们刺中这宝⻉！”

    - 强度应当低于上⻥

    - 暗合⻥的“duang”⼝感受 

    - 节奏短促、快；

  - 后续屏幕边缘变⾊将和⼿机震动、镜头抖动协同呈现感觉；

  - 参考

    - 1：（下载⽹⻚后打开） 

nibble.html
13.08KB

    - 2： 

https://www.bilibili.com/video/BV14AmxBPEvp/?spm_id_from=333.337.search-card.all.click&vd_source=4191e…
www.bilibili.com 
 （视频0:03处）； 

  - 需尝试：

    - 强度 & 形式； 

    - 后续的尝饵过程，是否反复触发特效/持续； 

- ⽤中间⽂字？通过预研来测试效果；（暂不使⽤）

- ⼒度条正确反馈、刺⻥按钮优化、声⾳、震动（放在其他单中预研）

## 3.3极速⾸次轻尝 

原逻辑⻅： 咬饵 
优化：改变顺序，以在惊⻥补偿后，只要姿态合理即⽴即感受到尝饵；

# 4. 视觉/程序触发规则 (Program Trigger Rules)

分为 `FX_fish_bite_mood_linger` 和 `FX_fish_bite_instant` 两个部分。其希望的 alpha 曲线参考 `documents\瞬间感知FX-程序驱动思路-板报.jpg`。

## FX_fish_bite_mood_linger
- **描述**: 表达玩家的情绪唤起状态 (Mood Linger)。对应板报中的蓝色曲线。
- **触发**: 从第一次感知到咬钩开始触发。
- **表现**: 持续一段时间的情绪渲染/氛围效果，强度随时间变化。
  - **曲线类型**: 参考 `easeInOutCubic` (类似于缓入缓出的三次曲线)。
     ![easeInOutCubic Curve](easeInOutCubic.png)
- **变量定义 (Variables)**:
  - `float MoodLingerDuration = 10.0f;` // 默认Mood Linger持续时间为10秒
  - `float InitialBiteMood = 0.7f;` // 初始咬口情绪值 (Initial Bite Mood Value)

- **表现**: 持续一段时间的情绪渲染/氛围效果，强度随时间变化。
  - **曲线类型**: 参考 `easeInOutCubic` (类似于缓入缓出的三次曲线)。
     ![easeInOutCubic Curve](easeInOutCubic.png)
     - **公式 fallback**: 若无法直接使用 Tween 库，可使用以下公式计算 Alpha 值 (其中 `t` 为归一化时间 `[0, 1]`, `initialMood` 为初始情绪值):
       ```csharp
       float GetAlpha(float timeSinceBite, float duration = 10.0f, float initialMood = 0.7f) {
           float t = Mathf.Clamp01(timeSinceBite / duration);
           // Standard easeInOutCubic: goes from 0 to 1
           float ease = t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
           // Decay from initialMood to 0 based on ease
           // When t=0, ease=0, result=initialMood
           // When t=1, ease=1, result=0
           return initialMood * (1f - ease);
       }
       ```
  - **再次触发规则 (Re-trigger Rule)**: 
    - 若在 Mood Linger 效果尚未消散时 (Alpha > 0 或 timeSinceBite < duration) 再次检测到咬口：
    - **立即停止** 当前的 Tween/计时。
    - **重置** 时间 `timeSinceBite = 0`。
    - **重新开始** 新的 Mood Linger 过程，初始值重置为 `InitialBiteMood` (例如 0.7)。即：忽略之前的剩余值，强制从新的一次“咬口情绪”峰值开始。

## FX_fish_bite_instant
- **描述**: 表达手上传来的咬口触感 (Instant)。
- **触发**: 在鱼咬钩的瞬间触发。
- **表现**: 短促、直接的视觉/震动反馈，模拟真实的“咬口”感觉。
� ��
�� ��*cascade08
�� 
�� �� *cascade08�� *cascade08��*cascade08�� *cascade08�� *cascade08�� *cascade08��*cascade08�� *cascade08��*cascade08�� *cascade08��*cascade08�� *cascade08�� *cascade08��*cascade08�� *cascade08��*cascade08�� *cascade08��*cascade08�� *cascade08��*cascade08�� *cascade08��*cascade08�� *cascade08��*cascade08��  *cascade08� �  *cascade08� � *cascade08� � *cascade08� �  *cascade08� � *cascade08� �  *cascade08� � *cascade08� �  *cascade08� � *cascade08� �  *cascade08� � *cascade08� �  *cascade08� �!�!�! *cascade08�!�! *cascade08�!�"*cascade08�"�" *cascade08�"�"*cascade08�"�" *cascade08�"�"*cascade08�"�" *cascade08�"�"*cascade08�"�" *cascade08�"�"*cascade08�"�" *cascade08�"�"*cascade08�"�" *cascade08�"�"*cascade08�"�" *cascade08�"�"*cascade08�"�" *cascade08�"�"*cascade08�"�" *cascade08�"�"*cascade08�"�" *cascade08�"�"*cascade08�"�# *cascade08�#�#*cascade08�#�# *cascade08�#�#*cascade08�#�# *cascade08�#�#*cascade08�#�# *cascade08�#�# *cascade08
�#�# �#�#*cascade08
�#�# �#�#*cascade08
�#�$ �$�$*cascade08
�$�$ �$�$*cascade08�$�$ *cascade08�$�%*cascade08�%�% *cascade08�%�%*cascade08�%�% *cascade08�%�%*cascade08�%�% *cascade08�%�&*cascade08�&�& *cascade08�&�&*cascade08�&�& *cascade08�&�&*cascade08�&�& *cascade08�&�&*cascade08
�&�( 22file:///d:/workOnSsd/biteFX/documents/TargetGDD.md