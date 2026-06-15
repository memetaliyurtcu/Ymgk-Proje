using ARFishing.Localization;

namespace ARFishing.Creatures
{
    public static class CreatureLabels
    {
        // Turkish (default — content language).
        public static string ToTurkish(this CreatureCategory v) => v switch
        {
            CreatureCategory.Fish => "Balık",
            CreatureCategory.Invertebrate => "Omurgasız",
            CreatureCategory.Producer => "Üretici",
            CreatureCategory.Coral => "Mercan",
            CreatureCategory.DeepSea => "Derin Deniz",
            CreatureCategory.Endangered => "Tehlike Altında",
            CreatureCategory.Mammal => "Memeli",
            CreatureCategory.Reptile => "Sürüngen",
            _ => "Bilinmiyor",
        };

        public static string ToTurkish(this Habitat v) => v switch
        {
            Habitat.Surface => "Yüzey",
            Habitat.OpenSea => "Açık Deniz",
            Habitat.Reef => "Mercan Resifi",
            Habitat.Seabed => "Deniz Tabanı",
            Habitat.DeepSea => "Derin Deniz",
            _ => "Bilinmiyor",
        };

        public static string ToTurkish(this DietType v) => v switch
        {
            DietType.Carnivore => "Etçil",
            DietType.Herbivore => "Otçul",
            DietType.Omnivore => "Hepçil",
            DietType.FilterFeeder => "Süzerek Beslenen",
            DietType.Photosynthesizer => "Fotosentez Yapan",
            _ => "Bilinmiyor",
        };

        public static string ToTurkish(this EcosystemRole v) => v switch
        {
            EcosystemRole.Predator => "Avcı",
            EcosystemRole.Prey => "Av",
            EcosystemRole.Producer => "Üretici",
            EcosystemRole.Cleaner => "Temizleyici",
            EcosystemRole.ShelterProvider => "Barınak Sağlayıcı",
            _ => "Bilinmiyor",
        };

        // English (i18n stub — verified pedagogical labels).
        public static string ToEnglish(this CreatureCategory v) => v switch
        {
            CreatureCategory.Fish => "Fish",
            CreatureCategory.Invertebrate => "Invertebrate",
            CreatureCategory.Producer => "Producer",
            CreatureCategory.Coral => "Coral",
            CreatureCategory.DeepSea => "Deep Sea",
            CreatureCategory.Endangered => "Endangered",
            CreatureCategory.Mammal => "Mammal",
            CreatureCategory.Reptile => "Reptile",
            _ => "Unknown",
        };

        public static string ToEnglish(this Habitat v) => v switch
        {
            Habitat.Surface => "Surface",
            Habitat.OpenSea => "Open Sea",
            Habitat.Reef => "Coral Reef",
            Habitat.Seabed => "Seabed",
            Habitat.DeepSea => "Deep Sea",
            _ => "Unknown",
        };

        public static string ToEnglish(this DietType v) => v switch
        {
            DietType.Carnivore => "Carnivore",
            DietType.Herbivore => "Herbivore",
            DietType.Omnivore => "Omnivore",
            DietType.FilterFeeder => "Filter Feeder",
            DietType.Photosynthesizer => "Photosynthesizer",
            _ => "Unknown",
        };

        public static string ToEnglish(this EcosystemRole v) => v switch
        {
            EcosystemRole.Predator => "Predator",
            EcosystemRole.Prey => "Prey",
            EcosystemRole.Producer => "Producer",
            EcosystemRole.Cleaner => "Cleaner",
            EcosystemRole.ShelterProvider => "Shelter Provider",
            _ => "Unknown",
        };

        // Arabic stubs (return TR fallback; replace with native-reviewed translations before AR release).
        public static string ToArabic(this CreatureCategory v) => v.ToTurkish();
        public static string ToArabic(this Habitat v) => v.ToTurkish();
        public static string ToArabic(this DietType v) => v.ToTurkish();
        public static string ToArabic(this EcosystemRole v) => v.ToTurkish();

        // Locale-aware accessors — preferred for UI code (calls into Localizer.Active automatically).
        public static string Localize(this CreatureCategory v) => Localize(v, Localizer.Active);
        public static string Localize(this CreatureCategory v, Locale locale) => locale switch
        {
            Locale.English => v.ToEnglish(),
            Locale.Arabic => v.ToArabic(),
            _ => v.ToTurkish(),
        };

        public static string Localize(this Habitat v) => Localize(v, Localizer.Active);
        public static string Localize(this Habitat v, Locale locale) => locale switch
        {
            Locale.English => v.ToEnglish(),
            Locale.Arabic => v.ToArabic(),
            _ => v.ToTurkish(),
        };

        public static string Localize(this DietType v) => Localize(v, Localizer.Active);
        public static string Localize(this DietType v, Locale locale) => locale switch
        {
            Locale.English => v.ToEnglish(),
            Locale.Arabic => v.ToArabic(),
            _ => v.ToTurkish(),
        };

        public static string Localize(this EcosystemRole v) => Localize(v, Localizer.Active);
        public static string Localize(this EcosystemRole v, Locale locale) => locale switch
        {
            Locale.English => v.ToEnglish(),
            Locale.Arabic => v.ToArabic(),
            _ => v.ToTurkish(),
        };
    }
}
