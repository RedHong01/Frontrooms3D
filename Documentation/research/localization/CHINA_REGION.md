# 中国区上架与 LANGUAGE 行的旗子 · 交互设计

- 线：交互设计（FrontRooms，设置里 LANGUAGE 行的十个钥匙扣）
- 日期：2026-11-01
- Red 的原话（11-01，交互设计对话）：「繁中：香港、澳门、台湾三合一；」「为了避免中国争议，我需要你research相关的做法，来保证我可以在中国区域上架」
- 做了什么：只做网上调研，分三路并行（R1 规则、R2 业界做法、R3 上架条件），本文是三路的合成。合成时复查了 5 个关键出处〔1, 2, 17, 49, 50〕，也只读看了项目里的语言表和 `LANGUAGE_CHARMS.md`。网页都当文本读，没有主动下载文件，游戏和仓库一处没改。抓取工具自动把两个 PDF 响应缓存在本机 Claude 会话的 tool-results 文件夹里，从没打开过，删不删由 Red 决定。
- 这不是法律意见。真要提交大陆版本之前，请持证的中国出版商或中国律所确认。
- 底稿：`~/FrontRoomsInteractionWork/china/` 里的 `R1_rules.md`、`R2_practice.md`、`R3_listing.md`（仓库外），所有原话和出处都在里面。核查笔记在同处的 `R4_verify.md`。
- 标记：[V] 在一手或可靠来源里读到；[S] 二手、厂商或论坛；[U] 未核实或推断。〔数字〕是第 5 节的出处编号。
- 核查（2026-11-01，同一天做的对抗式复核）：本文最关键的 15 条结论，逐条重读了出处，也专门找了反例。7 条成立；5 条改了措辞或数字（规范条文的原话、广告罚款是谁罚的、2026 年进口版号更少、"官方不收费"改成"没列收费"、适龄提示是试行的团体标准）；3 条本来就是推断，措辞降了一级。另外补了两处：苹果的版号关卡对免费游戏是否真的执行没有文档，但 ICP 备案这一关结果一样；还发现一个中国以外的问题，西班牙语钥匙扣里的墨西哥半面旗。建议仍是 (a)，但多了一个前提。正文已经改好，逐条结果见第 6 节。

## 2. 给 Red 的结论

### 2.1 背景（先看这个）
1. Red 说的"中国区域"，在苹果后台是四个独立的地区：中国大陆、香港、澳门、台湾。港澳台三个没有版号要求，难的只有中国大陆。〔17, 22〕
2. 中国大陆 App Store 上的游戏都要有国家新闻出版署的批准号，也就是版号。苹果现在的说明只写"游戏"，没有"付费""内购"的限定，也没有"免费游戏除外"；2020 年的旧说明只管付费和内购游戏。没有版号，后台状态是"Missing Game registration number"，苹果的说明是这款 App 在中国大陆不提供。〔17, 18, 19〕核查补充：苹果对免费、没有内购的游戏是否真的执行这一关，没有文档，一家中介 2026 年还说免费游戏实际不查〔73〕〔S〕。但 2023 年 9 月起，中国区的新 App 还要过 ICP 备案这一关，而游戏没有版号就备不了案（第 3 节），所以结果一样：上不了。〔68, 71, 72〕而且法规本身没有给免费游戏开口子："未经国家新闻出版广电总局批准的移动游戏，不得上网出版运营。"〔1, 3〕
3. 版号只能由中国持证的出版单位申请。外国个人和外资公司都不能自己申请。〔1, 66〕
4. 所以，语言行改得再好，也不能单独让游戏上架大陆。语言行能做到的是"不挡路"：将来找到出版商时，这一行不用重画，也不会在审核或玩家那里出事。
5. 审核看的是提交的整个游戏，设置页面也在里面。游戏内容规范专门写了台湾、香港、澳门怎么表示，也写了国旗不能以不庄重的形式出现。〔2, 3〕
6. 这一行在全球版里也会被大陆、香港、澳门、台湾的玩家看到（港澳台 App Store，以后的 Steam 全球版）。所以要找的是四个地方都不出事的方案。
7. 现在的状态：其余八面旗 11-01 19:33 已落地；两个中文暂时还是 10-31 的天坛和珍珠奶茶；仓库里没有任何中国、香港、澳门、台湾的旗子图片，平面视觉画好的放在仓库外。〔95〕所以现在不管选哪个方案，都不会出现"撤掉某面旗"的新闻。

### 2.2 语言行要能上大陆：不能出现什么，必须做到什么
不能出现：
- 台湾的青天白日满地红旗。用它代表今天的台湾，任何形式都不行，包括拼在合体旗里的三分之一。规范第 7 条第（四）项禁止"出现中华人民共和国成立后的台湾地方当局官方机构的标识"。条文没有点名旗子，把这面旗算进去是推断〔U〕，但这是最直接的读法。〔2〕
- 把台湾、香港、澳门当成国家。规范第 7 条第（二）项禁止"将台湾、香港特别行政区、澳门特别行政区等中国任何地区作为主权国家表示"。把它们的旗和一排国旗并排放，很可能被读成这样。〔2〕〔U〕
- 和五星红旗一样大、平起平坐的香港区旗或澳门区旗。港澳都规定：两种旗同时出现时，区旗要比国旗小，国旗在显著位置。〔14, 16〕这是港澳的挂旗规定，能不能管到游戏里的图标，没有判例。〔U〕
- 做旧、磨损、褪色、切半、改比例或者倒挂的五星红旗。商店截图和预告片里也不要出现五星红旗。〔4, 7〕
- 标签里带地名：台湾、台灣、香港、澳门、港澳台、中国台湾。在一排语言里带上地名，就等于给这些地方定了身份，大陆或台湾总有一边不满（台湾 2019 年就因为"Taiwan, China"的写法叫停过华为手机的销售）。〔59〕
- "Taiwanese""台语""中华民国"这类写法。任天堂 2019 年把繁中写成"Taiwanese"，两天就改了。〔10, 32〕

必须做到：
- 中文按字体分，只写"简体中文""繁體中文"。游戏现在就是这么写的，旁边的短标是「简」「繁」。〔30, 31, 96〕
- 将来如果做大陆版：默认简体中文，用规范简体字，不要功能性的英文提示，游戏要有中文名。这些是大陆版的要求，见第 3 节。〔84, 85〕
- 别的地方如果出现地区列表（不是这一行），标题写"国家和地区"。〔10〕

### 2.3 五星红旗能不能做简体中文的图标？
直接回答：法律上可以用，但我们不建议用。
- 可以用的依据：国旗法第 9 条倡导"在适宜的场合"使用国旗图案。网上使用要用官方发布的标准图案，不得损害国旗尊严。〔4〕拿国旗做头像是合法的。〔6〕
- 不建议的四个原因：
  1. 国旗法第 19 条：不得使用破损、污损、褪色或者不合规格的国旗，不得倒挂。钥匙扣如果做旧、磨损、切半、拉伸比例，就碰线了。〔4〕
  2. 国旗法第 20 条和广告法第 9 条：国旗及其图案不得用于商业广告，广告法连"变相使用"也管。2024 年 9 月，淮安市洪泽区市场监管局罚了一家公司的理财广告 10 万元，认定了两件事：用了国家机关的名义，和"以近似五星红旗的图案为背景"变相使用国旗；市场监管总局把它列为典型案例。10 万元是整条广告的罚款，不是单罚这个背景；原文也没写是第几条。〔8〕App Store 截图和预告片里出现国旗钥匙扣，有被当成广告使用的风险。〔4, 7, 8〕
  3. 游戏内容规范第 8 条第（四）项：不得"以不庄重形式表现"国旗。恐怖游戏菜单里晃来晃去的小挂件，审核员有解释的空间。〔2〕
  4. 简中一旦用了国旗，繁中那一格就必须回答"用哪面旗"。这个问题没有安全答案（见方案 d）。
- 没找到任何游戏因为用国旗当语言图标被罚，所以 1 到 3 是风险判断，不是判例。〔U〕查过的大平台也没有一个这么做：Steam、苹果、任天堂都只写字。〔22, 30, 33〕可能的反例是 Duolingo，它的课程图标用旗，但它给中文用哪面旗、在中国区怎么处理，没查（见 4.9）。〔U〕
- 在香港商店卖时，香港规定国旗、区旗图案用于"贸易"相关用途要事先书面批准。对游戏界面是否适用，没有判例。〔13〕

### 2.4 四个方案（玩家看到什么，代价，风险）

**(a) 其他八种语言用国旗，两个中文不用旗**
- 玩家看到：八块国旗牌，加两块同样形状的字牌。字牌印什么由平面视觉定：可以两块都印「中文」，靠旁边的「简」「繁」区分；也可以印同一个字的简繁两种写法，比如「汉」「漢」。旁边照旧写简体中文、繁體中文。
- 代价：平面视觉在现有旗牌模具上画两块字牌。代码大概率不用改：语言条按贴图自动算轮廓，名字本来就在旁边。〔95〕〔U〕
- 风险：在大陆、香港、澳门、台湾都没找到禁止它的规则〔U〕，因为这一行里没有任何中国、台湾、香港、澳门的标志。对中文玩家来说，字比旗更直接。缺点是这一排不统一，两块中文牌会被注意到。大陆玩家可能问"为什么英美有旗、中国没有"；台湾玩家可能想起 2002 年的红帽：当时选国家和语言的列表里只有台湾没有旗，引发抵制，红帽随后把所有国家的旗都删了，但这也没让台湾用户消气。〔51〕这两种都是舆论风险，没找到违反哪条规定。〔U〕字牌不能用国旗配色：不要五星红旗的红黄，也不要青天白日旗的红蓝白。〔63〕
- 核查时发现的、中国以外的问题：西班牙语那块是西班牙在上、墨西哥在下的斜切旗，墨西哥国徽是简化重画、挪了位置的。〔95〕墨西哥的国旗法把"改动国旗的特征"和"未经内政部（SEGOB）授权使用国徽"列为违法行为，国徽的复制必须和官方样式一致；媒体说《使命召唤：现代战争》《战争机器 5》的国旗头像里都没有墨西哥。〔97, 98〕〔S〕游戏图标算不算这种复制，没有判例。〔U〕这和中国无关，但 (a) 要保留八面旗，就得先回答它；(b) 没有这个问题。
- 变体，就是现在的临时状态：八面旗加天坛和珍珠奶茶。规则上也干净：天坛不在规范第 8 条列出的国家象征里。长城、天安门在这份名单上，做成恐怖游戏里的小挂件，就有"不庄重"的风险。〔2〕但物件夹在旗中间更不统一；珍奶暗指"繁体等于台湾"，香港、澳门的玩家被漏掉。〔U〕

**(b) 全部不用旗：只写字，或者回到 10-31 的地标和物件**
- 玩家看到：十块统一的牌。要么十块字牌（EN、简、РУ、ES、PT、DE、FR、日、한、繁），要么 10-31 的十个物件：大本钟加自由女神像、天坛、套娃、西班牙吉他、救世基督像、勃兰登堡门、埃菲尔铁塔、富士山、河回面具、珍珠奶茶。〔95, 96〕
- 代价：10-31 的图还在仓库里，代码不用改。但它推翻了 Red 11-01 傍晚改用国旗钥匙扣、不用物品钥匙扣的决定，也要撤掉刚落地的八面旗。〔95〕远看时，物件和字一般不如国旗好认。〔U〕
- 风险：四个地方都最低，也不碰任何国家的国旗法（包括上面墨西哥的问题）。这是苹果、Steam、任天堂的做法，也是 W3C 的建议；红帽（2002）和 hermes-agent（2026）出事以后，最后都改成了这样。hermes-agent 删旗那次提交的原话是"不选边，把语言和旗的配对整个去掉"。〔22, 30, 33, 61, 50, 51, 101〕地标版里珍奶仍然暗指台湾（轻微）；字牌版没有这个问题。

**(c) 全球版到处用旗，只在大陆版里换掉两个中文**
- 玩家看到：美国、台湾、香港等地的玩家看到五星红旗和港澳台合体旗；大陆版玩家看到字牌，或者只有简体中文。
- 代价：两套图，一个按商店或地区切换的开关，两种状态都要测。大陆版本来就要出版商来做（第 3 节），所以找到出版商之前，这个开关没有用处。
- 风险：不推荐。全球版仍然把青天白日旗带进香港、澳门的商店（苹果在港澳 iPhone 的键盘上都藏起了台湾旗 emoji）〔27〕，也带到大陆玩家玩得到的地方。藏起来的图会被拆包、换区的人找出来，再小的图也一样：《还愿》墙上的一张符咒，发售两天就被玩家发现，游戏被下架，大陆发行商随后被吊销营业执照（通知没点名，媒体认为与此有关）。〔53, 54〕"只对大陆改图"在台湾也会上新闻：War Thunder 2019 年把台湾载具上的旗换成五星红旗，就上了台湾媒体。〔56〕苹果审核指南 4.3(a) 也不鼓励为不同地区做多个版本。〔28〕

**(d) Red 现在的选择：繁中是港澳台合体旗，简中是五星红旗**
- 玩家看到：一排国旗里，简体中文是五星红旗，繁體中文是一面香港、澳门、台湾拼起来的旗。
- 代价：图已经画好（放在仓库外），马上能用。〔95〕
- 准确的风险：
  - 大陆：几乎可以肯定过不了审〔U〕。合体旗里的青天白日旗，最直接地对上规范第 7 条第（四）项"出现中华人民共和国成立后的台湾地方当局官方机构的标识"（条文没有点名旗子）；把台港澳的旗和一排国旗并排，对上第（二）项"将台湾、香港特别行政区、澳门特别行政区等中国任何地区作为主权国家表示"。〔2〕这是对条文的推断，没有一份审核意见专门讲过语言图标。法律依据是《网络出版服务管理规定》第 24 条第（二）项"危害国家统一、主权和领土完整"，处罚写在第 52 条：删除、罚款、停业整顿，直到吊销许可证。〔1〕
  - 先例：2004 年文化部禁售了一款把台港澳列成国家的足球经理游戏（Soccer Manager 2005）〔44〕；2018 年万豪的问卷把港澳台列成国家，中文网站和 App 被关了一周〔45, 46〕；2023 年台湾机箱厂商联力在网站的繁体中文选项旁放了台湾旗，被大陆网民批评后道歉，说那是设置错误〔49〕；2026 年 5 月，开源项目 hermes-agent 的繁中选项显示台湾旗，被举报后，维护者当天删掉了语言选择器里的全部国旗〔50, 101〕。
  - 上线后才被发现：下架，并连带中国合作方（《还愿》）。〔53, 54〕
  - 香港、澳门：两地规定区旗与国旗同时出现时，区旗要更小，国旗在显著位置；不得使用不合规格的区旗；香港把旗帜图案用于"贸易"相关用途要事先书面批准。〔13, 14, 16〕合体旗让区旗和国旗一样大，还把区旗切成三分之一拼进另一面旗，和这些规定的字面对不上。这些规定管的是挂旗和实物，能不能管到外国游戏的图标、外国开发者是否真的违法，都没有判例〔U〕，但没有任何好处。苹果自己在港澳 iPhone 的键盘上也藏起了台湾旗 emoji。〔27〕
  - 台湾：把青天白日旗和两个特区的旗拼成一面，台湾玩家会读成"台湾等于特区"。〔U〕
  - 撤回也是新闻：2021 年 AIDA 撤下直播画面里的台湾旗，后来又向台湾道歉。〔57〕
  - 简中那面五星红旗，另外还有 2.3 里说的风险。
- 一句话：两边都不讨好；只要这面旗在，大陆上架基本没有可能。〔U〕

### 2.5 建议：选 (a)，但先处理墨西哥半面旗（核查后改）
- 做法：八面旗不动。两个中文换成同一模具的字牌，墨色印在纸色上，不用任何国旗配色。名字照旧写简体中文、繁體中文，标签里不出现任何地名。
- 为什么：在中国这个问题上，它只改真正有问题的两块。在大陆、香港、澳门、台湾都没找到禁止它的规则〔U〕。八种语言保留了 Red 想要的国旗辨识度，代价最小。仓库里从没出现过那几面旗，所以不会有"撤旗"的新闻。
- 核查后多的一个前提：西班牙语那块里的墨西哥半面旗（国徽简化重画、挪了位置），在墨西哥的国旗法下有疑问（见 (a) 的风险）。落地 (a) 之前，要么 Red 决定接受这个风险，要么平面视觉把西班牙语那块改成不带墨西哥国徽的样子，比如只用西班牙旗，或者这一块也用字牌。怎么改由 Red 和平面视觉定；英语那块的美英拼旗和其余六面旗，这次没有查各国的国旗法。〔95, 97, 98〕〔U〕
- 真实的缺点：这一排不统一，两块中文牌会被看见、被问到。如果以后出现"为什么中国没有旗""为什么台湾没有旗"这样的抱怨，或者出版商要求统一，就换成 (b) 的字牌版。模具相同，代价很小。红帽和 hermes-agent 最后都走到了"全部不用旗"这一步。
- 如果 Red 不想一个国家一个国家地回答国旗法的问题，(b) 的字牌版是唯一完全不碰国旗的方案。它推翻 11-01 改用国旗的决定，所以要 Red 自己选。
- 再说一遍：(a) 只是让语言行不挡路，它本身不能让游戏上架大陆。真正的门槛在下一节。
- 如果 Red 同意 (a)，下一步：平面视觉画两块字牌；交互设计按计划把本文放进 `research/localization/`，并通知平面视觉。〔95〕

## 3. 上架中国区还需要什么（和旗子无关）
一句话：只改旗子，FrontRooms 也上不了中国大陆。现在的 FrontRooms 免费、离线、没有内购、用个人账号、只在美国上架。〔94〕在大陆，它是一款没有版号的游戏，法律上不能上线。〔1〕

**版号（网络游戏出版物号）**
- 谁能申请：只有中国持证、有网络游戏出版范围的出版单位。外资、合资单位不能从事网络出版。〔1, 66〕
- 进口游戏（境外作者授权）还要两样：版权合同在国家版权局登记；运营方有 ICP 经营许可证。〔66〕
- 要交的材料：中文脚本全文、屏蔽词库、防沉迷说明（实名注册、时段时长、付费、游客模式）、审核用的账号。〔66〕
- 多久：官方是受理后 80 个工作日。实际各家说法是 6 到 12 个月、12 到 18 个月，甚至 12 到 24 个月。〔66, 77, 78, 79, 80〕
- 多难：进口名额很少。2025 年一共批了 1,771 款，其中进口只有 95 款；2024 年 10 月到 2025 年底，进口大约每月 9 到 10 款。2026 年更少：1 到 8 月进口一共 43 款，平均每月约 5 款；5 月 4 款，8 月 6 款，10 月 12 款。〔74, 75, 76, 99, 100〕
- 多少钱：国家新闻出版署的办事页没有列任何收费项，各家资料也没提官方收费；这是"没写"，不是"写明不收"。〔66〕出版社的服务费，2020 年的说法是每款 7,000 到 10,000 美元一次性，中介另外收；2024 年的说法是出版合作方可能拿走一大块利润。〔78, 79〕FrontRooms 免费、没有收入，出版商分不到钱，所以要么付服务费，要么找不到人。〔U〕"1 到 5 万美元""30% 到 50% 分成"这些数字只在搜索摘要里见过，没有核实。
- 如果 Red 是中国公民，可以走"国产"审批，但同样需要出版商和有 ICP 证的运营方。〔67〕上海计划中的外资游戏试点，管不到海外个人。〔93〕

**ICP 备案（App 备案）**
- 工信部 2023 年起要求 App 先备案再运营，平台不得分发未备案的 App。苹果 2023 年起对中国区新 App 查备案号。〔68, 71〕
- 备案主体必须是中国大陆注册的企业或中国大陆居民（阿里云的说法）。境外公司可以用在大陆注册的分公司，或者《外国（地区）企业常驻代表机构登记证》来备案；境外个人没有路。〔69〕〔S〕
- 游戏要先有前置审批才能备案：工信部的通知要求出版等行业交主管部门的批准文件；华为云对游戏列的是有效期内的《游戏版号》，或涉及游戏的《网络文化经营许可证》。〔68, 70〕所以顺序是：出版商，版号，备案，最后填进苹果的两个中国字段。
- 材料齐全后，省通信管理局在 20 个工作日内备案。备案号要放在 App 里的显著位置，一般放在"关于"或设置页。〔68〕
- 中国独立开发者 2024 年的经验：免费单机没有版号，一律不给备案。〔72〕〔S〕同一个帖子里也有人说，当时仍有没有版号的游戏在新上架，所以执行并非滴水不漏。

**苹果这一侧**
- 2020 年苹果清理的是没有版号的付费和内购游戏，免费游戏当时没动；现在的说明已经不分付费和免费。〔17, 18, 20, 21〕2023 年 9 月底起，新 App 上中国区还要 ICP 备案；路透说开发者要在中国有公司，或者找本地出版商。〔71〕
- 大陆版一般挂在出版商自己的开发者账号下，用它的营业执照、版号和备案号。〔17, 28〕〔U〕中国区因法规被拒时，常见的两条路是：大陆版删掉那个功能，或者不选中国大陆。〔29〕
- 不要在已上架的 App 上"先勾选中国大陆试试看"。如果苹果没拦住，游戏就以没有版号的状态在大陆上线了，这本身就违法。想确认，就问 Apple Developer Support。〔U〕

**恐怖游戏的内容审核（大陆版要改的地方）**
- 恐怖本身可以上：中式恐怖（比如《纸嫁衣》）是主流品类。〔87〕但规定禁止"迷信""暴力"，也禁止对未成年人有害的"恐怖、残酷"内容。〔1〕
- 审核实际会挑：血（不能有血滴、血腥的图片或文字）、尸体（要很快消失）、"亡灵""尸体"这类词、迷信和算命。〔84, 85, 86〕
- 文字：简体界面全用规范简体字，里面不能夹繁体字（2017 年的审核意见：一个"競"字要全面排查、改成简体），不能有功能性的英文提示，游戏名里不能有英文。〔84, 85〕这管的是简体文字本身，不等于不能保留繁体中文这个语言选项（见 4.8）。FrontRooms 是英文为主的 1990 年美国办公楼，墙上的标牌、海报和屏幕上的字都要汉化。〔79〕
- 实名和防沉迷：未成年人只能在周五、周六、周日和法定节假日的晚上 8 点到 9 点玩，没有实名登录就不能提供服务；单机游戏下载或购买时也要实名。〔81, 82〕这意味着要联网、要收身份证信息，隐私标签不再是"不收集数据"。
- 适龄提示：音数协游戏工委牵头的团体标准，2020 年 12 月起试行，不是法律，但行业普遍照做。分 8+、12+、16+ 三档，没有 18+。FrontRooms 大概率是 16+；标识要放在显著位置，包括官网、注册登录、付费界面、宣传视频和广告。〔83〕
- 地图：任何真实世界的地图或地球仪，都要完整表示中国疆域，否则干脆别出现。FrontRooms 的楼层平面图不算地图。〔12〕
- 商店素材：广告法也禁止广告里有"恐怖、暴力"内容，大陆商店的截图和预告片要另外挑。〔7〕〔U〕

**独立开发者实际怎么做**
- App Store 不勾选中国大陆。FrontRooms 现在就是这样。〔94〕
- 大陆玩家主要在 Steam 全球版上玩，这是法律上的灰色地带；2021 年底还有报道说 Steam 的国际域名在工信部的查询里被标成"非法"，商店后来照常能用。〔91, 102〕〔S〕2026 年 10 月，Steam 用户里简体中文占 21.98%；这个比例每月波动很大（比 9 月少了 9.41 个百分点），只能看量级。Steam 中国（蒸汽平台）同样要版号。〔90, 92〕
- 安卓和 PC 可以走 TapTap「开放试玩」：没有版号的游戏只能这样上，而且不能收费、不能内购；离线的可以直接上，联网的还要 ICP 备案；都要提供防沉迷系统的证明，并在显著位置放隐私声明。TapTap 的审核规则不收"色情类、血腥暴力类的游戏"。海外开发者能不能用，没查到。这是平台的做法，法律上网络游戏上线前要审批的条文并没有变。〔1, 88, 89〕
- 香港、澳门、台湾：没有版号要求，在 App Store Connect 里勾选就行，最好配繁中的商店文字。台湾自己的游戏分级规定，这次没有查。〔17, 22〕
- 只有当大陆市场值得一年以上的投入时，才去找出版商做大陆版。

## 4. Evidence (English)
Each line is tagged and points to section 5.

### 4.1 PRC rules a mainland build is reviewed against
- [V 1] Online Publishing Provisions (NPPA and MIIT Order No. 5, in force 2016-03-10): foreign-invested and joint-venture entities may not do online publishing (Art. 10); banned content includes "危害国家统一、主权和领土完整" (Art. 24(2)), cults and superstition (24(5)), violence (24(7)); content harmful to minors, incl. "恐怖、残酷" in R3's reading (Art. 25); online games need approval before going online (Art. 27), overseas-licensed ones too (Art. 32). Art. 51 punishes unapproved games (removal, confiscation, 5-10x revenue, or up to RMB 50,000 below RMB 10,000); Art. 52 punishes banned content (deletion, fines, suspension, licence revocation). Re-read 2026-11-01.
- [V 3] NPPA notice 新广出办发〔2016〕44号: unapproved mobile games may not go online; publishers self-review against CADPA's content standard.
- [V 2] CADPA Mobile Game Content Standard 2016 (音数协字【2016】第005号, 2016-05-24; an industry-association standard that NPPA notice 44 tells publishers to review against, not a law). Art. 7 (national unity): (1) splitting any region from China; (2) exact text "将台湾、香港特别行政区、澳门特别行政区等中国任何地区作为主权国家表示"; (3) dropping quotation marks on Taiwan authorities' titles; (4) exact text "出现中华人民共和国成立后的台湾地方当局官方机构的标识". Art. 8(4): no national flag, anthem, emblem, Tiananmen, Great Wall or similar state symbol "以不庄重形式表现". Art. 4: standard use of characters. Re-read 2026-11-01 (R4 corrected the two quotations, which earlier versions of this file had compressed).
- [U] The ROC flag, used for present-day Taiwan, is the plainest case of Art. 7(4); the text does not name it, and its scope is symbols of the post-1949 Taiwan authorities.
- [V 4, 5] National Flag Law (amended 2020-10-17, in force 2021-01-01): Art. 9 encourages use "在适宜的场合"; online use must follow network rules, keep the flag's dignity and use the published standard pattern; Art. 19 bans "破损、污损、褪色或者不合规格" flags and upside-down display; Art. 20 bans the flag and its pattern in trademarks, design patents, commercial ads and other inappropriate settings; Art. 23 makes public insult a crime.
- [V 6] A flag avatar is lawful if the flag is kept intact (2021 fact-check citing police and a justice bureau).
- [V 7, 8, 9] Advertising Law Art. 9(1) bans ads that "使用或者变相使用" the flag; 9(8) bans ads with terror or violence. 2024 typical case (published by SAMR, republished by AllBright): the Hongze district regulator in Huai'an fined an investment-product ad RMB 100,000 (2024-09) for two findings, using state organs' name or image, and "变相使用中华人民共和国国旗" through a background "近似五星红旗"; the fine covers the whole ad and no article number is given (R4 corrected the earlier "SAMR fined a flag-like background"). Cases published 2026-10-13 include RMB 280,000 for an ad that used the national emblem and implied state backing (it also promised no risk).
- [U] No case found of an app or game punished for a PRC-flag language or region icon (searches 2026-11-01).
- [V 10] Xinhua banned-terms list (2016 revision, CDT copy; state-media style rules, not law): HK and Macau never appear as countries in text, maps or charts; a mixed list is titled 国家和地区; Taiwan is not set beside other countries; joint events must not show "中、港、台"; avoid names with 中华民国; Hokkien is not 台语.
- [V 11] The 2022 Taiwan white paper rejects "两个中国" and "一中一台".
- [V 12] Map regulation: no maps harming territorial integrity; world and national maps show PRC territory completely; public maps need review, simple local maps exempt.

### 4.2 Hong Kong and Macau flag rules
- [V 13] HK: national and regional flags and emblems, or their designs, may not be used in trademarks, registered designs or commercial ads; use connected with a trade or profession needs prior written approval; damaged, defiled, faded or substandard flags may not be used; desecrating the flags or their image is an offence. [S 15] The 2021 and 2023 amendments reach the virtual world.
- [V 14] HK display: the national flag takes the centre, the top or the prominent place, and "the regional flag shall be smaller than the national flag" (re-read R4). These are rules for displaying flags; nothing on the page addresses icons in software.
- [V 16] Macau Law 6/1999 (on the regional flag and emblem): no trademarks or ads (Art. 4); no damaged or non-conforming flags (Art. 5); insult punishable by up to two years or 240 day-fines, incl. copies close enough to be taken for the flag (Art. 7); with the national flag, "國旗或國徽應當大於區旗或區徽" and takes the central, higher or prominent place (Art. 12(1)). Re-read R4.
- [U] Whether HK's trade-use approval reaches a foreign game's UI on the HK storefront is untested.

### 4.3 Apple
- [V 17] App Store Connect, China mainland: games "must secure an approval number" from the NPPA, shown by the ISBN Issuing Letter or Approval Reply plus a business licence; MIIT requires an ICP Filing Number for "some apps", displayed on the product page. No paid or IAP qualifier (re-read 2026-11-01, again in R4). [S 18] The 2020 text covered only paid games and games with in-app purchases (a forum post quoting Apple).
- [V 19] Without the number the status is "Missing Game registration number": "Your app isn't available in China mainland"; "ICP Filing Number Missing" and "ICP Filing Number Invalid" also exist (re-read R4).
- [V 20, 21] 2020: Apple removed unlicensed paid and IAP games (final deadline 2020-12-31); about 160,000 free games stayed. [U] Whether the 版号 gate fires today for a free, no-IAP game from an overseas account is undocumented; [S 73] a vendor page updated 2026-06-03 says free, ad-supported games are "currently exempt" in practice. [V 71] But since late September 2023 new China apps also need an ICP filing, which a game cannot get without a 版号 (69, 70, 72), so the practical outcome is the same.
- [V 22] Storefronts China mainland, Hong Kong, Macau, Taiwan; Chinese (Simplified) is the default for China mainland, Chinese (Traditional) for the other three; no flags. [V 23] zh-Hant is defined as the traditional script.
- [V 24, 25] apple.com's country selector is text only (中国大陆, 香港, 澳門, 台灣); in 2013 it used flags.
- [V 26, 27] China-region Apple devices hide the Taiwan flag emoji and show a missing glyph (since at least 2017); iOS 13.1.1 also removed it from the keyboard in HK and Macau.
- [V 28] Review Guidelines (2026-10-16): no China clause, nothing on flags; 5.0 requires legal compliance wherever offered; 4.3(a) discourages extra bundle IDs for location variants. [S 29] A China Guideline 5 rejection offered two paths: remove the feature for China, or deselect China mainland.

### 4.4 Practice: how products label Chinese
- [V 30] Steam: Chinese (Simplified) 简体中文 and Chinese (Traditional) 繁體中文; one Traditional entry; no flags.
- [V 31] CLDR: 简体中文 / 繁体中文 (繁體中文 in Traditional); regions 中国香港特别行政区, 中国澳门特别行政区, 台湾.
- [V 32, 33] Switch 7.0.0 (2019-01-28) labelled Traditional "Taiwanese"; after boycott calls it read Chinese (Traditional) by 2019-01-30; Nintendo's support pages use text names.
- [V 34] Windows: Chinese (Simplified, China), (Traditional, Taiwan), (Traditional, Hong Kong SAR); no flags; Home Country Specific editions cannot add languages ([S] users identify that edition as the China one).
- [V 35] Huawei China: regions 中国香港, 中国澳门, 中国台湾; languages 简体中文, 繁体中文.
- [V 36, 37] China App Store: 原神 lists 15 languages incl. 繁体中文; bilibili's 重生细胞 lists 13 incl. 繁体中文. [V 38, 39, 40] 王者荣耀 and 第五人格 list 简体中文 only; Identity V in Taiwan is a separate LongE app, 繁體中文 only; 喵斯快跑 lists 简体中文 and English.
- [S 41] Genshin's global server "TW、HK、MO" is a text bucket. [V 42] China-model PS5 accounts allow only 中国大陆 and 简体中文 (2021). [S 43] Minecraft labels 繁體中文 (台灣) and (香港特別行政區); a forum post asks for 中國台灣.
- [U] No large platform checked shows a flag beside a Chinese language name; in-game pickers of Tencent, NetEase and HoYoverse games could not be checked from text sources.
- [V 96] FrontRooms already names the languages 简体中文 and 繁體中文 (tokens 简, 繁); its text tables name no Taiwan, HK or Macau place.

### 4.5 Controversies
- [V 44] 2004: China's Ministry of Culture banned Soccer Manager 2005 for listing Taiwan, HK and Macau as countries (harm to sovereignty and territorial integrity).
- [V 45, 46] 2018: a Marriott survey listed HK, Macau, Tibet and Taiwan as countries; its Chinese website and app were shut for a week; Delta, Zara, Qantas and Medtronic also corrected or apologised.
- [V 47, 48] 2018: the CAAC told 44 foreign airlines to stop presenting Taiwan, HK and Macau as separate from China; US carriers dropped country labels and showed cities only.
- [V 49] 2023: Lian Li (Taiwan-based) showed a Taiwan flag beside its traditional-Chinese language option; after mainland criticism it apologised, called it a setting error and said the option serves users in HK, Macao and Taiwan. Re-read 2026-11-01.
- [V 50, 101] 2026-05-21, hermes-agent #29750: 繁體中文 showed the Taiwan flag and 简体中文 the PRC flag; the reporter wanted the PRC flag on both; the maintainer closed it that day with PR #29997, "remove country flags from language picker", which dropped the flag for every locale and uninstalled the flag-icons package: "Rather than picking a side, drop the language-flag pairings entirely." Re-read 2026-11-01 (PR read in R4).
- [V 51] 2002: Red Hat 8.0's KDE setup list of countries and languages showed a flag per country but left out only Taiwan's, citing China's objections; Taiwanese users boycotted and switched distributions; Red Hat then removed every country's flag, which, per Salon, did little to calm Taiwanese users (R4: the boycott did not end with the removal).
- [V 52] 2019: Warframe was review-bombed, mostly by Chinese players, partly for listing China, HK and Taiwan separately.
- [V 53] 2019: Devotion (Taiwanese horror): one hidden talisman prop led to a review bomb and removal from Steam (China, then worldwide); mainland publishers cut ties, one later lost its business licence. [S 54] The notice did not name the game; a journalist tied it to Devotion.
- [V 55] 2020: Cytus II was pulled from the China App Store over a coded HK protest slogan in a staff musician's track.
- [V 56] 2019: War Thunder (Tencent in China) swapped ROC flags on Taiwanese vehicles for PRC flags; Taiwan press reported it.
- [V 57] 2021: AIDA removed Taiwan's flag from a live feed after a CCTV complaint, then apologised to Taiwan. [V 58] 2024: Huya halted Chinese LCK streams after Gen.G called Taiwan a country.
- [V 59] 2019: Taiwan's NCC halted sales of three Huawei phones that labelled Taiwan "Taiwan, China".
- [V 60] 2018: PUBG Mobile removed a Rising Sun flag item and refunded buyers after Chinese players objected.
- [U] Pressure comes from both sides, and taking a flag down later is itself news.

### 4.6 Standards
- [V 61, 62] W3C: do not use flag icons for languages; flags stand for countries and carry national connotations.
- [V 63, 64] Microsoft: avoid political images; colour combinations tied to national flags can be problematic; say country/region; language and region are separate concepts.
- [V 65] Unicode UTS #51: a flag emoji pair stands for a region, its images may be constrained by that region's administration, and each platform picks which flags it supports.
- [U] No Apple HIG text on flags for languages was found; Apple's practice (4.3) is the evidence.

### 4.7 Listing requirements, timelines, costs
- [V 66] Imported online game: applicant is a licensed online publisher with game scope ("出版单位须为具有网络游戏出版范围的网络出版服务单位"); licence contract registered with the National Copyright Administration; operator holds an ICP licence; file includes the full Chinese script, a blocked-word list and the anti-addiction design (real-name, time limits, payment, guest mode); province, then NPPA; 许可期限 "自受理申请之日起80个工作日"; quota clause "符合有关总量、结构、布局规划要求"; the page has no fee field at all (re-read R4).
- [V 67] Domestic route: copyright holder is a Chinese citizen or domestic-capital firm; same publisher and operator conditions.
- [V 68] MIIT 工信部信管〔2023〕105号 (dated 2023-07-21): apps file before operating; platforms may not distribute unfiled apps; news, publishing, education, film and TV and religion apps add "相关主管部门审核同意的文件"; the provincial administration files within 20 working days of complete materials; "APP主办者应当在APP显著位置标明其备案编号". [S 69] Filers are mainland-registered entities or mainland residents; an overseas firm can file through a mainland branch or a registered representative office (some provinces, non-commercial filings only); an overseas individual has no route. [S 70] For games the listed pre-approval is a valid 《游戏版号》 or a game-related 《网络文化经营许可证》. [V 71] Apple began checking filings for new China apps in late September 2023; developers "need to have a company in China or work with a local publisher". [S 72] Chinese indies (2024): no 版号, no filing, even for free single-player games. [S 73] A vendor says free ad-supported games are still not enforced in practice.
- [V 74, 75, 76, 99, 100] 2025: 1,771 approvals, 1,676 domestic, 95 imported; about 9-10 imports a month from Oct 2024 through 2025. 2026 is lower: 4 imports in May, 6 in August, 43 in January-August (about 5 a month), 12 in October (R4 corrected the earlier "about 9-10 a month since Oct 2024").
- [S 77, 78, 79, 80] Real timelines: 12-18 months (sponsored, 2020); imported 6-12 months or more (2020); ISBN up to 12 months, sometimes years, NPPA review 6-8 months or longer (2024); 12-24 months (vendor, c. 2023).
- [S 78, 79] Cost: no fee is listed on the NPPA page and no source names an official fee (absence, not a statement); about USD 7-10K per title as a one-time press fee (2020); a partner may take a large profit share (2024); Chinese stores take about 50 %. [U] USD 10,000-50,000 and 30-50 % shares were seen only in search summaries.
- [V 81] Minors: 20:00-21:00 on Fridays, weekends and holidays; no service without real-name login. [S 82] Single-player games also check real names at download or purchase.
- [V 83] 适龄提示: a group standard (团体标准) led by CADPA's game committee, on trial since 2020-12, not a law: 8+, 12+, 16+ (no 18+); the label "必须安放在游戏产品界面的显著位置", including the site, sign-up and login, payment screens, promo videos and ads (R4 added the standard's status).
- [V 84] CADPA (2017) relayed review findings: a traditional character had to become simplified, English prompts are prohibited, no blood images or text. [S 85] Reported review notes (2019): no English in game names; standard simplified characters; no superstition, fortune-telling, green blood or fight liquids; corpses vanish fast. [S 86] Forum: avoid words like 亡灵 and 尸体.
- [S 79] All visible text must be translated, including text inside visual elements.
- [V 87] Chinese folk horror (纸嫁衣) is mainstream; horror is weighed, not banned.
- [V 88, 89] TapTap: without a 版号 a game can list only as 开放试玩 ("若厂商没有提供游戏版号，只能以「开放试玩」形式上架"), which covers offline no-IAP games and online no-IAP games with an ICP filing; anti-addiction proof and a prominent privacy statement are required; no charging or IAP without a 版号 (88). Review rules 1.6.6 refuse "色情类、血腥暴力类的游戏"; a China listing needs Simplified Chinese materials (89). [U] This is a platform practice; it does not change Provisions Art. 27.
- [V 90, 91, 92] Steam China needs government approval; global Steam stays reachable in a grey area; Simplified Chinese is 21.98 % of Steam users, Traditional 1.30 % (Oct 2026; Simplified fell 9.41 points from September, so the share swings). [S 102] Dec 2021: reports that Steam's international domain showed as "illegal" in an MIIT lookup, during an outage that other reports (search summaries only) put down to a DNS attack; no 2026 block was found.
- [V 93] Shanghai's planned pilot (foreign-invested studios in Shanghai treated as domestic) has no start date and does not cover an overseas individual.

### 4.8 FrontRooms facts and inferences
- [V 94] FrontRooms: free, no IAP, US only, 13+ with Horror/Fear Themes = Frequent, offline, collects no data.
- [V 95] LANGUAGE row today: eight flags landed; zh-Hans and zh-Hant keep the 10-31 Temple of Heaven and bubble tea; no China, HK, Macau or Taiwan flag image in the repo.
- [U] The combined charm almost certainly fails mainland review on CADPA 7(2) and 7(4) and Art. 24(2) (an inference from the text; no review finding on a language icon was found); proportions cannot fix it. It also conflicts with the wording of the SAR display rules (regional flag smaller and subordinate, no non-conforming flags), whose reach to a game icon is untested, and in Taiwan it may read as equating Taiwan with an SAR (R4 softened "fails" and "breaks").
- [S 97, 98] Outside China: the Español charm's Mexico half (arms redrawn plain and moved, per 95) meets Mexico's flag law: LEBHN Art. 56 lists "Alterar o modificar las características de la Bandera Nacional" and using the arms without the Art. 6 authorisation as infractions, and Art. 5 requires arms reproductions to follow the official model; large games left Mexico out of their flag pickers. [U] Whether a game icon counts is untested. (b) avoids it.
- [U] A PRC-flag charm is lawful but sits near Flag Law Arts. 19-20 and CADPA 8(4) if worn, cut, re-proportioned or shown in store art.
- [U] A licensed mainland build may keep 繁体中文 as a text option (36, 37) or drop to Simplified only (38, 39). The 2017 finding (84) concerned a traditional character inside Simplified text, not a language option, so this is the publisher's call. R3 read it as Simplified-only; the listings show both patterns.
- [U] Region-gated art (option c) is found by data-miners and region switches, and the NPPA reviews the submitted build.
- [U] No China-related flag was ever in the repo or a release, so (a) or (b) creates no "removed flag" story now.

### 4.9 Open questions
- Does Apple's 版号 gate fire today for a free, no-IAP game from an overseas account? Undocumented, and one vendor says it does not; the ICP gate for new apps (since 2023-09) blocks it anyway. Ask Apple Developer Support; never test it on the live app.
- Mexico (outside China, found in R4): does LEBHN reach a split, simplified Mexican flag on a game charm? Decide before (a) lands, or change the Español charm. The other foreign flags' own laws were not checked.
- In a licensed mainland build, does the picker show the endonym 繁體中文 (traditional characters) or 繁体中文? The China store lists show 繁体中文 because the store renders names in Simplified; the in-game labels of 原神 and 重生细胞 could not be checked from text. The publisher decides; the same applies to a 漢 charm in option (a).
- Duolingo-style course flags: a possible counterexample to "no large product puts a flag on Chinese"; which flag it shows for Chinese, and how its China app handles it, was not checked.
- Does the Mac App Store China storefront use the same two fields? Likely (shared App Information), unverified.
- May a licensed China build keep 繁體中文 and English? No rule text found; the publisher decides.
- Does HK's trade-use approval reach a foreign game's UI on the HK storefront? Untested.
- Taiwan's game-rating rules for the TW storefront and TapTap's terms for overseas developers were not checked.
- Red's citizenship and residence decide the domestic or import route; this matters only once there is a publisher.

## 5. Sources
"Read" means the page was fetched and read as text in that research pass; "re-read" means checked again for this synthesis on 2026-11-01. R4 (the verification pass, same day, notes in `R4_verify.md`) re-read 1-9, 12, 14, 16-19, 22, 24, 27 (The Register), 30-32, 35-39 (39: China store only), 44, 49-51, 56, 59, 61, 63, 66-84, 88, 89, 91, 92 and part of 95, and added 97-102.
1. 网络出版服务管理规定 (NPPA): https://www.nppa.gov.cn/xxgk/fdzdgknr/zcfg_210/bmgz_213/202112/t20211209_442163.html (read R3; re-read; gov.cn copy read R1, R2: https://www.gov.cn/zhengce/2022-11/09/content_5724634.htm)
2. CADPA 移动游戏内容规范（2016年版）: https://www.cadpa.org.cn/3269/201705/41234.html (read R1; re-read)
3. NPPA 新广出办发〔2016〕44号: https://www.nppa.gov.cn/xxgk/fdzdgknr/zcfg_210/gfxwj_215/201606/t20160602_4684.html (read R1)
4. 国旗法 full text (12371.cn): https://www.12371.cn/2021/05/23/ARTI1621736435095903.shtml (read R1, R2)
5. 国旗法 2020 amendment (gov.cn): https://www.gov.cn/xinwen/2020-10/18/content_5552081.htm (read R3)
6. The Paper, flag-avatar fact-check: https://m.thepaper.cn/baijiahao_14738769 (read R1)
7. 广告法 (Wikisource mirror): https://zh.wikisource.org/wiki/中华人民共和国广告法 (read R1)
8. AllBright, republished SAMR typical cases 2024: https://www.allbrightlaw.com/CN/10531/c0bc0a7d63336d0.aspx (read R1)
9. Xinhua, SAMR typical cases 2026-10-13: https://www.news.cn/fortune/20261013/e823dc3ba0cd43059fea222537cf8b11/c.html (read R1)
10. Xinhua banned-terms list, 2016 revision (China Digital Times copy): https://chinadigitaltimes.net/chinese/2020/07/%e3%80%90%e7%ab%8b%e6%ad%a4%e5%ad%98%e7%85%a7%e3%80%91%e6%96%b0%e5%8d%8e%e7%a4%be%e5%85%ac%e5%b8%83%e6%96%b0%e5%a2%9e57%e4%b8%aa%e7%a6%81%e7%94%a8%e8%af%8d%ef%bc%9a%e6%85%8e%e7%94%a8%e4%ba%b2/ (read R2; the original was deleted)
11. Taiwan white paper 2022 (MFA): https://www.fmprc.gov.cn/ziliao_674904/zt_674979/dnzt_674981/qtzt/twwt/twwtbps/202208/t20220810_10738763.html (read R1)
12. 地图管理条例 (Wikisource mirror): https://zh.wikisource.org/wiki/%E5%9C%B0%E5%9B%BE%E7%AE%A1%E7%90%86%E6%9D%A1%E4%BE%8B (read R3)
13. HK Protocol Division, prohibitions: https://www.protocol.gov.hk/en/proh/proh.html (read R1, R2)
14. HK Protocol Division, display: https://www.protocol.gov.hk/en/show/show.html (read R1, R2)
15. HKFP 2021, online desecration: https://hongkongfp.com/2021/08/12/hong-kong-to-outlaw-acts-desecrating-chinese-flag-on-the-internet/ (search snippet only, R1)
16. Macau Law 6/1999 (Wikisource mirror; gazette returned 403): https://zh.wikisource.org/wiki/第6/1999號法律 (read R1)
17. App Store Connect, App information (China mainland): https://developer.apple.com/help/app-store-connect/reference/app-information/app-information (read R1-R3; re-read)
18. Apple Developer Forums 657959, 2020 wording: https://developer.apple.com/forums/thread/657959 (read R3; forum)
19. App Store Connect, app statuses: https://developer.apple.com/help/app-store-connect/reference/app-information/app-and-submission-statuses (read R1, R3)
20. 9to5Mac 2020-08-01: https://9to5mac.com/2020/08/01/app-store-china-games-removed/ (read R3)
21. TechNode 2020-12-09: https://technode.com/2020/12/09/apple-to-remove-all-unlicensed-games-from-china-app-store/ (read R3)
22. App Store Connect, localizations and territories: https://developer.apple.com/help/app-store-connect/reference/app-information/app-store-localizations (read R1, R2)
23. Apple Xcode doc, regions and scripts: https://developer.apple.com/tutorials/data/documentation/xcode/choosing-localization-regions-and-scripts.json (read R2)
24. apple.com country selector: https://www.apple.com/choose-country-region/ (read R2, R3)
25. Global by Design 2013: https://globalbydesign.com/2013/08/02/apple-opens-online-store-for-russia-and-a-call-for-improving-the-global-gateway/ (read R2)
26. Emojipedia 2018, China: https://blog.emojipedia.org/one-emoji-doesnt-show-on-ios-in-china/ (read R1, R2)
27. Emojipedia 2019, Hong Kong: https://blog.emojipedia.org/apple-hides-taiwan-flag-in-hong-kong/ ; The Register 2019-10-07, Hong Kong and Macau: https://www.theregister.com/2019/10/07/apple_taiwan_flag/ (read R1, R2)
28. App Review Guidelines (2026-10-16): https://developer.apple.com/app-store/review/guidelines/ (read R1, R3)
29. AngularCorp 2024, Guideline 5 rejection: https://www.angularcorp.com/post/apple-review-reject-guideline5legal/ (read R3; developer blog)
30. Steamworks language table: https://partner.steamgames.com/doc/store/localization/languages (read R2)
31. Unicode CLDR: https://www.unicode.org/cldr/charts/latest/summary/zh.html and https://raw.githubusercontent.com/unicode-org/cldr/main/common/main/zh.xml (read R1, R2)
32. Taiwan News 2019, Nintendo "Taiwanese": https://www.taiwannews.com.tw/en/news/3628605 (read R2)
33. Nintendo support (AU), system languages: https://www.nintendo.com/au/support/articles/how-to-change-the-system-language/ (read R2)
34. Microsoft, Windows language packs: https://learn.microsoft.com/en-us/windows-hardware/manufacture/desktop/available-language-packs-for-windows (read R2)
35. Huawei China support: https://consumer.huawei.com/cn/support/content/zh-cn15869641/ (read R2)
36. China App Store, 原神: https://apps.apple.com/cn/app/id1467190251 (read R2)
37. China App Store, 重生细胞: https://apps.apple.com/cn/app/%E9%87%8D%E7%94%9F%E7%BB%86%E8%83%9E/id1523446532 (read R2)
38. China App Store, 王者荣耀: https://apps.apple.com/cn/app/id989673964 (read R2)
39. 第五人格, China and Taiwan stores: https://apps.apple.com/cn/app/%E7%AC%AC%E4%BA%94%E4%BA%BA%E6%A0%BC/id1330863325 and https://apps.apple.com/tw/app/id1370284166 (read R2)
40. China App Store, 喵斯快跑: https://apps.apple.com/cn/app/1361473095 (read R2)
41. Biligame wiki, Genshin servers: https://wiki.biligame.com/ys/%E6%9C%8D%E5%8A%A1%E5%99%A8%E5%88%86%E5%8C%BA (read R2; community wiki)
42. GCORES 2021, China PS5: https://www.gcores.com/articles/137059 (read R2)
43. Minecraft Wiki, languages: https://minecraft.wiki/w/Language (read R2; community wiki)
44. NBC News / Reuters 2004-12-07: https://www.nbcnews.com/id/wbna6667257 (read R1-R3)
45. HKFP 2018-01-19, Marriott: https://hongkongfp.com/2018/01/19/hotelier-marriott-unveils-eight-point-rectification-plan-tibet-hong-kong-geography-gaffe/ (read R2)
46. China Digital Times / Reuters 2018-01-12: https://chinadigitaltimes.net/2018/01/foreign-firms-fire-taiwan-tibet-references/ (read R3)
47. Foreign Policy 2018-04-27, CAAC: https://foreignpolicy.com/2018/04/27/china-threatens-u-s-airlines-over-taiwan-references-united-american-flight-beijing/ (read R1)
48. Fortune 2018-07-25, airlines: https://fortune.com/2018/07/25/us-airlines-taiwan-china-taipei (read R2)
49. Global Times 2023-06-13, Lian Li: https://www.globaltimes.cn/page/202306/1292487.shtml (read R3; re-read)
50. GitHub, hermes-agent #29750: https://github.com/NousResearch/hermes-agent/issues/29750 (read R1; re-read)
51. Salon 2002-10-31, Red Hat: https://www.salon.com/2002/10/31/taiwan_flag (read R2)
52. PCGamesN 2019, Warframe: https://www.pcgamesn.com/warframe/review-bomb (read R2)
53. Kotaku 2019-07-15, Devotion: https://kotaku.com/makers-of-popular-horror-game-pulled-from-steam-say-its-1836375382 (read R2)
54. PCGamesN 2019-07-10, Devotion's publisher: https://www.pcgamesn.com/devotion/china (read R3)
55. PocketGamer.biz 2020, Cytus II: https://www.pocketgamer.biz/china-pulls-cytus-ii-from-app-store/ (read R2)
56. Taiwan News 2019-09-09, War Thunder: https://www.taiwannews.com.tw/news/3773345 (read R1)
57. Focus Taiwan 2021-10-01, AIDA: https://focustaiwan.tw/sports/202110010006 (read R2)
58. Korea JoongAng Daily 2024-01-30, Gen.G: https://www.koreajoongangdaily.com/business/korean-league-of-legends-streams-remain-halted-in-china-after-taiwan-comment/12358615 (read R2)
59. Taiwan News 2019-11-16, Huawei and the NCC: https://www.taiwannews.com.tw/en/news/3818340 (read R1)
60. CRI 2018-07-18, PUBG Mobile: https://news.cri.cn/20180718/cdf6366f-94bb-ddea-c976-7f9e2685ff1a.html (read R1)
61. W3C, Authoring HTML: language declarations: https://www.w3.org/International/techniques/authoring-html (read R2)
62. W3C Q&A, language of a link destination: https://www.w3.org/International/questions/qa-link-lang (read R2)
63. Microsoft, make your app localizable: https://learn.microsoft.com/en-us/windows/apps/design/globalizing/prepare-your-app-for-localization (read R2)
64. Microsoft, globalization guidelines: https://learn.microsoft.com/en-us/windows/apps/design/globalizing/globalizing-portal (read R2)
65. Unicode UTS #51: https://www.unicode.org/reports/tr51/ (read R2)
66. NPPA, imported online game approval: https://www.nppa.gov.cn/bsfw/xksx/cbfxl/wlcbfwspsx/202210/t20221013_600723.html (read R3)
67. NPPA, domestic online game approval: https://www.nppa.gov.cn/bsfw/xksx/cbfxl/wlcbfwspsx/202210/t20221013_600725.html (read R3)
68. MIIT 工信部信管〔2023〕105号: https://www.miit.gov.cn/zwgk/zcwj/wjfb/tz/art/2023/art_920db564162e4312916a01bed6540ad8.html (read twice, R3)
69. Alibaba Cloud, ICP filing for overseas firms: https://help.aliyun.com/zh/icp-filing/basic-icp-service/product-overview/icp-filing-application-for-enterprises-outside-the-chinese-mainland (read R3; vendor help)
70. Huawei Cloud, ICP pre-approval categories: https://support.huaweicloud.com/prepare-icp/zh-cn_topic_0000002085124433.html (read R3; vendor help)
71. DTNext / Reuters 2023-10-03: https://www.dtnext.in/lifestyle/technology/apple-enforces-new-check-on-apps-in-china-as-beijing-tightens-oversight-739784 (read R3)
72. Cocos forum, 2023-12 to 2024-02: https://forum.cocos.org/t/topic/155235 (read R3; forum)
73. AppInChina, free games (2026): https://appinchina.co/blog/how-to-make-money-in-china-from-free-mobile-games/ (read R3; vendor)
74. Yicai 2025-12-25: https://www.yicai.com/news/102975370.html (read R3)
75. Niko Partners 2026-01-15: https://substack.nikopartners.com/p/chinas-nppa-approved-1771-video-games (read R3)
76. IT之家 2026-05-26: https://www.ithome.com/0/955/580.htm (read R3)
77. PocketGamer.biz 2020, Yodo1 guide: https://www.pocketgamer.biz/yodo1s-guide-to-publishing-your-mobile-game-in-china-part-1-isbn-copyright-and-taxes/ (read R3; sponsored)
78. APPTUTTi on LinkedIn 2020: https://www.linkedin.com/pulse/5-faqs-isbn-license-application-games-china-neo-liu (read R3; vendor)
79. LocalizeDirect 2024-06-07: https://www.localizedirect.com/posts/how-to-publish-games-in-china (read R3; vendor)
80. EastLab, ISBN: https://eastlab.co/isbn/ (read R3; vendor)
81. China Daily 2021-08-31, minors: https://language.chinadaily.com.cn/a/202108/31/WS612d9f5da310efa1bd66c535.html (read R3)
82. GameRes 2020-07-21, real-name: https://www.gameres.com/871348.html (read R3; trade media)
83. 适龄提示: https://www.nbd.com.cn/articles/2020-12-16/1573626.html and https://news.qq.com/rain/a/20201216A0CSGT00 (read R3)
84. CADPA 2017-06-09, review findings: https://www.cadpa.org.cn/3270/201706/41289.html (read R3)
85. PingWest 2019-04-20, review notes: https://www.pingwest.com/w/186698 (read R3; press report, not a rule)
86. GameRes forum 2017-04-21: https://www.gameres.com/forum/t/747910 (read R3; forum)
87. PingWest 2022-08-16, Chinese horror: https://www.pingwest.com/a/268769 (read R3)
88. TapTap, operation standards: https://developer.taptap.cn/docs/en/store/standardies-operation/ (read R3)
89. TapTap, review rules: https://developer.taptap.cn/docs/en/store/release/publish/agree/ (read R3)
90. Steamworks, Steam China: https://partner.steamgames.com/doc/store/china (read R1, R3)
91. Sixth Tone 2021-01-15: https://www.sixthtone.com/news/1006711 (read R3)
92. Steam Hardware & Software Survey, Oct 2026: https://store.steampowered.com/hwsurvey/ (read R3)
93. IT之家 2025-07-07, Shanghai pilot: https://ithome.com/0/866/374.htm (read R3)
94. Project: `Documentation/AppStore/ASC_Upload_0.1.0/METADATA.md`, `REVIEW_REPLY_iOS_20261030.md` (read-only, R3)
95. Project: `Frontrooms3D/Documentation/research/desktop_ui/LANGUAGE_CHARMS.md`, `Documentation/Takeover_2026-10-18/handoff/interaction.md` (read-only, this synthesis)
96. Project: `Frontrooms3D/Assets/Scripts/FrontRoomsLanguage.cs`, `Assets/Resources/Language/*.txt` (read-only grep, this synthesis)
97. Mexico, Ley sobre el Escudo, la Bandera y el Himno Nacionales, Art. 56 (SDV legal compendium, text current to the 2025-07-21 reform): https://sdv.com.mx/compendio/ley-sobre-el-escudo-la-bandera-y-el-himno-nacionales/articulo-56/ (read R4; a compendium, the official DOF text was not read)
98. LevelUp 2023-09-11, why Mexico's national symbols are missing from games: https://www.levelup.com/noticias/754963/Por-que-los-simbolos-patrios-de-Mexico-no-aparecen-en-videojuegos (read R4; press)
99. 财联社 2026-08-31, August approvals and 2026 year to date: https://www.cls.cn/detail/2469958 (read R4)
100. 17173 2026-10-29, October approvals: https://news.17173.com/content/10292026/185324055.shtml (read R4; trade media)
101. GitHub, hermes-agent PR #29997: https://github.com/NousResearch/hermes-agent/pull/29997 (read R4)
102. KrASIA 2021-12-29, Steam's global domain: https://kr-asia.com/chinese-regulators-ban-game-distributor-steam-citing-unlawful-behaviors (read R4; other reports, seen only in search summaries, called the outage a DNS attack)

## 6. Verification
R4, 2026-11-01, an adversarial pass over the 15 claims the recommendation rests on. Each claim's sources were fetched again as text, and the pass searched for newer rules and counterexamples: Apple's practice for free games, a newer CADPA standard, the fate of NPPA's 2023 draft game rules, approval numbers after May 2026, Steam's status in China, flag-in-language-picker cases. When a source did not say something, the claim was marked unsupported. Notes and quotes are in `R4_verify.md`.

给 Red 的一句话：15 条里 7 条成立，5 条改了措辞或数字，3 条本来就是推断、措辞降了一级。结论的方向没变：合体旗不能用；能不能上大陆取决于版号，不取决于旗子；建议仍是 (a)，但西班牙语钥匙扣里的墨西哥半面旗要先处理，(b) 是完全不碰国旗的退路。

| # | Claim (short) | Verdict | Re-read | What changed |
|---|---|---|---|---|
| 1 | Apple requires a 版号 for every game on the China mainland store; no free or no-IAP exception now (2020: paid and IAP only); without it "Missing Game registration number" and not available | Confirmed, caveat added | 17, 19, 18 (forum); contrary 73 | Apple's text and status wording hold. Whether the 版号 gate is applied to a free, no-IAP game is undocumented, and a vendor (2026-06-03) says it is not enforced; the ICP gate for new apps since 2023-09 (71) gives the same outcome. 2.1(2) and 4.3 now say so. |
| 2 | Only a licensed Chinese online publisher can apply; Art. 10 bars foreign-invested and JV entities; imports need NCA contract registration and an ICP-licensed operator; 80 working days; Arts. 51 and 52 | Confirmed | 1 (NPPA and gov.cn copies), 66 | None of substance. The 80 working days run from acceptance (许可期限). No final version of NPPA's 2023 draft game rules was found. |
| 3 | CADPA 2016 standard, named in NPPA notice 44: Art. 7(2), 7(4), 8(4) | Corrected | 2, 3 | 7(2) and 7(4) are now quoted exactly. 7(4) covers symbols of the post-1949 Taiwan authorities and names no flag. The standard is an industry reference, not a law. No newer edition was found. |
| 4 | Red's pick cannot pass mainland review; it breaks the HK and Macau display rules; Taiwanese players may read it as equating Taiwan with an SAR | Unsupported as stated (inference), downgraded | 2, 14, 16, 56 | "Cannot pass" became "almost certainly fails" [U], since no review finding on a language icon was found. "Breaks" became "conflicts with the wording of" the SAR display rules, whose reach to a game icon is untested. No source suggests the combined flag could pass. |
| 5 | Precedents: Lian Li 2023; hermes-agent #29750 closed the same day by PR #29997; Soccer Manager 2005 banned in 2004 | Confirmed | 49, 50, 101, 44 | PR #29997 removed the flag for every locale. |
| 6 | Flag Law Arts. 9, 19, 20; Advertising Law Art. 9 covers look-alike flags; "SAMR fined a flag-like ad background RMB 100,000" | Corrected | 4, 5, 7, 8, 9 | The RMB 100,000 was a Huai'an district regulator's fine on a whole investment ad with two findings (state-organ name or image, and disguised use of the flag), published by SAMR as a typical case. The source names no article. |
| 7 | The PRC flag may lawfully be the 简体中文 icon but is not recommended; no enforcement case against a game UI | Unsupported (judgment, kept as U) | 4, 6 | No review guidance or case either way was found. The textual bases (Arts. 19, 20; CADPA 8(4)) hold. |
| 8 | Recommendation (a): eight foreign flags, two script tags; fallback (b); Red Hat 2002 was boycotted until it removed every flag | Unsupported (judgment); holds with a new precondition | 51, 50, 101, 63; new 97, 98 | The Red Hat boycott did not end with the removal. "Clean in all four regions" became "no rule found against it". New: the Mexico half of the Español charm meets Mexico's flag law (97, 98), and (b) is now named as the only option that touches no flag. |
| 9 | Steam, ASC, apple.com, CLDR and Huawei label Chinese by script with no flags; Nintendo's "Taiwanese" fixed within two days; W3C says no flags | Confirmed | 30, 22, 24, 31, 35, 32, 61 | Steam's API codes are zh-CN and zh-TW, while its display names go by script. Duolingo's course flags are a possible counterexample that was not checked (4.9). |
| 10 | Licensed mainland builds follow two patterns: 简体中文 only (王者荣耀, 第五人格), or 繁体中文 as a text option (原神 15 languages, 重生细胞 13) | Confirmed | 36, 37, 38, 39, 84 | Store lists show the localizations bundled in the app, not the label inside the game (4.9). |
| 11 | 2025: 95 of 1,771 approvals were imports; about 9-10 a month since Oct 2024; 4 in May 2026 | Corrected | 74, 75, 76; new 99, 100 | The 9-10 a month held from Oct 2024 through 2025. In 2026 imports were 43 in January-August (about 5 a month), 6 in August and 12 in October. |
| 12 | Timelines of 6-18 months, 12-24 by some vendors; no official fee; USD 7-10K per title (2020) plus agency fees or a revenue share | Confirmed as secondary [S]; one phrase corrected | 77, 78, 79, 80, 66 | "No official fee" became "no fee listed" (an absence). The revenue share comes from the 2024 source. |
| 13 | MIIT 2023 filing: platforms may not distribute unfiled apps; publishing apps need prior approval (for games, the 版号); 20 working days; number shown prominently; filers are mainland entities or residents | Confirmed, with precisions | 68, 69, 70, 72 | The 20 working days run from complete materials. Huawei Cloud's list for games also has a game-related 网络文化经营许可证. An overseas firm can file through a mainland branch or representative office; an overseas individual cannot. |
| 14 | A China edition is a different product: real-name, minors' hours, 8+/12+/16+, standard Simplified with no traditional characters or functional English, no blood, complete maps, signage translated | Corrected (one item) | 81, 83, 84, 82, 12, 79 | The age label is a CADPA group standard on trial since 2020-12, not a law. "No traditional characters" means none inside the Simplified text, not a ban on a 繁体 option. |
| 15 | Routes without a 版号: leave China mainland unticked; global Steam (grey area, Simplified Chinese 21.98 %); TapTap 开放试玩; no 版号 requirement on the HK, Macau and Taiwan storefronts | Confirmed, with precisions | 17, 91, 92, 88, 89; new 102 | TapTap's open trial also covers online no-IAP games that have an ICP filing, and the refusal of bloody and violent games is in its review rules. The Steam share swings (-9.41 points from September). Dec 2021 brought reports about Steam's domain. |

Effect on the recommendation: in the China question it holds. (d) stays out, and (a) remains the cheapest option that removes every China, Taiwan, Hong Kong and Macau symbol. Two things changed. "Clean" now reads "no rule found", and (a) gains a precondition outside China, the Mexico half of the Español charm. If Red would rather not answer flag-law questions country by country, (b) is the clean exit.
