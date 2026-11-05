# دراسة أنظمة ألعاب Hero Collector / Idle RPG — الفجوات المطلوب سدّها قبل الإصدار الأول
**التاريخ:** 2026-11-05  
**المشروع:** لعبة الأساطير والتاريخ المترابطة؛ الاسم النهائي لم يُعتمد.  
**المستودع:** khaledhamdyhemdan-sketch/AutoBattler  
**الفرع:** feature/egyptian-heroes-5v5  
**نوع المستند:** دراسة منافسين + وثيقة متطلبات مقترحة؛ ليست تنفيذًا أو اختبارًا.

## نطاق البحث
مراجعة مصادر المطورين الرسمية والأدلة العامة لأنظمة Omniheroes وAFK Journey وMythic Heroes وIdle Heroes، مع تعليقات مجتمع اللاعبين عن كثرة مهام AFK والتكرار. الهدف هو اقتباس **أفكار أنظمة عامة فقط** وتصميم تطبيق أصلي، وليس نقل شخصياتهم أو النصوص أو الواجهات أو الأصول الفنية.

مراجع قابلة للمتابعة:
- Omniheroes — أنظمة الفريق والفصائل والتآزر: https://www.omniheroes-topup.com/us
- Omniheroes — المعدات والتطوير (ويكي مجتمعي): https://omniheroesgame.fandom.com/wiki/Upgrading_gears
- Omniheroes — المواهب والاستيقاظ (ويكي مجتمعي): https://omniheroesgame.fandom.com/wiki/Talents_and_Awakening
- AFK Journey — دليل البداية (أدوار التطوير والمعدات والسحب): https://www.prydwen.gg/afk-journey/guides/beginner-guide
- AFK Journey — أنماط اللعب: https://www.prydwen.gg/afk-journey/guides/game-modes
- AFK Journey — تحديث موسم 2026 الذي يشمل PvP وGuilds وResonance: https://www.afk.global/afk-journey/patch-notes/1-8-1
- Mythic Heroes — دليل المطور للترقيات: https://www.mythicheroes.com/ascension-enhancement/
- Mythic Heroes — سلاح كل بطل وشجرة Divinity وتشكيلات محفوظة: https://www.mythicheroes.com/day-5-guide/
- Mythic Heroes — أحداث يومية ونظام Guild: https://www.mythicheroes.com/day2-guide/
- Mythic Heroes — تحديث 1.7 الذي يشمل 30 يومًا لتسجيل الدخول والمتجر والتحف: https://www.mythicheroes.com/patch-notes-1-7-0/
- Mythic Heroes — Bifrost Bridge والرونات/الفئات: https://www.mythicheroes.com/bifrost-bridge/
- ردود اللاعبين (تجنّب الأعمال اليومية المرهقة): https://www.reddit.com/r/AFKJourney/comments/1e060xa/for_an_afk_game_the_game_has_an_overwhelming/ و https://www.reddit.com/r/AFKJourney/comments/1ryzc2a/ive_been_playing_this_game_since_release_and_i/

## الوضع الحالي للكود (لا يعني أنه اختُبر في Unity)
- متاح **بشكل أولي**: 30 تعريف بطل، مهارات / ultimates / passive مميزة على مستوى كود، قتال 5v5، 6 فصول و60 مرحلة معرفة، ترقية مستوى ونجوم، زيادة مستوى سلاح ودرع، تآزر فرق، سحب مفرد مبسط، نسخ بطل تصبح fragments، مكافآت AFK حتى 12 ساعة، 5 مهام يومية + 5 أسبوعية، تعريف سكين أساسي وبديل لكل بطل، حفظ محلي، شاشة قصة ومهام، واجهة Unity مؤقتة.
- **ليس مكتملًا**: معدات فعلية في جرد 4–6 خانات وبمستويات ندرة، نظام إسقاطات، دمج وصناعة وحزم معدات، شجرة مهارات ومواهب قابلة للتخصيص، رونات، أثر Artifact، سلاح Signature حقيقي، resonance/reset، عداد ضمان pity وقائمة Wishlist وأنواع Banner وسجل سحب، هدايا دخول يومية/شهرية، إنجازات، Season Pass، صناديق، البريد، أحداث أسبوعية فعلية، dungeon/tower/maze/boss challenge مستقل، Arena/Guilds/Friends، backend حسابات، شراء رقمي حقيقي آمن، رسوم وأصوات وتحريك سينمائي نهائي، اختبار Unity وAPK/AAB حقيقي.
- أي نظام شبكة / نقود مدفوعة / PvP / ترتيب حقيقي يحتاج backend موثوقًا؛ لا توصف محاكاة محلية على أنها لاعبين حقيقيين.
- العرض الحالي للمهارات والأبطال والعوالم تمهيدي وغير متوازن أو مُختبر.

## مصفوفة الفجوات حسب الأولوية الداخلية (كلها مستهدفة قبل أول تسليم متكامل)

الأولوية هنا **ترتيب بناء داخلي** وليست إذنًا بتسليم إصدار ناقص.

| القسم | موجود عندنا | المطلوب للإصدار الأول | درجة الاعتماد |
|---|---|---|---|
| Hero Level/Ascension | رفع Level وStars بموارد محدودة | EXP وGold ومواد Ascension وسقف حسب النجوم، مضاعفات قابلة للضبط | P0 |
| Hero Skill Upgrade | ملفات مهارات خاصة | مستويات فعلية للهجوم/Skill/Ultimate/Passive مع تأثير متغير بالمعركة | P0 |
| Talent Tree | غير موجود | شجرتان على الأقل لكل بطل: هجومية/دفاعية أو دعم، نقاط قابلة للإعادة | P1 |
| Signature Weapon | غير موجود | سلاح منفرد لكل بطل يفتح مهارة/تأثيرًا، يغيّر شكله ويُطور | P1 |
| 4–6 Gear Slots | عدادات سلاح ودرع فقط | Weapon/Armor/Helmet/Boots/Accessory/Charm في Inventory مع equip/unequip | P0 |
| Gear Rarity/Sets | غير موجود | Common→Legendary وتصاميم Set bonus لقطعتين/4 قطع | P0 |
| Gear Drops/Crafting | غير موجود | جداول Drop، تفكيك، دمج وصناعة، Auto Equip، حدود وإصلاح الأخطاء | P0 |
| Runes | غير موجود | Slots برونق حضاري وخصائص محدودة، دمج وتطوير ووصف تأثير | P1 |
| Artifacts/Relics | غير موجود | أثر أسطوري للفريق مع مهارة/تأثير أثناء القتال ونظام تطوير | P1 |
| Resonance | غير موجود | أعلى 5 أبطال يحددون مستوى مزامنة لبقية التشكيلات | P0 |
| Hero Reset | غير موجود | إعادة توزيع الموارد بوضوح وعدم ضياع العناصر/السكينات | P0 |
| Saved Formations | تشكيلة واحدة محليًا | 3–5 تشكيلات لطور القصة والبوس والـArena | P0 |
| Combat Strategy | AI + مهارات أولية | Front/Back Slots، هدف المهارة، تفاصيل Debuff، تقرير ضرر/شفاء، إعادة تشغيل | P0 |
| World Campaign | 60 تعريف مرحلة | Encounter data حقيقية، Boss Mechanics، بيئات، أحداث قصصية، 3 نجوم ومكافآت | P0 |
| AFK Endless Stages | مكافأة غياب محلية فقط | Endless PvE قابل للتقدم، ينمو معدل الغنائم مع المراحل | P1 |
| Tower of Trials | غير موجود | Tower ثابت بتصاعد صعوبة ومكافآت ترقية | P1 |
| Faction/Origin Trial | غير موجود | تحديات متناوبة تطلب عوالم / أدوار معينة | P1 |
| Roguelike Labyrinth | غير موجود | خريطة خيارات وباف مؤقت ومعارك/كنوز/بوس أسبوعي | P1 |
| Daily/World Boss | بوسات فصل القصة | تحدي منفصل على أساس أقصى ضرر، مستويات صعوبة وجدول دوران | P1 |
| Arena PvP | غير موجود | تشكيلة دفاع، ترتيب ومكافآت؛ backend/مطابقة موثوقة | P1/network |
| Guild / Guild Boss | غير موجود | إنشاء/انضمام، أعضاء/رتب، بوس جماعي، متجر Guild، Missions | P1/network |
| Friends / Helper | غير موجود | طلب صداقة، بطل إعارة بشروط، أصدقاء نشطون | P2/network |
| Summon Banners | سحب واحدة مبسطة | Standard وFeatured وFaction؛ ×1/×10؛ جداول نسب ومعاينة | P0 |
| Pity/Wishlist | غير موجود | ضمان بعد X سحب، عداد مشاهد، تاريخ سحب، اختيار أبطال مرجحين | P0 |
| Dupes / Shards | Fragments ابتدائية | حساب فريد للشظايا، متجر صرف المكرر والارتقاء | P0 |
| Sign-in rewards | غير موجود | تقويم 7/28 يوم، مكافآت مرحلية، أيام حضور تراكمية | P0 |
| Rookie path | غير موجود | مسار 7/14 يوم + مهام تعليمية بمكافآت مضمونة | P0 |
| Daily/Weekly Missions | موجود مبدئيًا | Activity points/صناديق، تكامل كل الأنماط، جداول UTC وتعويض | P0 |
| Achievements / Titles | غير موجود | دائم، تقدم متدرج، أيقونات ولقب الملف الشخصي | P0 |
| Battle Pass / Events | غير موجود | مسار موسم ومكافآت ومحتوى متناوب؛ لا بيع دون Billing | P1 |
| Shop / Mail / Inventory | تعريفات عملة محدودة | متاجر موارد، حقائب بفلاتر، بريد يوزع الجوائز بسجل معاملات | P0 |
| Account / Cloud Sync | JSON محلي فقط | Guest→link Google، استرجاع الحساب، ربط موثوق، anti-duplication | P0/network |
| Analytics / Live Ops | غير موجود | أحداث قياس تقدم وصعوبة/تسرب، أرقام قابلة للتعديل، لوحة إدارة | P1 |
| UI / anime / entrances | تعريفات وخطافات فقط | رسومات أصلية وRig وVFX وحركات دخول وكاميرات ومؤثرات صوتية كاملة | P0/art |
| Android/Google Play | إعدادات build دون إثبات | Unity compile + QA + signed APK/AAB + متطلبات المتجر | P0/release |

## مواصفات مقترحة لأنظمة أساسية

### 1) تطوير الأبطال (متصل بكل أنماط اللعب)
- 5 أو 6 Slots معدات: سلاح/خوذة/درع/حذاء/إكسسوار/تعويذة. لكل قطعة `itemInstanceId`, `itemTemplateId`, `rarity`, `level`, `setId`, `ownerHeroId`, `affixes`.
- ندرة المعدات (مثال وليس اعتمادًا نهائيًا): Common / Rare / Epic / Legendary / Mythic. تأثير طقم 2 قطع و4 قطع، مع حد أقصى لمنع مضاعفات بلا سقف.
- Hero Level يحتاج EXP/ذهب ومواد قليلة، وAscension يحكم مستوى القوة والمهارات المفتوحة.
- Skill level لكل من Active وUltimate وPassive، ويظهر تغير الأرقام قبل شراء الترقية.
- سلاح مميز (Signature) مرتبط بالبطل، يفتح تأثيرًا حقيقيًا وليس مجرد Stat.
- Runes وArtifacts تأثيرات للفريق/البطل مرتبطة بالعالم والوظيفة، لا تخلق عشرات العملات غير الضرورية.
- Resonance يرفع مستوى أبطال لم يتم استثمار مواردهم؛ والـReset يعيد المواد طبقًا لقواعد معلنة دون فقد التجميع أو المظهر.
- 3 تشكيلات محفوظة مبدئيًا: Campaign، Boss، Arena؛ إمكان التوسع إلى 5. اختيار الفريق يوضح Synergy وحالة الـGear.

### 2) الاستدعاء/التجميع
- 3 Banners: Standard، Featured، Origin. واجهة توضح احتمالات كاملة، فترات التفعيل، Wishlist، ×1/×10، Log وتكرار المكافآت.
- Pity مقترح للاختبار: 50 سحبة كحد أقصى للحصول على Legendary في Standard؛ الرقم **افتراضي ويحتاج محاكاة اقتصاد وتوازن** قبل الاعتماد. العداد محفوظ على السيرفر ويتعامل مع الاسترجاعات ونتائج الشبكة.
- حماية اللاعبين من النتائج المكررة: كل عملية `summonTransactionId` تنفذ مرة واحدة حتى لو تكرر الطلب.
- كرت بطل جديد/مكرر + دخلة نادرة، مع Skip/عرض ×10 واضح.
- المتجر يحوّل فائض shards إلى مواد اختيارية، بدون الحاجة لتدمير أبطال بشكل غير مفهوم.

### 3) الاحتفاظ اليومي والأسبوعي
- Daily Sign-in بنظام تراكمي 28 خانة، بدون معاقبة من يغيب يومًا، مع هدية كبيرة يوم 7 و28.
- Rookie 7/14 يوم: السحب الأول + تعلم التطوير + اختيار الفريق + الحصول على بطل مضمون.
- Daily Quests + Weekly Quests مع نقاط نشاط وصناديق تقدم، Achievements دائمة وMailbox يحفظ المكافآت غير المستلمة.
- Weekly Event Rotation: يوم بوس عالم، يوم برج فصيلة، متاهة أسبوعية، موسم بسيط، تحدي جماعي.
- الاستخدام اليومي الأساسي يستهدف **10–15 دقيقة تقريبًا**؛ المحتوى الإضافي اختيارى. Auto Battle وSkip للمعارك التي سبق إتمامها وAuto Equip وLevel +10 لتجنب الإرهاق.

### 4) أنماط اللعب داخل نفس اللعبة
- Campaign Story `6×10` حاليًا، تُنتج لها مشاهد ومواجهات مختلفة، وليس مجرد ضبط قوة الأعداء.
- Tower of Ages متصاعد (محتوى قابل للتكوين) ومكافآت كل 5/10 طوابق.
- Origin Trials بحسب حضارة/أسطورة والمهارة، بمواعيد متناوبة.
- Rift Labyrinth: تختار مسارًا، وتجمع buffs مؤقتة، وتقاتل بوسًا أخيرًا؛ replay مبني على seed.
- Daily Raid Boss + Rotation؛ يتطلب تكتيكات مختلفة ويكافئ الإنجاز لا الدفع فقط.
- Arena 5v5 وGuild Boss مع ترتيب حقيقيين لاحقًا داخل **الإصدار الأول المتكامل** وبعد بناء backend؛ لا ادّعاء PvP حقيقي عند غيابه.
- معارك PvE قابلة للتجاوز بعد إنجازها ويُحتفظ بملخص الضرر والاستشفاء.

### 5) الاقتصاد، الأمان والخوادم
- 4 موارد أساسية كحد أولي: Gold, Hero EXP, Gems, Ascension Stones، مع عناصر سحب ومعدات/رونات؛ قلّل عملات زائدة.
- كل مصدر/مصرف معروف في جدول Economy؛ محاكاة أسبوع/شهر لمستخدم مجاني ومتوسط؛ لا نمو غير محدود ولا احتكار مورد واحد.
- الجوائز والوقت/AFK والسحب وترقية الموارد/المشتريات وPvP تتطلب رصيدًا وسجل عمليات **server-authoritative** مع حماية retry, duplicate claims, refunds, clock tampering.
- فاتورة Google Play للمبيعات الرقمية إن فُعّلت، تحقق backend، مع إظهار احتمالات السحب والشروط.
- Guest account بربط Google واسترجاع، وإعدادات أمان، سياسة خصوصية، أدوات حذف الحساب.
- Dashboard محتوى/حدث/مكافآت/نسب/مستويات مع صلاحيات وتدقيق.

## ترتيب التنفيذ الداخلي (بدون تسليم ديمو للمستخدم)
1. **النواة:** استقرار القتال + معادلات ضرر + أصل حفظ آمن وبيانات العناصر + حالات المعارك.
2. **تقدم الأبطال:** Gear, Drops, Equip, Skill levels, Ascension, Resonance, Reset, Team presets.
3. **اقتصاد وتجميع:** currencies, reliable inventory, banners/wishlist/pity/duplication-safe rewards.
4. **دورة اللعب:** rookie, sign-in, activity, achievements, inbox, endless stages.
5. **أنماط PvE:** tower, origin trials, labyrinth, raid bosses, stage progression.
6. **الخدمات الحية:** accounts/backend, guilds/friends, real Arena/ranks, rewards, shop/billing if enabled.
7. **الهوية والإنهاء:** تصميم أصلــي كامل، rigs/skins/entrances/ultimates، UI، صوتيات، لغة عربية/إنجليزية، توازن.
8. **القبول النهائي:** import/compile Unity، استعادة حساب، اختبارات معارك ومكافآت، أجهزة Android، أداء/سخونة/ذاكرة، بناء APK/AAB موقّع ومتوافق.

## بنود ممنوع الادّعاء باكتمالها الآن
لا يوجد دليل تشغيل Unity أو Android نهائي. لم تُنتج صور/تحريك كل الـ30 ولا أزياء بديلة جاهزة للعبة. رتب PvP وGuilds تحتاج حسابات وخوادم حقيقية. وجود تعريف للنظام أو شاشة ليس إثباتًا أن الاقتصاد والمهام وشراء العناصر يعمل في بيئة حقيقية.

هذه وثيقة **بحث ومقارنة وتوصيات** قابلة للتعديل بناءً على فيديوهات صاحب المشروع وموازنة تكلفة الأصول والتشغيل.
