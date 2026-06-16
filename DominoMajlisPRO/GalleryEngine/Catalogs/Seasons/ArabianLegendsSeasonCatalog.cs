using DominoMajlisPRO.GalleryEngine.Models;

namespace DominoMajlisPRO.GalleryEngine.Catalogs.Seasons;

public static class ArabianLegendsSeasonCatalog
{
    public static GalleryCatalog Build()
    {
        var seasonId = "arabian_legends_s01";

        var season = new GallerySeason
        {
            Id = seasonId,
            Title = "أساطير العرب",
            Chapter = "الفصل الأول",
            Subtitle = "بدايات المجد",
            Description = "اكتشف مقتنيات الموسم واصنع هوية فريقك الأسطورية.",
            BadgeText = "الموسم الحالي",
            ButtonText = "استكشف الموسم",
            BackgroundImage = "",
            CharacterImage = "gallery_lion.png",
            StartDate = DateTime.Now.AddDays(-5),
            EndDate = DateTime.Now.AddDays(25),

            Theme = new GalleryTheme
            {
                Id = "arabian_royal_theme",
                Name = "Arabian Royal",

                BackgroundImage = "",
                CharacterImage = "gallery_lion.png",
                PrimaryColor = "#D8A63A",
                SecondaryColor = "#7A3E12",
                AccentColor = "#FFB84A",
                TextColor = "#F7D98A",

                ButtonStartColor = "#FFE08A",
                ButtonEndColor = "#B8860B",

                CardBackgroundStart = "#2A1A08",
                CardBackgroundEnd = "#050505",
                BorderColor = "#D4AE62",

                GlowColor = "#FFB84A",
                GlowOpacity = 0.45,
                OverlayOpacity = 0.42,
                ShadowOpacity = 0.35,

                Mood = "Royal Desert",
                Lighting = "Warm Sunset",
                EnableParticles = false,
                EnableAnimatedGlow = true
            },

            HeroLayout = new HeroLayout
            {
                Height = DeviceInfo.Idiom == DeviceIdiom.Phone ? 320 : 420,

                CharacterWidth = DeviceInfo.Idiom == DeviceIdiom.Phone ? 220 : 320,
                CharacterRightMargin = 4,
                CharacterBottomMargin = -4,

                ContentLeftMargin = 22,
                ContentTopMargin = 0,
                ContentSpacing = 8,

                CountdownTopMargin = 12,
                ButtonTopMargin = 16,

                OverlayOpacity = 0.38
            }
        };

        var items = new List<GalleryItem>
        {
            new GalleryItem
            {
                Id = "desert_lion",
                SeasonId = seasonId,
                Name = "أسد الصحراء",
                Subtitle = "ملك الصحراء الذهبية",
                Category = "الشعارات",
                Rarity = "أسطوري",
                Image = "gallery_lion.png",
                Description = "رمز القوة والهيبة لفريق لا يعرف التراجع.",
                Lore = "في قلب الصحراء الذهبية، كان أسد واحد يحكم الممرات القديمة. لم يكن الأقوى فقط، بل كان رمزًا للقيادة والصبر والانتصار.",
                Price = 250,
                Currency = "Gems",
                IsNew = true,
                IsLimited = false,
                IsOwned = false
            },

            new GalleryItem
            {
                Id = "sand_falcon",
                SeasonId = seasonId,
                Name = "صقر الرمال",
                Subtitle = "حارس الأفق",
                Category = "الشعارات",
                Rarity = "نادر",
                Image = "gallery_falcon.png",
                Description = "صقر يراقب الخصوم من أعلى القمم.",
                Lore = "لم يظهر صقر الرمال إلا عند بداية المواسم الكبرى. يقال إن ظهوره يعني أن فريقًا جديدًا سيصعد إلى المجد.",
                Price = 200,
                Currency = "Gems",
                IsNew = true,
                IsLimited = false,
                IsOwned = false
            },

            new GalleryItem
            {
                Id = "glory_crown",
                SeasonId = seasonId,
                Name = "تاج المجد",
                Subtitle = "رمز السيادة",
                Category = "الإطارات",
                Rarity = "ملحمي",
                Image = "gallery_crown.png",
                Description = "تاج ذهبي يليق بالفرق المتصدرة.",
                Lore = "صُنع تاج المجد للفرق التي لا تبحث عن الفوز فقط، بل عن ترك أثر في تاريخ المجلس.",
                Price = 150,
                Currency = "Gems",
                IsNew = true,
                IsLimited = false,
                IsOwned = false
            },

            new GalleryItem
            {
                Id = "ember_dragon",
                SeasonId = seasonId,
                Name = "تنين اللهب",
                Subtitle = "غضب الموسم",
                Category = "الشعارات",
                Rarity = "أسطوري",
                Image = "gallery_dragon.png",
                Description = "شعار ناري للفرق التي تدخل المباراة بلا خوف.",
                Lore = "ظهر تنين اللهب في الليالي التي تحولت فيها الرمال إلى جمر. كان ظهوره نذيرًا ببداية صراع جديد.",
                Price = 350,
                Currency = "Gems",
                IsNew = false,
                IsLimited = true,
                OldPrice = 500,
                LimitedUntil = DateTime.Now.AddHours(12)
            }
        };

        return new GalleryCatalog
        {
            Seasons = new List<GallerySeason> { season },
            Items = items
        };
    }
}